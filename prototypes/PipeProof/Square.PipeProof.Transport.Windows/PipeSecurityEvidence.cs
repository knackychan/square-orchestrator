namespace Square.PipeProof.Transport.Windows;

public sealed record PipeAceEvidence(
    string Sid,
    uint AccessMask,
    byte AceType,
    byte AceFlags,
    bool Inherited,
    bool GrantsFullControl);

public sealed record PipeAclEvidence(
    string CurrentUserSid,
    string SystemSid,
    string RequestedSddl,
    string ActualSddl,
    bool DaclPresent,
    bool DaclProtected,
    IReadOnlyList<string> AllowedSids,
    IReadOnlyList<PipeAceEvidence> Aces,
    bool GrantsOnlyCurrentUserAndSystem);

public sealed record RestrictedTokenProbeResult(
    bool Attempted,
    bool AccessDenied,
    int Win32Error,
    string ProbeIdentity,
    string PipePath,
    string Outcome);
