namespace Square.Domain.Practices;

/// <summary>Fail-closed validation of a practice record against the closed M1 lifecycle vocabulary.</summary>
public static class PracticeRecordValidator
{
    private static readonly HashSet<string> ValidStates = new(
        new[] { "OBSERVED", "CANDIDATE", "TRIAL", "ADOPTED", "REJECTED", "DEPRECATED" },
        StringComparer.Ordinal);

    private static readonly HashSet<string> DecisionStates = new(
        new[] { "ADOPTED", "REJECTED", "DEPRECATED" },
        StringComparer.Ordinal);

    private static readonly HashSet<string> RequiredFields = new(StringComparer.Ordinal)
    {
        "schema", "id", "category", "statement", "proposed_scope", "source_type",
        "provenance_reference", "observed_context", "trade_offs", "counterexamples",
        "confidence", "review_date", "state", "approving_authority", "affected_profiles"
    };

    private static readonly string[] StringFields =
    {
        "schema", "id", "category", "statement", "proposed_scope", "source_type",
        "provenance_reference", "observed_context", "review_date"
    };

    private static readonly string[] ListFields = { "trade_offs", "counterexamples", "affected_profiles" };

    /// <exception cref="PracticeValidationException">Thrown with INVALID_INPUT when the record violates the contract.</exception>
    public static PracticeRecord Validate(IReadOnlyDictionary<string, object?> record)
    {
        HashSet<string> keys = new(record.Keys, StringComparer.Ordinal);
        if (!keys.SetEquals(RequiredFields))
            throw new PracticeValidationException("INVALID_INPUT", "Practice record has incomplete or unknown fields");

        foreach (string field in StringFields)
        {
            if (record[field] is not string value || value.Length == 0)
                throw new PracticeValidationException("INVALID_INPUT", $"Practice record has invalid {field}");
        }

        foreach (string field in ListFields)
        {
            if (record[field] is not IReadOnlyList<object?> list || list.Any(item => item is not string))
                throw new PracticeValidationException("INVALID_INPUT", $"Practice record has invalid {field}");
        }

        object? confidenceValue = record["confidence"];
        if (confidenceValue is not long and not double)
            throw new PracticeValidationException("INVALID_INPUT", "Practice record has invalid confidence");
        double confidence = confidenceValue is double d ? d : (long)confidenceValue;
        if (confidence is < 0 or > 1)
            throw new PracticeValidationException("INVALID_INPUT", "Practice confidence must be between 0 and 1");

        string? state = record["state"] as string;
        if (state is null || !ValidStates.Contains(state))
            throw new PracticeValidationException("INVALID_INPUT", $"Practice state must be one of {string.Join(", ", ValidStates.Order())}");

        string? approvingAuthority = record["approving_authority"] as string;
        if (DecisionStates.Contains(state))
        {
            if (string.IsNullOrEmpty(approvingAuthority))
                throw new PracticeValidationException("INVALID_INPUT", $"{state} practice must have an approving_authority");
        }
        else if (record["approving_authority"] is not null &&
                 (approvingAuthority is null || approvingAuthority.Length == 0))
        {
            throw new PracticeValidationException("INVALID_INPUT", "Practice record has invalid approving_authority");
        }

        return new PracticeRecord(
            Schema: (string)record["schema"]!,
            Id: (string)record["id"]!,
            Category: (string)record["category"]!,
            Statement: (string)record["statement"]!,
            ProposedScope: (string)record["proposed_scope"]!,
            SourceType: (string)record["source_type"]!,
            ProvenanceReference: (string)record["provenance_reference"]!,
            ObservedContext: (string)record["observed_context"]!,
            TradeOffs: ToStringList(record["trade_offs"]),
            Counterexamples: ToStringList(record["counterexamples"]),
            Confidence: confidence,
            ReviewDate: (string)record["review_date"]!,
            State: state,
            ApprovingAuthority: approvingAuthority,
            AffectedProfiles: ToStringList(record["affected_profiles"]));
    }

    private static IReadOnlyList<string> ToStringList(object? value) =>
        value is IReadOnlyList<object?> list ? list.Cast<string>().ToArray() : Array.Empty<string>();
}
