using System.Diagnostics;
using System.Text.Json;

namespace Square.TerminalProof.Harness;

internal static class OwnerCrashProbe
{
    internal static async Task<OwnerCrashProbeEvidence> ExecuteAsync(
        ProofOptions options,
        ProofManifest manifest,
        CancellationToken cancellationToken)
    {
        if (options.SkipOwnerCrash)
        {
            return CreateSkipped();
        }

        string readyFile = Path.Combine(options.EvidenceDirectory, "owner-crash-ready.json");
        if (File.Exists(readyFile))
        {
            File.Delete(readyFile);
        }

        Stopwatch totalTimer = Stopwatch.StartNew();
        string? firstException = null;
        string? lastException = null;
        string? ownerStdout = null;
        string? ownerStderr = null;
        string? readyFileSha256 = null;
        bool parentKillUsed = false;
        double? processStartReturnMs = null;
        double? readyFileObserveMs = null;
        double? ownerExitMs = null;
        double? descendantEmptyMs = null;
        OwnerCrashFailureStage stage = OwnerCrashFailureStage.Unknown;

        ProcessStartInfo startInfo = new(options.CrashOwnerPath)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = options.WorkingDirectory
        };
        startInfo.ArgumentList.Add("--fixture");
        startInfo.ArgumentList.Add(options.FixturePath);
        startInfo.ArgumentList.Add("--ready-file");
        startInfo.ArgumentList.Add(readyFile);
        startInfo.ArgumentList.Add("--working-directory");
        startInfo.ArgumentList.Add(options.WorkingDirectory);
        startInfo.ArgumentList.Add("--child-count");
        startInfo.ArgumentList.Add(manifest.Settings.NestedChildCount.ToString(System.Globalization.CultureInfo.InvariantCulture));
        startInfo.ArgumentList.Add("--timeout-ms");
        startInfo.ArgumentList.Add(manifest.Settings.DefaultTimeoutMilliseconds.ToString(System.Globalization.CultureInfo.InvariantCulture));

        Process? owner = null;
        CrashOwnerReady? ready = null;

