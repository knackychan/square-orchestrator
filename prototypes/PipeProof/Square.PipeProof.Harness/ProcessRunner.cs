using System.Diagnostics;

namespace Square.PipeProof.Harness;

internal sealed record ProcessResult(
    int ExitCode,
    string StandardOutput,
    string StandardError,
    TimeSpan Duration);

internal sealed class StartedProcess : IAsyncDisposable
{
    private readonly Task<string> _stdout;
    private readonly Task<string> _stderr;
    private readonly DateTimeOffset _startedAt;
    private int _disposed;

    internal StartedProcess(Process process)
    {
        Process = process;
        _startedAt = DateTimeOffset.UtcNow;
        _stdout = process.StandardOutput.ReadToEndAsync();
        _stderr = process.StandardError.ReadToEndAsync();
    }

    internal Process Process { get; }

    internal bool HasExited => Process.HasExited;

    internal async Task<ProcessResult> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linked.CancelAfter(timeout);
        try
        {
            await Process.WaitForExitAsync(linked.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            Kill();
            throw new TimeoutException($"Process {Process.Id} exceeded {timeout}.");
        }
        string stdout = await _stdout.ConfigureAwait(false);
        string stderr = await _stderr.ConfigureAwait(false);
        return new(
            Process.ExitCode,
            stdout,
            stderr,
            DateTimeOffset.UtcNow - _startedAt);
    }

    internal void Kill()
    {
        if (!Process.HasExited)
        {
            Process.Kill(entireProcessTree: true);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }
        if (!Process.HasExited)
        {
            Kill();
            try
            {
                await Process.WaitForExitAsync().ConfigureAwait(false);
            }
            catch (InvalidOperationException)
            {
            }
        }
        Process.Dispose();
    }
}

internal static class ProcessRunner
{
    internal static StartedProcess StartArtifact(string artifact, IEnumerable<string> arguments)
    {
        ProcessStartInfo start = CreateStartInfoForArtifact(artifact, arguments);
        return Start(start);
    }

    internal static StartedProcess StartExecutable(string executable, IEnumerable<string> arguments)
    {
        ProcessStartInfo start = CreateBase(executable);
        foreach (string argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }
        return Start(start);
    }

    internal static async Task<ProcessResult> RunArtifactAsync(
        string artifact,
        IEnumerable<string> arguments,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        await using StartedProcess process = StartArtifact(artifact, arguments);
        return await process.WaitAsync(timeout, cancellationToken).ConfigureAwait(false);
    }

    internal static async Task<ProcessResult> RunExecutableAsync(
        string executable,
        IEnumerable<string> arguments,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        await using StartedProcess process = StartExecutable(executable, arguments);
        return await process.WaitAsync(timeout, cancellationToken).ConfigureAwait(false);
    }

    private static StartedProcess Start(ProcessStartInfo start)
    {
        Process process = new() { StartInfo = start, EnableRaisingEvents = true };
        if (!process.Start())
        {
            process.Dispose();
            throw new InvalidOperationException($"Could not start '{start.FileName}'.");
        }
        return new StartedProcess(process);
    }

    private static ProcessStartInfo CreateStartInfoForArtifact(
        string artifact,
        IEnumerable<string> arguments)
    {
        string fullPath = Path.GetFullPath(artifact);
        ProcessStartInfo start;
        if (string.Equals(Path.GetExtension(fullPath), ".dll", StringComparison.OrdinalIgnoreCase))
        {
            start = CreateBase("dotnet");
            start.ArgumentList.Add(fullPath);
        }
        else
        {
            start = CreateBase(fullPath);
        }
        foreach (string argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }
        return start;
    }

    private static ProcessStartInfo CreateBase(string executable) => new()
    {
        FileName = executable,
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true,
        WorkingDirectory = Environment.CurrentDirectory
    };
}
