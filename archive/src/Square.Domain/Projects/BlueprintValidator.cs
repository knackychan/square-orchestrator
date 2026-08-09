namespace Square.Domain.Projects;

/// <summary>
/// Validates a project blueprint: exact required/unknown field sets, responsibility and
/// dependency integrity, and acyclic dependency order (Kahn's algorithm).
/// </summary>
public static class BlueprintValidator
{
    private static readonly HashSet<string> RequiredFields = new(StringComparer.Ordinal)
    {
        "product_boundary", "owner", "language", "deployment_context", "external_effects",
        "data_sensitivity", "expected_scale", "acceptance_authority", "responsibilities", "dependencies"
    };

    private static readonly string[] RootAuthorityFiles = { "AGENTS.md", "CLAUDE.md", "SPEC.md", "STATUS.md", "HANDOVER.md" };

    private static readonly string[] ContextPairs =
    {
        "AGENTS.md", "CLAUDE.md", "docs/superpowers/AGENTS.md", "docs/superpowers/CLAUDE.md",
        "docs/superpowers/specs/AGENTS.md", "docs/superpowers/specs/CLAUDE.md",
        "docs/superpowers/plans/AGENTS.md", "docs/superpowers/plans/CLAUDE.md"
    };

    private static readonly string[] FirstSliceFiles =
    {
        "AGENTS.md", "CLAUDE.md", "SPEC.md", "STATUS.md", "HANDOVER.md",
        "docs/superpowers/AGENTS.md", "docs/superpowers/CLAUDE.md",
        "docs/superpowers/specs/AGENTS.md", "docs/superpowers/specs/CLAUDE.md",
        "docs/superpowers/plans/AGENTS.md", "docs/superpowers/plans/CLAUDE.md"
    };

    /// <exception cref="ProjectValidationException">Thrown with INVALID_INPUT on any contract violation.</exception>
    public static BlueprintPreview Preview(IReadOnlyDictionary<string, object?> blueprint)
    {
        HashSet<string> keys = new(blueprint.Keys, StringComparer.Ordinal);
        string[] missing = RequiredFields.Where(field => !keys.Contains(field)).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        if (missing.Length > 0)
            throw new ProjectValidationException("INVALID_INPUT", $"Blueprint is missing fields: {string.Join(", ", missing)}");
        string[] unknown = keys.Where(field => !RequiredFields.Contains(field)).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        if (unknown.Length > 0)
            throw new ProjectValidationException("INVALID_INPUT", $"Blueprint has unknown fields: {string.Join(", ", unknown)}");

        IReadOnlyList<Responsibility> responsibilities = ValidateResponsibilities(blueprint["responsibilities"]);
        HashSet<string> nodeIds = new(responsibilities.Select(item => item.Id), StringComparer.Ordinal);
        IReadOnlyList<Dependency> dependencies = ValidateDependencies(blueprint["dependencies"], nodeIds);
        IReadOnlyList<string> order = DependencyOrder(responsibilities, dependencies);

        return new BlueprintPreview(
            Responsibilities: responsibilities,
            Dependencies: dependencies,
            DependencyOrder: order,
            AuthorityFiles: RootAuthorityFiles,
            ContextPairs: ContextPairs,
            FirstSliceFiles: FirstSliceFiles);
    }

