using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Square.Domain.Authority;

namespace Square.Application.Authority;

/// <summary>
/// Compiles the hash-bound execution manifest from a target repository's canonical documents.
/// Mirrors the proven M1 manifest compiler: fail-closed on any authority drift.
/// </summary>
public static partial class ManifestCompiler
{
    private const string TaskBlockStart = "<!-- sqorch:task v1 -->";
    private const string TaskBlockEnd = "<!-- /sqorch:task -->";

    private static readonly Regex TomlFenceRegex = new(@"\A\s*```toml\n(.*?)```\s*\Z", RegexOptions.Singleline);
    private static readonly Regex ActivePacketRegex = new(@"^\s*-?\s*Active planning subplan:\s*`([^`]+)`\s*$", RegexOptions.Multiline);
    private static readonly Regex ActiveTaskRegex = new(@"^\s*-?\s*Application implementation authorized:\s*\*\*yes\s+—\s+([^*]+?)\s+only\*\*\s*$", RegexOptions.Multiline);
    private static readonly Regex WorktreeStateRegex = new(@"^\s*-?\s*Worktree state:\s*\*\*(clean|dirty)(?:\s+—[^*]*)?\*\*\s*$", RegexOptions.Multiline);

    /// <summary>Compiles the canonical JSON manifest for the given repository and task, or throws a typed authority error.</summary>
    public static string Compile(string repositoryPath, string taskId)
    {
        string repo = Path.GetFullPath(repositoryPath);
        string statusPath = Path.Combine(repo, "STATUS.md");
        if (!File.Exists(statusPath))
            throw new AuthorityValidationException("AUTHORITY_MISSING", "STATUS.md is missing");

        string statusContent = File.ReadAllText(statusPath, Encoding.UTF8);
        string docsRelative = ReadExactlyOne(ActivePacketRegex, statusContent, "active planning subplan");
        PathValidation.ValidateRelativePosixPath(docsRelative);
        string docsPath = Path.GetFullPath(Path.Combine(repo, docsRelative.Replace('/', Path.DirectorySeparatorChar)));
        if (!Directory.Exists(docsPath) || !IsInside(repo, docsPath))
            throw new AuthorityValidationException("AUTHORITY_DRIFT", "Active packet is outside the repository");

        string activeTask = ReadExactlyOne(ActiveTaskRegex, statusContent, "implementation authorization");
        if (activeTask != taskId)
            throw new AuthorityValidationException("AUTHORITY_DRIFT", "Requested task is not the active authorized task");

        TaskContract contract = ExtractAndValidateTaskBlock(repo, docsPath, taskId);
        ValidateRepositoryState(repo, statusContent, contract);

        Dictionary<string, object?> task = new(StringComparer.Ordinal)
        {
            ["acceptance_authority"] = contract.AcceptanceAuthority,
            ["allowed_paths"] = contract.AllowedPaths,
            ["automatic_fallback"] = contract.AutomaticFallback,
            ["client"] = contract.Client,
            ["evidence_destination"] = contract.EvidenceDestination,
            ["expected_commit_message"] = contract.ExpectedCommitMessage,
            ["external_call_limit"] = contract.ExternalCallLimit,
            ["forbidden_paths"] = contract.ForbiddenPaths,
            ["id"] = contract.Id,
            ["mode"] = contract.Mode,
            ["model"] = contract.Model,
            ["role"] = contract.Role,
            ["spend_limit_usd"] = contract.SpendLimitUsd,
            ["starting_commit"] = contract.StartingCommit,
            ["token_rotation_limit"] = contract.TokenRotationLimit,
            ["turn_limit"] = contract.TurnLimit,
            ["validation"] = contract.Validation
        };

        Dictionary<string, object?> manifest = new(StringComparer.Ordinal)
        {
            ["schema"] = 1,
            ["task"] = task,
            ["hashes"] = ComputeDocumentHashes(repo, docsPath)
        };

        return ManifestJson.Serialize(manifest);
    }

    /// <summary>Reads the active task block from BUILD-TASKS.md and validates it against the exact schema.</summary>
    public static TaskContract ExtractAndValidateTaskBlock(string repo, string docsPath, string taskId)
    {
        string tasksPath = Path.Combine(docsPath, "BUILD-TASKS.md");
        if (!File.Exists(tasksPath))
            throw new AuthorityValidationException("AUTHORITY_MISSING", "BUILD-TASKS.md is missing");

        string content = File.ReadAllText(tasksPath, Encoding.UTF8).Replace("\r\n", "\n");
        var blocks = new List<IReadOnlyDictionary<string, object?>>();
        int position = 0;
        while (true)
        {
            int start = content.IndexOf(TaskBlockStart, position, StringComparison.Ordinal);
            if (start < 0) break;
            int endMarker = content.IndexOf(TaskBlockEnd, start + TaskBlockStart.Length, StringComparison.Ordinal);
            if (endMarker < 0)
                throw new AuthorityValidationException("VALIDATION_FAILED", "Unterminated task block");
            string inner = content[(start + TaskBlockStart.Length)..endMarker];
            Match match = TomlFenceRegex.Match(inner);
            if (!match.Success)
                throw new AuthorityValidationException("VALIDATION_FAILED", "Task block must contain one TOML fence");
            blocks.Add(ParseTomlBlock(match.Groups[1].Value));
            position = endMarker + TaskBlockEnd.Length;
        }

        if (blocks.Count == 0)
            throw new AuthorityValidationException("AUTHORITY_MISSING", "No task blocks found in BUILD-TASKS.md");
        var matching = blocks.Where(block => block.TryGetValue("id", out object? id) && id as string == taskId).ToList();
        if (matching.Count != 1)
            throw new AuthorityValidationException(
                matching.Count == 0 ? "AUTHORITY_MISSING" : "VALIDATION_FAILED",
                $"Task {taskId} is missing or duplicated");

        return TaskContractValidator.Validate(matching[0], taskId);
    }

