namespace Square.Domain.Authority;

/// <summary>Route validation for the M1 task contract: exact client/model IDs, no aliases, no automatic fallback.</summary>
public static class RouteValidation
{
    private static readonly System.Collections.Generic.HashSet<string> AliasModels = new(
        new[] { "latest", "auto", "default", "best", "strongest" },
        StringComparer.OrdinalIgnoreCase);

    /// <exception cref="AuthorityValidationException">Thrown with ROUTE_INVALID when the route is not an exact, fallback-free ID.</exception>
    public static void ValidateRoute(string? client, string? model, bool automaticFallback)
    {
        if (string.IsNullOrEmpty(client))
            throw new AuthorityValidationException("ROUTE_INVALID", "Client must not be empty");
        if (string.IsNullOrEmpty(model) || AliasModels.Contains(model))
            throw new AuthorityValidationException("ROUTE_INVALID", "Model must be an exact non-alias ID");
        if (automaticFallback)
            throw new AuthorityValidationException("ROUTE_INVALID", "Automatic fallback must be disabled");
    }
}
