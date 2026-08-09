using Square.Application.Authority;
using Square.Domain.Authority;
using Square.Domain.Practices;
using Square.Domain.Projects;

namespace Square.Application.UseCases;

/// <summary>Use cases that mirror the proven M1 CLI commands.</summary>
public static class M1Handlers
{
    /// <summary>Compiles the execution manifest for a project/task, returning the canonical JSON string.</summary>
    public static string Validate(string project, string taskId)
    {
        try
        {
            return ManifestCompiler.Compile(Path.GetFullPath(project), taskId);
        }
        catch (AuthorityValidationException error)
        {
            throw new ApplicationError(error.Code, error.Message, exitCode: 3);
        }
    }

    /// <summary>Validates a practice record JSON file, returning the canonical record object.</summary>
    public static IReadOnlyDictionary<string, object?> ValidatePractices(string path)
    {
        IReadOnlyDictionary<string, object?>? record;
        try
        {
            byte[] bytes = File.ReadAllBytes(path);
            using var document = System.Text.Json.JsonDocument.Parse(bytes);
            record = JsonToDictionary(document.RootElement);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            throw new ApplicationError("INVALID_INPUT", $"Cannot read practice file: {error.Message}", exitCode: 2);
        }
        if (record is null)
            throw new ApplicationError("INVALID_INPUT", "Practice record must be a JSON object", exitCode: 2);
        try
        {
            PracticeRecordValidator.Validate(record);
        }
        catch (PracticeValidationException error)
        {
            throw new ApplicationError(error.Code, error.Message, exitCode: 2);
        }
        return record;
    }

    /// <summary>Runs the project-foundry blueprint preview (read-only).</summary>
    public static BlueprintPreview PreviewProject(string blueprintPath)
    {
        try
        {
            byte[] bytes = File.ReadAllBytes(blueprintPath);
            using var document = System.Text.Json.JsonDocument.Parse(bytes);
            IReadOnlyDictionary<string, object?>? blueprint = JsonToDictionary(document.RootElement);
            if (blueprint is null)
                throw new ProjectValidationException("INVALID_INPUT", "Blueprint input must be a JSON object");
            return BlueprintValidator.Preview(blueprint);
        }
        catch (ProjectValidationException error)
        {
            throw new ApplicationError(error.Code, error.Message, exitCode: 2);
        }
        catch (Exception error) when (error is IOException or System.Text.Json.JsonException)
        {
            throw new ApplicationError("INVALID_INPUT", $"Cannot read blueprint: {error.Message}", exitCode: 2);
        }
    }

