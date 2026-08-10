using System.Globalization;

namespace Square.PipeProof.DotNetClient;

internal sealed record ClientCommandLineOptions
{
    internal required string PipeName { get; init; }
    internal required string Scenario { get; init; }
    internal string? OutputPath { get; init; }
    internal int MaximumWriteChunkBytes { get; init; } = int.MaxValue;
    internal int TimeoutMilliseconds { get; init; } = 20_000;
    internal string Topic { get; init; } = "reconnect";
    internal long FromSequence { get; init; }
    internal int EventCount { get; init; } = 1;

    internal static ClientCommandLineOptions Parse(string[] args)
    {
        Dictionary<string, string> values = ParsePairs(args);
        string scenario = Required(values, "--scenario");
        if (scenario is not ("parity" or "incompatible" or "replay"))
        {
            throw new ArgumentException("Scenario must be parity, incompatible, or replay.");
        }

        string? outputPath = values.TryGetValue("--output", out string? output)
            && !string.IsNullOrWhiteSpace(output)
                ? Path.GetFullPath(output)
                : null;

        return new()
        {
            PipeName = Required(values, "--pipe-name"),
            Scenario = scenario,
            OutputPath = outputPath,
            MaximumWriteChunkBytes = Integer(values, "--write-fragment-bytes", int.MaxValue, 1, int.MaxValue),
            TimeoutMilliseconds = Integer(values, "--timeout-ms", 20_000, 1, 120_000),
            Topic = Optional(values, "--topic", "reconnect"),
            FromSequence = Long(values, "--from-sequence", 0, 0, long.MaxValue),
            EventCount = Integer(values, "--event-count", 1, 1, 65_536)
        };
    }

    private static Dictionary<string, string> ParsePairs(string[] args)
    {
        HashSet<string> known = new(StringComparer.Ordinal)
        {
            "--pipe-name",
            "--scenario",
            "--output",
            "--write-fragment-bytes",
            "--timeout-ms",
            "--topic",
            "--from-sequence",
            "--event-count"
        };
        Dictionary<string, string> result = new(StringComparer.Ordinal);
        for (int index = 0; index < args.Length; index++)
        {
            string option = args[index];
            if (!known.Contains(option))
            {
                throw new ArgumentException($"Unknown option '{option}'.");
            }
            if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
            {
                throw new ArgumentException($"Option '{option}' requires a value.");
            }
            if (!result.TryAdd(option, args[++index]))
            {
                throw new ArgumentException($"Option '{option}' was specified more than once.");
            }
        }
        return result;
    }

    private static string Required(IReadOnlyDictionary<string, string> values, string name) =>
        values.TryGetValue(name, out string? value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new ArgumentException($"Required option '{name}' was not supplied.");

    private static string Optional(
        IReadOnlyDictionary<string, string> values,
        string name,
        string fallback) => values.TryGetValue(name, out string? value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : fallback;

    private static int Integer(
        IReadOnlyDictionary<string, string> values,
        string name,
        int fallback,
        int minimum,
        int maximum)
    {
        long value = Long(values, name, fallback, minimum, maximum);
        return checked((int)value);
    }

    private static long Long(
        IReadOnlyDictionary<string, string> values,
        string name,
        long fallback,
        long minimum,
        long maximum)
    {
        if (!values.TryGetValue(name, out string? text))
        {
            return fallback;
        }
        if (!long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out long value)
            || value < minimum
            || value > maximum)
        {
            throw new ArgumentException($"Option '{name}' must be in {minimum}..{maximum}.");
        }
        return value;
    }
}
