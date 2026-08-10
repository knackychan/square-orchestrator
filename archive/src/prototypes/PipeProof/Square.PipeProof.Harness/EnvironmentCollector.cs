using System.Runtime.InteropServices;
using System.Security.Principal;

namespace Square.PipeProof.Harness;

internal static class EnvironmentCollector
{
    internal static async Task<HarnessEnvironmentEvidence> CollectAsync(
        HarnessOptions options,
        CancellationToken cancellationToken = default)
    {
        using WindowsIdentity identity = WindowsIdentity.GetCurrent(TokenAccessLevels.Query);
        string sid = identity.User?.Value
            ?? throw new InvalidOperationException("Current identity has no SID.");
        WindowsPrincipal principal = new(identity);
        bool elevated = principal.IsInRole(WindowsBuiltInRole.Administrator);
        string dotnet = await ReadVersionAsync("dotnet", ["--version"], cancellationToken).ConfigureAwait(false);
        string node = await ReadVersionAsync(options.NodeExecutable, ["--version"], cancellationToken).ConfigureAwait(false);
        return new(
            "1.0",
            DateTimeOffset.UtcNow,
            RuntimeInformation.OSDescription,
            RuntimeInformation.OSArchitecture.ToString(),
            RuntimeInformation.ProcessArchitecture.ToString(),
            RuntimeInformation.FrameworkDescription,
            dotnet,
            node,
            sid,
            elevated,
            Environment.ProcessId,
            Environment.MachineName,
            options.SourceRoot,
            await EvidenceWriter.ComputeSha256Async(options.DispatchPacket, cancellationToken).ConfigureAwait(false),
            await EvidenceWriter.ComputeSha256Async(options.ScenarioManifest, cancellationToken).ConfigureAwait(false),
            options.Quick);
    }

    private static async Task<string> ReadVersionAsync(
        string executable,
        IEnumerable<string> arguments,
        CancellationToken cancellationToken)
    {
        ProcessResult result = await ProcessRunner.RunExecutableAsync(
            executable,
            arguments,
            TimeSpan.FromSeconds(10),
            cancellationToken).ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Version command '{executable}' failed: {result.StandardError}");
        }
        return result.StandardOutput.Trim();
    }
}
