namespace Square.Domain.Authority;

/// <summary>The parsed M1 task contract from a BUILD-TASKS.md task block.</summary>
public sealed record TaskContract(
    int Schema,
    string Id,
    string Role,
    string Mode,
    string StartingCommit,
    IReadOnlyList<string> AllowedPaths,
    IReadOnlyList<string> ForbiddenPaths,
    IReadOnlyList<string> Validation,
    string ExpectedCommitMessage,
    int ExternalCallLimit,
    double SpendLimitUsd,
    int TurnLimit,
    int TokenRotationLimit,
    string Client,
    string Model,
    bool AutomaticFallback,
    string EvidenceDestination,
    string AcceptanceAuthority);
