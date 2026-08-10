namespace Square.Domain.Projects;

/// <summary>A responsibility node in a project blueprint.</summary>
public sealed record Responsibility(string Id, string Description, string OwnedPath);

/// <summary>A directed dependency edge between responsibilities.</summary>
public sealed record Dependency(string From, string To);

/// <summary>The validated result of a blueprint preview.</summary>
public sealed record BlueprintPreview(
    IReadOnlyList<Responsibility> Responsibilities,
    IReadOnlyList<Dependency> Dependencies,
    IReadOnlyList<string> DependencyOrder,
    IReadOnlyList<string> AuthorityFiles,
    IReadOnlyList<string> ContextPairs,
    IReadOnlyList<string> FirstSliceFiles);