        try
        {
            stage = OwnerCrashFailureStage.ProcessStart;
            Stopwatch startTimer = Stopwatch.StartNew();
            owner = Process.Start(startInfo);
            processStartReturnMs = startTimer.Elapsed.TotalMilliseconds;
            if (owner is null)
            {
                return Failure("Failed to start the owner-crash probe executable.", OwnerCrashFailureStage.ProcessStart);
            }

            Task<string> standardOutput = owner.StandardOutput.ReadToEndAsync(cancellationToken);
            Task<string> standardError = owner.StandardError.ReadToEndAsync(cancellationToken);

            stage = OwnerCrashFailureStage.ReadyFileObserve;
            Stopwatch readyTimer = Stopwatch.StartNew();
            try
            {
                ready = await WaitForReadyFileAsync(readyFile, manifest.Settings.DefaultTimeout, cancellationToken).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                firstException = "Timeout waiting for ready file.";
                lastException = firstException;
                throw;
            }
            readyFileObserveMs = readyTimer.Elapsed.TotalMilliseconds;

            if (File.Exists(readyFile))
            {
                try
                {
                    readyFileSha256 = Hashing.Sha256(await File.ReadAllBytesAsync(readyFile, cancellationToken).ConfigureAwait(false));
                }
                catch { }
            }

            stage = OwnerCrashFailureStage.OwnerTermination;
            Stopwatch ownerExitTimer = Stopwatch.StartNew();
            await owner.WaitForExitAsync(cancellationToken)
                .WaitAsync(manifest.Settings.DefaultTimeout, cancellationToken).ConfigureAwait(false);
            ownerExitMs = ownerExitTimer.Elapsed.TotalMilliseconds;
            ownerStdout = await standardOutput.ConfigureAwait(false);
            ownerStderr = await standardError.ConfigureAwait(false);

            stage = OwnerCrashFailureStage.DescendantTermination;
            Stopwatch descTimer = Stopwatch.StartNew();
            IReadOnlyList<int> survivors = await WaitForProcessesToExitAsync(
                ready.JobProcesses,
                manifest.Settings.DescendantExitTimeout,
                cancellationToken).ConfigureAwait(false);
            descendantEmptyMs = descTimer.Elapsed.TotalMilliseconds;

            stage = OwnerCrashFailureStage.IdentityValidation;
            bool passed = owner.ExitCode != 0 && survivors.Count == 0;

            return new OwnerCrashProbeEvidence
            {
                Executed = true,
                Passed = passed,
                Failure = passed ? null
                    : $"Owner exit={owner.ExitCode}; survivors=[{string.Join(", ", survivors)}]; stdout_excerpt={Truncate(ownerStdout, 256)}; stderr_excerpt={Truncate(ownerStderr, 256)}",
                FailureStage = passed ? OwnerCrashFailureStage.Unknown : stage,
                OwnerProcessId = ready.OwnerProcessId,
                OwnerExitCode = owner.ExitCode,
                RootProcessId = ready.RootProcessId,
                JobProcessIds = ready.JobProcesses.Select(process => process.ProcessId).Order().ToArray(),
                SurvivingProcessIds = survivors,
                ProcessStartReturnMilliseconds = processStartReturnMs,
                ReadyFileObservationMilliseconds = readyFileObserveMs,
                OwnerExitMilliseconds = ownerExitMs,
                DescendantEmptyingMilliseconds = descendantEmptyMs,
                TotalProbeDurationMilliseconds = totalTimer.Elapsed.TotalMilliseconds,
                OwnerStdout = Truncate(ownerStdout, 1024),
                OwnerStderr = Truncate(ownerStderr, 1024),
                ReadyFileSha256 = readyFileSha256,
                ParentKillUsed = parentKillUsed,
                FirstException = firstException,
                LastException = lastException,
                LivePtyReattachSupported = false,
                ReattachConclusion = "This proof architecture keeps the ConPTY, anonymous pipe, process, and unnamed Job Object handles only in the owner process and provides no surviving broker or transferable handle. After owner failure, KILL_ON_JOB_CLOSE must terminate the observed process tree; restart recovery must reconcile durable evidence and report a lost/terminated terminal rather than claim live PTY reattachment."
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            lastException = exception.ToString();
            firstException ??= lastException;

            if (owner is not null)
            {
                try
                {
                    if (!owner.HasExited)
                    {
                        owner.Kill(entireProcessTree: true);
                        parentKillUsed = true;
                        await owner.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
                catch { }
            }

            return new OwnerCrashProbeEvidence
            {
                Executed = true,
                Passed = false,
                Failure = exception.ToString(),
                FailureStage = stage,
                OwnerProcessId = ready?.OwnerProcessId,
                OwnerExitCode = owner?.HasExited == true ? owner.ExitCode : null,
                RootProcessId = ready?.RootProcessId,
                JobProcessIds = ready?.JobProcesses.Select(process => process.ProcessId).Order().ToArray()
                    ?? Array.Empty<int>(),
                SurvivingProcessIds = ready is null
                    ? Array.Empty<int>()
                    : ready.JobProcesses.Where(IsSameProcessRunning).Select(process => process.ProcessId).Order().ToArray(),
                ProcessStartReturnMilliseconds = processStartReturnMs,
                ReadyFileObservationMilliseconds = readyFileObserveMs,
                OwnerExitMilliseconds = ownerExitMs,
                DescendantEmptyingMilliseconds = descendantEmptyMs,
                TotalProbeDurationMilliseconds = totalTimer.Elapsed.TotalMilliseconds,
                OwnerStdout = Truncate(ownerStdout, 1024),
                OwnerStderr = Truncate(ownerStderr, 1024),
                ReadyFileSha256 = readyFileSha256,
                ParentKillUsed = parentKillUsed,
                FirstException = firstException,
                LastException = lastException,
                LivePtyReattachSupported = false,
                ReattachConclusion = "Probe failed before a complete reattachment/cleanup conclusion could be accepted."
            };
        }
    }

    private static async Task<CrashOwnerReady> WaitForReadyFileAsync(
        string path,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        while (!File.Exists(path))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (stopwatch.Elapsed >= timeout)
            {
                throw new TimeoutException($"Owner-crash probe did not create '{path}' within {timeout}.");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(20), cancellationToken).ConfigureAwait(false);
        }

        string json = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
        CrashOwnerReady ready = JsonSerializer.Deserialize<CrashOwnerReady>(json, ProofJson.Create())
            ?? throw new InvalidDataException("Owner-crash ready evidence deserialized to null.");
        ValidateReadyEvidence(ready);
        return ready;
    }

    private static async Task<IReadOnlyList<int>> WaitForProcessesToExitAsync(
        IReadOnlyList<CrashProcessIdentity> processes,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        while (true)
        {
            IReadOnlyList<int> survivors = processes
                .Where(IsSameProcessRunning)
                .Select(process => process.ProcessId)
                .Distinct()
                .Order()
                .ToArray();
            if (survivors.Count == 0 || stopwatch.Elapsed >= timeout)
            {
                return survivors;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(25), cancellationToken).ConfigureAwait(false);
        }
    }

    private static bool IsSameProcessRunning(CrashProcessIdentity identity)
    {
        try
        {
            using Process process = Process.GetProcessById(identity.ProcessId);
            process.Refresh();
            return !process.HasExited
                && process.StartTime.ToUniversalTime().Ticks == identity.StartTimeUtcTicks;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return true;
        }
    }

    private static void ValidateReadyEvidence(CrashOwnerReady ready)
    {
        if (!string.Equals(ready.SchemaVersion, "1.0", StringComparison.Ordinal))
        {
            throw new InvalidDataException($"Unsupported owner-crash evidence schema '{ready.SchemaVersion}'.");
        }

        if (ready.OwnerProcessId < 1 || ready.RootProcessId < 1 || ready.JobProcesses.Count < 2)
        {
            throw new InvalidDataException("Owner-crash evidence is missing a valid owner, root, or nested process set.");
        }

        if (!ready.JobProcesses.Any(process => process.ProcessId == ready.RootProcessId)
            || ready.JobProcesses.Any(process => process.ProcessId < 1 || process.StartTimeUtcTicks < 1)
            || ready.JobProcesses.Select(process => process.ProcessId).Distinct().Count() != ready.JobProcesses.Count)
        {
            throw new InvalidDataException("Owner-crash process identities are invalid, duplicated, or omit the root process.");
        }
    }

    private static OwnerCrashProbeEvidence CreateSkipped() => new()
    {
        Executed = false,
        Passed = false,
        Failure = "Owner-crash probe was explicitly skipped; SP00-T02 cannot be accepted from this run.",
        JobProcessIds = Array.Empty<int>(),
        SurvivingProcessIds = Array.Empty<int>(),
        LivePtyReattachSupported = false,
        ReattachConclusion = "Not measured because the owner-crash probe was skipped."
    };

    private static OwnerCrashProbeEvidence Failure(string message, OwnerCrashFailureStage stage) => new()
    {
        Executed = true,
        Passed = false,
        Failure = message,
        FailureStage = stage,
        JobProcessIds = Array.Empty<int>(),
        SurvivingProcessIds = Array.Empty<int>(),
        LivePtyReattachSupported = false,
        ReattachConclusion = "Probe did not reach the point where reattachment behavior could be measured."
    };

    private static string? Truncate(string? value, int maxLength) =>
        value is null || value.Length <= maxLength ? value : value[..maxLength] + "…";

    private sealed record CrashOwnerReady(
        string SchemaVersion,
        int OwnerProcessId,
        int RootProcessId,
        IReadOnlyList<CrashProcessIdentity> JobProcesses,
        DateTimeOffset ReadyAtUtc);

    private sealed record CrashProcessIdentity(int ProcessId, long StartTimeUtcTicks);
}