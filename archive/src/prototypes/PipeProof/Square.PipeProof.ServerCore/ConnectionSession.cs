using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Square.PipeProof.Protocol;

namespace Square.PipeProof.ServerCore;

internal sealed class ConnectionSession : IAsyncDisposable
{
    private static readonly UTF8Encoding StrictUtf8 = new(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true);

    private readonly Stream _stream;
    private readonly EventHub _events;
    private readonly ProofServerOptions _options;
    private readonly ServerMetrics _metrics;
    private readonly ConnectionMetrics _connectionMetrics;
    private readonly Action<string> _requestShutdown;
    private readonly LengthFramedJsonCodec _codec;
    private readonly BoundedOutboundQueue _outbound;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _requests = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<int, Task> _backgroundTasks = new();
    private readonly ConcurrentDictionary<string, EventSubscription> _subscriptions = new(StringComparer.Ordinal);
    private Task? _writerTask;
    private int _nextBackgroundTaskId;
    private int _disposed;

    internal ConnectionSession(
        Stream stream,
        EventHub events,
        ProofServerOptions options,
        ServerMetrics metrics,
        ConnectionMetrics connectionMetrics,
        Action<string> requestShutdown)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _events = events ?? throw new ArgumentNullException(nameof(events));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        _connectionMetrics = connectionMetrics ?? throw new ArgumentNullException(nameof(connectionMetrics));
        _requestShutdown = requestShutdown ?? throw new ArgumentNullException(nameof(requestShutdown));
        _codec = new LengthFramedJsonCodec(options.MaximumPayloadBytes, options.MaximumWriteChunkBytes);
        _outbound = new BoundedOutboundQueue(options, metrics, connectionMetrics);
    }

    internal async Task RunAsync(CancellationToken serverCancellationToken)
    {
        using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(
            serverCancellationToken,
            _lifetime.Token);
        try
        {
            if (!await PerformHandshakeAsync(linked.Token).ConfigureAwait(false))
            {
                return;
            }
            _writerTask = Task.Run(() => WriterLoopAsync(linked.Token));

            while (!linked.IsCancellationRequested)
            {
                IProtocolMessage? message = await ReadMessageAsync(linked.Token).ConfigureAwait(false);
                if (message is null)
                {
                    return;
                }

                switch (message)
                {
                    case RequestMessage request:
                        StartRequest(request, linked.Token);
                        break;
                    case CancelMessage cancel:
                        await HandleCancelAsync(cancel, linked.Token).ConfigureAwait(false);
                        break;
                    case SubscribeMessage subscribe:
                        await HandleSubscribeAsync(subscribe, linked.Token).ConfigureAwait(false);
                        break;
                    case UnsubscribeMessage unsubscribe:
                        await HandleUnsubscribeAsync(unsubscribe, linked.Token).ConfigureAwait(false);
                        break;
                    default:
                        await SendProtocolErrorAsync(
                            replyTo: null,
                            ProtocolErrorCodes.UnexpectedMessage,
                            $"Message kind '{message.Kind}' is not valid after handshake.",
                            data: new { received_kind = message.Kind },
                            linked.Token).ConfigureAwait(false);
                        _metrics.ProtocolViolation();
                        return;
                }
            }
        }
        catch (OperationCanceledException) when (linked.IsCancellationRequested)
        {
        }
        catch (FrameSizeException)
        {
            _metrics.ProtocolViolation();
            _metrics.OversizedFrame();
        }
        catch (InvalidUtf8ProtocolException)
        {
            _metrics.ProtocolViolation();
            _metrics.InvalidUtf8Frame();
        }
        catch (MalformedProtocolException)
        {
            _metrics.ProtocolViolation();
            _metrics.MalformedFrame();
        }
        catch (EndOfStreamException)
        {
            _metrics.ProtocolViolation();
            _metrics.MalformedFrame();
        }
        catch (IOException)
        {
        }
        finally
        {
            _lifetime.Cancel();
            foreach (CancellationTokenSource request in _requests.Values)
            {
                request.Cancel();
            }
            await RemoveAllSubscriptionsAsync().ConfigureAwait(false);
            _outbound.Complete();
            if (_writerTask is not null)
            {
                try
                {
                    await _writerTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
                catch (IOException)
                {
                }
            }
            Task[] background = _backgroundTasks.Values.ToArray();
            if (background.Length > 0)
            {
                try
                {
                    await Task.WhenAll(background).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
            }
        }
    }

    internal async Task NotifyServerGoingAwayAsync(string reason, CancellationToken cancellationToken)
    {
        if (Volatile.Read(ref _disposed) != 0 || _lifetime.IsCancellationRequested)
        {
            return;
        }
        _ = await _outbound.EnqueueControlAsync(
            new ServerGoingAwayMessage
            {
                Reason = reason,
                ReconnectDelayMilliseconds = 100
            },
            cancellationToken).ConfigureAwait(false);
    }

    internal void ForceStop() => _lifetime.Cancel();

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }
        _lifetime.Cancel();
        _outbound.Complete();
        try
        {
            await _stream.DisposeAsync().ConfigureAwait(false);
        }
        catch (IOException)
        {
        }
        foreach (CancellationTokenSource request in _requests.Values)
        {
            request.Dispose();
        }
        _lifetime.Dispose();
    }

    private async Task<bool> PerformHandshakeAsync(CancellationToken cancellationToken)
    {
        using CancellationTokenSource handshake = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        handshake.CancelAfter(_options.HandshakeTimeout);
        IProtocolMessage? first;
        try
        {
            first = await ReadMessageAsync(handshake.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _metrics.HandshakeRejected();
            return false;
        }

        if (first is not HelloMessage hello)
        {
            _metrics.HandshakeRejected();
            await WriteHandshakeMessageAsync(
                new ProtocolErrorMessage
                {
                    Error = new(
                        ProtocolErrorCodes.HandshakeRequired,
                        "The first frame must be a hello message.",
                        first is null ? null : ProtocolJson.ToElement(new { received_kind = first.Kind }))
                },
                cancellationToken).ConfigureAwait(false);
            return false;
        }

        bool protocolMatches = string.Equals(
            hello.Protocol,
            ProtocolConstants.ProtocolName,
            StringComparison.Ordinal);
        bool versionMatches = ProtocolConstants.SupportedVersions.Contains(
            hello.Version,
            StringComparer.Ordinal);
        if (!protocolMatches || !versionMatches)
        {
            _metrics.HandshakeRejected();
            string code = protocolMatches
                ? ProtocolErrorCodes.IncompatibleVersion
                : ProtocolErrorCodes.IncompatibleProtocol;
            ProtocolErrorMessage rejected = new()
            {
                ReplyTo = hello.Id,
                Error = new(
                    code,
                    $"Protocol '{hello.Protocol}' version '{hello.Version}' is not supported.",
                    ProtocolJson.ToElement(new
                    {
                        supported_protocol = ProtocolConstants.ProtocolName,
                        supported_versions = ProtocolConstants.SupportedVersions
                    })),
                SupportedVersions = ProtocolConstants.SupportedVersions
            };
            await WriteHandshakeMessageAsync(rejected, cancellationToken).ConfigureAwait(false);
            return false;
        }

        _connectionMetrics.SetClient(hello.Client.Kind, hello.Client.InstanceId);
        _metrics.HandshakeCompleted();
        EventJournalBounds bounds = _events.Bounds;
        HelloAckMessage accepted = new()
        {
            ReplyTo = hello.Id,
            Server = new(
                "pipe-proof-server",
                "0.1.0",
                _metrics.ServerInstanceId,
                _metrics.ServerEpoch),
            Capabilities = ProtocolConstants.ServerCapabilities,
            Limits = _options.ToLimits(),
            MinimumAvailableSequence = bounds.MinimumAvailableSequence,
            LatestSequence = bounds.LatestSequence
        };
        await WriteHandshakeMessageAsync(accepted, cancellationToken).ConfigureAwait(false);
        return true;
    }


    private async Task WriteHandshakeMessageAsync(
        IProtocolMessage message,
        CancellationToken cancellationToken)
    {
        using CancellationTokenSource write = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        write.CancelAfter(TimeSpan.FromMilliseconds(_options.WriteTimeoutMilliseconds));
        await _codec.WriteMessageAsync(_stream, message, write.Token).ConfigureAwait(false);
    }

    private void StartRequest(RequestMessage request, CancellationToken connectionCancellationToken)
    {
        if (_requests.Count >= _options.MaximumInFlightRequests)
        {
            Track(SendResponseAsync(
                request.Id,
                result: null,
                new ProtocolError(
                    ProtocolErrorCodes.ServerBusy,
                    "The connection has reached its in-flight request limit."),
                connectionCancellationToken));
            return;
        }

        CancellationTokenSource requestCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            connectionCancellationToken,
            _lifetime.Token);
        if (!_requests.TryAdd(request.Id, requestCancellation))
        {
            requestCancellation.Dispose();
            Track(SendResponseAsync(
                request.Id,
                result: null,
                new ProtocolError(
                    ProtocolErrorCodes.DuplicateRequest,
                    $"Request identifier '{request.Id}' is already active."),
                connectionCancellationToken));
            return;
        }

        _metrics.RequestStarted();
        _connectionMetrics.RequestStarted();
        Track(ExecuteRequestAsync(request, requestCancellation));
    }

    private async Task ExecuteRequestAsync(
        RequestMessage request,
        CancellationTokenSource requestCancellation)
    {
        bool requestShutdown = false;
        try
        {
            JsonElement result = request.Method switch
            {
                "proof.echo" => ExecuteEcho(request.Params),
                "proof.delay" => await ExecuteDelayAsync(request.Params, requestCancellation.Token).ConfigureAwait(false),
                "proof.publish" => await ExecutePublishAsync(request.Params, requestCancellation.Token).ConfigureAwait(false),
                "proof.stats" => ProtocolJson.ToElement(_metrics.Snapshot(_events.Bounds)),
                "proof.shutdown" => ExecuteShutdown(out requestShutdown),
                _ => throw new RequestFailureException(
                    ProtocolErrorCodes.MethodNotFound,
                    $"Unknown proof method '{request.Method}'.",
                    new { method = request.Method })
            };
            await SendResponseAsync(request.Id, result, error: null, _lifetime.Token).ConfigureAwait(false);
            if (requestShutdown)
            {
                _requestShutdown("proof.shutdown");
            }
        }
        catch (OperationCanceledException) when (requestCancellation.IsCancellationRequested)
        {
            _metrics.RequestCancelled();
            await SendResponseAsync(
                request.Id,
                result: null,
                new ProtocolError(
                    ProtocolErrorCodes.RequestCancelled,
                    $"Request '{request.Id}' was cancelled."),
                CancellationToken.None).ConfigureAwait(false);
        }
        catch (RequestFailureException exception)
        {
            await SendResponseAsync(
                request.Id,
                result: null,
                new ProtocolError(
                    exception.Code,
                    exception.Message,
                    ProtocolJson.ToElement(exception.DataObject)),
                _lifetime.Token).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is InvalidDataException or ArgumentException)
        {
            await SendResponseAsync(
                request.Id,
                result: null,
                new ProtocolError(
                    ProtocolErrorCodes.RequestInvalid,
                    exception.Message),
                _lifetime.Token).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            await SendResponseAsync(
                request.Id,
                result: null,
                new ProtocolError(
                    ProtocolErrorCodes.InternalError,
                    "The proof server encountered an internal error.",
                    ProtocolJson.ToElement(new { exception_type = exception.GetType().FullName })),
                _lifetime.Token).ConfigureAwait(false);
        }
        finally
        {
            _requests.TryRemove(request.Id, out _);
            requestCancellation.Dispose();
            _metrics.RequestCompleted();
            _connectionMetrics.RequestCompleted();
        }
    }

    private static JsonElement ExecuteEcho(JsonElement parameters)
    {
        string text = RequireString(parameters, "text");
        return ProtocolJson.ToElement(new { text });
    }

    private static async Task<JsonElement> ExecuteDelayAsync(
        JsonElement parameters,
        CancellationToken cancellationToken)
    {
        int milliseconds = RequireInt32(parameters, "milliseconds", minimum: 1, maximum: 60_000);
        await Task.Delay(milliseconds, cancellationToken).ConfigureAwait(false);
        return ProtocolJson.ToElement(new { completed = true, milliseconds });
    }

    private async Task<JsonElement> ExecutePublishAsync(
        JsonElement parameters,
        CancellationToken cancellationToken)
    {
        string topic = RequireString(parameters, "topic");
        string label = RequireString(parameters, "label");
        int count = RequireInt32(parameters, "count", minimum: 1, maximum: _options.MaximumPublishCount);
        int payloadBytes = RequireInt32(
            parameters,
            "payload_bytes",
            minimum: 0,
            maximum: _options.MaximumPublishedPayloadBytes);
        string blob = payloadBytes == 0 ? string.Empty : new string('x', payloadBytes);
        long firstSequence = 0;
        long latestSequence = 0;
        for (int ordinal = 1; ordinal <= count; ordinal++)
        {
            DurableEventRecord record = await _events.PublishAsync(
                topic,
                "proof.event",
                ProtocolJson.ToElement(new { label, ordinal, blob }),
                cancellationToken).ConfigureAwait(false);
            if (firstSequence == 0)
            {
                firstSequence = record.Sequence;
            }
            latestSequence = record.Sequence;
        }
        return ProtocolJson.ToElement(new
        {
            count,
            first_sequence = firstSequence,
            latest_sequence = latestSequence
        });
    }

    private static JsonElement ExecuteShutdown(out bool requestShutdown)
    {
        requestShutdown = true;
        return ProtocolJson.ToElement(new { accepted = true });
    }

    private async Task HandleCancelAsync(CancelMessage cancel, CancellationToken cancellationToken)
    {
        bool acknowledged = _requests.TryGetValue(cancel.TargetRequestId, out CancellationTokenSource? target);
        if (acknowledged)
        {
            target!.Cancel();
        }
        await SendResponseAsync(
            cancel.Id,
            ProtocolJson.ToElement(new
            {
                acknowledged,
                target_request_id = cancel.TargetRequestId
            }),
            error: null,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task HandleSubscribeAsync(
        SubscribeMessage subscribe,
        CancellationToken cancellationToken)
    {
        try
        {
            _ = await _events.RegisterAsync(
                subscribe.Topic,
                subscribe.FromSequence,
                SendEvent,
                HandleSubscriptionBackpressureAsync,
                async (registration, activationCancellationToken) =>
                {
                    bool added = false;
                    try
                    {
                        if (!_subscriptions.TryAdd(registration.Subscription.Id, registration.Subscription))
                        {
                            throw new InvalidOperationException("Generated a duplicate subscription identifier.");
                        }
                        added = true;
                        _connectionMetrics.SubscriptionAdded();
                        SubscribedMessage accepted = new()
                        {
                            ReplyTo = subscribe.Id,
                            SubscriptionId = registration.Subscription.Id,
                            Topic = subscribe.Topic,
                            FromSequence = subscribe.FromSequence,
                            ReplayedThroughSequence = registration.Replay.ReplayedThroughSequence,
                            LiveFromSequence = registration.Replay.LatestSequence + 1,
                            MinimumAvailableSequence = registration.Replay.MinimumAvailableSequence,
                            LatestSequence = registration.Replay.LatestSequence
                        };
                        if (!await _outbound.EnqueueControlAsync(accepted, activationCancellationToken)
                            .ConfigureAwait(false))
                        {
                            return false;
                        }
                        registration.Subscription.Start();
                        return true;
                    }
                    finally
                    {
                        if (added && !registration.Subscription.IsStarted
                            && _subscriptions.TryRemove(registration.Subscription.Id, out _))
                        {
                            _connectionMetrics.SubscriptionRemoved();
                        }
                    }
                },
                cancellationToken).ConfigureAwait(false);
        }
        catch (ReplayUnavailableException exception)
        {
            await SendProtocolErrorAsync(
                subscribe.Id,
                exception.Code,
                exception.Message,
                exception.DataObject,
                cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task HandleUnsubscribeAsync(
        UnsubscribeMessage unsubscribe,
        CancellationToken cancellationToken)
    {
        bool removed = await RemoveSubscriptionAsync(unsubscribe.SubscriptionId).ConfigureAwait(false);
        if (!removed)
        {
            await SendResponseAsync(
                unsubscribe.Id,
                result: null,
                new ProtocolError(
                    ProtocolErrorCodes.SubscriptionNotFound,
                    $"Subscription '{unsubscribe.SubscriptionId}' was not found."),
                cancellationToken).ConfigureAwait(false);
            return;
        }
        await SendResponseAsync(
            unsubscribe.Id,
            ProtocolJson.ToElement(new { removed = true }),
            error: null,
            cancellationToken).ConfigureAwait(false);
    }

    private bool SendEvent(EventMessage message) => _outbound.TryEnqueueEvent(message);

    private async Task HandleSubscriptionBackpressureAsync(
        EventSubscription subscription,
        long resumeFromSequence)
    {
        if (_subscriptions.TryRemove(subscription.Id, out _))
        {
            _connectionMetrics.SubscriptionRemoved();
        }
        _ = await _outbound.EnqueueControlAsync(
            new SubscriptionClosedMessage
            {
                SubscriptionId = subscription.Id,
                Code = ProtocolErrorCodes.BackpressureExceeded,
                Message = "The subscriber did not consume event presentation frames within the bounded queue.",
                ResumeFromSequence = resumeFromSequence
            },
            CancellationToken.None).ConfigureAwait(false);
    }

    private async Task<bool> RemoveSubscriptionAsync(string subscriptionId)
    {
        if (!_subscriptions.TryRemove(subscriptionId, out _))
        {
            return false;
        }
        _connectionMetrics.SubscriptionRemoved();
        _ = await _events.UnregisterAsync(subscriptionId).ConfigureAwait(false);
        return true;
    }

    private async Task RemoveAllSubscriptionsAsync()
    {
        foreach (string subscriptionId in _subscriptions.Keys.ToArray())
        {
            _ = await RemoveSubscriptionAsync(subscriptionId).ConfigureAwait(false);
        }
    }

    private async Task SendResponseAsync(
        string replyTo,
        JsonElement? result,
        ProtocolError? error,
        CancellationToken cancellationToken)
    {
        ResponseMessage response = new()
        {
            ReplyTo = replyTo,
            Result = result,
            Error = error
        };
        if (!await _outbound.EnqueueControlAsync(response, cancellationToken).ConfigureAwait(false))
        {
            _lifetime.Cancel();
        }
    }

    private async Task SendProtocolErrorAsync(
        string? replyTo,
        string code,
        string message,
        object? data,
        CancellationToken cancellationToken)
    {
        ProtocolErrorMessage error = new()
        {
            ReplyTo = replyTo,
            Error = new(
                code,
                message,
                data is null ? null : ProtocolJson.ToElement(data))
        };
        if (!await _outbound.EnqueueControlAsync(error, cancellationToken).ConfigureAwait(false))
        {
            _lifetime.Cancel();
        }
    }

    private async Task<IProtocolMessage?> ReadMessageAsync(CancellationToken cancellationToken)
    {
        byte[]? payload = await _codec.ReadPayloadAsync(_stream, cancellationToken).ConfigureAwait(false);
        if (payload is null)
        {
            return null;
        }
        try
        {
            _ = StrictUtf8.GetCharCount(payload);
        }
        catch (DecoderFallbackException exception)
        {
            throw new InvalidUtf8ProtocolException(exception);
        }
        try
        {
            return ProtocolMessageCodec.Deserialize(payload);
        }
        catch (InvalidDataException exception)
        {
            throw new MalformedProtocolException(exception);
        }
    }

    private async Task WriterLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            IProtocolMessage? message = await _outbound.ReadAsync(cancellationToken).ConfigureAwait(false);
            if (message is null)
            {
                return;
            }
            using CancellationTokenSource write = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            write.CancelAfter(TimeSpan.FromMilliseconds(_options.WriteTimeoutMilliseconds));
            try
            {
                await _codec.WriteMessageAsync(_stream, message, write.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _lifetime.Cancel();
                return;
            }
        }
    }

    private void Track(Task task)
    {
        int id = Interlocked.Increment(ref _nextBackgroundTaskId);
        if (!_backgroundTasks.TryAdd(id, task))
        {
            throw new InvalidOperationException("Generated a duplicate background task identifier.");
        }
        _ = task.ContinueWith(
            static (completed, state) =>
            {
                (ConcurrentDictionary<int, Task> tasks, int taskId, CancellationTokenSource lifetime) =
                    ((ConcurrentDictionary<int, Task>, int, CancellationTokenSource))state!;
                tasks.TryRemove(taskId, out _);
                if (completed.IsFaulted)
                {
                    _ = completed.Exception;
                    lifetime.Cancel();
                }
            },
            (_backgroundTasks, id, _lifetime),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    private static string RequireString(JsonElement parameters, string name)
    {
        if (!parameters.TryGetProperty(name, out JsonElement value)
            || value.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(value.GetString()))
        {
            throw new InvalidDataException($"Parameter '{name}' must be a non-empty string.");
        }
        return value.GetString()!;
    }

    private static int RequireInt32(
        JsonElement parameters,
        string name,
        int minimum,
        int maximum)
    {
        if (!parameters.TryGetProperty(name, out JsonElement value)
            || value.ValueKind != JsonValueKind.Number
            || !value.TryGetInt32(out int result)
            || result < minimum
            || result > maximum)
        {
            throw new InvalidDataException(
                $"Parameter '{name}' must be an integer in the range {minimum}..{maximum}.");
        }
        return result;
    }

    private sealed class RequestFailureException(string code, string message, object dataObject)
        : InvalidOperationException(message)
    {
        internal string Code { get; } = code;
        internal object DataObject { get; } = dataObject;
    }

    private sealed class InvalidUtf8ProtocolException(Exception innerException)
        : IOException("Protocol frame is not valid UTF-8.", innerException);

    private sealed class MalformedProtocolException(Exception innerException)
        : IOException("Protocol frame is not a valid strict message.", innerException);
}