    private static void ValidateRepositoryState(string repo, string statusContent, TaskContract contract)
    {
        RouteValidation.ValidateRoute(contract.Client, contract.Model, contract.AutomaticFallback);

        string head = GitOutput(repo, "rev-parse", "HEAD").Trim();
        if (head != contract.StartingCommit)
            throw new AuthorityValidationException("AUTHORITY_DRIFT", "HEAD does not match task starting_commit");

        string declaredWorktree = ReadExactlyOne(WorktreeStateRegex, statusContent, "worktree state");
        bool isDirty = GitOutput(repo, "status", "--porcelain").Trim().Length > 0;
        if ((declaredWorktree == "clean") == isDirty)
            throw new AuthorityValidationException("AUTHORITY_DRIFT", "Worktree disclosure does not match Git status");

        ValidateContextPairs(repo);
    }

    private static void ValidateContextPairs(string repo)
    {
        var ignored = new HashSet<string>(StringComparer.Ordinal) { ".git", "__pycache__" };
        var directories = new List<string> { repo };
        directories.AddRange(Directory.EnumerateDirectories(repo, "*", SearchOption.AllDirectories));
        foreach (string directory in directories)
        {
            string relative = directory == repo ? "." : Path.GetRelativePath(repo, directory).Replace(Path.DirectorySeparatorChar, '/');
            if (relative != "." && relative.Split('/').Any(part => ignored.Contains(part) || part.StartsWith('.')))
                continue;
            if (!File.Exists(Path.Combine(directory, "AGENTS.md")) || !File.Exists(Path.Combine(directory, "CLAUDE.md")))
                throw new AuthorityValidationException("AUTHORITY_DRIFT", $"Context pair is incomplete at {relative}");
        }
    }

    private static Dictionary<string, object?> ComputeDocumentHashes(string repo, string docsPath) =>
        new(StringComparer.Ordinal)
        {
            ["STATUS.md"] = Sha256Hex(File.ReadAllBytes(Path.Combine(repo, "STATUS.md"))),
            ["PACKET.md"] = Sha256Hex(File.ReadAllBytes(Path.Combine(docsPath, "PACKET.md"))),
            ["BUILD.md"] = Sha256Hex(File.ReadAllBytes(Path.Combine(docsPath, "BUILD.md"))),
            ["BUILD-TASKS.md"] = Sha256Hex(File.ReadAllBytes(Path.Combine(docsPath, "BUILD-TASKS.md")))
        };

    private static string Sha256Hex(byte[] content) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(content)).ToLowerInvariant();

    private static string GitOutput(string repo, params string[] args)
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
            ?? throw new AuthorityValidationException("AUTHORITY_DRIFT", "Git repository inspection failed");
        string stdout = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new AuthorityValidationException("AUTHORITY_DRIFT", "Git repository inspection failed");
        return stdout;
    }

    private static string ReadExactlyOne(Regex pattern, string content, string label)
    {
        MatchCollection matches = pattern.Matches(content);
        if (matches.Count != 1)
            throw new AuthorityValidationException("AUTHORITY_MISSING", $"Exactly one {label} field is required");
        return matches[0].Groups[1].Value;
    }

    private static bool IsInside(string root, string candidate)
    {
        string rootFull = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return candidate.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyDictionary<string, object?> ParseTomlBlock(string toml)
    {
        // Minimal TOML subset sufficient for M1 task blocks: string, integer, float, boolean,
        // array-of-string, and array-of-integer values. This mirrors the exact Python tomllib contract.
        var result = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (string rawLine in toml.Split('\n'))
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            int equals = line.IndexOf('=');
            if (equals <= 0) continue;
            string key = line[..equals].Trim();
            string value = line[(equals + 1)..].Trim();
            result[key] = ParseTomlValue(value);
        }
        return result;
    }

    private static object? ParseTomlValue(string value)
    {
        if (value == "true") return true;
        if (value == "false") return false;
        if (value.StartsWith('"') && value.EndsWith('"') && value.Length >= 2)
            return UnescapeTomlString(value[1..^1]);
        if (value.StartsWith('[') && value.EndsWith(']'))
        {
            string inner = value[1..^1].Trim();
            if (inner.Length == 0) return Array.Empty<object?>();
            var items = new List<object?>();
            foreach (string item in SplitTomlArray(inner))
                items.Add(ParseTomlValue(item));
            return items;
        }
        if (long.TryParse(value, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out long integer))
            return integer;
        if (double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double real))
            return real;
        throw new AuthorityValidationException("VALIDATION_FAILED", "Malformed TOML task block");
    }

    private static string UnescapeTomlString(string value) =>
        value.Replace("\\\"", "\"").Replace("\\\\", "\\").Replace("\\n", "\n").Replace("\\t", "\t");

    private static IEnumerable<string> SplitTomlArray(string inner)
    {
        var items = new List<string>();
        int start = 0, depth = 0;
        for (int i = 0; i < inner.Length; i++)
        {
            char c = inner[i];
            if (c == '[') depth++;
            else if (c == ']') depth--;
            else if (c == ',' && depth == 0)
            {
                items.Add(inner[start..i].Trim());
                start = i + 1;
            }
        }
        items.Add(inner[start..].Trim());
        return items.Where(item => item.Length > 0);
    }
}
