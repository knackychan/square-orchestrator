using System.Collections.Concurrent;
using System.Text.Json;
using Square.PipeProof.Protocol;

namespace Square.PipeProof.Client;

public sealed class ProtocolClientConnection : IAsyncDisposable
{
    private readonly Stream _stream;
    private readonly ProtocolClientOptions _options;
    private readonly LengthFramedJsonCodec _codec;
    private readonly SemaphoreSlim _writeGate = new(1, 1);
    private readonly ConcurrentDictionary<string, TaskCompletionSource<IProtocolMessage>> _pending = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, TaskCompletionSource<ProtocolSubscription>> _pendingSubscriptions = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, ProtocolSubscription> _subscriptions = new(StringComparer.Ordinal);
    private readonly CancellationTokenSource _lifetime = new();
    private readonly TaskCompletionSource<object?> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private Task? _readerTask;
    private int _handshakeStarted;
    private int _disposed;

    public ProtocolClientConnection(Stream stream, ProtocolClientOptions? options = null)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _options = options ?? new ProtocolClientOptions();
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(_options.MaximumPayloadBytes);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(_options.MaximumWriteChunkBytes);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(_options.LocalSubscriptionCapacity);
        _codec = new LengthFramedJsonCodec(
            _options.MaximumPayloadBytes,
            _options.MaximumWriteChunkBytes);
    }

    public HelloAckMessage? Handshake { get; private set; }

    public bool IsConnected => Handshake is not null && !_lifetime.IsCancellationRequested;

    public Task Completion => _completion.Task;

    public async Task<HelloAckMessage> HandshakeAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (Interlocked.Exchange(ref _handshakeStarted, 1) != 0)
        {
            throw new InvalidOperationException("The protocol handshake has already been attempted.");
        }

        string id = NewId("hello");
        HelloMessage hello = new()
        {
            Protocol = _options.RequestedProtocol,
            Version = _options.RequestedVersion,
            Id = id,
            Client = new(
                _options.ClientKind,
                _options.ClientVersion,
                _options.ClientInstanceId),
            Capabilities = ["request", "cancel", "subscribe", "replay"]
        };

        await WriteAsync(hello, cancellationToken).ConfigureAwait(false);
        IProtocolMessage? response = await _codec.ReadMessageAsync(_stream, cancellationToken).ConfigureAwait(false);
        if (response is null)
        {
            throw new ProtocolDisconnectedException("The server disconnected during handshake.");
        }

        switch (response)
        {
            case ProtocolErrorMessage protocolError:
                throw new RemoteProtocolException(protocolError.Error, protocolError.SupportedVersions);
            case HelloAckMessage accepted when string.Equals(accepted.ReplyTo, id, StringComparison.Ordinal):
                if (!string.Equals(accepted.Protocol, _options.RequestedProtocol, StringComparison.Ordinal)
                    || !string.Equals(accepted.Version, _options.RequestedVersion, StringComparison.Ordinal))
                {
                    throw new InvalidDataException("The server acknowledged a different protocol or version.");
                }
                Handshake = accepted;
                _readerTask = Task.Run(ReadLoopAsync);
                return accepted;
            default:
                throw new InvalidDataException($"Expected hello_ack for '{id}', received '{response.Kind}'.");
        }
    }

    public ProtocolPendingRequest BeginRequest(string method, object? parameters = null)
    {
        EnsureReady();
        ArgumentException.ThrowIfNullOrWhiteSpace(method);
        string id = NewId("request");
        TaskCompletionSource<IProtocolMessage> completion = NewCompletion();
        if (!_pending.TryAdd(id, completion))
        {
            throw new InvalidOperationException("Generated a duplicate request identifier.");
        }

        RequestMessage request = new()
        {
            Id = id,
            Method = method,
            Params = ProtocolJson.ToElement(parameters ?? new { })
        };
        _ = SendRegisteredAsync(id, request, completion);
        return new ProtocolPendingRequest(id, AwaitResponseAsync(id, completion.Task));
    }

    public async Task<JsonElement> RequestAsync(
        string method,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        ProtocolPendingRequest pending = BeginRequest(method, parameters);
        ResponseMessage response = await pending.Response.WaitAsync(cancellationToken).ConfigureAwait(false);
        if (response.Error is not null)
        {
            throw new RemoteProtocolException(response.Error);
        }
        return response.Result ?? throw new InvalidDataException("Successful response contained no result.");
    }

    public async Task<ResponseMessage> CancelAsync(
        string targetRequestId,
        CancellationToken cancellationToken = default)
    {
        EnsureReady();
        ArgumentException.ThrowIfNullOrWhiteSpace(targetRequestId);
        string id = NewId("cancel");
        TaskCompletionSource<IProtocolMessage> completion = NewCompletion();
        if (!_pending.TryAdd(id, completion))
        {
            throw new InvalidOperationException("Generated a duplicate cancellation identifier.");
        }
        CancelMessage cancel = new() { Id = id, TargetRequestId = targetRequestId };
        await SendRegisteredAsync(id, cancel, completion).ConfigureAwait(false);
        IProtocolMessage message = await completion.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        ResponseMessage response = message as ResponseMessage
            ?? throw new InvalidDataException($"Cancellation received unexpected '{message.Kind}' response.");
        return response;
    }

    public async Task<ProtocolSubscription> SubscribeAsync(
        string topic,
        long fromSequence = 0,
        CancellationToken cancellationToken = default)
    {
        EnsureReady();
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentOutOfRangeException.ThrowIfNegative(fromSequence);
        string id = NewId("subscribe");
        TaskCompletionSource<ProtocolSubscription> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_pendingSubscriptions.TryAdd(id, completion))
        {
            throw new InvalidOperationException("Generated a duplicate subscription identifier.");
        }
        SubscribeMessage subscribe = new() { Id = id, Topic = topic, FromSequence = fromSequence };
        try
        {
            await WriteAsync(subscribe, _lifetime.Token).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _pendingSubscriptions.TryRemove(id, out _);
            completion.TrySetException(exception);
        }
        return await completion.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UnsubscribeAsync(
        string subscriptionId,
        CancellationToken cancellationToken = default)
    {
        EnsureReady();
        ArgumentException.ThrowIfNullOrWhiteSpace(subscriptionId);
        string id = NewId("unsubscribe");
        TaskCompletionSource<IProtocolMessage> completion = NewCompletion();
        if (!_pending.TryAdd(id, completion))
        {
            throw new InvalidOperationException("Generated a duplicate unsubscribe identifier.");
        }
        UnsubscribeMessage unsubscribe = new() { Id = id, SubscriptionId = subscriptionId };
        await SendRegisteredAsync(id, unsubscribe, completion).ConfigureAwait(false);
        IProtocolMessage message = await completion.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        if (message is not ResponseMessage response)
        {
            throw new InvalidDataException($"Unsubscribe received unexpected '{message.Kind}' response.");
        }
        if (response.Error is not null)
        {
            throw new RemoteProtocolException(response.Error);
        }
        if (_subscriptions.TryRemove(subscriptionId, out ProtocolSubscription? subscription))
        {
            subscription.Complete();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _lifetime.Cancel();
        try
        {
            await _stream.DisposeAsync().ConfigureAwait(false);
        }
        catch (IOException)
        {
        }

        if (_readerTask is not null)
        {
            try
            {
                await _readerTask.ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is IOException or OperationCanceledException)
            {
            }
        }

        CompleteAll(new ObjectDisposedException(nameof(ProtocolClientConnection)));
        _completion.TrySetResult(null);
        _writeGate.Dispose();
        _lifetime.Dispose();
    }

    private async Task SendRegisteredAsync(
        string id,
        IProtocolMessage message,
        TaskCompletionSource<IProtocolMessage> completion)
    {
        try
        {
            await WriteAsync(message, _lifetime.Token).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _pending.TryRemove(id, out _);
            completion.TrySetException(exception);
        }
    }

    private static async Task<ResponseMessage> AwaitResponseAsync(
        string id,
        Task<IProtocolMessage> responseTask)
    {
        IProtocolMessage message = await responseTask.ConfigureAwait(false);
        return message as ResponseMessage
            ?? throw new InvalidDataException($"Request '{id}' received unexpected '{message.Kind}' response.");
    }

    private async Task ReadLoopAsync()
    {
        Exception? failure = null;
        try
        {
            while (!_lifetime.IsCancellationRequested)
            {
                IProtocolMessage? message = await _codec.ReadMessageAsync(_stream, _lifetime.Token).ConfigureAwait(false);
                if (message is null)
                {
                    failure = new ProtocolDisconnectedException("The protocol server closed the connection.");
                    break;
                }
                DispatchInbound(message);
            }
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            failure = exception is ProtocolDisconnectedException
                ? exception
                : new ProtocolDisconnectedException("The protocol connection failed.", exception);
        }
        finally
        {
            _lifetime.Cancel();
            Exception completionError = failure ?? new ProtocolDisconnectedException("The protocol connection stopped.");
            CompleteAll(completionError);
            if (failure is null)
            {
                _completion.TrySetResult(null);
            }
            else
            {
                _completion.TrySetException(failure);
            }
        }
    }

    private void DispatchInbound(IProtocolMessage message)
    {
        switch (message)
        {
            case ResponseMessage response:
                CompletePending(response.ReplyTo, response);
                break;
            case SubscribedMessage subscribed:
                if (!_pendingSubscriptions.TryRemove(
                    subscribed.ReplyTo,
                    out TaskCompletionSource<ProtocolSubscription>? pendingSubscription))
                {
                    throw new InvalidDataException(
                        $"Received subscription acknowledgement for unknown operation '{subscribed.ReplyTo}'.");
                }
                ProtocolSubscription subscription = new(this, subscribed, _options.LocalSubscriptionCapacity);
                if (!_subscriptions.TryAdd(subscription.Id, subscription))
                {
                    throw new InvalidDataException($"Duplicate server subscription identifier '{subscription.Id}'.");
                }
                pendingSubscription.TrySetResult(subscription);
                break;
            case ProtocolErrorMessage protocolError when protocolError.ReplyTo is not null:
                RemoteProtocolException remote = new(protocolError.Error, protocolError.SupportedVersions);
                if (_pendingSubscriptions.TryRemove(
                    protocolError.ReplyTo,
                    out TaskCompletionSource<ProtocolSubscription>? failedSubscription))
                {
                    failedSubscription.TrySetException(remote);
                }
                else if (_pending.TryRemove(protocolError.ReplyTo, out TaskCompletionSource<IProtocolMessage>? failedOperation))
                {
                    failedOperation.TrySetException(remote);
                }
                else
                {
                    throw new InvalidDataException(
                        $"Received protocol error for unknown operation '{protocolError.ReplyTo}'.");
                }
                break;
            case ProtocolErrorMessage protocolError:
                throw new RemoteProtocolException(protocolError.Error, protocolError.SupportedVersions);
            case EventMessage eventMessage:
                if (!_subscriptions.TryGetValue(eventMessage.SubscriptionId, out ProtocolSubscription? eventSubscription))
                {
                    throw new InvalidDataException(
                        $"Received event for unknown subscription '{eventMessage.SubscriptionId}'.");
                }
                if (!eventSubscription.TryPublish(eventMessage))
                {
                    throw new InvalidDataException(
                        $"Local subscription '{eventMessage.SubscriptionId}' exceeded its bounded capacity.");
                }
                break;
            case SubscriptionClosedMessage closed:
                if (_subscriptions.TryRemove(closed.SubscriptionId, out ProtocolSubscription? closedSubscription))
                {
                    closedSubscription.Complete(new RemoteProtocolException(
                        new ProtocolError(closed.Code, closed.Message, ProtocolJson.ToElement(new
                        {
                            resume_from_sequence = closed.ResumeFromSequence
                        }))));
                }
                break;
            case ServerGoingAwayMessage goingAway:
                throw new ProtocolDisconnectedException(
                    $"Server is going away: {goingAway.Reason}; suggested reconnect delay {goingAway.ReconnectDelayMilliseconds} ms.");
            default:
                throw new InvalidDataException($"Unexpected inbound protocol message '{message.Kind}'.");
        }
    }

    private void CompletePending(string id, IProtocolMessage message)
    {
        if (!_pending.TryRemove(id, out TaskCompletionSource<IProtocolMessage>? completion))
        {
            throw new InvalidDataException($"Received response for unknown operation '{id}'.");
        }
        completion.TrySetResult(message);
    }

    private async Task WriteAsync(IProtocolMessage message, CancellationToken cancellationToken)
    {
        await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await _codec.WriteMessageAsync(_stream, message, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _writeGate.Release();
        }
    }

    private void CompleteAll(Exception exception)
    {
        foreach ((string id, TaskCompletionSource<IProtocolMessage> completion) in _pending)
        {
            if (_pending.TryRemove(id, out _))
            {
                completion.TrySetException(exception);
            }
        }
        foreach ((string id, TaskCompletionSource<ProtocolSubscription> completion) in _pendingSubscriptions)
        {
            if (_pendingSubscriptions.TryRemove(id, out _))
            {
                completion.TrySetException(exception);
            }
        }
        foreach ((string id, ProtocolSubscription subscription) in _subscriptions)
        {
            if (_subscriptions.TryRemove(id, out _))
            {
                subscription.Complete(exception);
            }
        }
    }

    private void EnsureReady()
    {
        ThrowIfDisposed();
        if (Handshake is null)
        {
            throw new InvalidOperationException("Protocol handshake has not completed.");
        }
        if (_lifetime.IsCancellationRequested)
        {
            throw new ProtocolDisconnectedException("Protocol connection is no longer active.");
        }
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);

    private static string NewId(string prefix) => $"{prefix}-{Guid.NewGuid():N}";

    private static TaskCompletionSource<IProtocolMessage> NewCompletion() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}
