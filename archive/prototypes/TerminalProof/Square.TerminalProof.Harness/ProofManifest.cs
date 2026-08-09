using System.Text.Json;
using System.Text.Json.Serialization;

namespace Square.TerminalProof.Harness;

internal sealed record ProofManifest
{
    internal static IReadOnlyList<string> CanonicalScenarioOrder { get; } = Array.AsReadOnly(new[]
    {
        "unicode",
        "ansi",
        "large_burst",
        "quiet_child",
        "stdin_question",
        "resize",
        "normal_exit",
        "crash",
        "graceful_cancel",
        "forced_termination",
        "nested_children"
    });

    private static readonly HashSet<string> KnownScenarios = new(CanonicalScenarioOrder, StringComparer.Ordinal);

    internal static IReadOnlySet<string> AllKnownScenarios => KnownScenarios;

    internal bool HasCanonicalScenarioSet =>
        Scenarios.SequenceEqual(CanonicalScenarioOrder, StringComparer.Ordinal);

    [JsonPropertyName("schema_version")]
    public required string SchemaVersion { get; init; }

    [JsonPropertyName("repeat_each")]
    public required int RepeatEach { get; init; }

    [JsonPropertyName("scale_repeat_each")]
    public required int ScaleRepeatEach { get; init; }

    [JsonPropertyName("session_counts")]
    public required IReadOnlyList<int> SessionCounts { get; init; }

    [JsonPropertyName("scenarios")]
    public required IReadOnlyList<string> Scenarios { get; init; }

    [JsonPropertyName("required_metrics")]
    public required IReadOnlyList<string> RequiredMetrics { get; init; }

    [JsonPropertyName("settings")]
    public required ProofSettings Settings { get; init; }

    internal static async Task<ProofManifest> LoadAsync(string path, CancellationToken cancellationToken)
    {
        await using FileStream stream = File.OpenRead(path);
        ProofManifest? manifest = await JsonSerializer.DeserializeAsync<ProofManifest>(stream, ProofJson.Create(), cancellationToken).ConfigureAwait(false);
        return manifest ?? throw new InvalidDataException("The terminal proof manifest deserialized to null.");
    }

    internal ProofManifest Apply(ProofOptions options)
    {
        int repeat = options.Quick ? 1 : options.RepeatOverride ?? RepeatEach;
        int scaleRepeat = options.Quick ? 0 : options.ScaleRepeatOverride ?? ScaleRepeatEach;
        IReadOnlyList<string> scenarios = options.ScenarioFilter is null
            ? Scenarios
            : new[] { options.ScenarioFilter };
        return this with
        {
            RepeatEach = repeat,
            ScaleRepeatEach = scaleRepeat,
            Scenarios = scenarios
        };
    }

    internal void Validate()
    {
        if (!string.Equals(SchemaVersion, "1.0", StringComparison.Ordinal))
        {
            throw new InvalidDataException($"Unsupported terminal proof manifest schema '{SchemaVersion}'.");
        }

        if (RepeatEach < 1)
        {
            throw new InvalidDataException("repeat_each must be positive.");
        }

        if (ScaleRepeatEach < 0)
        {
            throw new InvalidDataException("scale_repeat_each cannot be negative.");
        }

        if (SessionCounts.Count == 0 || SessionCounts.Any(value => value < 1))
        {
            throw new InvalidDataException("session_counts must contain positive values.");
        }

        if (!SessionCounts.Contains(1) || !SessionCounts.Contains(4) || !SessionCounts.Contains(8))
        {
            throw new InvalidDataException("session_counts must include 1, 4, and 8 for SP00-T02.");
        }

        if (Scenarios.Count == 0 || Scenarios.Count != Scenarios.Distinct(StringComparer.Ordinal).Count())
        {
            throw new InvalidDataException("scenarios must be non-empty and unique.");
        }

        foreach (string scenario in Scenarios)
        {
            if (!KnownScenarios.Contains(scenario))
            {
                throw new InvalidDataException($"Unknown terminal proof scenario '{scenario}'.");
            }
        }

        string[] required =
        {
            "cpu",
            "working_set",
            "output_latency_ms",
            "bytes_written",
            "handle_count",
            "leaked_descendants"
        };
        foreach (string metric in required)
        {
            if (!RequiredMetrics.Contains(metric, StringComparer.Ordinal))
            {
                throw new InvalidDataException($"required_metrics is missing '{metric}'.");
            }
        }

        Settings.Validate();
    }
}

internal sealed record ProofSettings
{
    [JsonPropertyName("initial_columns")]
    public required int InitialColumns { get; init; }

    [JsonPropertyName("initial_rows")]
    public required int InitialRows { get; init; }

    [JsonPropertyName("resize_columns")]
    public required int ResizeColumns { get; init; }

    [JsonPropertyName("resize_rows")]
    public required int ResizeRows { get; init; }

    [JsonPropertyName("default_timeout_ms")]
    public required int DefaultTimeoutMilliseconds { get; init; }

    [JsonPropertyName("cleanup_timeout_ms")]
    public required int CleanupTimeoutMilliseconds { get; init; }

    [JsonPropertyName("quiet_duration_ms")]
    public required int QuietDurationMilliseconds { get; init; }

    [JsonPropertyName("quiet_observation_ms")]
    public required int QuietObservationMilliseconds { get; init; }

    [JsonPropertyName("large_burst_bytes")]
    public required int LargeBurstBytes { get; init; }

    [JsonPropertyName("nested_child_count")]
    public required int NestedChildCount { get; init; }

    [JsonPropertyName("sample_interval_ms")]
    public required int SampleIntervalMilliseconds { get; init; }

    [JsonPropertyName("handle_growth_tolerance")]
    public required int HandleGrowthTolerance { get; init; }

    [JsonPropertyName("descendant_exit_timeout_ms")]
    public required int DescendantExitTimeoutMilliseconds { get; init; }

    internal TimeSpan DefaultTimeout => TimeSpan.FromMilliseconds(DefaultTimeoutMilliseconds);

    internal TimeSpan CleanupTimeout => TimeSpan.FromMilliseconds(CleanupTimeoutMilliseconds);

    internal TimeSpan SampleInterval => TimeSpan.FromMilliseconds(SampleIntervalMilliseconds);

    internal TimeSpan DescendantExitTimeout => TimeSpan.FromMilliseconds(DescendantExitTimeoutMilliseconds);

    internal void Validate()
    {
        _ = new Square.TerminalProof.Native.TerminalSize(InitialColumns, InitialRows);
        _ = new Square.TerminalProof.Native.TerminalSize(ResizeColumns, ResizeRows);
        if (DefaultTimeoutMilliseconds < 1000
            || CleanupTimeoutMilliseconds < 1000
            || QuietDurationMilliseconds < 100
            || QuietObservationMilliseconds < 50
            || QuietObservationMilliseconds >= QuietDurationMilliseconds
            || LargeBurstBytes < 1024
            || NestedChildCount < 1
            || SampleIntervalMilliseconds is < 5 or > 1000
            || HandleGrowthTolerance < 0
            || DescendantExitTimeoutMilliseconds < 1000)
        {
            throw new InvalidDataException("One or more terminal proof settings are outside their safety bounds.");
        }
    }
}