    /// <summary>Runs the read-only repository audit.</summary>
    public static IReadOnlyDictionary<string, object?> AuditProject(string repository)
    {
        string repo = Path.GetFullPath(repository);
        try
        {
            string head = Git(repo, "rev-parse", "HEAD").Trim();
            string porcelain = Git(repo, "status", "--porcelain").Trim();
            bool worktreeClean = porcelain.Length == 0;
            var authorityFiles = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["AGENTS.md"] = File.Exists(Path.Combine(repo, "AGENTS.md")),
                ["CLAUDE.md"] = File.Exists(Path.Combine(repo, "CLAUDE.md")),
                ["SPEC.md"] = File.Exists(Path.Combine(repo, "SPEC.md")),
                ["STATUS.md"] = File.Exists(Path.Combine(repo, "STATUS.md")),
                ["HANDOVER.md"] = File.Exists(Path.Combine(repo, "HANDOVER.md"))
            };
            var topLevel = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (string entry in Directory.EnumerateFileSystemEntries(repo).OrderBy(x => x, StringComparer.Ordinal))
            {
                string name = Path.GetFileName(entry);
                if (name == ".git") continue;
                if (Directory.Exists(entry))
                {
                    string key = name is "tests" or "test" ? "test" : "directory";
                    AddToList(topLevel, key, name);
                }
                else if (name.EndsWith(".py", StringComparison.Ordinal))
                    AddToList(topLevel, "source", name);
                else if (name is "setup.py" or "pyproject.toml" or "package.json" or "Cargo.toml" or "go.mod")
                    AddToList(topLevel, "package_metadata", name);
            }
            string? activePacket = ReadActivePacket(repo);
            bool activePacketExists = activePacket is not null && Directory.Exists(Path.Combine(repo, activePacket.Replace('/', Path.DirectorySeparatorChar)));
            return new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["head"] = head,
                ["worktree_clean"] = worktreeClean,
                ["authority_files"] = authorityFiles,
                ["context_pair_gaps"] = ContextPairGaps(repo),
                ["top_level"] = topLevel,
                ["active_packet_exists"] = activePacketExists
            };
        }
        catch (Exception)
        {
            throw new ApplicationError("NOT_A_REPOSITORY", "Git repository inspection failed", exitCode: 2);
        }
    }

    private static string UtcNow() => DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");

    private static void AddToList(Dictionary<string, object?> target, string key, string value)
    {
        if (!target.TryGetValue(key, out object? existing))
        {
            target[key] = new List<object?> { value };
            return;
        }
        ((List<object?>)existing!).Add(value);
    }

    private static IReadOnlyList<object?> ContextPairGaps(string repo)
    {
        var gaps = new List<object?>();
        foreach (string directory in Directory.EnumerateDirectories(repo, "*", SearchOption.AllDirectories))
        {
            if (directory.Split(Path.DirectorySeparatorChar).Contains(".git")) continue;
            if (!File.Exists(Path.Combine(directory, "AGENTS.md")) || !File.Exists(Path.Combine(directory, "CLAUDE.md")))
                gaps.Add(Path.GetRelativePath(repo, directory).Replace(Path.DirectorySeparatorChar, '/'));
        }
        return gaps.OrderBy(x => (string)x!, StringComparer.Ordinal).ToList();
    }

    private static string? ReadActivePacket(string repo)
    {
        string statusPath = Path.Combine(repo, "STATUS.md");
        if (!File.Exists(statusPath)) return null;
        foreach (string line in File.ReadAllLines(statusPath))
        {
            string stripped = line.Trim().TrimStart('-').Trim();
            if (stripped.StartsWith("Active planning subplan:", StringComparison.Ordinal))
                return stripped["Active planning subplan:".Length..].Trim().Trim('`').Trim();
        }
        return null;
    }

    private static string Git(string repo, params string[] args)
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo("git")
        {
            WorkingDirectory = repo,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (string arg in args) startInfo.ArgumentList.Add(arg);
        using var process = System.Diagnostics.Process.Start(startInfo)
            ?? throw new InvalidOperationException("git process could not start");
        string stdout = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new InvalidOperationException("git command failed");
        return stdout;
    }

    private static IReadOnlyDictionary<string, object?>? JsonToDictionary(System.Text.Json.JsonElement element)
    {
        if (element.ValueKind != System.Text.Json.JsonValueKind.Object) return null;
        var result = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var property in element.EnumerateObject())
            result[property.Name] = JsonElementToObject(property.Value);
        return result;
    }

    private static object? JsonElementToObject(System.Text.Json.JsonElement element) => element.ValueKind switch
    {
        System.Text.Json.JsonValueKind.Object => JsonToDictionary(element),
        System.Text.Json.JsonValueKind.Array => element.EnumerateArray().Select(JsonElementToObject).ToList(),
        System.Text.Json.JsonValueKind.String => element.GetString(),
        System.Text.Json.JsonValueKind.Number when element.TryGetInt64(out long integer) => integer,
        System.Text.Json.JsonValueKind.Number when element.TryGetDouble(out double real) => real,
        System.Text.Json.JsonValueKind.True => true,
        System.Text.Json.JsonValueKind.False => false,
        System.Text.Json.JsonValueKind.Null or _ => null
    };
}
