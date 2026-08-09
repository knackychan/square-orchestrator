using System.Text.RegularExpressions;

namespace Square.Domain.Authority;

/// <summary>
/// Schema validation for the M1 task contract. Enforces the exact field set, types, and
/// fail-closed ceilings that the Python M1 validator established.
/// </summary>
public static partial class TaskContractValidator
{
    private static readonly HashSet<string> RequiredFields = new(StringComparer.Ordinal)
    {
        "schema", "id", "role", "mode", "starting_commit", "allowed_paths", "forbidden_paths",
        "validation", "expected_commit_message", "external_call_limit", "spend_limit_usd",
        "turn_limit", "token_rotation_limit", "client", "model", "automatic_fallback",
        "evidence_destination", "acceptance_authority"
    };

    private static readonly string[] NonEmptyStringFields =
    {
        "role", "expected_commit_message", "client", "model", "evidence_destination", "acceptance_authority"
    };

    /// <summary>Validates the task block against the exact M1 schema.</summary>
    /// <exception cref="AuthorityValidationException">Thrown with VALIDATION_FAILED or AUTHORITY_DRIFT.</exception>
    public static TaskContract Validate(IReadOnlyDictionary<string, object?> block, string taskId)
    {
        HashSet<string> keys = new(block.Keys, StringComparer.Ordinal);
        if (!keys.SetEquals(RequiredFields))
            throw new AuthorityValidationException("VALIDATION_FAILED", "Task block fields are incomplete or unknown");

        object? schemaValue = block["schema"];
        if (schemaValue is not long schemaLong || schemaLong != 1)
            throw new AuthorityValidationException("VALIDATION_FAILED", "Task schema must be 1");

        string? id = block["id"] as string;
        if (id != taskId)
            throw new AuthorityValidationException("AUTHORITY_DRIFT", "Requested task does not match task block");

        foreach (string field in NonEmptyStringFields)
        {
            string? value = block[field] as string;
            if (string.IsNullOrEmpty(value))
                throw new AuthorityValidationException("VALIDATION_FAILED", $"Task block has invalid {field}");
        }

        string? evidenceDestination = block["evidence_destination"] as string;
        PathValidation.ValidateRelativePosixPath(evidenceDestination);

        string? mode = block["mode"] as string;
        if (mode is not ("read" or "write"))
            throw new AuthorityValidationException("VALIDATION_FAILED", "Task mode must be read or write");

        string? startingCommit = block["starting_commit"] as string;
        if (startingCommit is null || !FullShaRegex().IsMatch(startingCommit))
            throw new AuthorityValidationException("VALIDATION_FAILED", "Task starting_commit must be a 40-character SHA");

        foreach (string field in new[] { "allowed_paths", "forbidden_paths", "validation" })
        {
            object? value = block[field];
            if (value is not IReadOnlyList<object?> list || list.Count == 0 || list.Any(item => item is not string s || s.Length == 0))
                throw new AuthorityValidationException("VALIDATION_FAILED", $"Task block has invalid {field}");
        }

        if (block["automatic_fallback"] is not bool automaticFallback)
            throw new AuthorityValidationException("VALIDATION_FAILED", "automatic_fallback must be boolean");

        foreach (string field in new[] { "external_call_limit", "turn_limit", "token_rotation_limit" })
        {
            long minimum = field == "external_call_limit" ? 0 : 1;
            object? value = block[field];
            if (value is not long number || number < minimum)
                throw new AuthorityValidationException("VALIDATION_FAILED", $"Task block has invalid {field}");
        }

        object? spendValue = block["spend_limit_usd"];
        if (spendValue is not long and not double || (spendValue is long spendLong && spendLong < 0) || (spendValue is double spendDouble && spendDouble < 0))
            throw new AuthorityValidationException("VALIDATION_FAILED", "Task block has invalid spend_limit_usd");

        IReadOnlyList<string> allowed = ToStringList(block["allowed_paths"]);
        IReadOnlyList<string> forbidden = ToStringList(block["forbidden_paths"]);
        PathValidation.ValidatePathClaims(allowed, forbidden);
        RouteValidation.ValidateRoute(block["client"] as string, block["model"] as string, automaticFallback);

        return new TaskContract(
            Schema: 1,
            Id: id,
            Role: (string)block["role"]!,
            Mode: mode,
            StartingCommit: startingCommit,
            AllowedPaths: allowed,
            ForbiddenPaths: forbidden,
            Validation: ToStringList(block["validation"]),
            ExpectedCommitMessage: (string)block["expected_commit_message"]!,
            ExternalCallLimit: checked((int)(long)block["external_call_limit"]!),
            SpendLimitUsd: spendValue is double d ? d : (long)spendValue!,
            TurnLimit: checked((int)(long)block["turn_limit"]!),
            TokenRotationLimit: checked((int)(long)block["token_rotation_limit"]!),
            Client: (string)block["client"]!,
            Model: (string)block["model"]!,
            AutomaticFallback: automaticFallback,
            EvidenceDestination: evidenceDestination!,
            AcceptanceAuthority: (string)block["acceptance_authority"]!);
    }

    private static IReadOnlyList<string> ToStringList(object? value) =>
        value is IReadOnlyList<object?> list
            ? list.Cast<string>().ToArray()
            : Array.Empty<string>();

    [GeneratedRegex("^[0-9a-f]{40}$")]
    private static partial Regex FullShaRegex();
}
