using System.Globalization;
using Square.PipeProof.Protocol;

namespace Square.PipeProof.Server;

internal sealed record ServerCommandLineOptions
{
    internal required string PipeName { get; init; }
    internal required string StateDirectory { get; init; }
    internal required string ReadyFile { get; init; }
    internal required string MetricsFile { get; init; }
    internal int MaximumPayloadBytes { get; init; } = ProtocolConstants.DefaultMaximumPayloadBytes;
    internal int ControlQueueCapacity { get; init; } = 16;
    internal int EventQueueCapacity { get; init; } = 16;
    internal int SubscriptionQueueCapacity { get; init; } = 32;
    internal int JournalRetentionCapacity { get; init; } = 256;
    internal int MaximumReplayEvents { get; init; } = 32;
    internal int MaximumInFlightRequests { get; init; } = 16;
    internal int MaximumConnections { get; init; } = 32;
    internal int WriteTimeoutMilliseconds { get; init; } = 750;
    internal int MaximumWriteChunkBytes { get; init; } = int.MaxValue;
    internal int MaximumPublishCount { get; init; } = 20_000;
    internal int MaximumPublishedPayloadBytes { get; init; } = 65_536;
    internal int HandshakeTimeoutMilliseconds { get; init; } = 5_000;
    internal int ShutdownDrainTimeoutMilliseconds { get; init; } = 3_000;

    internal static ServerCommandLineOptions Parse(string[] args)
    {
        Dictionary<string, string> values = ParsePairs(args);
        return new()
        {
            PipeName = Required(values, "--pipe-name"),
            StateDirectory = Path.GetFullPath(Required(values, "--state-dir")),
            ReadyFile = Path.GetFullPath(Required(values, "--ready-file")),
            MetricsFile = Path.GetFullPath(Required(values, "--metrics-file")),
            MaximumPayloadBytes = Integer(values, "--maximum-payload-bytes", ProtocolConstants.DefaultMaximumPayloadBytes, 1, 16 * 1_048_576),
            ControlQueueCapacity = Integer(values, "--control-queue-capacity", 16, 1, 65_536),
            EventQueueCapacity = Integer(values, "--event-queue-capacity", 16, 1, 65_536),
            SubscriptionQueueCapacity = Integer(values, "--subscription-queue-capacity", 32, 1, 65_536),
            JournalRetentionCapacity = Integer(values, "--journal-retention-capacity", 256, 1, 1_000_000),
            MaximumReplayEvents = Integer(values, "--maximum-replay-events", 32, 1, 65_536),
            MaximumInFlightRequests = Integer(values, "--maximum-inflight-requests", 16, 1, 1_024),
            MaximumConnections = Integer(values, "--maximum-connections", 32, 1, 254),
            WriteTimeoutMilliseconds = Integer(values, "--write-timeout-ms", 750, 1, 120_000),
            MaximumWriteChunkBytes = Integer(values, "--write-fragment-bytes", int.MaxValue, 1, int.MaxValue),
            MaximumPublishCount = Integer(values, "--maximum-publish-count", 20_000, 1, 1_000_000),
            MaximumPublishedPayloadBytes = Integer(values, "--maximum-published-payload-bytes", 65_536, 0, 1_048_576),
            HandshakeTimeoutMilliseconds = Integer(values, "--handshake-timeout-ms", 5_000, 1, 120_000),
            ShutdownDrainTimeoutMilliseconds = Integer(values, "--shutdown-drain-timeout-ms", 3_000, 1, 120_000)
        };
    }

    internal static string Usage => """
        Square.PipeProof.Server
          --pipe-name <local-name>
          --state-dir <directory>
          --ready-file <path>
          --metrics-file <path>
          [--maximum-payload-bytes <bytes>]
          [--control-queue-capacity <count>]
          [--event-queue-capacity <count>]
          [--subscription-queue-capacity <count>]
          [--journal-retention-capacity <count>]
          [--maximum-replay-events <count>]
          [--maximum-inflight-requests <count>]
          [--maximum-connections <1..254>]
          [--write-timeout-ms <milliseconds>]
          [--write-fragment-bytes <bytes>]
          [--maximum-publish-count <count>]
          [--maximum-published-payload-bytes <bytes>]
          [--handshake-timeout-ms <milliseconds>]
          [--shutdown-drain-timeout-ms <milliseconds>]
        """;

    private static Dictionary<string, string> ParsePairs(string[] args)
    {
        HashSet<string> known = new(StringComparer.Ordinal)
        {
            "--pipe-name",
            "--state-dir",
            "--ready-file",
            "--metrics-file",
            "--maximum-payload-bytes",
            "--control-queue-capacity",
            "--event-queue-capacity",
            "--subscription-queue-capacity",
            "--journal-retention-capacity",
            "--maximum-replay-events",
            "--maximum-inflight-requests",
            "--maximum-connections",
            "--write-timeout-ms",
            "--write-fragment-bytes",
            "--maximum-publish-count",
            "--maximum-published-payload-bytes",
            "--handshake-timeout-ms",
            "--shutdown-drain-timeout-ms"
        };
        Dictionary<string, string> result = new(StringComparer.Ordinal);
        for (int index = 0; index < args.Length; index++)
        {
            string option = args[index];
            if (!known.Contains(option))
            {
                throw new ArgumentException($"Unknown option '{option}'.\n{Usage}");
            }
            if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
            {
                throw new ArgumentException($"Option '{option}' requires a value.\n{Usage}");
            }
            if (!result.TryAdd(option, args[++index]))
            {
                throw new ArgumentException($"Option '{option}' was supplied more than once.");
            }
        }
        return result;
    }

    private static string Required(IReadOnlyDictionary<string, string> values, string option) =>
        values.TryGetValue(option, out string? value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new ArgumentException($"Required option '{option}' was not supplied.\n{Usage}");

    private static int Integer(
        IReadOnlyDictionary<string, string> values,
        string option,
        int defaultValue,
        int minimum,
        int maximum)
    {
        if (!values.TryGetValue(option, out string? text))
        {
            return defaultValue;
        }
        if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out int value)
            || value < minimum
            || value > maximum)
        {
            throw new ArgumentException($"Option '{option}' must be an integer in {minimum}..{maximum}.");
        }
        return value;
    }
}
