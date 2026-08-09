using System.Runtime.InteropServices;

namespace Square.TerminalProof.Harness;

internal static class ProofProgram
{
    internal static async Task<int> RunAsync(string[] args)
    {
        if (args.Any(argument => argument is "--help" or "-h" or "/?"))
        {
            Console.WriteLine(ProofOptions.Usage);
            return 0;
        }

        using CancellationTokenSource cancellation = new();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellation.Cancel();
        };

        try
        {
            ProofOptions options = ProofOptions.Parse(args);
            ValidatePathsAndEvidenceDirectory(options);

            ProofManifest manifest = (await ProofManifest.LoadAsync(options.ManifestPath, cancellation.Token).ConfigureAwait(false))
                .Apply(options);
            manifest.Validate();

            await using ProofEvidenceWriter evidence = new(options.EvidenceDirectory);
            ProofEnvironmentEvidence environment = await ProofEnvironmentCollector.CaptureAsync(options, cancellation.Token).ConfigureAwait(false);
            await evidence.WriteEnvironmentAsync(environment, cancellation.Token).ConfigureAwait(false);
            if (environment.IsElevated && !options.AllowElevated)
            {
                throw new InvalidOperationException(
                    "SP00-T02 must run from a normal non-elevated user token. " +
                    "Use --allow-elevated only for a diagnostic run that cannot satisfy G0 acceptance.");
            }
            await evidence.WriteManifestSnapshotAsync(manifest, cancellation.Token).ConfigureAwait(false);

            ProofRunner runner = new(options, manifest, environment, evidence);
            ProofSummaryEvidence summary = await runner.ExecuteAsync(cancellation.Token).ConfigureAwait(false);
            await evidence.WriteSummaryAsync(summary, cancellation.Token).ConfigureAwait(false);
            await evidence.WriteEvidenceManifestAsync(cancellation.Token).ConfigureAwait(false);

            Console.WriteLine(
                $"SP00-T02 {summary.Status}: reliability={summary.ReliabilityRuns}, " +
                $"scale_sessions={summary.ScaleSessionRuns}, failed_runs={summary.FailedRuns}");
            Console.WriteLine($"Evidence: {summary.EvidenceDirectory}");
            foreach (string failure in summary.GlobalFailures)
            {
                Console.Error.WriteLine($"GLOBAL FAILURE: {failure}");
            }

            foreach (string limitation in summary.Limitations)
            {
                Console.WriteLine($"ACCEPTANCE LIMITATION: {limitation}");
            }

            return summary.Status switch
            {
                "PASS" => 0,
                "DIAGNOSTIC_PASS" => 0,
                _ => 1
            };
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("SP00-T02 proof cancelled after bounded cleanup.");
            return 130;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            Console.Error.WriteLine(ProofOptions.Usage);
            return 2;
        }
    }

    private static void ValidatePathsAndEvidenceDirectory(ProofOptions options)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763))
        {
            throw new PlatformNotSupportedException("SP00-T02 requires Windows 10 version 1809 (build 17763) or later.");
        }

        if (RuntimeInformation.OSArchitecture != Architecture.X64
            || RuntimeInformation.ProcessArchitecture != Architecture.X64)
        {
            throw new PlatformNotSupportedException(
                $"SP00-T02 is the Windows x64 proof and requires x64 Windows plus an x64 process; " +
                $"observed OS={RuntimeInformation.OSArchitecture}, process={RuntimeInformation.ProcessArchitecture}.");
        }

        if (!File.Exists(options.ManifestPath))
        {
            throw new FileNotFoundException("Terminal proof manifest was not found.", options.ManifestPath);
        }

        string dispatchPacketPath = ProofEnvironmentCollector.GetDispatchPacketPath(options.ManifestPath);
        if (!File.Exists(dispatchPacketPath))
        {
            throw new FileNotFoundException("Terminal proof dispatch packet was not found.", dispatchPacketPath);
        }

        if (!File.Exists(options.FixturePath))
        {
            throw new FileNotFoundException("Terminal proof fixture was not found.", options.FixturePath);
        }

        if (!File.Exists(options.CrashOwnerPath))
        {
            throw new FileNotFoundException("Terminal proof owner-crash executable was not found.", options.CrashOwnerPath);
        }

        if (!Directory.Exists(options.WorkingDirectory))
        {
            throw new DirectoryNotFoundException($"Terminal proof working directory does not exist: {options.WorkingDirectory}");
        }

        if (Directory.Exists(options.EvidenceDirectory)
            && Directory.EnumerateFileSystemEntries(options.EvidenceDirectory).Any())
        {
            throw new IOException($"The proof evidence directory must be empty: {options.EvidenceDirectory}");
        }

        Directory.CreateDirectory(options.EvidenceDirectory);
    }
}
