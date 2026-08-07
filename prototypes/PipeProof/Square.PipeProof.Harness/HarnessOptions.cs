using System.Globalization;

namespace Square.PipeProof.Harness;

internal sealed record HarnessOptions
{
    internal required string ServerArtifact { get; init; }
    internal required string DotNetClientArtifact { get; init; }
    internal required string NodeExecutable { get; init; }
    internal required string NodeFixture { get; init; }
    internal required string OutputDirectory { get; init; }
    internal required string SourceRoot { get; init; }
    internal required string DispatchPacket { get; init; }
    internal required string ScenarioManifest { get; init; }
    internal bool Quick { get; init; }
    internal TimeSpan ScenarioTimeout { get; init; } = TimeSpan.FromSeconds(45);

    internal static HarnessOptions Parse(string[] args)
    {
        Dictionary<string, string> values = new(StringComparer.Ordinal);
        bool quick = false;
        for (int index = 0; index < args.Length; index++)
        {
            string option = args[index];
            if (string.Equals(option, "--quick", StringComparison.Ordinal))
            {
                quick = true;
                continue;
            }
            if (!option.StartsWith("--", StringComparison.Ordinal)
                || index + 1 >= args.Length
                || args[index + 1].StartsWith("--", StringComparison.Ordinal))
            {
                throw new ArgumentException($"Invalid option sequence near '{option}'.\n{Usage}");
            }
            if (!values.TryAdd(option, args[++index]))
            {
                throw new ArgumentException($"Option '{option}' was supplied more than once.");
            }
        }

        return new HarnessOptions
        {
            ServerArtifact = FullPath(Required(values, "--server")),
            DotNetClientArtifact = FullPath(Required(values, "--dotnet-client")),
            NodeExecutable = Required(values, "--node"),
            NodeFixture = FullPath(Required(values, "--node-fixture")),
            OutputDirectory = FullPath(Required(values, "--output")),
            SourceRoot = FullPath(Required(values, "--source-root")),
            DispatchPacket = FullPath(Required(values, "--dispatch")),
            ScenarioManifest = FullPath(Required(values, "--manifest")),
            Quick = quick,
            ScenarioTimeout = TimeSpan.FromMilliseconds(Integer(values, "--scenario-timeout-ms", 45_000, 1_000, 300_000))
        };
    }

    internal static string Usage => """
        Square.PipeProof.Harness
          --server <Square.PipeProof.Server.dll|exe>
          --dotnet-client <Square.PipeProof.DotNetClient.dll|exe>
          --node <node executable>
          --node-fixture <node-client/fixture.mjs>
          --output <evidence directory>
          --source-root <repository root>
          --dispatch <dispatch.packet.json>
          --manifest <scenario-manifest.json>
          [--scenario-timeout-ms <milliseconds>]
          [--quick]
        """;

    private static string Required(IReadOnlyDictionary<string, string> values, string option) =>
        values.TryGetValue(option, out string? value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new ArgumentException($"Required option '{option}' was not supplied.\n{Usage}");

    private static string FullPath(string path) => Path.GetFullPath(path);

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
            throw new ArgumentException($"Option '{option}' must be in {minimum}..{maximum}.");
        }
        return value;
    }
}
