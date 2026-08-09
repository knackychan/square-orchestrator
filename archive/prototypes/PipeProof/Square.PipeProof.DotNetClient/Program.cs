using System.Text.Json;
using Square.PipeProof.Client;
using Square.PipeProof.Protocol;
using Square.PipeProof.Transport.Windows;

namespace Square.PipeProof.DotNetClient;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (!OperatingSystem.IsWindows())
        {
            Console.Error.WriteLine("SP00-T03 .NET named-pipe client fixture requires Windows.");
            return 2;
        }

        try
        {
            ClientCommandLineOptions options = ClientCommandLineOptions.Parse(args);
            using CancellationTokenSource timeout = new(TimeSpan.FromMilliseconds(options.TimeoutMilliseconds));
            object result = options.Scenario switch
            {
                "parity" => await RunParityAsync(options, timeout.Token).ConfigureAwait(false),
                "incompatible" => await RunIncompatibleAsync(options, timeout.Token).ConfigureAwait(false),
                "replay" => await RunReplayAsync(options, timeout.Token).ConfigureAwait(false),
                _ => throw new InvalidOperationException($"Unsupported scenario '{options.Scenario}'.")
            };

            if (options.OutputPath is not null)
            {
                await AtomicJsonFile.WriteAsync(options.OutputPath, result, timeout.Token).ConfigureAwait(false);
            }
            Console.Out.WriteLine(ProtocolJson.SerializeText(result));
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static async Task<object> RunParityAsync(
        ClientCommandLineOptions options,
        CancellationToken cancellationToken)
    {
        await using ProtocolClientConnection connection = await ConnectAsync(
            options,
            ProtocolConstants.CurrentVersion,
            "dotnet-proof-client",
            cancellationToken).ConfigureAwait(false);
        HelloAckMessage handshake = connection.Handshake
            ?? throw new InvalidOperationException("Handshake evidence is missing.");

        JsonElement echo = await connection.RequestAsync(
            "proof.echo",
            new { text = "parity café 東京 🙂" },
            cancellationToken).ConfigureAwait(false);

        ProtocolPendingRequest delayed = connection.BeginRequest(
            "proof.delay",
            new { milliseconds = 10_000 });
        await Task.Delay(100, cancellationToken).ConfigureAwait(false);
        ResponseMessage cancel = await connection.CancelAsync(delayed.Id, cancellationToken).ConfigureAwait(false);
        bool cancelAcknowledged = cancel.Error is null
            && cancel.Result is JsonElement cancelResult
            && cancelResult.GetProperty("acknowledged").GetBoolean();
        ResponseMessage cancelled = await delayed.Response.WaitAsync(cancellationToken).ConfigureAwait(false);
        string cancellationCode = cancelled.Error?.Code
            ?? throw new InvalidDataException("Cancelled request did not return a typed error.");

        string topic = $"parity-dotnet-{Guid.NewGuid():N}";
        await using ProtocolSubscription subscription = await connection.SubscribeAsync(
            topic,
            0,
            cancellationToken).ConfigureAwait(false);
        JsonElement published = await connection.RequestAsync(
            "proof.publish",
            new { topic, count = 3, payload_bytes = 0, label = "parity" },
            cancellationToken).ConfigureAwait(false);

        List<long> sequences = [];
        List<string> labels = [];
        List<int> ordinals = [];
        for (int index = 0; index < 3; index++)
        {
            EventMessage message = await subscription.Events.ReadAsync(cancellationToken).ConfigureAwait(false);
            sequences.Add(message.Sequence);
            labels.Add(message.Payload.GetProperty("label").GetString()
                ?? throw new InvalidDataException("Event label was null."));
            ordinals.Add(message.Payload.GetProperty("ordinal").GetInt32());
        }

        JsonElement stats = await connection.RequestAsync("proof.stats", new { }, cancellationToken)
            .ConfigureAwait(false);
        bool queuesWithinBounds = QueuesWithinDeclaredBounds(stats, handshake.Limits);

        return new
        {
            schema_version = "1.0",
            scenario = "parity",
            client = "dotnet",
            protocol = handshake.Protocol,
            version = handshake.Version,
            echo_text = echo.GetProperty("text").GetString(),
            cancel_acknowledged = cancelAcknowledged,
            cancellation_code = cancellationCode,
            published_count = published.GetProperty("count").GetInt32(),
            labels,
            ordinals,
            event_sequences_strictly_increasing = IsStrictlyIncreasing(sequences),
            declared_control_queue_capacity = handshake.Limits.ControlQueueCapacity,
            declared_event_queue_capacity = handshake.Limits.EventQueueCapacity,
            declared_subscription_queue_capacity = handshake.Limits.SubscriptionQueueCapacity,
            server_queues_within_declared_bounds = queuesWithinBounds
        };
    }

    private static async Task<object> RunIncompatibleAsync(
        ClientCommandLineOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            await using ProtocolClientConnection _ = await ConnectAsync(
                options,
                "9.9",
                "dotnet-incompatible-client",
                cancellationToken).ConfigureAwait(false);
            throw new InvalidOperationException("Incompatible protocol handshake unexpectedly succeeded.");
        }
        catch (RemoteProtocolException exception)
        {
            return new
            {
                schema_version = "1.0",
                scenario = "incompatible",
                client = "dotnet",
                error_code = exception.Error.Code,
                supported_versions = exception.SupportedVersions
            };
        }
    }

    private static async Task<object> RunReplayAsync(
        ClientCommandLineOptions options,
        CancellationToken cancellationToken)
    {
        await using ProtocolClientConnection connection = await ConnectAsync(
            options,
            ProtocolConstants.CurrentVersion,
            "dotnet-replay-client",
            cancellationToken).ConfigureAwait(false);
        await using ProtocolSubscription subscription = await connection.SubscribeAsync(
            options.Topic,
            options.FromSequence,
            cancellationToken).ConfigureAwait(false);
        List<long> sequences = [];
        List<int> ordinals = [];
        for (int index = 0; index < options.EventCount; index++)
        {
            EventMessage message = await subscription.Events.ReadAsync(cancellationToken).ConfigureAwait(false);
            sequences.Add(message.Sequence);
            ordinals.Add(message.Payload.GetProperty("ordinal").GetInt32());
        }
        return new
        {
            schema_version = "1.0",
            scenario = "replay",
            client = "dotnet",
            requested_from_sequence = options.FromSequence,
            sequences,
            ordinals,
            latest_sequence_at_subscribe = subscription.Accepted.LatestSequence
        };
    }

    private static async Task<ProtocolClientConnection> ConnectAsync(
        ClientCommandLineOptions options,
        string requestedVersion,
        string clientKind,
        CancellationToken cancellationToken)
    {
        Stream stream = await NamedPipeConnector.ConnectAsync(
            options.PipeName,
            TimeSpan.FromMilliseconds(options.TimeoutMilliseconds),
            cancellationToken).ConfigureAwait(false);
        ProtocolClientConnection connection = new(
            stream,
            new ProtocolClientOptions
            {
                ClientKind = clientKind,
                ClientInstanceId = $"{clientKind}-{Guid.NewGuid():N}",
                RequestedVersion = requestedVersion,
                MaximumWriteChunkBytes = options.MaximumWriteChunkBytes
            });
        try
        {
            _ = await connection.HandshakeAsync(cancellationToken).ConfigureAwait(false);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    private static bool QueuesWithinDeclaredBounds(JsonElement stats, ProtocolLimits limits)
    {
        if (stats.GetProperty("maximum_observed_queue_depth").GetInt32()
                > limits.ControlQueueCapacity + limits.EventQueueCapacity
            || stats.GetProperty("maximum_observed_subscription_queue_depth").GetInt32()
                > limits.SubscriptionQueueCapacity)
        {
            return false;
        }

        foreach (JsonElement connection in stats.GetProperty("connections").EnumerateArray())
        {
            JsonElement queue = connection.GetProperty("queue");
            if (queue.GetProperty("peak_total_depth").GetInt32()
                > queue.GetProperty("control_capacity").GetInt32()
                    + queue.GetProperty("event_capacity").GetInt32())
            {
                return false;
            }
        }
        return true;
    }

    private static bool IsStrictlyIncreasing(IReadOnlyList<long> values)
    {
        for (int index = 1; index < values.Count; index++)
        {
            if (values[index] <= values[index - 1])
            {
                return false;
            }
        }
        return true;
    }
}
