using System.Text.Json;
using Square.TerminalProof.Harness;
using Square.TerminalProof.Native;
using Square.TestKit;

return TestRunner.Run(
    ("Windows command line preserves a simple argument", SimpleArgument),
    ("Windows command line quotes whitespace and empty values", WhitespaceAndEmpty),
    ("Windows command line escapes embedded quotes and trailing slashes", QuotesAndSlashes),
    ("Terminal size rejects non-positive and oversized dimensions", TerminalSizeBounds),
    ("Manifest accepts the complete SP00-T02 contract", ManifestAcceptsCompleteContract),
    ("Manifest rejects a missing required metric", ManifestRejectsMissingMetric),
    ("Manifest JSON rejects unknown fields", ManifestJsonRejectsUnknownFields),
    ("Quick mode produces one reliability run and no scale runs", QuickModeOverrides),
    ("Proof options reject duplicate switches", OptionsRejectDuplicateSwitch),
    ("Proof options reject zero reliability repetitions", OptionsRejectZeroRepeat),
    ("Proof options reject unknown values", OptionsRejectUnknownValue),
    ("Quick mode rejects misleading repeat overrides", QuickModeRejectsRepeatOverride),
    ("Canonical scenario shape is distinguishable from a subset", CanonicalScenarioShape));

static void SimpleArgument()
{
    AssertEx.Equal("plain", WindowsCommandLine.Quote("plain"));
}

static void WhitespaceAndEmpty()
{
    AssertEx.Equal("\"two words\"", WindowsCommandLine.Quote("two words"));
    AssertEx.Equal("\"\"", WindowsCommandLine.Quote(string.Empty));
}

static void QuotesAndSlashes()
{
    AssertEx.Equal("\"a\\\\\\\"b\"", WindowsCommandLine.Quote("a\\\"b"));
    AssertEx.Equal("\"C:\\path with space\\\\\"", WindowsCommandLine.Quote("C:\\path with space\\"));
}

static void TerminalSizeBounds()
{
    AssertEx.Throws<ArgumentOutOfRangeException>(() => _ = new TerminalSize(0, 25));
    AssertEx.Throws<ArgumentOutOfRangeException>(() => _ = new TerminalSize(80, short.MaxValue + 1));
    AssertEx.Equal("132x43", new TerminalSize(132, 43).ToString());
}

static void ManifestAcceptsCompleteContract()
{
    CreateManifest().Validate();
}

static void ManifestRejectsMissingMetric()
{
    ProofManifest manifest = CreateManifest() with
    {
        RequiredMetrics = ["cpu", "working_set", "output_latency_ms", "bytes_written", "handle_count"]
    };
    AssertEx.Throws<InvalidDataException>(manifest.Validate);
}

static void ManifestJsonRejectsUnknownFields()
{
    string path = Path.Combine(Path.GetTempPath(), $"square-terminal-proof-{Guid.NewGuid():N}.json");
    try
    {
        File.WriteAllText(path, """
            {
              "schema_version": "1.0",
              "repeat_each": 100,
              "scale_repeat_each": 1,
              "session_counts": [1, 4, 8],
              "scenarios": ["normal_exit"],
              "required_metrics": ["cpu", "working_set", "output_latency_ms", "bytes_written", "handle_count", "leaked_descendants"],
              "settings": {
                "initial_columns": 100,
                "initial_rows": 30,
                "resize_columns": 132,
                "resize_rows": 43,
                "default_timeout_ms": 20000,
                "cleanup_timeout_ms": 10000,
                "quiet_duration_ms": 700,
                "quiet_observation_ms": 250,
                "large_burst_bytes": 1048576,
                "nested_child_count": 3,
                "sample_interval_ms": 20,
                "handle_growth_tolerance": 24,
                "descendant_exit_timeout_ms": 5000,
                "unknown": true
              }
            }
            """);
        AssertEx.Throws<JsonException>(() =>
            _ = ProofManifest.LoadAsync(path, CancellationToken.None).GetAwaiter().GetResult());
    }
    finally
    {
        File.Delete(path);
    }
}

static void QuickModeOverrides()
{
    ProofOptions options = ProofOptions.Parse(
    [
        "--manifest", "manifest.json",
        "--fixture", "fixture.exe",
        "--crash-owner", "owner.exe",
        "--quick"
    ]);
    ProofManifest applied = CreateManifest().Apply(options);
    AssertEx.Equal(1, applied.RepeatEach);
    AssertEx.Equal(0, applied.ScaleRepeatEach);
}

static void OptionsRejectDuplicateSwitch()
{
    AssertEx.Throws<ArgumentException>(() => ProofOptions.Parse(
    [
        "--manifest", "manifest.json",
        "--fixture", "fixture.exe",
        "--crash-owner", "owner.exe",
        "--quick",
        "--quick"
    ]));
}

static void OptionsRejectZeroRepeat()
{
    AssertEx.Throws<ArgumentException>(() => ProofOptions.Parse(
    [
        "--manifest", "manifest.json",
        "--fixture", "fixture.exe",
        "--crash-owner", "owner.exe",
        "--repeat", "0"
    ]));
}

static void OptionsRejectUnknownValue()
{
    AssertEx.Throws<ArgumentException>(() => ProofOptions.Parse(
    [
        "--manifest", "manifest.json",
        "--fixture", "fixture.exe",
        "--crash-owner", "owner.exe",
        "--unknown", "value"
    ]));
}

static void QuickModeRejectsRepeatOverride()
{
    AssertEx.Throws<ArgumentException>(() => ProofOptions.Parse(
    [
        "--manifest", "manifest.json",
        "--fixture", "fixture.exe",
        "--crash-owner", "owner.exe",
        "--quick",
        "--repeat", "1"
    ]));
}

static void CanonicalScenarioShape()
{
    ProofManifest complete = CreateManifest();
    AssertEx.True(complete.HasCanonicalScenarioSet, "The complete scenario list must be canonical.");
    ProofManifest subset = complete with { Scenarios = ["normal_exit"] };
    AssertEx.False(subset.HasCanonicalScenarioSet, "A subset must not be acceptance-equivalent to the canonical scenario set.");
}

static ProofManifest CreateManifest() => new()
{
    SchemaVersion = "1.0",
    RepeatEach = 100,
    ScaleRepeatEach = 1,
    SessionCounts = [1, 4, 8],
    Scenarios =
    [
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
    ],
    RequiredMetrics =
    [
        "cpu",
        "working_set",
        "output_latency_ms",
        "bytes_written",
        "handle_count",
        "leaked_descendants"
    ],
    Settings = new ProofSettings
    {
        InitialColumns = 100,
        InitialRows = 30,
        ResizeColumns = 132,
        ResizeRows = 43,
        DefaultTimeoutMilliseconds = 20_000,
        CleanupTimeoutMilliseconds = 10_000,
        QuietDurationMilliseconds = 700,
        QuietObservationMilliseconds = 250,
        LargeBurstBytes = 1_048_576,
        NestedChildCount = 3,
        SampleIntervalMilliseconds = 20,
        HandleGrowthTolerance = 24,
        DescendantExitTimeoutMilliseconds = 5_000
    }
};
