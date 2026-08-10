using System.Buffers.Binary;
using System.Text;
using System.Text.Json;
using Square.PipeProof.Client;
using Square.PipeProof.Protocol;
using Square.PipeProof.Transport.Windows;

namespace Square.PipeProof.Harness;

internal sealed class ProofScenarios(
    HarnessOptions options,
    ServerProcess server)
{
    internal async Task<object?> ExecuteAsync(
        string scenarioId,
        CancellationToken cancellationToken) => scenarioId switch
    {
        "acl-security" => ExecuteAclSecurity(),
        "cross-language-parity" => await ExecuteCrossLanguageParityAsync(cancellationToken).ConfigureAwait(false),
        "version-negotiation" => await ExecuteVersionNegotiationAsync(cancellationToken).ConfigureAwait(false),
        "framing-failures" => await ExecuteFramingFailuresAsync(cancellationToken).ConfigureAwait(false),
        "disconnect-replay" => await ExecuteDisconnectReplayAsync(cancellationToken).ConfigureAwait(false),
        "daemon-restart-reconnect" => await ExecuteDaemonRestartReconnectAsync(cancellationToken).ConfigureAwait(false),
        "slow-subscriber" => await ExecuteSlowSubscriberAsync(cancellationToken).ConfigureAwait(false),
        "replay-window-refusal" => await ExecuteReplayWindowRefusalAsync(cancellationToken).ConfigureAwait(false),
        "graceful-shutdown" => await ExecuteGracefulShutdownAsync(cancellationToken).ConfigureAwait(false),
        _ => throw new ArgumentOutOfRangeException(nameof(scenarioId), scenarioId, "Unknown PipeProof scenario.")
    };

    private object ExecuteAclSecurity()
    {
        ServerReadyEvidence ready = RequireReady();
        ProofAssert.Equal(ProtocolConstants.ProtocolName, ready.Protocol, "Server announced wrong protocol.");
        ProofAssert.SequenceEqual(
            ProtocolConstants.SupportedVersions,
            ready.SupportedVersions,
            "Server announced wrong version set.");
        ProofAssert.True(ready.Acl.GrantsOnlyCurrentUserAndSystem, "Live pipe DACL did not match the required policy.");
        ProofAssert.True(ready.Acl.DaclPresent, "Named pipe has no DACL.");
        ProofAssert.True(ready.Acl.DaclProtected, "Named pipe DACL is not protected from inheritance.");
        ProofAssert.Equal(2, ready.Acl.AllowedSids.Count, "Pipe must grant exactly two identities.");
        ProofAssert.True(ready.NegativeAccessProbe.AccessDenied, "Anonymous identity was not denied.");
        ProofAssert.Equal(5, ready.NegativeAccessProbe.Win32Error, "Negative probe did not fail with access denied.");
        return new
        {
            ready.Server,
            ready.Acl,
            ready.NegativeAccessProbe
        };
    }

    private async Task<object> ExecuteCrossLanguageParityAsync(CancellationToken cancellationToken)
    {
        ClientParityResult dotnet = await RunDotNetAsync<ClientParityResult>(
            ["--pipe-name", server.PipeName, "--scenario", "parity", "--write-fragment-bytes", "1"],
            cancellationToken).ConfigureAwait(false);
        ClientParityResult node = await RunNodeAsync<ClientParityResult>(
            ["--pipe-name", server.PipeName, "--scenario", "parity"],
            cancellationToken).ConfigureAwait(false);

        ProofAssert.Equal(dotnet.Protocol, node.Protocol, "Protocol differs across clients.");
        ProofAssert.Equal(dotnet.Version, node.Version, "Version differs across clients.");
        ProofAssert.Equal(dotnet.EchoText, node.EchoText, "Unicode echo differs across clients.");
        ProofAssert.Equal(dotnet.CancelAcknowledged, node.CancelAcknowledged, "Cancellation acknowledgement differs.");
        ProofAssert.Equal(dotnet.CancellationCode, node.CancellationCode, "Cancellation error differs.");
        ProofAssert.Equal(dotnet.PublishedCount, node.PublishedCount, "Publish count differs.");
        ProofAssert.SequenceEqual(dotnet.Labels, node.Labels, "Event labels differ.");
        ProofAssert.SequenceEqual(dotnet.Ordinals, node.Ordinals, "Event ordinals differ.");
        ProofAssert.True(dotnet.EventSequencesStrictlyIncreasing, ".NET client observed non-increasing events.");
        ProofAssert.True(node.EventSequencesStrictlyIncreasing, "Node client observed non-increasing events.");
        ProofAssert.True(dotnet.ServerQueuesWithinDeclaredBounds, ".NET fixture observed an unbounded server queue.");
        ProofAssert.True(node.ServerQueuesWithinDeclaredBounds, "Node fixture observed an unbounded server queue.");
        ProofAssert.Equal(dotnet.DeclaredControlQueueCapacity, node.DeclaredControlQueueCapacity, "Control queue contract differs.");
        ProofAssert.Equal(dotnet.DeclaredEventQueueCapacity, node.DeclaredEventQueueCapacity, "Event queue contract differs.");
        ProofAssert.Equal(dotnet.DeclaredSubscriptionQueueCapacity, node.DeclaredSubscriptionQueueCapacity, "Subscription queue contract differs.");
        return new { dotnet, node };
    }

    private async Task<object> ExecuteVersionNegotiationAsync(CancellationToken cancellationToken)
    {
        ClientIncompatibleResult dotnet = await RunDotNetAsync<ClientIncompatibleResult>(
            ["--pipe-name", server.PipeName, "--scenario", "incompatible"],
            cancellationToken).ConfigureAwait(false);
        ClientIncompatibleResult node = await RunNodeAsync<ClientIncompatibleResult>(
            ["--pipe-name", server.PipeName, "--scenario", "incompatible"],
            cancellationToken).ConfigureAwait(false);
        ProofAssert.Equal(ProtocolErrorCodes.IncompatibleVersion, dotnet.ErrorCode, ".NET incompatibility code is wrong.");
        ProofAssert.Equal(dotnet.ErrorCode, node.ErrorCode, "Clients received different incompatibility codes.");
        ProofAssert.SequenceEqual(dotnet.SupportedVersions, node.SupportedVersions, "Clients report different supported versions.");
        return new { dotnet, node };
    }

    private async Task<object> ExecuteFramingFailuresAsync(CancellationToken cancellationToken)
    {
        await using (RawConnection raw = await ConnectRawAsync(maximumWriteChunkBytes: 1, cancellationToken)
            .ConfigureAwait(false))
        {
            LengthFramedJsonCodec codec = new();
            RequestMessage first = new()
            {
                Id = "coalesced-1",
                Method = "proof.echo",
                Params = ProtocolJson.ToElement(new { text = "coalesced-1" })
            };
            RequestMessage second = new()
            {
                Id = "coalesced-2",
                Method = "proof.echo",
                Params = ProtocolJson.ToElement(new { text = "coalesced-2" })
            };
            byte[] coalesced = [.. codec.Encode(first), .. codec.Encode(second)];
            await raw.Stream.WriteAsync(coalesced, cancellationToken).ConfigureAwait(false);
            await raw.Stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            Dictionary<string, int> replies = new(StringComparer.Ordinal);
            while (replies.Count < 2)
            {
                ResponseMessage response = RequireType<ResponseMessage>(
                    await raw.Codec.ReadMessageAsync(raw.Stream, cancellationToken).ConfigureAwait(false));
                ProofAssert.True(response.Error is null, "Coalesced request failed.");
                replies[response.ReplyTo] = response.Result!.Value
                    .GetProperty("text")
                    .GetString() switch
                    {
                        "coalesced-1" => 1,
                        "coalesced-2" => 2,
                        string unexpected => throw new InvalidDataException(
                            $"Unexpected coalesced echo '{unexpected}'."),
                        null => throw new InvalidDataException("Coalesced echo text was null.")
                    };
            }
            ProofAssert.Equal(1, replies["coalesced-1"], "First coalesced response is wrong.");
            ProofAssert.Equal(2, replies["coalesced-2"], "Second coalesced response is wrong.");
        }

        string malformedOutcome;
        await using (RawConnection malformed = await ConnectRawAsync(int.MaxValue, cancellationToken)
            .ConfigureAwait(false))
        {
            byte[] badFrame = malformed.Codec.EncodePayload("{broken"u8);
            await malformed.Stream.WriteAsync(badFrame, cancellationToken).ConfigureAwait(false);
            await malformed.Stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            malformedOutcome = await ExpectProtocolFailureOrDisconnectAsync(malformed, cancellationToken)
                .ConfigureAwait(false);
        }

        string oversizedOutcome;
        await using (Stream oversized = await NamedPipeConnector.ConnectAsync(
            server.PipeName,
            TimeSpan.FromSeconds(10),
            cancellationToken).ConfigureAwait(false))
        {
            byte[] prefix = new byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(
                prefix,
                checked((uint)ProtocolConstants.DefaultMaximumPayloadBytes + 1));
            await oversized.WriteAsync(prefix, cancellationToken).ConfigureAwait(false);
            await oversized.FlushAsync(cancellationToken).ConfigureAwait(false);
            RawConnection raw = new(oversized, new LengthFramedJsonCodec(), null, ownsStream: false);
            oversizedOutcome = await ExpectProtocolFailureOrDisconnectAsync(raw, cancellationToken)
                .ConfigureAwait(false);
        }

        return new
        {
            fragmented_handshake = "accepted",
            coalesced_request_count = 2,
            malformed_outcome = malformedOutcome,
            oversized_outcome = oversizedOutcome
        };
    }

    private async Task<object> ExecuteDisconnectReplayAsync(CancellationToken cancellationToken)
    {
        long cursor;
        IReadOnlyList<long> onlineSequences;
        await using (ProtocolClientConnection connection = await server.ConnectAsync(cancellationToken).ConfigureAwait(false))
        {
            await using ProtocolSubscription subscription = await connection.SubscribeAsync(
                "reconnect",
                0,
                cancellationToken).ConfigureAwait(false);
            _ = await connection.RequestAsync(
                "proof.publish",
                new { topic = "reconnect", count = 2, payload_bytes = 0, label = "online" },
                cancellationToken).ConfigureAwait(false);
            List<long> observed = [];
            for (int index = 0; index < 2; index++)
            {
                EventMessage message = await subscription.Events.ReadAsync(cancellationToken).ConfigureAwait(false);
                observed.Add(message.Sequence);
            }
            onlineSequences = observed;
            cursor = observed[^1];
            await subscription.DisposeAsync().ConfigureAwait(false);
            _ = await connection.RequestAsync(
                "proof.publish",
                new { topic = "reconnect", count = 3, payload_bytes = 0, label = "offline" },
                cancellationToken).ConfigureAwait(false);
        }

        ClientReplayResult dotnet = await RunDotNetAsync<ClientReplayResult>(
            [
                "--pipe-name", server.PipeName,
                "--scenario", "replay",
                "--from-sequence", cursor.ToString(System.Globalization.CultureInfo.InvariantCulture),
                "--event-count", "3"
            ],
            cancellationToken).ConfigureAwait(false);
        ClientReplayResult node = await RunNodeAsync<ClientReplayResult>(
            [
                "--pipe-name", server.PipeName,
                "--scenario", "replay",
                "--from-sequence", cursor.ToString(System.Globalization.CultureInfo.InvariantCulture),
                "--event-count", "3"
            ],
            cancellationToken).ConfigureAwait(false);
        ProofAssert.SequenceEqual(dotnet.Sequences, node.Sequences, "Replay sequences differ across clients.");
        ProofAssert.SequenceEqual(dotnet.Ordinals, node.Ordinals, "Replay payloads differ across clients.");
        ProofAssert.True(dotnet.Sequences.All(sequence => sequence > cursor), "Replay returned an event at or before the cursor.");
        return new { cursor, online_sequences = onlineSequences, dotnet, node };
    }

    private async Task<object> ExecuteDaemonRestartReconnectAsync(CancellationToken cancellationToken)
    {
        string directory = Path.Combine(options.OutputDirectory, "node-reconnect");
        Directory.CreateDirectory(directory);
        string readyFile = Path.Combine(directory, "ready.json");
        string progressFile = Path.Combine(directory, "progress.json");
        File.Delete(readyFile);
        File.Delete(progressFile);

        string[] nodeArguments =
        [
            options.NodeFixture,
            "--pipe-name", server.PipeName,
            "--scenario", "reconnect",
            "--event-count", "4",
            "--ready-file", readyFile,
            "--progress-file", progressFile,
            "--timeout-ms", "15000"
        ];
        await using StartedProcess node = ProcessRunner.StartExecutable(options.NodeExecutable, nodeArguments);
        await WaitForFileAsync(readyFile, node, TimeSpan.FromSeconds(15), cancellationToken).ConfigureAwait(false);

        ServerReadyEvidence before = RequireReady();
        await using (ProtocolClientConnection publisher = await server.ConnectAsync(cancellationToken).ConfigureAwait(false))
        {
            _ = await publisher.RequestAsync(
                "proof.publish",
                new { topic = "reconnect", count = 2, payload_bytes = 0, label = "before-restart" },
                cancellationToken).ConfigureAwait(false);
        }
        NodeReconnectProgress progress = await WaitForProgressAsync(
            progressFile,
            minimumCount: 2,
            node,
            TimeSpan.FromSeconds(15),
            cancellationToken).ConfigureAwait(false);
        long durableBeforeRestart = progress.Sequences[^1];

        _ = await server.KillAsync("intentional daemon crash", cancellationToken).ConfigureAwait(false);
        ServerReadyEvidence after = await server.StartAsync(cancellationToken).ConfigureAwait(false);
        ProofAssert.True(!string.Equals(before.Server.InstanceId, after.Server.InstanceId, StringComparison.Ordinal),
            "Server instance ID did not change after restart.");
        ProofAssert.Equal(before.Server.Epoch + 1, after.Server.Epoch, "Server epoch did not increment exactly once.");
        ProofAssert.True(after.LatestSequence >= durableBeforeRestart, "Restart lost a durable event cursor.");

        await using (ProtocolClientConnection publisher = await server.ConnectAsync(cancellationToken).ConfigureAwait(false))
        {
            _ = await publisher.RequestAsync(
                "proof.publish",
                new { topic = "reconnect", count = 2, payload_bytes = 0, label = "after-restart" },
                cancellationToken).ConfigureAwait(false);
        }

        ProcessResult result = await node.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"Node reconnect fixture failed: {result.StandardError}");
        }
        await File.WriteAllTextAsync(Path.Combine(directory, "stdout.log"), result.StandardOutput, cancellationToken)
            .ConfigureAwait(false);
        await File.WriteAllTextAsync(Path.Combine(directory, "stderr.log"), result.StandardError, cancellationToken)
            .ConfigureAwait(false);
        NodeReconnectResult reconnect = ParseLastJsonLine<NodeReconnectResult>(result.StandardOutput);
        ProofAssert.True(reconnect.SuccessfulConnections >= 2, "Node client did not establish a second connection.");
        ProofAssert.Equal(4, reconnect.Sequences.Count, "Node reconnect fixture received wrong event count.");
        ProofAssert.True(IsStrictlyIncreasing(reconnect.Sequences), "Node reconnect sequence is not strictly increasing.");
        ProofAssert.True(reconnect.Sequences.Take(2).SequenceEqual(progress.Sequences),
            "Node reconnect result does not preserve pre-restart observations.");
        return new
        {
            before_server = before.Server,
            after_server = after.Server,
            durable_sequence_before_restart = durableBeforeRestart,
            restart_ready_latest_sequence = after.LatestSequence,
            reconnect
        };
    }

    private async Task<object> ExecuteSlowSubscriberAsync(CancellationToken cancellationToken)
    {
        await using RawConnection slow = await ConnectRawAsync(int.MaxValue, cancellationToken).ConfigureAwait(false);
        SubscribeMessage subscribe = new()
        {
            Id = "slow-subscribe",
            Topic = "slow",
            FromSequence = 0
        };
        await slow.Codec.WriteMessageAsync(slow.Stream, subscribe, cancellationToken).ConfigureAwait(false);
        SubscribedMessage accepted = RequireType<SubscribedMessage>(
            await slow.Codec.ReadMessageAsync(slow.Stream, cancellationToken).ConfigureAwait(false));

        await using ProtocolClientConnection producer = await server.ConnectAsync(cancellationToken).ConfigureAwait(false);
        _ = await producer.RequestAsync(
            "proof.publish",
            new { topic = "slow", count = 48, payload_bytes = 32768, label = "backpressure" },
            cancellationToken).ConfigureAwait(false);
        await Task.Delay(500, cancellationToken).ConfigureAwait(false);
        JsonElement stats = await producer.RequestAsync("proof.stats", new { }, cancellationToken).ConfigureAwait(false);
        long disconnects = stats.GetProperty("slow_subscriber_disconnects").GetInt64();
        int maximumQueueDepth = stats.GetProperty("maximum_observed_queue_depth").GetInt32();
        int maximumSubscriptionDepth = stats.GetProperty("maximum_observed_subscription_queue_depth").GetInt32();
        ProtocolLimits limits = RequireReady().Limits;
        ProofAssert.True(disconnects >= 1, "Slow subscriber was not disconnected.");
        ProofAssert.True(
            maximumQueueDepth <= limits.ControlQueueCapacity + limits.EventQueueCapacity,
            "Combined connection queue exceeded its declared capacities.");
        ProofAssert.True(
            maximumSubscriptionDepth <= limits.SubscriptionQueueCapacity,
            "Subscription queue exceeded capacity.");
        foreach (JsonElement connection in stats.GetProperty("connections").EnumerateArray())
        {
            JsonElement queue = connection.GetProperty("queue");
            ProofAssert.True(
                queue.GetProperty("peak_total_depth").GetInt32()
                    <= queue.GetProperty("control_capacity").GetInt32()
                        + queue.GetProperty("event_capacity").GetInt32(),
                "A connection queue exceeded its declared capacities.");
        }
        return new
        {
            subscription_id = accepted.SubscriptionId,
            slow_subscriber_disconnects = disconnects,
            maximum_observed_queue_depth = maximumQueueDepth,
            maximum_observed_subscription_queue_depth = maximumSubscriptionDepth,
            declared_limits = limits
        };
    }

    private async Task<object> ExecuteReplayWindowRefusalAsync(CancellationToken cancellationToken)
    {
        await using ProtocolClientConnection connection = await server.ConnectAsync(cancellationToken).ConfigureAwait(false);
        _ = await connection.RequestAsync(
            "proof.publish",
            new { topic = "truncate", count = 70, payload_bytes = 0, label = "retention" },
            cancellationToken).ConfigureAwait(false);
        string errorCode;
        try
        {
            await using ProtocolSubscription _ = await connection.SubscribeAsync(
                "truncate",
                fromSequence: 1,
                cancellationToken).ConfigureAwait(false);
            throw new InvalidOperationException("Replay older than retention unexpectedly succeeded.");
        }
        catch (RemoteProtocolException exception)
        {
            errorCode = exception.Error.Code;
        }
        ProofAssert.Equal(ProtocolErrorCodes.ReplayTruncated, errorCode, "Old replay cursor returned wrong error.");
        JsonElement stats = await connection.RequestAsync("proof.stats", new { }, cancellationToken)
            .ConfigureAwait(false);
        long minimum = stats.GetProperty("minimum_available_event_sequence").GetInt64();
        ProofAssert.True(minimum > 2, "Retention window did not advance enough for the refusal test.");
        return new { error_code = errorCode, minimum_available_sequence = minimum };
    }


    private async Task<object> ExecuteGracefulShutdownAsync(CancellationToken cancellationToken)
    {
        ProcessResult result = await server.ShutdownAsync(cancellationToken).ConfigureAwait(false);
        ProofAssert.Equal(0, result.ExitCode, "Graceful server shutdown returned a nonzero exit code.");
        string metricsPath = server.MetricsFile
            ?? throw new InvalidOperationException("Server metrics path is unavailable.");
        ProofAssert.True(File.Exists(metricsPath), "Server did not write final metrics evidence.");
        ServerFinalEvidence final = ProtocolJson.DeserializeText<ServerFinalEvidence>(
            await File.ReadAllTextAsync(metricsPath, cancellationToken).ConfigureAwait(false));
        ProofAssert.True(final.Metrics.ClosedConnections <= final.Metrics.AcceptedConnections,
            "Closed connection count exceeds accepted connections.");
        ProofAssert.Equal(0, final.Metrics.ActiveConnections,
            "Graceful shutdown left active protocol connections.");
        ProofAssert.True(
            final.Metrics.MaximumObservedQueueDepth
                <= RequireReady().Limits.ControlQueueCapacity + RequireReady().Limits.EventQueueCapacity,
            "Final combined connection queue depth exceeded declared capacities.");
        ProofAssert.True(
            final.Metrics.MaximumObservedSubscriptionQueueDepth
                <= RequireReady().Limits.SubscriptionQueueCapacity,
            "Final subscription queue depth exceeded declared capacity.");
        return new
        {
            exit_code = result.ExitCode,
            final.ShutdownReason,
            final.Metrics
        };
    }

    private async Task<RawConnection> ConnectRawAsync(
        int maximumWriteChunkBytes,
        CancellationToken cancellationToken)
    {
        Stream stream = await NamedPipeConnector.ConnectAsync(
            server.PipeName,
            TimeSpan.FromSeconds(10),
            cancellationToken).ConfigureAwait(false);
        LengthFramedJsonCodec codec = new(
            ProtocolConstants.DefaultMaximumPayloadBytes,
            maximumWriteChunkBytes);
        HelloMessage hello = new()
        {
            Id = $"raw-hello-{Guid.NewGuid():N}",
            Client = new("raw-harness", "0.1.0", $"raw-{Guid.NewGuid():N}"),
            Capabilities = ["request", "cancel", "subscribe", "replay"]
        };
        await codec.WriteMessageAsync(stream, hello, cancellationToken).ConfigureAwait(false);
        HelloAckMessage accepted = RequireType<HelloAckMessage>(
            await codec.ReadMessageAsync(stream, cancellationToken).ConfigureAwait(false));
        ProofAssert.Equal(hello.Id, accepted.ReplyTo, "Raw handshake reply mismatch.");
        return new(stream, codec, accepted, ownsStream: true);
    }

    private async Task<string> ExpectProtocolFailureOrDisconnectAsync(
        RawConnection raw,
        CancellationToken cancellationToken)
    {
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));
        try
        {
            IProtocolMessage? message = await raw.Codec.ReadMessageAsync(raw.Stream, timeout.Token).ConfigureAwait(false);
            if (message is null)
            {
                return "clean-disconnect";
            }
            if (message is ProtocolErrorMessage protocolError
                && protocolError.Error.Code == ProtocolErrorCodes.MalformedMessage)
            {
                return protocolError.Error.Code;
            }
            throw new InvalidOperationException($"Expected malformed protocol error or disconnect, received '{message.Kind}'.");
        }
        catch (Exception exception) when (exception is IOException)
        {
            return "transport-disconnect";
        }
    }

    private async Task<T> RunDotNetAsync<T>(
        IEnumerable<string> arguments,
        CancellationToken cancellationToken)
    {
        ProcessResult result = await ProcessRunner.RunArtifactAsync(
            options.DotNetClientArtifact,
            arguments,
            options.ScenarioTimeout,
            cancellationToken).ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($".NET fixture failed: {result.StandardError}");
        }
        return ParseLastJsonLine<T>(result.StandardOutput);
    }

    private async Task<T> RunNodeAsync<T>(
        IEnumerable<string> arguments,
        CancellationToken cancellationToken)
    {
        ProcessResult result = await ProcessRunner.RunExecutableAsync(
            options.NodeExecutable,
            new[] { options.NodeFixture }.Concat(arguments),
            options.ScenarioTimeout,
            cancellationToken).ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"Node fixture failed: {result.StandardError}");
        }
        return ParseLastJsonLine<T>(result.StandardOutput);
    }

    private static T ParseLastJsonLine<T>(string output)
    {
        string line = output
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .LastOrDefault()
            ?? throw new InvalidDataException("Fixture produced no JSON output.");
        return ProtocolJson.DeserializeText<T>(line);
    }

    private static async Task WaitForFileAsync(
        string path,
        StartedProcess process,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow + timeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (File.Exists(path)) return;
            if (process.HasExited)
            {
                ProcessResult result = await process.WaitAsync(TimeSpan.FromSeconds(1), cancellationToken)
                    .ConfigureAwait(false);
                throw new InvalidOperationException($"Fixture exited before readiness: {result.StandardError}");
            }
            await Task.Delay(25, cancellationToken).ConfigureAwait(false);
        }
        throw new TimeoutException($"Timed out waiting for '{path}'.");
    }

    private static async Task<NodeReconnectProgress> WaitForProgressAsync(
        string path,
        int minimumCount,
        StartedProcess process,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow + timeout;
        Exception? lastError = null;
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (File.Exists(path))
            {
                try
                {
                    NodeReconnectProgress progress = ProtocolJson.DeserializeText<NodeReconnectProgress>(
                        await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false));
                    if (progress.Count >= minimumCount) return progress;
                }
                catch (Exception exception) when (exception is IOException or JsonException or InvalidDataException)
                {
                    lastError = exception;
                }
            }
            if (process.HasExited)
            {
                ProcessResult result = await process.WaitAsync(TimeSpan.FromSeconds(1), cancellationToken)
                    .ConfigureAwait(false);
                throw new InvalidOperationException($"Reconnect fixture exited early: {result.StandardError}");
            }
            await Task.Delay(25, cancellationToken).ConfigureAwait(false);
        }
        throw new TimeoutException($"Timed out waiting for reconnect progress '{path}'.", lastError);
    }

    private ServerReadyEvidence RequireReady() =>
        server.Ready ?? throw new InvalidOperationException("Server readiness evidence is unavailable.");

    private static T RequireType<T>(object? value) where T : class =>
        value as T ?? throw new InvalidOperationException(
            $"Expected {typeof(T).Name}, received {value?.GetType().Name ?? "null"}.");

    private static bool IsStrictlyIncreasing(IReadOnlyList<long> values)
    {
        for (int index = 1; index < values.Count; index++)
        {
            if (values[index] <= values[index - 1]) return false;
        }
        return true;
    }

    private sealed class RawConnection(
        Stream stream,
        LengthFramedJsonCodec codec,
        HelloAckMessage? handshake,
        bool ownsStream) : IAsyncDisposable
    {
        internal Stream Stream { get; } = stream;
        internal LengthFramedJsonCodec Codec { get; } = codec;
        internal HelloAckMessage? Handshake { get; } = handshake;

        public async ValueTask DisposeAsync()
        {
            if (ownsStream) await Stream.DisposeAsync().ConfigureAwait(false);
        }
    }
}
