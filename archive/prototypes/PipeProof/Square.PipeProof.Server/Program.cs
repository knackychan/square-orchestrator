using Square.PipeProof.Protocol;
using Square.PipeProof.ServerCore;
using Square.PipeProof.Transport.Windows;

namespace Square.PipeProof.Server;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (!OperatingSystem.IsWindows())
        {
            Console.Error.WriteLine("SP00-T03 PipeProof server requires Windows.");
            return 2;
        }

        try
        {
            ServerCommandLineOptions commandLine = ServerCommandLineOptions.Parse(args);
            return await RunAsync(commandLine).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static async Task<int> RunAsync(ServerCommandLineOptions commandLine)
    {
        Directory.CreateDirectory(commandLine.StateDirectory);
        long epoch = await ServerEpochStore.NextAsync(commandLine.StateDirectory, CancellationToken.None)
            .ConfigureAwait(false);
        string instanceId = $"server-{Guid.NewGuid():N}";
        string journalPath = Path.Combine(commandLine.StateDirectory, "events.ndjson");
        ProofServerOptions options = new()
        {
            MaximumPayloadBytes = commandLine.MaximumPayloadBytes,
            ControlQueueCapacity = commandLine.ControlQueueCapacity,
            EventQueueCapacity = commandLine.EventQueueCapacity,
            SubscriptionQueueCapacity = commandLine.SubscriptionQueueCapacity,
            JournalRetentionCapacity = commandLine.JournalRetentionCapacity,
            MaximumReplayEvents = commandLine.MaximumReplayEvents,
            MaximumInFlightRequests = commandLine.MaximumInFlightRequests,
            MaximumConnections = commandLine.MaximumConnections,
            WriteTimeoutMilliseconds = commandLine.WriteTimeoutMilliseconds,
            MaximumWriteChunkBytes = commandLine.MaximumWriteChunkBytes,
            MaximumPublishCount = commandLine.MaximumPublishCount,
            MaximumPublishedPayloadBytes = commandLine.MaximumPublishedPayloadBytes,
            HandshakeTimeout = TimeSpan.FromMilliseconds(commandLine.HandshakeTimeoutMilliseconds),
            ShutdownDrainTimeout = TimeSpan.FromMilliseconds(commandLine.ShutdownDrainTimeoutMilliseconds)
        };
        options.Validate();

        ServerMetrics metrics = new(instanceId, epoch);
        DurableEventJournal journal = await DurableEventJournal.OpenAsync(
            journalPath,
            options.JournalRetentionCapacity).ConfigureAwait(false);
        WindowsNamedPipeListener? listener = null;
        EventHub? events = null;
        ProofProtocolServer? server = null;
        ConsoleCancelEventHandler? cancelHandler = null;
        try
        {
            listener = new WindowsNamedPipeListener(commandLine.PipeName, options.MaximumConnections);
            RestrictedTokenProbeResult negativeProbe = RestrictedTokenAccessProbe.Execute(listener.Endpoint);
            if (!negativeProbe.AccessDenied || negativeProbe.Win32Error != 5)
            {
                throw new InvalidDataException(
                    $"Anonymous named-pipe access did not fail with ERROR_ACCESS_DENIED: {negativeProbe.Outcome}.");
            }
            events = new EventHub(journal, options, metrics);
            server = new ProofProtocolServer(listener, events, options, metrics);
            server.Start();

            EventJournalBounds bounds = server.EventBounds;
            ServerReadyEvidence ready = new(
                "1.0",
                ProtocolConstants.ProtocolName,
                ProtocolConstants.SupportedVersions,
                DateTimeOffset.UtcNow,
                Environment.ProcessId,
                new ServerDescriptor("pipe-proof-server", "0.1.0", instanceId, epoch),
                commandLine.PipeName,
                listener.Endpoint,
                commandLine.StateDirectory,
                journalPath,
                options.ToLimits(),
                options.JournalRetentionCapacity,
                options.MaximumConnections,
                options.MaximumWriteChunkBytes,
                options.MaximumPublishCount,
                options.MaximumPublishedPayloadBytes,
                bounds.MinimumAvailableSequence,
                bounds.LatestSequence,
                listener.PipeAcl,
                negativeProbe);
            await AtomicJsonFile.WriteAsync(commandLine.ReadyFile, ready).ConfigureAwait(false);

            ProofProtocolServer runningServer = server;
            cancelHandler = (_, eventArgs) =>
            {
                eventArgs.Cancel = true;
                runningServer.RequestShutdown("console_cancel");
            };
            Console.CancelKeyPress += cancelHandler;
            string reason = await server.ShutdownRequested.ConfigureAwait(false);
            await server.StopAsync(reason).ConfigureAwait(false);
            await AtomicJsonFile.WriteAsync(
                commandLine.MetricsFile,
                new ServerFinalEvidence(
                    "1.0",
                    DateTimeOffset.UtcNow,
                    reason,
                    server.Snapshot())).ConfigureAwait(false);
            return 0;
        }
        finally
        {
            if (cancelHandler is not null)
            {
                Console.CancelKeyPress -= cancelHandler;
            }
            if (server is not null)
            {
                await server.DisposeAsync().ConfigureAwait(false);
            }
            else
            {
                if (events is not null)
                {
                    await events.DisposeAsync().ConfigureAwait(false);
                }
                else
                {
                    await journal.DisposeAsync().ConfigureAwait(false);
                }
                if (listener is not null)
                {
                    await listener.DisposeAsync().ConfigureAwait(false);
                }
            }
        }
    }
}
