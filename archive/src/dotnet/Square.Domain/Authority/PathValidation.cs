namespace Square.Domain.Authority;

/// <summary>Fail-closed validation of task-path claims and relative POSIX path syntax.</summary>
public static class PathValidation
{
    /// <summary>Validates a relative POSIX path claim and returns it unchanged.</summary>
    /// <exception cref="AuthorityValidationException">Thrown with a VALIDATION_FAILED code when the path is not a valid relative POSIX path.</exception>
    public static string ValidateRelativePosixPath(string? path)
    {
        if (string.IsNullOrEmpty(path))
            throw new AuthorityValidationException("VALIDATION_FAILED", "Task paths must be non-empty strings");
        if (path.Contains('\\') || path.StartsWith('/') || (path.Length >= 2 && char.IsAsciiLetter(path[0]) && path[1] == ':'))
            throw new AuthorityValidationException("VALIDATION_FAILED", $"Task path is not relative POSIX: {path}");
        string body = path.EndsWith('/') ? path[..^1] : path;
        if (body.Length == 0 || body.Contains("//") || body.Split('/').Any(part => part is "" or "." or ".."))
            throw new AuthorityValidationException("VALIDATION_FAILED", $"Invalid task path: {path}");
        return path;
    }

    /// <summary>Validates that allowed and forbidden path claims are disjoint and well-formed.</summary>
    /// <exception cref="AuthorityValidationException">Thrown on invalid, duplicate, or overlapping claims.</exception>
    public static void ValidatePathClaims(IReadOnlyList<string> allowed, IReadOnlyList<string> forbidden)
    {
        if (allowed.Count == 0 || forbidden.Count == 0)
            throw new AuthorityValidationException("VALIDATION_FAILED", "Allowed and forbidden paths are required");
        string[] normalizedAllowed = allowed.Select(ValidateRelativePosixPath).ToArray();
        string[] normalizedForbidden = forbidden.Select(ValidateRelativePosixPath).ToArray();
        if (normalizedAllowed.Distinct(StringComparer.Ordinal).Count() != normalizedAllowed.Length ||
            normalizedForbidden.Distinct(StringComparer.Ordinal).Count() != normalizedForbidden.Length)
            throw new AuthorityValidationException("VALIDATION_FAILED", "Duplicate task path claim");
        foreach (string allowedPath in normalizedAllowed)
            foreach (string forbiddenPath in normalizedForbidden)
                if (ClaimsOverlap(allowedPath, forbiddenPath))
                    throw new AuthorityValidationException(
                        "VALIDATION_FAILED",
                        $"Allowed path '{allowedPath}' overlaps forbidden path '{forbiddenPath}'");
    }

    private static bool ClaimsOverlap(string left, string right)
    {
        string leftBase = left.TrimEnd('/');
        string rightBase = right.TrimEnd('/');
        return leftBase == rightBase
            || (left.EndsWith('/') && rightBase.StartsWith(leftBase + "/", StringComparison.Ordinal))
            || (right.EndsWith('/') && leftBase.StartsWith(rightBase + "/", StringComparison.Ordinal));
    }
}
