namespace Square.TerminalProof.Harness;

internal sealed record ProcessSampleSnapshot(
    long PeakWorkingSetBytes,
    int PeakActiveProcessCount,
    int PeakCombinedProcessHandleCount,
    IReadOnlyList<ObservedProcessIdentity> ObservedProcesses);

internal sealed record ObservedProcessIdentity(int ProcessId, long? StartTimeUtcTicks);

internal sealed record SessionRunEvidence
{
    public required string SchemaVersion { get; init; }

    public required string ProofRunId { get; init; }

    public required string RunId { get; init; }

    public required string Phase { get; init; }

    public required string Scenario { get; init; }

    public required int Iteration { get; init; }

    public required int Concurrency { get; init; }

    public required int SessionOrdinal { get; init; }

    public required DateTimeOffset StartedAtUtc { get; init; }

    public required double DurationMilliseconds { get; init; }

    public required bool Passed { get; init; }

    public string? Failure { get; init; }

    public int? RootProcessId { get; init; }

    public int? ExitCode { get; init; }

    public required long OutputBytes { get; init; }

    public string? OutputSha256 { get; init; }

    public string? OutputExcerpt { get; init; }

    public double? FirstOutputLatencyMilliseconds { get; init; }

    public required double JobCpuMilliseconds { get; init; }

    public required ulong JobReadTransferBytes { get; init; }

    public required ulong JobWriteTransferBytes { get; init; }

    public required long PeakWorkingSetBytes { get; init; }

    public required int PeakActiveProcessCount { get; init; }

    public required int PeakCombinedProcessHandleCount { get; init; }

    public required IReadOnlyList<int> ObservedProcessIds { get; init; }

    public required IReadOnlyList<int> LeakedProcessIds { get; init; }

    public required int HarnessHandleCountBefore { get; init; }

    public required int HarnessHandleCountAfter { get; init; }
}

internal sealed record HandleCheckpointEvidence(
    string Name,
    int HandleCount,
    int GrowthFromBaseline,
    DateTimeOffset CapturedAtUtc,
    bool WithinTolerance);

internal sealed record ScaleGroupEvidence(
    string Scenario,
    int Concurrency,
    int Iteration,
    int Sessions,
    int Passed,
    int Failed,
    double WallClockMilliseconds,
    long OutputBytes,
    ulong JobReadTransferBytes,
    ulong JobWriteTransferBytes,
    double TotalCpuMilliseconds,
    long SumPeakWorkingSetBytes,
    long MaximumPeakWorkingSetBytes,
    int MaximumPeakProcessCount,
    int MaximumPeakProcessHandleCount,
    double? MedianFirstOutputLatencyMilliseconds,
    double? P95FirstOutputLatencyMilliseconds,
    int HarnessHandleCountBefore,
    int HarnessHandleCountAfter,
    int HarnessHandleGrowth);

internal sealed record OwnerCrashProbeEvidence
{
    public required bool Executed { get; init; }

    public required bool Passed { get; init; }

    public string? Failure { get; init; }

    public int? OwnerProcessId { get; init; }

    public int? OwnerExitCode { get; init; }

    public int? RootProcessId { get; init; }

    public required IReadOnlyList<int> JobProcessIds { get; init; }

    public required IReadOnlyList<int> SurvivingProcessIds { get; init; }

    public required bool LivePtyReattachSupported { get; init; }

    public required string ReattachConclusion { get; init; }
}

internal sealed record ProofEnvironmentEvidence
{
    public required string SchemaVersion { get; init; }

    public required DateTimeOffset CapturedAtUtc { get; init; }

    public required string OsDescription { get; init; }

    public required string OsVersion { get; init; }

    public required int WindowsBuild { get; init; }

    public required string OsArchitecture { get; init; }

    public required string ProcessArchitecture { get; init; }

    public required string FrameworkDescription { get; init; }

    public required int ProcessorCount { get; init; }

    public required bool Is64BitOperatingSystem { get; init; }

    public required bool Is64BitProcess { get; init; }

    public required bool IsElevated { get; init; }

    public required bool CurrentProcessIsInJob { get; init; }

    public required int InitialHarnessHandleCount { get; init; }

    public required string ManifestSha256 { get; init; }

    public required string DispatchPacketSha256 { get; init; }

    public required string FixtureSha256 { get; init; }

    public required string CrashOwnerSha256 { get; init; }

    public required string HarnessSha256 { get; init; }
}

internal sealed record ScenarioSummaryEvidence(
    string Scenario,
    int RequiredRepetitions,
    int Executed,
    int Passed,
    int Failed,
    long TotalOutputBytes,
    ulong TotalJobWriteTransferBytes,
    double AverageDurationMilliseconds,
    double? P95FirstOutputLatencyMilliseconds,
    long MaximumPeakWorkingSetBytes,
    int MaximumPeakProcessCount,
    int MaximumPeakProcessHandleCount,
    int LeakedProcessCount);

internal sealed record ProofSummaryEvidence
{
    public required string SchemaVersion { get; init; }

    public required string TaskId { get; init; }

    public required string RunId { get; init; }

    public required DateTimeOffset StartedAtUtc { get; init; }

    public required DateTimeOffset CompletedAtUtc { get; init; }

    public required string Status { get; init; }

    public required bool AcceptanceEligible { get; init; }

    public required string Mode { get; init; }

    public required int EffectiveRepeatEach { get; init; }

    public required int EffectiveScaleRepeatEach { get; init; }

    public required IReadOnlyList<int> SessionCounts { get; init; }

    public required IReadOnlyList<string> Scenarios { get; init; }

    public required int WarmupRuns { get; init; }

    public required int ReliabilityRuns { get; init; }

    public required int ScaleSessionRuns { get; init; }

    public required int FailedRuns { get; init; }

    public required IReadOnlyList<ScenarioSummaryEvidence> ScenarioSummaries { get; init; }

    public required IReadOnlyList<ScaleGroupEvidence> ScaleGroups { get; init; }

    public required IReadOnlyList<HandleCheckpointEvidence> HandleCheckpoints { get; init; }

    public required OwnerCrashProbeEvidence OwnerCrashProbe { get; init; }

    public required IReadOnlyList<string> GlobalFailures { get; init; }

    public required IReadOnlyList<string> Limitations { get; init; }

    public required string EvidenceDirectory { get; init; }
}
