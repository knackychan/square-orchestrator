using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Square.TerminalProof.Native;

namespace Square.TerminalProof.Harness;

internal static class ProofEnvironmentCollector
{
    internal static async Task<ProofEnvironmentEvidence> CaptureAsync(
        ProofOptions options,
        CancellationToken cancellationToken)
    {
        string harnessPath = Environment.ProcessPath
            ?? Assembly.GetExecutingAssembly().Location;
        using Process current = Process.GetCurrentProcess();
        using WindowsIdentity identity = WindowsIdentity.GetCurrent();
        WindowsPrincipal principal = new(identity);

        return new ProofEnvironmentEvidence
        {
            SchemaVersion = "1.0",
            CapturedAtUtc = DateTimeOffset.UtcNow,
            OsDescription = RuntimeInformation.OSDescription,
            OsVersion = Environment.OSVersion.VersionString,
            WindowsBuild = Environment.OSVersion.Version.Build,
            OsArchitecture = RuntimeInformation.OSArchitecture.ToString(),
            ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
            FrameworkDescription = RuntimeInformation.FrameworkDescription,
            ProcessorCount = Environment.ProcessorCount,
            Is64BitOperatingSystem = Environment.Is64BitOperatingSystem,
            Is64BitProcess = Environment.Is64BitProcess,
            IsElevated = principal.IsInRole(WindowsBuiltInRole.Administrator),
            CurrentProcessIsInJob = WindowsProcessEnvironment.IsCurrentProcessInJob(),
            InitialHarnessHandleCount = current.HandleCount,
            ManifestSha256 = await Hashing.Sha256FileAsync(options.ManifestPath, cancellationToken).ConfigureAwait(false),
            DispatchPacketSha256 = await Hashing.Sha256FileAsync(
                GetDispatchPacketPath(options.ManifestPath),
                cancellationToken).ConfigureAwait(false),
            FixtureSha256 = await Hashing.Sha256FileAsync(options.FixturePath, cancellationToken).ConfigureAwait(false),
            CrashOwnerSha256 = await Hashing.Sha256FileAsync(options.CrashOwnerPath, cancellationToken).ConfigureAwait(false),
            HarnessSha256 = await Hashing.Sha256FileAsync(harnessPath, cancellationToken).ConfigureAwait(false)
        };
    }

    internal static string GetDispatchPacketPath(string manifestPath) =>
        Path.Combine(
            Path.GetDirectoryName(Path.GetFullPath(manifestPath))
                ?? throw new InvalidOperationException("The proof manifest path has no parent directory."),
            "dispatch.packet.json");
}