    private static IReadOnlyList<Responsibility> ValidateResponsibilities(object? value)
    {
        if (value is not IReadOnlyList<object?> list || list.Count == 0)
            throw new ProjectValidationException("INVALID_INPUT", "responsibilities must be a non-empty list");
        var result = new List<Responsibility>();
        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        var seenPaths = new HashSet<string>(StringComparer.Ordinal);
        foreach (object? item in list)
        {
            if (item is not IReadOnlyDictionary<string, object?> responsibility)
                throw new ProjectValidationException("INVALID_INPUT", "responsibility must be an object");
            HashSet<string> itemKeys = new(responsibility.Keys, StringComparer.Ordinal);
            if (!itemKeys.SetEquals(new[] { "id", "description", "owned_path" }))
                throw new ProjectValidationException("INVALID_INPUT", "responsibility must have id, description, owned_path");
            string? id = responsibility["id"] as string;
            string? description = responsibility["description"] as string;
            string? ownedPath = responsibility["owned_path"] as string;
            if (string.IsNullOrEmpty(id))
                throw new ProjectValidationException("INVALID_INPUT", "responsibility id must be a non-empty string");
            if (string.IsNullOrEmpty(description))
                throw new ProjectValidationException("INVALID_INPUT", "responsibility description must be a non-empty string");
            string validatedPath = ValidateRelativePath(ownedPath, "owned_path");
            if (!seenIds.Add(id))
                throw new ProjectValidationException("INVALID_INPUT", $"Duplicate responsibility id: {id}");
            if (!seenPaths.Add(validatedPath))
                throw new ProjectValidationException("INVALID_INPUT", $"Duplicate owned path: {validatedPath}");
            result.Add(new Responsibility(id, description, validatedPath));
        }
        return result;
    }

    private static IReadOnlyList<Dependency> ValidateDependencies(object? value, ISet<string> nodeIds)
    {
        if (value is not IReadOnlyList<object?> list)
            throw new ProjectValidationException("INVALID_INPUT", "dependencies must be a list");
        var result = new List<Dependency>();
        foreach (object? item in list)
        {
            if (item is not IReadOnlyDictionary<string, object?> edge || !edge.Keys.Order().SequenceEqual(new[] { "from", "to" }, StringComparer.Ordinal))
                throw new ProjectValidationException("INVALID_INPUT", "dependency edge must have from and to");
            string? source = edge["from"] as string;
            string? target = edge["to"] as string;
            if (source is null || target is null)
                throw new ProjectValidationException("INVALID_INPUT", "dependency endpoints must be strings");
            if (!nodeIds.Contains(source))
                throw new ProjectValidationException("INVALID_INPUT", $"Unknown dependency source: {source}");
            if (!nodeIds.Contains(target))
                throw new ProjectValidationException("INVALID_INPUT", $"Unknown dependency target: {target}");
            result.Add(new Dependency(source, target));
        }
        return result;
    }

    private static IReadOnlyList<string> DependencyOrder(IReadOnlyList<Responsibility> responsibilities, IReadOnlyList<Dependency> dependencies)
    {
        string[] nodeIds = responsibilities.Select(item => item.Id).ToArray();
        Dictionary<string, int> inDegree = nodeIds.ToDictionary(id => id, _ => 0, StringComparer.Ordinal);
        Dictionary<string, List<string>> adjacency = nodeIds.ToDictionary(id => id, _ => new List<string>(), StringComparer.Ordinal);
        foreach (Dependency edge in dependencies)
        {
            adjacency[edge.To].Add(edge.From);
            inDegree[edge.From]++;
        }
        var ready = new Queue<string>(nodeIds.Where(id => inDegree[id] == 0));
        var order = new List<string>();
        while (ready.Count > 0)
        {
            string node = ready.Dequeue();
            order.Add(node);
            foreach (string target in adjacency[node])
            {
                inDegree[target]--;
                if (inDegree[target] == 0)
                    ready.Enqueue(target);
            }
        }
        if (order.Count != nodeIds.Length)
            throw new ProjectValidationException("INVALID_INPUT", "Responsibility graph contains a cycle");
        return order;
    }

    private static string ValidateRelativePath(string? path, string label)
    {
        if (string.IsNullOrEmpty(path))
            throw new ProjectValidationException("INVALID_INPUT", $"{label} must be a non-empty relative path");
        if (path.Contains('\\') || path.StartsWith('/') || path.Contains(':'))
            throw new ProjectValidationException("INVALID_INPUT", $"{label} must be a relative POSIX path");
        string body = path.EndsWith('/') ? path[..^1] : path;
        if (body.Length == 0 || body.Contains("//") || body.Split('/').Any(part => part is "" or "." or ".."))
            throw new ProjectValidationException("INVALID_INPUT", $"{label} has invalid path segments");
        return path;
    }
}
