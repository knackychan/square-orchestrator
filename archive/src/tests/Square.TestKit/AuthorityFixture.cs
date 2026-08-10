using System.Diagnostics;
using System.Text;

namespace Square.TestKit;

/// <summary>
/// Builds temporary authority-fixture repositories for M1 tests, mirroring the proven Python
/// tests/support.py: git repo, context pairs, STATUS naming an active packet/task, and a
/// BUILD-TASKS.md containing one exact TOML task block.
/// </summary>
public static class AuthorityFixture
{
    public static string MakeAuthorityFixture(string root, string taskId = "T-TEST-01")
    {
        string docs = Path.Combine(root, "docs", "superpowers", "plans", "2026-08-05-m1-dry-run-foundation");
        Directory.CreateDirectory(docs);

        WriteContextPairs(root);
        WriteContextPairs(docs);

        string head = InitGitRepo(root);

        WriteStatus(root, "docs/superpowers/plans/2026-08-05-m1-dry-run-foundation", taskId);
        WritePacket(docs);
        string block = TomlTaskBlock(head, taskId);
        WriteBuildTasks(docs, block);
        return root;
    }

    public static string InitGitRepo(string root)
    {
        Run(root, "git", "init", "-b", "main");
        Run(root, "git", "config", "user.email", "test@example.com");
        Run(root, "git", "config", "user.name", "Test");
        File.WriteAllText(Path.Combine(root, ".gitkeep"), "");
        Run(root, "git", "add", ".gitkeep");
        Run(root, "git", "commit", "-m", "init");
        return Run(root, "git", "rev-parse", "HEAD").Trim();
    }

    public static void WriteStatus(string root, string packetRel, string? taskId = null, string worktreeState = "dirty")
    {
        var content = new StringBuilder();
        content.AppendLine("# Status");
        content.AppendLine();
        content.AppendLine($"Active planning subplan: `{packetRel}`");
        if (taskId is not null)
        {
            content.AppendLine();
            content.AppendLine($"Application implementation authorized: **yes — {taskId} only**");
        }
        content.AppendLine();
        content.AppendLine($"Worktree state: **{worktreeState}**");
        File.WriteAllText(Path.Combine(root, "STATUS.md"), content.ToString(), Encoding.UTF8);
    }

    public static void WritePacket(string packetDir)
    {
        File.WriteAllText(Path.Combine(packetDir, "PACKET.md"), "# Packet\n\nTest packet content.\n", Encoding.UTF8);
        File.WriteAllText(Path.Combine(packetDir, "BUILD.md"), "# Build\n\nTest build content.\n", Encoding.UTF8);
    }

    public static void WriteContextPairs(string root)
    {
        foreach (string directory in new[] { root, Path.Combine(root, "docs"), Path.Combine(root, "docs", "superpowers"), Path.Combine(root, "docs", "superpowers", "plans") })
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "AGENTS.md"), "# Context\n", Encoding.UTF8);
            File.WriteAllText(Path.Combine(directory, "CLAUDE.md"), "# Context\n", Encoding.UTF8);
        }
    }

    public static string TomlTaskBlock(string headSha, string taskId = "T-TEST-01")
    {
        var lines = new[]
        {
            "schema = 1",
            $"id = \"{taskId}\"",
            "role = \"IMPLEMENT\"",
            "mode = \"write\"",
            $"starting_commit = \"{headSha}\"",
            "allowed_paths = [\"sqorch/\"]",
            "forbidden_paths = [\"tests/\"]",
            "validation = [\"python -m unittest\"]",
            "expected_commit_message = \"feat: test\"",
            "external_call_limit = 0",
            "spend_limit_usd = 0",
            "turn_limit = 100",
            "token_rotation_limit = 150000",
            "client = \"cmdc\"",
            "model = \"deepseek/deepseek-v4-pro\"",
            "automatic_fallback = false",
            "evidence_destination = \"docs/STATE.md\"",
            "acceptance_authority = \"owner\""
        };
        return string.Join('\n', lines);
    }

    public static void WriteBuildTasks(string packetDir, params string[] blocks)
    {
        var parts = new StringBuilder();
        parts.AppendLine("# Build Tasks");
        parts.AppendLine();
        foreach (string block in blocks)
        {
            parts.AppendLine("<!-- sqorch:task v1 -->");
            parts.AppendLine("```toml");
            parts.AppendLine(block);
            parts.AppendLine("```");
            parts.AppendLine("<!-- /sqorch:task -->");
            parts.AppendLine();
        }
        File.WriteAllText(Path.Combine(packetDir, "BUILD-TASKS.md"), parts.ToString(), Encoding.UTF8);
    }

    private static string Run(string cwd, string fileName, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            WorkingDirectory = cwd,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (string argument in arguments) startInfo.ArgumentList.Add(argument);
        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Could not start {fileName}");
        string stdout = process.StandardOutput.ReadToEnd();
        string stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"{fileName} failed ({process.ExitCode}): {stderr}");
        return stdout;
    }
}
