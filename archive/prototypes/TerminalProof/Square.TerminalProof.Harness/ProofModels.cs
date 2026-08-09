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

    public TeardownEvidence? Teardown { get; init; }
}

internal static class CheckpointPhase
{
    public const string Active = "ACTIVE";
    public const string ImmediatePostDisposal = "IMMEDIATE_POST_DISPOSAL";
    public const string PostGc = "POST_GC";
    public const string Quiescent250Ms = "QUIESCENT_250MS";
    public const string Quiescent2S = "QUIESCENT_2S";
    public const string Quiescent10S = "QUIESCENT_10S";
    public const string Final = "FINAL";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(
        [Active, ImmediatePostDisposal, PostGc, Quiescent250Ms, Quiescent2S, Quiescent10S, Final],
        StringComparer.Ordinal);
}

internal sealed record HandleCheckpointEvidence(
    string Name,
    string Phase,
    int HandleCount,
    int GrowthFromBaseline,
    DateTimeOffset CapturedAtUtc,
    bool WithinTolerance,
    IReadOnlyList<Square.TerminalProof.Native.OwnedResourceCount> OwnedResourceCounts,
    int ProcessThreadCount = 0,
    int ThreadPoolThreadCount = 0,
    int ThreadPoolPendingWorkItemCount = -1,
    int ThreadPoolCompletedWorkItemCount = -1,
    int Gen0Collections = 0,
    int Gen1Collections = 0,
    int Gen2Collections = 0,
    long TotalManagedMemoryBytes = 0,
    double DelaySinceLastSessionMilliseconds = 0)
{
    public bool OwnedResourcesZero => OwnedResourceCounts.All(count => count.Count == 0);
}

internal enum TeardownMode
{
    NaturalExit,
    ScenarioAuthorizedHardStop,
    CleanupHardStop,
    OwnerProcessLoss,
    TeardownFailure
}

internal sealed record TeardownEvidence
{
    public required TeardownMode Mode { get; init; }
    public string? RootExitObserved { get; init; }
    public int ActiveProcessCountBeforeShutdown { get; init; }
    public bool HardStopCalled { get; init; }
    public string? HardStopReason { get; init; }
    public int ActiveProcessCountAfterHardStop { get; init; }
    public double? ClosePseudoConsoleDurationMilliseconds { get; init; }
    public double? OutputPumpCompletionDurationMilliseconds { get; init; }
    public IReadOnlyList<int> FinalProcessSurvivors { get; init; } = Array.Empty<int>();
}

internal enum OwnerCrashFailureStage
{
    ProcessStart,
    ConPtyStart,
    TreeReady,
    ProcessCount,
    ReadyFileWrite,
    ReadyFileObserve,
    OwnerTermination,
    DescendantTermination,
    IdentityValidation,
    OutputDrain,
    Unknown
}

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

    public OwnerCrashFailureStage FailureStage { get; init; } = OwnerCrashFailureStage.Unknown;

    public int? OwnerProcessId { get; init; }

    public int? OwnerExitCode { get; init; }

    public int? RootProcessId { get; init; }

    public required IReadOnlyList<int> JobProcessIds { get; init; }

    public required IReadOnlyList<int> SurvivingProcessIds { get; init; }

    public double? ProcessStartReturnMilliseconds { get; init; }

    public double? ReadyFileObservationMilliseconds { get; init; }

    public double? OwnerExitMilliseconds { get; init; }

    public double? DescendantEmptyingMilliseconds { get; init; }

    public double? TotalProbeDurationMilliseconds { get; init; }

    public string? OwnerStdout { get; init; }

    public string? OwnerStderr { get; init; }

    public string? ReadyFileSha256 { get; init; }

    public string? ReadyFilePath { get; init; }

    public string? ReadyFileSharingMode { get; init; }

    public bool ParentKillUsed { get; init; }

    public string? FirstException { get; init; }

    public string? LastException { get; init; }

    public IReadOnlyList<Square.TerminalProof.Native.OwnedResourceCount> OwnedResourceCounts { get; init; }
        = Array.Empty<Square.TerminalProof.Native.OwnedResourceCount>();

    public bool OwnedResourcesZero => OwnedResourceCounts.All(count => count.Count == 0);

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

    public required int ProcessId { get; init; }

    public required DateTimeOffset ProcessStartUtc { get; init; }

    public required long ProcessStartUtcTicks { get; init; }

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

    public int ProcessId { get; init; }

    public long ProcessStartUtcTicks { get; init; }

    public string BaselineCheckpointName { get; init; } = string.Empty;

    public string StableCheckpointPolicy { get; init; } = string.Empty;

    public IReadOnlyList<string> StableCheckpointNames { get; init; }
        = Array.Empty<string>();

    public int HandleGrowthTolerance { get; init; }

    public bool DiagnosticsProcessSeparated { get; init; }

    public IReadOnlyList<HandleGrowthClassificationEvidence> HandleGrowthClassifications { get; init; }
        = Array.Empty<HandleGrowthClassificationEvidence>();

    public IsolationRegressionEvidence? IsolationRegression { get; init; }

    public IReadOnlyList<DiagnosticProcessEvidence> DiagnosticProcesses { get; init; }
        = Array.Empty<DiagnosticProcessEvidence>();
}

internal sealed record HandleGrowthClassificationEvidence(
    string SeriesName,
    string RuleVersion,
    string Classification,
    string MatchedRule,
    IReadOnlyList<int> RawValues,
    IReadOnlyList<int> FirstDifferences,
    int LastNNetChange,
    int Minimum,
    int Maximum,
    int Range,
    double LinearRegressionSlope,
    int FinalWindowSize,
    int MeasurementNoiseBand);

internal sealed record IsolationProbeEvidenceEntry(
    string Mode,
    bool Passed,
    bool StdoutMarkerCaptured,
    bool StderrMarkerCaptured,
    bool AbsentFromParentStdout,
    bool AbsentFromParentStderr,
    int? ExitCode)
{
    public string? Failure { get; init; }
}

internal sealed record IsolationRegressionEvidence(
    string CapturedMarkerPrefix,
    IReadOnlyList<IsolationProbeEvidenceEntry> Probes,
    string RunId)
{
    public bool Passed => Probes.All(probe => probe.Passed);
}

internal sealed record DiagnosticProcessEvidence(
    string Name,
    int ProcessId,
    int ExitCode,
    string Status,
    string EvidenceDirectory)
{
    public bool Passed => ExitCode == 0;
}

internal sealed record DiagnosticsOrchestrationRecord(
    IReadOnlyList<DiagnosticProcessEvidence> DiagnosticProcesses);
