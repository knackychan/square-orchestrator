using Square.PipeProof.Protocol;
using Square.PipeProof.ServerCore;
using Square.PipeProof.Transport.Windows;

namespace Square.PipeProof.Server;

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
