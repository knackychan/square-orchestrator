using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Square.TerminalProof.Native;

return await CrashOwnerProgram.RunAsync(args).ConfigureAwait(false);

internal static class CrashOwnerProgram
{
    internal static async Task<int> RunAsync(string[] args)
    {
        Dictionary<string, string> options = Parse(args);
        string fixture = Path.GetFullPath(Required(options, "--fixture"));
        string readyFile = Path.GetFullPath(Required(options, "--ready-file"));
        string workingDirectory = Path.GetFullPath(Required(options, "--working-directory"));
        int childCount = ReadPositive(options, "--child-count", 3);
        int timeoutMilliseconds = ReadPositive(options, "--timeout-ms", 10_000);
        TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);

        ConPtyTerminalSession session = await ConPtyTerminalSession.StartAsync(new TerminalLaunchOptions
        {
            ExecutablePath = fixture,
            WorkingDirectory = workingDirectory,
            Arguments =
            [
                "--scenario",
                "nested_children",
                "--child-count",
                childCount.ToString(CultureInfo.InvariantCulture)
            ],
            InitialSize = new TerminalSize(100, 30),
            CleanupTimeout = timeout
        }).ConfigureAwait(false);

        await session.WaitForOutputAsync("TREE-READY", timeout).ConfigureAwait(false);
        await WaitForProcessCountAsync(session, childCount + 1, timeout).ConfigureAwait(false);
        IReadOnlyList<CrashProcessIdentity> processes = session.GetActiveProcessIds()
            .Distinct()
            .Order()
            .Select(CaptureProcessIdentity)
            .ToArray();
        CrashOwnerReady evidence = new(
            "1.0",
            Environment.ProcessId,
            session.ProcessId,
            processes,
            DateTimeOffset.UtcNow);

        await WriteReadyEvidenceAtomicallyAsync(readyFile, evidence).ConfigureAwait(false);

        // Deliberately bypass IAsyncDisposable and finalizers. Process termination closes the sole Job
        // Object handle; JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE must terminate every observed descendant.
        GC.KeepAlive(session);
        CrashOwnerProcess();
        throw new UnreachableException("Environment.FailFast unexpectedly returned.");
    }

    [DoesNotReturn]
    private static void CrashOwnerProcess() =>
        Environment.FailFast("Intentional SP00-T02 owner crash after nested-process evidence was flushed.");

    private static async Task WaitForProcessCountAsync(
        ConPtyTerminalSession session,
        int expected,
        TimeSpan timeout)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        while (session.GetAccounting().ActiveProcesses < expected)
        {
            if (stopwatch.Elapsed >= timeout)
            {
                throw new TimeoutException($"Owner-crash fixture did not reach {expected} active Job Object processes within {timeout}.");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(20)).ConfigureAwait(false);
        }
    }

    private static CrashProcessIdentity CaptureProcessIdentity(int processId)
    {
        using Process process = Process.GetProcessById(processId);
        process.Refresh();
        if (process.HasExited)
        {
            throw new InvalidOperationException($"Job Object process {processId} exited before owner-crash evidence was captured.");
        }

        return new CrashProcessIdentity(processId, process.StartTime.ToUniversalTime().Ticks);
    }

    private static async Task WriteReadyEvidenceAtomicallyAsync(string path, CrashOwnerReady evidence)
    {
        string directory = Path.GetDirectoryName(path)
            ?? throw new InvalidOperationException("The owner-crash ready file must have a parent directory.");
        Directory.CreateDirectory(directory);
        string temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";
        try
        {
            JsonSerializerOptions options = new()
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
                WriteIndented = true
            };
            byte[] json = JsonSerializer.SerializeToUtf8Bytes(evidence, options);
            await using (FileStream stream = new(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 16 * 1024,
                FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await stream.WriteAsync(json).ConfigureAwait(false);
                await stream.WriteAsync("\n"u8.ToArray()).ConfigureAwait(false);
                await stream.FlushAsync().ConfigureAwait(false);
                stream.Flush(flushToDisk: true);
            }

            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static Dictionary<string, string> Parse(IReadOnlyList<string> args)
    {
        HashSet<string> allowed = new(StringComparer.Ordinal)
        {
            "--fixture",
            "--ready-file",
            "--working-directory",
            "--child-count",
            "--timeout-ms"
        };
        Dictionary<string, string> values = new(StringComparer.Ordinal);
        for (int index = 0; index < args.Count; index++)
        {
            string key = args[index];
            if (!allowed.Contains(key))
            {
                throw new ArgumentException($"Unknown owner-crash argument '{key}'.");
            }

            if (index + 1 >= args.Count || args[index + 1].StartsWith("--", StringComparison.Ordinal))
            {
                throw new ArgumentException($"Owner-crash argument '{key}' requires a value.");
            }

            if (!values.TryAdd(key, args[++index]))
            {
                throw new ArgumentException($"Duplicate owner-crash argument '{key}'.");
            }
        }

        return values;
    }

    private static string Required(IReadOnlyDictionary<string, string> values, string name) =>
        values.TryGetValue(name, out string? value)
            ? value
            : throw new ArgumentException($"Required owner-crash argument '{name}' was not provided.");

    private static int ReadPositive(
        IReadOnlyDictionary<string, string> values,
        string name,
        int defaultValue)
    {
        if (!values.TryGetValue(name, out string? text))
        {
            return defaultValue;
        }

        if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out int value) || value < 1)
        {
            throw new ArgumentException($"Owner-crash argument '{name}' must be a positive integer.");
        }

        return value;
    }

    private sealed record CrashOwnerReady(
        string SchemaVersion,
        int OwnerProcessId,
        int RootProcessId,
        IReadOnlyList<CrashProcessIdentity> JobProcesses,
        DateTimeOffset ReadyAtUtc);

    private sealed record CrashProcessIdentity(int ProcessId, long StartTimeUtcTicks);
}
