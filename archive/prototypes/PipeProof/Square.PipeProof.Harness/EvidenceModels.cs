using Square.PipeProof.Protocol;
using Square.PipeProof.ServerCore;
using Square.PipeProof.Transport.Windows;

namespace Square.PipeProof.Harness;

internal sealed record HarnessEnvironmentEvidence(
    string SchemaVersion,
    DateTimeOffset CapturedAtUtc,
    string OsDescription,
    string OsArchitecture,
    string ProcessArchitecture,
    string FrameworkDescription,
    string DotNetSdkVersion,
    string NodeVersion,
    string UserSid,
    bool Elevated,
    int ProcessId,
    string MachineName,
    string SourceRoot,
    string DispatchSha256,
    string ScenarioManifestSha256,
    bool QuickMode);

internal sealed record ServerReadyEvidence(
    string SchemaVersion,
    string Protocol,
    IReadOnlyList<string> SupportedVersions,
    DateTimeOffset ReadyAtUtc,
    int ProcessId,
    ServerDescriptor Server,
    string PipeName,
    string PipePath,
    string StateDirectory,
    string EventJournalPath,
    ProtocolLimits Limits,
    int JournalRetentionCapacity,
    int MaximumConnections,
    int MaximumWriteChunkBytes,
    int MaximumPublishCount,
    int MaximumPublishedPayloadBytes,
    long MinimumAvailableSequence,
    long LatestSequence,
    PipeAclEvidence Acl,
    RestrictedTokenProbeResult NegativeAccessProbe);

internal sealed record ServerFinalEvidence(
    string SchemaVersion,
    DateTimeOffset StoppedAtUtc,
    string ShutdownReason,
    ServerMetricsSnapshot Metrics);

internal sealed record ScenarioEvidence(
    string SchemaVersion,
    string ScenarioId,
    string Title,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    long DurationMilliseconds,
    bool Passed,
    object? Details,
    string? ErrorType,
    string? ErrorMessage);

internal sealed record HarnessSummary(
    string SchemaVersion,
    string TaskId,
    string Status,
    bool AcceptanceEligible,
    IReadOnlyList<string> IneligibilityReasons,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    int ScenarioCount,
    int PassedScenarioCount,
    int FailedScenarioCount,
    string DispatchSha256,
    string ScenarioManifestSha256,
    string EvidenceManifestFile,
    IReadOnlyList<string> ScenarioIds,
    string Conclusion);

internal sealed record ClientParityResult(
    string SchemaVersion,
    string Scenario,
    string Client,
    string Protocol,
    string Version,
    string EchoText,
    bool CancelAcknowledged,
    string CancellationCode,
    int PublishedCount,
    IReadOnlyList<string> Labels,
    IReadOnlyList<int> Ordinals,
    bool EventSequencesStrictlyIncreasing,
    int DeclaredControlQueueCapacity,
    int DeclaredEventQueueCapacity,
    int DeclaredSubscriptionQueueCapacity,
    bool ServerQueuesWithinDeclaredBounds);

internal sealed record ClientIncompatibleResult(
    string SchemaVersion,
    string Scenario,
    string Client,
    string ErrorCode,
    IReadOnlyList<string> SupportedVersions);

internal sealed record ClientReplayResult(
    string SchemaVersion,
    string Scenario,
    string Client,
    long RequestedFromSequence,
    IReadOnlyList<long> Sequences,
    IReadOnlyList<int> Ordinals,
    long LatestSequenceAtSubscribe);

internal sealed record NodeReconnectResult(
    string SchemaVersion,
    string Scenario,
    string Client,
    IReadOnlyList<long> Sequences,
    IReadOnlyList<int> Ordinals,
    int SuccessfulConnections,
    long LastSequence);

internal sealed record NodeReconnectProgress(
    string SchemaVersion,
    int Count,
    IReadOnlyList<long> Sequences,
    IReadOnlyList<int> Ordinals,
    int SuccessfulConnections);

internal sealed record ScenarioDefinition(
    string Id,
    string Title,
    bool RequiredInQuickMode);

internal sealed record ScenarioManifestDocument(
    string SchemaVersion,
    string TaskId,
    IReadOnlyList<ScenarioDefinition> Scenarios);
