namespace Square.Domain.Practices;

/// <summary>A validated M1 practice record.</summary>
public sealed record PracticeRecord(
    string Schema,
    string Id,
    string Category,
    string Statement,
    string ProposedScope,
    string SourceType,
    string ProvenanceReference,
    string ObservedContext,
    IReadOnlyList<string> TradeOffs,
    IReadOnlyList<string> Counterexamples,
    double Confidence,
    string ReviewDate,
    string State,
    string? ApprovingAuthority,
    IReadOnlyList<string> AffectedProfiles);
