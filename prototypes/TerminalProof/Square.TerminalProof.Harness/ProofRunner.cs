using System.Diagnostics;
using System.Globalization;

namespace Square.TerminalProof.Harness;

internal sealed class ProofRunner
{
    private const int AcceptanceReliabilityRepetitions = 100;
    private const int WarmupRepetitions = 3;
    private const int DiagnosticRepetitions = 20;
    private const int DiagnosticCycles = 5;
    private readonly ProofOptions _options;
    private readonly ProofManifest _manifest;
    private readonly ProofEnvironmentEvidence _environment;
    private readonly ProofEvidenceWriter _evidence;
    private readonly ScenarioExecutor _executor;
    private DateTimeOffset? _lastSessionCompletedAt;

    internal ProofRunner(
        ProofOptions options,
        ProofManifest manifest,
        ProofEnvironmentEvidence environment,
        ProofEvidenceWriter evidence)
    {
        _options = options;
        _manifest = manifest;
        _environment = environment;
        _evidence = evidence;
        _executor = new ScenarioExecutor(manifest, options.FixturePath, options.WorkingDirectory);
    }

    internal async Task<ProofSummaryEvidence> ExecuteAsync(CancellationToken cancellationToken)
    {
        DateTimeOffset startedAtUtc = DateTimeOffset.UtcNow;
        string proofRunId = $"sp00-t02-{startedAtUtc:yyyyMMddTHHmmssZ}-{Guid.NewGuid():N}";
        List<SessionRunEvidence> warmupRuns = new();
        List<SessionRunEvidence> reliabilityRuns = new();
        List<SessionRunEvidence> scaleRuns = new();
        List<ScaleGroupEvidence> scaleGroups = new();
        List<HandleCheckpointEvidence> handleCheckpoints = new();
        List<string> globalFailures = new();
        List<string> limitations = BuildLimitations();
        bool stopEarly = false;

        Console.WriteLine($"SP00-T02 run {proofRunId}");

        // Cold owner-crash diagnostics (non-acceptance)
        if (!_options.Quick && !_options.SkipDiagnostics && !_options.SkipOwnerCrash)
        {
            Console.WriteLine("Owner-crash cold diagnostic: 20 probes");
            await RunOwnerCrashDiagnosticsAsync(
                "cold", DiagnosticRepetitions, handleCheckpoints, globalFailures, cancellationToken).ConfigureAwait(false);
        }

        Console.WriteLine($"Warm-up: normal_exit x {WarmupRepetitions}");
        for (int iteration = 1; iteration <= WarmupRepetitions; iteration++)
        {
            SessionRunEvidence warmup = await _executor.ExecuteAsync(
                "normal_exit",
                "warmup",
                iteration,
                concurrency: 1,
                sessionOrdinal: 1,
                proofRunId,
                cancellationToken).ConfigureAwait(false);
            warmupRuns.Add(warmup);
            await _evidence.AppendRunAsync(warmup, cancellationToken).ConfigureAwait(false);
            if (!warmup.Passed)
            {
                globalFailures.Add($"Warm-up run {iteration} failed: {warmup.Failure}");
                if (_options.FailFast)
                {
                    stopEarly = true;
                    break;
                }
            }
        }

        int baselineHandleCount = await StabilizeAndGetHandleCountAsync(cancellationToken).ConfigureAwait(false);
        handleCheckpoints.Add(CaptureExtendedCheckpoint("post-warmup-baseline", baselineHandleCount, baselineHandleCount));

        if (!stopEarly)
        {
            foreach (string scenario in _manifest.Scenarios)
            {
                Console.WriteLine($"Reliability: {scenario} x {_manifest.RepeatEach}");
                for (int iteration = 1; iteration <= _manifest.RepeatEach; iteration++)
                {
                    SessionRunEvidence run = await _executor.ExecuteAsync(
                        scenario,
                        "reliability",
                        iteration,
                        concurrency: 1,
                        sessionOrdinal: 1,
                        proofRunId,
                        cancellationToken).ConfigureAwait(false);
                    reliabilityRuns.Add(run);
                    await _evidence.AppendRunAsync(run, cancellationToken).ConfigureAwait(false);
                    WriteRunProgress(run, _manifest.RepeatEach);

                    if (!run.Passed && _options.FailFast)
                    {
                        globalFailures.Add(
                            $"Execution stopped after reliability failure in '{scenario}' iteration {iteration} " +
                            "because --fail-fast was set.");
                        stopEarly = true;
                        break;
                    }
                }

                _lastSessionCompletedAt = DateTimeOffset.UtcNow;
                HandleCheckpointEvidence checkpoint = await CaptureExtendedCheckpointAsync(
                    $"after-reliability-{scenario}",
                    baselineHandleCount,
                    cancellationToken).ConfigureAwait(false);
                handleCheckpoints.Add(checkpoint);
                RegisterHandleFailure(checkpoint, globalFailures);
                if (!checkpoint.WithinTolerance && _options.FailFast)
                {
                    stopEarly = true;
                }

                if (stopEarly)
                {
                    break;
                }
            }
        }

        if (!stopEarly && _manifest.ScaleRepeatEach > 0)
        {
            foreach (string scenario in _manifest.Scenarios)
            {
                foreach (int concurrency in _manifest.SessionCounts.Order())
                {
                    for (int iteration = 1; iteration <= _manifest.ScaleRepeatEach; iteration++)
                    {
                        Console.WriteLine(
                            $"Scale: {scenario} concurrency={concurrency} " +
                            $"group={iteration}/{_manifest.ScaleRepeatEach}");
                        int groupHandleBefore = GetCurrentHandleCount();
                        Stopwatch wallClock = Stopwatch.StartNew();
                        Task<SessionRunEvidence>[] pending = Enumerable.Range(1, concurrency)
                            .Select(sessionOrdinal => _executor.ExecuteAsync(
                                scenario,
                                "scale",
                                iteration,
                                concurrency,
                                sessionOrdinal,
                                proofRunId,
                                cancellationToken))
                            .ToArray();
                        SessionRunEvidence[] groupRuns = await Task.WhenAll(pending).ConfigureAwait(false);
                        wallClock.Stop();

                        foreach (SessionRunEvidence run in groupRuns.OrderBy(item => item.SessionOrdinal))
                        {
                            scaleRuns.Add(run);
                            await _evidence.AppendRunAsync(run, cancellationToken).ConfigureAwait(false);
                        }

                        int groupHandleAfter = await StabilizeAndGetHandleCountAsync(cancellationToken).ConfigureAwait(false);
                        ScaleGroupEvidence group = CreateScaleGroup(
                            scenario,
                            concurrency,
                            iteration,
                            wallClock.Elapsed,
                            groupHandleBefore,
                            groupHandleAfter,
                            groupRuns);
                        scaleGroups.Add(group);
                        Console.WriteLine(
                            $"  result: passed={group.Passed}/{group.Sessions}, " +
                            $"wall_ms={group.WallClockMilliseconds:F1}, " +
                            $"p95_first_output_ms={FormatNullable(group.P95FirstOutputLatencyMilliseconds)}, " +
                            $"handle_delta={group.HarnessHandleGrowth}");

                        if (group.Failed != 0 && _options.FailFast)
                        {
                            globalFailures.Add(
                                $"Execution stopped after scale failure in '{scenario}' at concurrency {concurrency} " +
                                "because --fail-fast was set.");
                            stopEarly = true;
                            break;
                        }
                    }

                    _lastSessionCompletedAt = DateTimeOffset.UtcNow;
                    HandleCheckpointEvidence checkpoint = await CaptureExtendedCheckpointAsync(
                        $"after-scale-{scenario}-x{concurrency}",
                        baselineHandleCount,
                        cancellationToken).ConfigureAwait(false);
                    handleCheckpoints.Add(checkpoint);
                    RegisterHandleFailure(checkpoint, globalFailures);
                    if (!checkpoint.WithinTolerance && _options.FailFast)
                    {
                        stopEarly = true;
                    }

                    if (stopEarly)
                    {
                        break;
                    }
                }

                if (stopEarly)
                {
                    break;
                }
            }
        }

        // Additional diagnostic handle-groth mode (non-acceptance, evidence only)
        if (!_options.Quick && !_options.SkipDiagnostics)
        {
            Console.WriteLine("Handle-growth diagnostic: 20-cycle eight-session normal_exit");
            await RunHandleGrowthDiagnosticAsync(baselineHandleCount, handleCheckpoints, globalFailures, cancellationToken).ConfigureAwait(false);

            Console.WriteLine("Handle-growth diagnostic: 5-cycle mixed scenario at concurrency 8");
            await RunMixDiagnosticAsync(baselineHandleCount, handleCheckpoints, globalFailures, cancellationToken).ConfigureAwait(false);

            Console.WriteLine("Owner-crash post-stress diagnostic: 20 probes");
            await RunOwnerCrashDiagnosticsAsync(
                "post-stress", DiagnosticRepetitions, handleCheckpoints, globalFailures, cancellationToken).ConfigureAwait(false);
        }

        // Quiescence checkpoints
        if (!stopEarly || !_options.FailFast)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken).ConfigureAwait(false);
            handleCheckpoints.Add(await CaptureExtendedCheckpointAsync(
                "quiescence-250ms", baselineHandleCount, cancellationToken).ConfigureAwait(false));

