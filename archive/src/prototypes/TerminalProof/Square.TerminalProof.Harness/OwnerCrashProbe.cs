using System.Diagnostics;
using System.Text.Json;
using Square.TerminalProof.Native;

namespace Square.TerminalProof.Harness;

/// <summary>
/// Deterministic parent-side completion sequence for the owner-crash probe.
///
/// The crash-owner is launched WITHOUT .NET output redirection. Redirected StandardOutput /
/// StandardError keep an AsyncStreamReader pipe plus a blocking read thread alive in this
/// process, and on this runtime the pair is not reclaimed even across forced GC and a 10s
/// quiescence (measured ~+8 handles and +1 thread per probe). The crash-owner communicates
/// exclusively through its atomically-published ready file, so no probe information is lost
/// by disabling redirection; the parent holds zero pipe or reader handles for the probe.
///
/// Completion order: start process; publish/observe ready file; owner self-terminates;
/// wait for exit; verify exit code; verify every exact descendant identity vanished;
/// dispose the Process; the probe scope counter is released. An owned-resource counter
/// snapshot is captured at scope exit and is verified zero by the caller.
/// </summary>
internal static class OwnerCrashProbe
{
    private const string ReadyFileName = "owner-crash-ready.json";

    internal static async Task<OwnerCrashProbeEvidence> ExecuteAsync(
        ProofOptions options,
        ProofManifest manifest,
        CancellationToken cancellationToken)
    {
        if (options.SkipOwnerCrash)
        {
            return CreateSkipped();
        }

        string readyFile = Path.Combine(options.EvidenceDirectory, ReadyFileName);
        if (File.Exists(readyFile))
        {
            File.Delete(readyFile);
        }

        Stopwatch totalTimer = Stopwatch.StartNew();
        string? firstException = null;
        string? lastException = null;
        string? readyFileSha256 = null;
        bool parentKillUsed = false;
        double? processStartReturnMs = null;
        double? readyFileObserveMs = null;
        double? ownerExitMs = null;
        double? descendantEmptyMs = null;
        OwnerCrashFailureStage stage = OwnerCrashFailureStage.Unknown;
        CrashOwnerReady? ready = null;
        int? ownerExitCode = null;

        ProcessStartInfo startInfo = BuildStartInfo(options, manifest, readyFile);

        OwnedResourceCounters.Increment(OwnedResourceKind.OwnerCrashTimeoutScope);
        Process? owner = null;
        OwnerCrashProbeEvidence evidence = CreateSkipped();

        try
        {
            stage = OwnerCrashFailureStage.ProcessStart;
            Stopwatch startTimer = Stopwatch.StartNew();
            owner = Process.Start(startInfo);
            processStartReturnMs = startTimer.Elapsed.TotalMilliseconds;
            if (owner is null)
            {
                evidence = Failure("Failed to start the owner-crash probe executable.", OwnerCrashFailureStage.ProcessStart);
            }
            else
            {
                OwnedResourceCounters.Increment(OwnedResourceKind.OwnerCrashProcess);

                stage = OwnerCrashFailureStage.ReadyFileObserve;
                Stopwatch readyTimer = Stopwatch.StartNew();
                string readyText = await ReadyFile.ReadValidatedAsync(
                    readyFile,
                    manifest.Settings.DefaultTimeout,
                    validate: text => ready = ParseAndValidateReady(text),
                    cancellationToken).ConfigureAwait(false);
                readyFileObserveMs = readyTimer.Elapsed.TotalMilliseconds;
                if (string.IsNullOrEmpty(readyText))
                {
                    throw new InvalidDataException("Owner-crash ready evidence read as empty stable content.");
                }

                try
                {
                    readyFileSha256 = Hashing.Sha256(await File.ReadAllBytesAsync(readyFile, cancellationToken).ConfigureAwait(false));
                }
                catch
                {
                }

                ready = ready ?? throw new InvalidDataException("Owner-crash ready evidence was not parsed.");

                stage = OwnerCrashFailureStage.OwnerTermination;
                Stopwatch ownerExitTimer = Stopwatch.StartNew();
                await owner.WaitForExitAsync(cancellationToken)
                    .WaitAsync(manifest.Settings.DefaultTimeout, cancellationToken)
                    .ConfigureAwait(false);
                ownerExitMs = ownerExitTimer.Elapsed.TotalMilliseconds;
                ownerExitCode = owner.ExitCode;

                stage = OwnerCrashFailureStage.DescendantTermination;
                Stopwatch descTimer = Stopwatch.StartNew();
                IReadOnlyList<int> survivors = await WaitForProcessesToExitAsync(
                    ready.JobProcesses,
                    manifest.Settings.DescendantExitTimeout,
                    cancellationToken).ConfigureAwait(false);
                descendantEmptyMs = descTimer.Elapsed.TotalMilliseconds;

                stage = OwnerCrashFailureStage.IdentityValidation;
                bool passed = ownerExitCode != 0 && survivors.Count == 0;

                evidence = new OwnerCrashProbeEvidence
                {
                    Executed = true,
                    Passed = passed,
                    Failure = passed ? null
                        : $"Owner exit={ownerExitCode}; survivors=[{string.Join(", ", survivors)}]",
                    FailureStage = passed ? OwnerCrashFailureStage.Unknown : stage,
                    OwnerProcessId = ready.OwnerProcessId,
                    OwnerExitCode = ownerExitCode,
                    RootProcessId = ready.RootProcessId,
                    JobProcessIds = ready.JobProcesses.Select(process => process.ProcessId).Order().ToArray(),
                    SurvivingProcessIds = survivors,
                    ProcessStartReturnMilliseconds = processStartReturnMs,
                    ReadyFileObservationMilliseconds = readyFileObserveMs,
                    OwnerExitMilliseconds = ownerExitMs,
                    DescendantEmptyingMilliseconds = descendantEmptyMs,
                    TotalProbeDurationMilliseconds = totalTimer.Elapsed.TotalMilliseconds,
                    ReadyFileSha256 = readyFileSha256,
                    ReadyFilePath = readyFile,
                    ReadyFileSharingMode = ReadyFile.FinalShareMode.ToString(),
                    ParentKillUsed = parentKillUsed,
                    FirstException = firstException,
                    LastException = lastException,
                    LivePtyReattachSupported = false,
                    ReattachConclusion = "This proof architecture keeps the ConPTY, anonymous pipe, process, and unnamed Job Object handles only in the owner process and provides no surviving broker or transferable handle. After owner failure, KILL_ON_JOB_CLOSE must terminate the observed process tree; restart recovery must reconcile durable evidence and report a lost/terminated terminal rather than claim live PTY reattachment."
                };
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            lastException = exception.ToString();
            firstException ??= lastException;

            if (owner is not null && !owner.HasExited)
            {
                try
                {
                    owner.Kill(entireProcessTree: true);
                    parentKillUsed = true;
                    await owner.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                }
            }

            evidence = new OwnerCrashProbeEvidence
            {
                Executed = true,
                Passed = false,
                Failure = exception.ToString(),
                FailureStage = stage,
                OwnerProcessId = ready?.OwnerProcessId,
                OwnerExitCode = ownerExitCode ?? (owner?.HasExited == true ? owner.ExitCode : null),
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
                ReadyFileSha256 = readyFileSha256,
                ReadyFilePath = readyFile,
                ReadyFileSharingMode = ReadyFile.FinalShareMode.ToString(),
                ParentKillUsed = parentKillUsed,
                FirstException = firstException,
                LastException = lastException,
                LivePtyReattachSupported = false,
                ReattachConclusion = "Probe failed before a complete reattachment/cleanup conclusion could be accepted."
            };
        }
        finally
        {
            if (owner is not null)
            {
                owner.Dispose();
                OwnedResourceCounters.Decrement(OwnedResourceKind.OwnerCrashProcess);
            }

            OwnedResourceCounters.Decrement(OwnedResourceKind.OwnerCrashTimeoutScope);
        }

        return evidence with { OwnedResourceCounts = OwnedResourceCounters.Snapshot() };
    }

    private static ProcessStartInfo BuildStartInfo(ProofOptions options, ProofManifest manifest, string readyFile)
    {
        // No RedirectStandardOutput/RedirectStandardError: .NET keeps per-stream reader pipes and
        // threads alive in the parent on this runtime, and the crash-owner publishes its evidence
        // exclusively through the atomic ready file. Null child handles mean no parent stream
        // can capture crash-owner output.
        ProcessStartInfo startInfo = new(options.CrashOwnerPath)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
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
        return startInfo;
    }

    private static CrashOwnerReady ParseAndValidateReady(string json)
    {
        CrashOwnerReady parsed = JsonSerializer.Deserialize<CrashOwnerReady>(json, ProofJson.Create())
            ?? throw new InvalidDataException("Owner-crash ready evidence deserialized to null.");
        ValidateReadyEvidence(parsed);
        return parsed;
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

    private sealed record CrashOwnerReady(
        string SchemaVersion,
        int OwnerProcessId,
        int RootProcessId,
        IReadOnlyList<CrashProcessIdentity> JobProcesses,
        DateTimeOffset ReadyAtUtc);

    private sealed record CrashProcessIdentity(int ProcessId, long StartTimeUtcTicks);
}
