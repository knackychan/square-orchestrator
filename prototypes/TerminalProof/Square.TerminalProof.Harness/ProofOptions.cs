using System.Globalization;

namespace Square.TerminalProof.Harness;

internal sealed record ProofOptions(
    string ManifestPath,
    string FixturePath,
    string CrashOwnerPath,
    string WorkingDirectory,
    string EvidenceDirectory,
    int? RepeatOverride,
    int? ScaleRepeatOverride,
    bool Quick,
    string? ScenarioFilter,
    bool AllowElevated,
    bool SkipOwnerCrash,
    bool FailFast)
{
    private static readonly HashSet<string> ValueOptions = new(StringComparer.Ordinal)
    {
        "--manifest",
        "--fixture",
        "--crash-owner",
        "--working-directory",
        "--evidence-dir",
        "--repeat",
        "--scale-repeat",
        "--scenario"
    };

    internal static ProofOptions Parse(string[] args)
    {
        Dictionary<string, string> values = new(StringComparer.Ordinal);
        HashSet<string> switches = new(StringComparer.Ordinal);
        for (int index = 0; index < args.Length; index++)
        {
            string current = args[index];
            if (current is "--quick" or "--allow-elevated" or "--skip-owner-crash" or "--fail-fast")
            {
                if (!switches.Add(current))
                {
                    throw new ArgumentException($"Option '{current}' was specified more than once.");
                }

                continue;
            }

            if (!current.StartsWith("--", StringComparison.Ordinal))
            {
                throw new ArgumentException($"Unexpected positional argument '{current}'.");
            }

            if (!ValueOptions.Contains(current))
            {
                throw new ArgumentException($"Unknown option '{current}'.\n{Usage}");
            }

            if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
            {
                throw new ArgumentException($"Option '{current}' requires a value.");
            }

            if (!values.TryAdd(current, args[++index]))
            {
                throw new ArgumentException($"Option '{current}' was specified more than once.");
            }
        }

        if (switches.Contains("--quick")
            && (values.ContainsKey("--repeat") || values.ContainsKey("--scale-repeat")))
        {
            throw new ArgumentException("--quick cannot be combined with --repeat or --scale-repeat.");
        }

        if (values.TryGetValue("--scenario", out string? requestedScenario)
            && string.IsNullOrWhiteSpace(requestedScenario))
        {
            throw new ArgumentException("--scenario cannot be empty.");
        }

        string manifest = Required(values, "--manifest");
        string fixture = Required(values, "--fixture");
        string crashOwner = Required(values, "--crash-owner");
        string workingDirectory = values.TryGetValue("--working-directory", out string? working)
            ? working
            : Path.GetDirectoryName(Path.GetFullPath(manifest))
                ?? throw new InvalidOperationException("The manifest path has no parent directory.");
        string evidence = values.TryGetValue("--evidence-dir", out string? evidenceValue)
            ? evidenceValue
            : Path.Combine(workingDirectory, "artifacts", "proofs", "SP00-T02", DateTimeOffset.UtcNow.ToString("yyyyMMddTHHmmssZ", CultureInfo.InvariantCulture));

        return new ProofOptions(
            Path.GetFullPath(manifest),
            Path.GetFullPath(fixture),
            Path.GetFullPath(crashOwner),
            Path.GetFullPath(workingDirectory),
            Path.GetFullPath(evidence),
            OptionalPositiveInt(values, "--repeat", maximum: 10_000),
            OptionalNonNegativeInt(values, "--scale-repeat", maximum: 1_000),
            switches.Contains("--quick"),
            values.GetValueOrDefault("--scenario"),
            switches.Contains("--allow-elevated"),
            switches.Contains("--skip-owner-crash"),
            switches.Contains("--fail-fast"));
    }

    internal static string Usage => """
        Square.TerminalProof.Harness
          --manifest <scenarios.json>
          --fixture <Square.TerminalProof.Fixture.exe>
          --crash-owner <Square.TerminalProof.CrashOwner.exe>
          [--working-directory <path>]
          [--evidence-dir <path>]
          [--repeat <positive integer>]
          [--scale-repeat <non-negative integer>]
          [--scenario <id>]
          [--quick]
          [--allow-elevated]
          [--skip-owner-crash]
          [--fail-fast]
        """;

    private static string Required(IReadOnlyDictionary<string, string> values, string name) =>
        values.TryGetValue(name, out string? value)
            ? value
            : throw new ArgumentException($"Required option '{name}' was not supplied.\n{Usage}");

    private static int? OptionalPositiveInt(IReadOnlyDictionary<string, string> values, string name, int maximum)
    {
        int? value = OptionalNonNegativeInt(values, name, maximum);
        if (value == 0)
        {
            throw new ArgumentException($"Option '{name}' must be greater than zero.");
        }

        return value;
    }

    private static int? OptionalNonNegativeInt(IReadOnlyDictionary<string, string> values, string name, int maximum)
    {
        if (!values.TryGetValue(name, out string? text))
        {
            return null;
        }

        if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out int value)
            || value < 0
            || value > maximum)
        {
            throw new ArgumentException(
                $"Option '{name}' must be a non-negative integer no greater than {maximum}.");
        }

        return value;
    }
}