            await Task.Delay(TimeSpan.FromMilliseconds(1750), cancellationToken).ConfigureAwait(false);
            handleCheckpoints.Add(await CaptureExtendedCheckpointAsync(
                "quiescence-2s", baselineHandleCount, cancellationToken).ConfigureAwait(false));

            await Task.Delay(TimeSpan.FromMilliseconds(8000), cancellationToken).ConfigureAwait(false);
            handleCheckpoints.Add(await CaptureExtendedCheckpointAsync(
                "quiescence-10s", baselineHandleCount, cancellationToken).ConfigureAwait(false));
        }

        OwnerCrashProbeEvidence ownerCrashProbe;
        if (stopEarly && _options.FailFast)
        {
            ownerCrashProbe = new OwnerCrashProbeEvidence
            {
                Executed = false,
                Passed = false,
                Failure = "Owner-crash probe was not reached because --fail-fast stopped the run.",
                JobProcessIds = Array.Empty<int>(),
                SurvivingProcessIds = Array.Empty<int>(),
                LivePtyReattachSupported = false,
                ReattachConclusion = "Not measured because the run stopped before the owner-crash probe."
            };
        }
        else
        {
            Console.WriteLine("Owner-crash containment probe");
            ownerCrashProbe = await OwnerCrashProbe.ExecuteAsync(_options, _manifest, cancellationToken).ConfigureAwait(false);
        }

        // Extended final checkpoint with forced-GC distinction
        if (!stopEarly || !_options.FailFast)
        {
            int preGcHandles = GetCurrentHandleCount();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
            int postGcHandles = GetCurrentHandleCount();
            GC.WaitForPendingFinalizers();
            int postFinalizerHandles = GetCurrentHandleCount();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
            int postSecondGcHandles = GetCurrentHandleCount();
            await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken).ConfigureAwait(false);
            int postQuiescenceHandles = GetCurrentHandleCount();

            HandleCheckpointEvidence finalCheckpoint = CaptureExtendedCheckpoint("final", postQuiescenceHandles, baselineHandleCount);
            handleCheckpoints.Add(finalCheckpoint);
            RegisterHandleFailure(finalCheckpoint, globalFailures);
        }

        ValidateRunShape(reliabilityRuns, scaleGroups, globalFailures);
        if (ownerCrashProbe.Executed && !ownerCrashProbe.Passed)
        {
            globalFailures.Add($"Owner-crash containment probe failed: {ownerCrashProbe.Failure}");
        }
        else if (!ownerCrashProbe.Executed && !_options.SkipOwnerCrash)
        {
            globalFailures.Add("Owner-crash containment probe did not execute.");
        }

        int failedRuns = warmupRuns.Count(run => !run.Passed)
            + reliabilityRuns.Count(run => !run.Passed)
            + scaleRuns.Count(run => !run.Passed);
        bool technicalPass = failedRuns == 0 && globalFailures.Count == 0;
        bool acceptanceEligible = technicalPass && limitations.Count == 0;
        string status = technicalPass
            ? acceptanceEligible ? "PASS" : "DIAGNOSTIC_PASS"
            : "FAIL";

        return new ProofSummaryEvidence
        {
            SchemaVersion = "1.0",
            TaskId = "SP00-T02",
            RunId = proofRunId,
            StartedAtUtc = startedAtUtc,
            CompletedAtUtc = DateTimeOffset.UtcNow,
            Status = status,
            AcceptanceEligible = acceptanceEligible,
            Mode = ResolveMode(),
            EffectiveRepeatEach = _manifest.RepeatEach,
            EffectiveScaleRepeatEach = _manifest.ScaleRepeatEach,
            SessionCounts = _manifest.SessionCounts,
            Scenarios = _manifest.Scenarios,
            WarmupRuns = warmupRuns.Count,
            ReliabilityRuns = reliabilityRuns.Count,
            ScaleSessionRuns = scaleRuns.Count,
            FailedRuns = failedRuns,
            ScenarioSummaries = CreateScenarioSummaries(reliabilityRuns),
            ScaleGroups = scaleGroups,
            HandleCheckpoints = handleCheckpoints,
            OwnerCrashProbe = ownerCrashProbe,
            GlobalFailures = globalFailures,
            Limitations = limitations,
            EvidenceDirectory = _evidence.DirectoryPath
        };
    }

    private string ResolveMode()
    {
        if (_options.Quick)
        {
            return "quick";
        }

        return _options.ScenarioFilter is null ? "full" : "scenario";
    }

    private List<string> BuildLimitations()
    {
        List<string> limitations = new();
        if (!string.Equals(
                _environment.ManifestSha256,
                ProofSourceIdentity.CanonicalManifestSha256,
                StringComparison.Ordinal))
        {
            limitations.Add(
                $"The scenario manifest hash {_environment.ManifestSha256} does not match the checked-in " +
                $"acceptance manifest {ProofSourceIdentity.CanonicalManifestSha256}.");
        }

        if (!string.Equals(
                _environment.DispatchPacketSha256,
                ProofSourceIdentity.CanonicalDispatchPacketSha256,
                StringComparison.Ordinal))
        {
            limitations.Add(
                $"The dispatch packet hash {_environment.DispatchPacketSha256} does not match the checked-in " +
                $"task contract {ProofSourceIdentity.CanonicalDispatchPacketSha256}.");
        }

        if (_options.Quick)
        {
            limitations.Add("Quick mode reduces reliability repetitions to one and omits scale groups; it is development evidence only.");
        }

        if (_options.ScenarioFilter is not null)
        {
            limitations.Add($"Only scenario '{_options.ScenarioFilter}' was selected; the complete scenario set was not exercised.");
        }
        else if (!_manifest.HasCanonicalScenarioSet)
        {
            limitations.Add("The supplied manifest does not contain the complete canonical SP00-T02 scenario set in its declared order.");
        }

        if (_manifest.RepeatEach < AcceptanceReliabilityRepetitions)
        {
            limitations.Add(
                $"Reliability repetitions are {_manifest.RepeatEach}; SP00-T02 acceptance requires at least " +
                $"{AcceptanceReliabilityRepetitions} per scenario.");
        }

        if (_manifest.ScaleRepeatEach < 1)
        {
            limitations.Add("No 1/4/8-session scale groups were requested.");
        }

        if (_environment.IsElevated)
        {
            limitations.Add("The harness ran elevated; this does not prove the required normal per-user execution path.");
        }

        if (_options.SkipOwnerCrash)
        {
            limitations.Add("The owner-crash containment probe was skipped.");
        }

        return limitations;
    }

    private void ValidateRunShape(
        IReadOnlyList<SessionRunEvidence> reliabilityRuns,
        IReadOnlyList<ScaleGroupEvidence> scaleGroups,
        ICollection<string> failures)
    {
        foreach (string scenario in _manifest.Scenarios)
        {
            int executed = reliabilityRuns.Count(run => string.Equals(run.Scenario, scenario, StringComparison.Ordinal));
            if (executed != _manifest.RepeatEach)
            {
                failures.Add($"Reliability scenario '{scenario}' executed {executed} times; expected {_manifest.RepeatEach}.");
            }
        }

        if (_manifest.ScaleRepeatEach == 0)
        {
            return;
        }

        foreach (string scenario in _manifest.Scenarios)
        {
            foreach (int concurrency in _manifest.SessionCounts)
            {
                int groups = scaleGroups.Count(group =>
                    string.Equals(group.Scenario, scenario, StringComparison.Ordinal)
                    && group.Concurrency == concurrency);
                if (groups != _manifest.ScaleRepeatEach)
                {
                    failures.Add(
                        $"Scale scenario '{scenario}' x{concurrency} executed {groups} groups; " +
                        $"expected {_manifest.ScaleRepeatEach}.");
                }
            }
        }
    }

    private IReadOnlyList<ScenarioSummaryEvidence> CreateScenarioSummaries(
        IReadOnlyList<SessionRunEvidence> reliabilityRuns)
    {
        List<ScenarioSummaryEvidence> summaries = new();
        foreach (string scenario in _manifest.Scenarios)
        {
            SessionRunEvidence[] runs = reliabilityRuns
                .Where(run => string.Equals(run.Scenario, scenario, StringComparison.Ordinal))
                .ToArray();
            summaries.Add(new ScenarioSummaryEvidence(
                scenario,
                _manifest.RepeatEach,
                runs.Length,
                runs.Count(run => run.Passed),
                runs.Count(run => !run.Passed),
                runs.Sum(run => run.OutputBytes),
                runs.Aggregate(0UL, (total, run) => total + run.JobWriteTransferBytes),
                runs.Length == 0 ? 0 : runs.Average(run => run.DurationMilliseconds),
                Statistics.Percentile(runs.Select(run => run.FirstOutputLatencyMilliseconds), 0.95),
                runs.Length == 0 ? 0 : runs.Max(run => run.PeakWorkingSetBytes),
                runs.Length == 0 ? 0 : runs.Max(run => run.PeakActiveProcessCount),
                runs.Length == 0 ? 0 : runs.Max(run => run.PeakCombinedProcessHandleCount),
                runs.Sum(run => run.LeakedProcessIds.Count)));
        }

        return summaries;
    }

    private static ScaleGroupEvidence CreateScaleGroup(
        string scenario,
        int concurrency,
        int iteration,
        TimeSpan wallClock,
        int harnessHandleCountBefore,
        int harnessHandleCountAfter,
        IReadOnlyList<SessionRunEvidence> runs) => new(
            scenario,
            concurrency,
            iteration,
            runs.Count,
            runs.Count(run => run.Passed),
            runs.Count(run => !run.Passed),
            wallClock.TotalMilliseconds,
            runs.Sum(run => run.OutputBytes),
            runs.Aggregate(0UL, (total, run) => total + run.JobReadTransferBytes),
            runs.Aggregate(0UL, (total, run) => total + run.JobWriteTransferBytes),
            runs.Sum(run => run.JobCpuMilliseconds),
            runs.Sum(run => run.PeakWorkingSetBytes),
            runs.Count == 0 ? 0 : runs.Max(run => run.PeakWorkingSetBytes),
            runs.Count == 0 ? 0 : runs.Max(run => run.PeakActiveProcessCount),
            runs.Count == 0 ? 0 : runs.Max(run => run.PeakCombinedProcessHandleCount),
            Statistics.Percentile(runs.Select(run => run.FirstOutputLatencyMilliseconds), 0.50),
            Statistics.Percentile(runs.Select(run => run.FirstOutputLatencyMilliseconds), 0.95),
            harnessHandleCountBefore,
            harnessHandleCountAfter,
            harnessHandleCountAfter - harnessHandleCountBefore);

    private async Task<HandleCheckpointEvidence> CaptureExtendedCheckpointAsync(
        string name,
        int baseline,
        CancellationToken cancellationToken)
    {
        int handles = await StabilizeAndGetHandleCountAsync(cancellationToken).ConfigureAwait(false);
        return CaptureExtendedCheckpoint(name, handles, baseline);
    }

    private HandleCheckpointEvidence CaptureExtendedCheckpoint(string name, int handles, int baseline)
    {
        int growth = handles - baseline;
        return new HandleCheckpointEvidence(
            name,
            handles,
            growth,
            DateTimeOffset.UtcNow,
            growth <= _manifest.Settings.HandleGrowthTolerance,
            GetCurrentThreadCount(),
            ThreadPool.ThreadCount,
            ThreadPool.PendingWorkItemCount == -1 ? -1 : checked((int)ThreadPool.PendingWorkItemCount),
            ThreadPool.CompletedWorkItemCount == -1 ? -1 : checked((int)ThreadPool.CompletedWorkItemCount),
            GC.CollectionCount(0),
            GC.CollectionCount(1),
            GC.CollectionCount(2),
            GC.GetTotalMemory(forceFullCollection: false),
            _lastSessionCompletedAt is null ? 0 : (DateTimeOffset.UtcNow - _lastSessionCompletedAt.Value).TotalMilliseconds);
    }

    private void RegisterHandleFailure(HandleCheckpointEvidence checkpoint, ICollection<string> failures)
    {
        if (!checkpoint.WithinTolerance)
        {
            failures.Add(
                $"Handle checkpoint '{checkpoint.Name}' exceeded tolerance: " +
                $"growth={checkpoint.GrowthFromBaseline}, tolerance={_manifest.Settings.HandleGrowthTolerance}.");
        }
    }

    private static async Task<int> StabilizeAndGetHandleCountAsync(CancellationToken cancellationToken)
    {
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken).ConfigureAwait(false);
        return GetCurrentHandleCount();
    }

    private static int GetCurrentHandleCount()
    {
        using Process current = Process.GetCurrentProcess();
        current.Refresh();
        return current.HandleCount;
    }

    private static int GetCurrentThreadCount()
    {
        using Process current = Process.GetCurrentProcess();
        current.Refresh();
        return current.Threads.Count;
    }

    private async Task RunHandleGrowthDiagnosticAsync(
        int baselineHandleCount,
        List<HandleCheckpointEvidence> checkpoints,
        List<string> failures,
        CancellationToken cancellationToken)
    {
        // Warm once
        await _executor.ExecuteAsync("normal_exit", "diagnostic-warmup", 0, concurrency: 1, sessionOrdinal: 1, "diagnostic", cancellationToken).ConfigureAwait(false);
        await StabilizeAndGetHandleCountAsync(cancellationToken).ConfigureAwait(false);

        int prevHandles = GetCurrentHandleCount();
        string classification = "NO_GROWTH";

        for (int round = 1; round <= DiagnosticRepetitions; round++)
        {
            Task<SessionRunEvidence>[] pending = Enumerable.Range(1, 8)
                .Select(sessionOrdinal => _executor.ExecuteAsync(
                    "normal_exit", "handle-diag", round, concurrency: 8, sessionOrdinal,
                    "diagnostic", cancellationToken))
                .ToArray();
            await Task.WhenAll(pending).ConfigureAwait(false);

            int afterHandles = await StabilizeAndGetHandleCountAsync(cancellationToken).ConfigureAwait(false);
            int roundGrowth = afterHandles - prevHandles;

            checkpoints.Add(CaptureExtendedCheckpoint(
                $"handle-diag-normal_exit-x8-round{round}", afterHandles, baselineHandleCount));

            if (round == 1 && roundGrowth > 0)
                classification = "PLATEAU";
            if (round > 1 && roundGrowth > 2)
                classification = "LINEAR_GROWTH";

            prevHandles = afterHandles;
        }

        Console.WriteLine($"Handle-growth normal_exit x8 classification: {classification}");
    }

    private async Task RunMixDiagnosticAsync(
        int baselineHandleCount,
        List<HandleCheckpointEvidence> checkpoints,
        List<string> failures,
        CancellationToken cancellationToken)
    {
        string[] mixScenarios = { "normal_exit", "large_burst", "graceful_cancel", "forced_termination", "nested_children" };
        string mixClassification = "NO_GROWTH";
        int prevHandles = GetCurrentHandleCount();

        for (int cycle = 1; cycle <= DiagnosticCycles; cycle++)
        {
            foreach (string scenario in mixScenarios)
            {
                Task<SessionRunEvidence>[] pending = Enumerable.Range(1, 8)
                    .Select(sessionOrdinal => _executor.ExecuteAsync(
                        scenario, "mix-diag", cycle, concurrency: 8, sessionOrdinal,
                        "diagnostic", cancellationToken))
                    .ToArray();
                await Task.WhenAll(pending).ConfigureAwait(false);
            }

            int afterHandles = await StabilizeAndGetHandleCountAsync(cancellationToken).ConfigureAwait(false);
            int cycleGrowth = afterHandles - prevHandles;

            checkpoints.Add(CaptureExtendedCheckpoint(
                $"handle-diag-mix-cycle{cycle}", afterHandles, baselineHandleCount));

            if (cycle == 1 && cycleGrowth > 0)
                mixClassification = "PLATEAU";
            if (cycle > 1 && cycleGrowth > 2)
                mixClassification = "LINEAR_GROWTH";

            prevHandles = afterHandles;
        }

        Console.WriteLine($"Handle-growth mix x8 classification: {mixClassification}");
    }

    private async Task RunOwnerCrashDiagnosticsAsync(
        string phase,
        int repetitions,
        List<HandleCheckpointEvidence> checkpoints,
        List<string> failures,
        CancellationToken cancellationToken)
    {
        string diagnosticDir = Path.Combine(_evidence.DirectoryPath, $"owner-crash-diag-{phase}");
        Directory.CreateDirectory(diagnosticDir);
        ProofOptions diagOptions = _options with { EvidenceDirectory = diagnosticDir };

        int passed = 0;
        int failed = 0;
        for (int i = 1; i <= repetitions; i++)
        {
            OwnerCrashProbeEvidence probe = await OwnerCrashProbe.ExecuteAsync(
                diagOptions, _manifest, cancellationToken).ConfigureAwait(false);
            if (probe.Passed) passed++; else failed++;
            if (!probe.Passed)
            {
                failures.Add($"Owner-crash {phase} probe {i}/{repetitions} FAILED stage={probe.FailureStage}: {probe.Failure}");
            }
        }
        Console.WriteLine($"Owner-crash {phase} diagnostic: passed={passed}/{repetitions}, failed={failed}/{repetitions}");
    }

    private static void WriteRunProgress(SessionRunEvidence run, int total)
    {
        if (!run.Passed || run.Iteration == 1 || run.Iteration == total || run.Iteration % 10 == 0)
        {
            Console.WriteLine(
                $"  {run.Scenario} {run.Iteration}/{total}: " +
                $"{(run.Passed ? "PASS" : "FAIL")}, duration_ms={run.DurationMilliseconds:F1}, " +
                $"output_bytes={run.OutputBytes}, handles={run.HarnessHandleCountBefore}->{run.HarnessHandleCountAfter}");
        }
    }

    private static string FormatNullable(double? value) =>
        value is null ? "n/a" : value.Value.ToString("F1", CultureInfo.InvariantCulture);
}
