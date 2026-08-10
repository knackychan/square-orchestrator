using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Square.TerminalProof.Native;

namespace Square.TerminalProof.Harness;

internal sealed class ProofRunner
{
    private const int AcceptanceReliabilityRepetitions = 100;
    private const int WarmupRepetitions = 3;
    private const int DiagnosticRepetitions = 20;
    private const int DiagnosticCycles = 5;

    private static readonly string[] MixScenarios = { "normal_exit", "large_burst", "graceful_cancel", "forced_termination", "nested_children" };

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
        if (_options.DiagOwnerCrash)
        {
            return await RunOwnerCrashDiagnosticProcessAsync(cancellationToken).ConfigureAwait(false);
        }

        if (_options.DiagHandleGrowth)
        {
            return await RunHandleGrowthDiagnosticProcessAsync(cancellationToken).ConfigureAwait(false);
        }

        if (_options.DiagIsolation)
        {
            return await RunIsolationDiagnosticProcessAsync(cancellationToken).ConfigureAwait(false);
        }

        return await RunCanonicalAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<ProofSummaryEvidence> RunCanonicalAsync(CancellationToken cancellationToken)
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

        Console.WriteLine($"SP00-T02 canonical run {proofRunId} pid={_environment.ProcessId} started_utc={_environment.ProcessStartUtc:O}");

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
        handleCheckpoints.Add(CaptureExtendedCheckpoint("post-warmup-baseline", CheckpointPhase.PostGc, baselineHandleCount, baselineHandleCount));

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
                    CheckpointPhase.PostGc,
                    baselineHandleCount,
                    cancellationToken).ConfigureAwait(false);
                handleCheckpoints.Add(checkpoint);
                RegisterCheckpointFailures(checkpoint, globalFailures);
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
                    await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken).ConfigureAwait(false);
                    HandleCheckpointEvidence checkpoint = await CaptureExtendedCheckpointAsync(
                        $"after-scale-{scenario}-x{concurrency}",
                        CheckpointPhase.PostGc,
                        baselineHandleCount,
                        cancellationToken).ConfigureAwait(false);
                    handleCheckpoints.Add(checkpoint);
                    RegisterCheckpointFailures(checkpoint, globalFailures);
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

        // Quiescence checkpoints before the canonical final checkpoint.
        if (!stopEarly || !_options.FailFast)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken).ConfigureAwait(false);
            handleCheckpoints.Add(await CaptureExtendedCheckpointAsync(
                "quiescence-250ms", CheckpointPhase.Quiescent250Ms, baselineHandleCount, cancellationToken).ConfigureAwait(false));

            await Task.Delay(TimeSpan.FromMilliseconds(1750), cancellationToken).ConfigureAwait(false);
            handleCheckpoints.Add(await CaptureExtendedCheckpointAsync(
                "quiescence-2s", CheckpointPhase.Quiescent2S, baselineHandleCount, cancellationToken).ConfigureAwait(false));

            await Task.Delay(TimeSpan.FromMilliseconds(8000), cancellationToken).ConfigureAwait(false);
            handleCheckpoints.Add(await CaptureExtendedCheckpointAsync(
                "quiescence-10s", CheckpointPhase.Quiescent10S, baselineHandleCount, cancellationToken).ConfigureAwait(false));
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
            if (!ownerCrashProbe.OwnedResourcesZero)
            {
                string retained = DescribeRetainedResources(ownerCrashProbe);
                globalFailures.Add($"Owner-crash containment probe left owned TerminalProof resources after disposal: {retained}.");
                ownerCrashProbe = ownerCrashProbe with
                {
                    Passed = false,
                    Failure = $"Owned-resource retention after probe disposal: {retained}"
                };
            }
        }

        // Extended final checkpoint with forced-GC distinction.
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

            HandleCheckpointEvidence finalCheckpoint = CaptureExtendedCheckpoint("final-canonical", CheckpointPhase.Final, postQuiescenceHandles, baselineHandleCount);
            handleCheckpoints.Add(finalCheckpoint);
            RegisterCheckpointFailures(finalCheckpoint, globalFailures);
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

        List<HandleGrowthClassificationEvidence> classifications = new();
        HandleCheckpointEvidence[] canonicalStablePoints = handleCheckpoints
            .Where(checkpoint => checkpoint.Name is "quiescence-250ms" or "quiescence-2s" or "quiescence-10s" or "final-canonical")
            .OrderBy(checkpoint => checkpoint.CapturedAtUtc)
            .ToArray();
        IReadOnlyList<int> canonicalStable = canonicalStablePoints.Select(checkpoint => checkpoint.HandleCount).ToArray();
        string[] stablePhases = canonicalStablePoints.Select(checkpoint => checkpoint.Name).ToArray();
        if (canonicalStable.Count >= 3)
        {
            classifications.Add(HandleGrowthClassifier.Classify("canonical-stable", canonicalStable));
        }

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
            EvidenceDirectory = _evidence.DirectoryPath,
            ProcessId = _environment.ProcessId,
            ProcessStartUtcTicks = _environment.ProcessStartUtcTicks,
            BaselineCheckpointName = "post-warmup-baseline",
            StableCheckpointPolicy =
                "Acceptance keeps the checked-in rule: Process.HandleCount growth_from_baseline must be <= "
                + HandleGrowthToleranceForDisplay() + " at every named handle checkpoint. "
                + "Checkpoints are additionally phase-labelled (ACTIVE/IMMEDIATE_POST_DISPOSAL/POST_GC/QUIESCENT_*/FINAL). "
                + "An ACTIVE or immediate post-disposal peak is not called a leak and is not removed from evidence. "
                + "Owned TerminalProof resource counters must be zero at every checkpoint and after every session and owner-crash probe.",
            HandleGrowthTolerance = _manifest.Settings.HandleGrowthTolerance,
            DiagnosticsProcessSeparated = true,
            HandleGrowthClassifications = classifications,
            DiagnosticProcesses = LoadDiagnosticsReport(_options.DiagnosticsReport),
            StableCheckpointNames = stablePhases
        };
    }

    private async Task<ProofSummaryEvidence> RunOwnerCrashDiagnosticProcessAsync(CancellationToken cancellationToken)
    {
        DateTimeOffset startedAtUtc = DateTimeOffset.UtcNow;
        string runId = $"sp00-t02-diag-owner-crash-{startedAtUtc:yyyyMMddTHHmmssZ}-{Guid.NewGuid():N}";
        List<string> globalFailures = new();
        List<string> limitations = BuildLimitations();
        limitations.Add("Owner-crash diagnostic process: repeated probes and retention reproducer are development evidence, not canonical acceptance.");
        List<HandleCheckpointEvidence> checkpoints = new();
        OwnerCrashProbeEvidence lastProbe = CreateSkippedProbe("No owner-crash probe executed in the owner-crash diagnostic process.");

        Console.WriteLine($"SP00-T02 owner-crash diagnostic run {runId} pid={_environment.ProcessId}");

        Console.WriteLine("Diagnostic warm-up: normal_exit x 1");
        await _executor.ExecuteAsync("normal_exit", "diagnostic-warmup", 0, concurrency: 1, sessionOrdinal: 1, runId, cancellationToken).ConfigureAwait(false);
        int baseline = await StabilizeAndGetHandleCountAsync(cancellationToken).ConfigureAwait(false);
        checkpoints.Add(CaptureExtendedCheckpoint("diag-baseline", CheckpointPhase.PostGc, baseline, baseline));

        Console.WriteLine("Owner-crash retention reproducer: single probe, staged checkpoints");
        await RunRetentionReproducerAsync(checkpoints, baseline, cancellationToken).ConfigureAwait(false);

        Console.WriteLine($"Owner-crash cold diagnostic: {DiagnosticRepetitions} probes");
        IReadOnlyList<OwnerCrashProbeEvidence> cold = await RunProbeSeriesAsync(
            "cold", DiagnosticRepetitions, baseline, checkpoints, globalFailures, cancellationToken).ConfigureAwait(false);
        lastProbe = cold.Count == 0 ? lastProbe : cold[^1];

        Console.WriteLine("Eight-session terminal stress cycle");
        await RunStressCycleAsync(runId, cancellationToken).ConfigureAwait(false);
        int stressBaseline = await StabilizeAndGetHandleCountAsync(cancellationToken).ConfigureAwait(false);
        checkpoints.Add(CaptureExtendedCheckpoint("diag-stress-after", CheckpointPhase.PostGc, stressBaseline, baseline));

        Console.WriteLine($"Owner-crash post-stress diagnostic: {DiagnosticRepetitions} probes");
        IReadOnlyList<OwnerCrashProbeEvidence> postStress = await RunProbeSeriesAsync(
            "post-stress", DiagnosticRepetitions, baseline, checkpoints, globalFailures, cancellationToken).ConfigureAwait(false);
        if (postStress.Count != 0)
        {
            lastProbe = postStress[^1];
        }

        bool technicalPass = globalFailures.Count == 0;
        return new ProofSummaryEvidence
        {
            SchemaVersion = "1.0",
            TaskId = "SP00-T02",
            RunId = runId,
            StartedAtUtc = startedAtUtc,
            CompletedAtUtc = DateTimeOffset.UtcNow,
            Status = technicalPass ? "DIAGNOSTIC_PASS" : "FAIL",
            AcceptanceEligible = false,
            Mode = "diag-owner-crash",
            EffectiveRepeatEach = 0,
            EffectiveScaleRepeatEach = 0,
            SessionCounts = _manifest.SessionCounts,
            Scenarios = Array.Empty<string>(),
            WarmupRuns = 0,
            ReliabilityRuns = 0,
            ScaleSessionRuns = 0,
            FailedRuns = 0,
            ScenarioSummaries = Array.Empty<ScenarioSummaryEvidence>(),
            ScaleGroups = Array.Empty<ScaleGroupEvidence>(),
            HandleCheckpoints = checkpoints,
            OwnerCrashProbe = lastProbe with
            {
                Executed = true,
                Passed = cold.All(probe => probe.Passed) && postStress.All(probe => probe.Passed),
                Failure = globalFailures.Count == 0 ? null : $"Owner-crash diagnostic failed: {globalFailures[0]}"
            },
            GlobalFailures = globalFailures,
            Limitations = limitations,
            EvidenceDirectory = _evidence.DirectoryPath,
            ProcessId = _environment.ProcessId,
            ProcessStartUtcTicks = _environment.ProcessStartUtcTicks,
            BaselineCheckpointName = "diag-baseline",
            StableCheckpointPolicy = "Diagnostic process: baseline and staged stable checkpoints; not canonical acceptance evidence.",
            HandleGrowthTolerance = _manifest.Settings.HandleGrowthTolerance,
            DiagnosticsProcessSeparated = true,
            DiagnosticProcesses = Array.Empty<DiagnosticProcessEvidence>()
        };
    }

    private async Task<ProofSummaryEvidence> RunHandleGrowthDiagnosticProcessAsync(CancellationToken cancellationToken)
    {
        DateTimeOffset startedAtUtc = DateTimeOffset.UtcNow;
        string runId = $"sp00-t02-diag-handle-growth-{startedAtUtc:yyyyMMddTHHmmssZ}-{Guid.NewGuid():N}";
        List<string> globalFailures = new();
        List<string> limitations = BuildLimitations();
        limitations.Add("Handle-growth diagnostic process: repeated eight-session rounds and mixed cycles are development evidence, not canonical acceptance.");
        List<HandleCheckpointEvidence> checkpoints = new();

        Console.WriteLine($"SP00-T02 handle-growth diagnostic run {runId} pid={_environment.ProcessId}");

        Console.WriteLine("Diagnostic warm-up: normal_exit x 1");
        await _executor.ExecuteAsync("normal_exit", "diagnostic-warmup", 0, concurrency: 1, sessionOrdinal: 1, runId, cancellationToken).ConfigureAwait(false);
        int baseline = await StabilizeAndGetHandleCountAsync(cancellationToken).ConfigureAwait(false);
        checkpoints.Add(CaptureExtendedCheckpoint("diag-baseline", CheckpointPhase.PostGc, baseline, baseline));

        List<int> normalSeries = new();
        for (int round = 1; round <= DiagnosticRepetitions; round++)
        {
            await RunConcurrentSessionsAsync("normal_exit", 8, "handle-diag", round, runId, cancellationToken).ConfigureAwait(false);
            int after = await StabilizeAndGetHandleCountAsync(cancellationToken).ConfigureAwait(false);
            normalSeries.Add(after);
            HandleCheckpointEvidence checkpoint = CaptureExtendedCheckpoint(
                $"handle-diag-normal_exit-x8-round{round}", CheckpointPhase.PostGc, after, baseline);
            checkpoints.Add(checkpoint);
            RegisterCheckpointFailures(checkpoint, globalFailures);
        }

        List<int> mixSeries = new();
        for (int cycle = 1; cycle <= DiagnosticCycles; cycle++)
        {
            foreach (string scenario in MixScenarios)
            {
                await RunConcurrentSessionsAsync(scenario, 8, "mix-diag", cycle, runId, cancellationToken).ConfigureAwait(false);
            }

            int after = await StabilizeAndGetHandleCountAsync(cancellationToken).ConfigureAwait(false);
            mixSeries.Add(after);
            HandleCheckpointEvidence checkpoint = CaptureExtendedCheckpoint(
                $"handle-diag-mix-cycle{cycle}", CheckpointPhase.PostGc, after, baseline);
            checkpoints.Add(checkpoint);
            RegisterCheckpointFailures(checkpoint, globalFailures);
        }

        HandleGrowthClassificationEvidence normalClassification = HandleGrowthClassifier.Classify("handle-growth-normal-exit-x8", normalSeries);
        HandleGrowthClassificationEvidence mixClassification = HandleGrowthClassifier.Classify("handle-growth-mix-x8", mixSeries);
        await _evidence.WriteClassificationsAsync([normalClassification, mixClassification], cancellationToken).ConfigureAwait(false);
        Console.WriteLine($"Handle-growth normal_exit x8 classification: {normalClassification.Classification}");
        Console.WriteLine($"Handle-growth mix x8 classification: {mixClassification.Classification}");

        bool technicalPass = globalFailures.Count == 0;
        return new ProofSummaryEvidence
        {
            SchemaVersion = "1.0",
            TaskId = "SP00-T02",
            RunId = runId,
            StartedAtUtc = startedAtUtc,
            CompletedAtUtc = DateTimeOffset.UtcNow,
            Status = technicalPass ? "DIAGNOSTIC_PASS" : "FAIL",
            AcceptanceEligible = false,
            Mode = "diag-handle-growth",
            EffectiveRepeatEach = 0,
            EffectiveScaleRepeatEach = 0,
            SessionCounts = _manifest.SessionCounts,
            Scenarios = Array.Empty<string>(),
            WarmupRuns = 0,
            ReliabilityRuns = 0,
            ScaleSessionRuns = 0,
            FailedRuns = 0,
            ScenarioSummaries = Array.Empty<ScenarioSummaryEvidence>(),
            ScaleGroups = Array.Empty<ScaleGroupEvidence>(),
            HandleCheckpoints = checkpoints,
            OwnerCrashProbe = CreateSkippedProbe("Handle-growth diagnostic process does not run the canonical owner-crash probe."),
            GlobalFailures = globalFailures,
            Limitations = limitations,
            EvidenceDirectory = _evidence.DirectoryPath,
            ProcessId = _environment.ProcessId,
            ProcessStartUtcTicks = _environment.ProcessStartUtcTicks,
            BaselineCheckpointName = "diag-baseline",
            StableCheckpointPolicy = "Diagnostic process: classifier operates only on stabilized post-quiescence readings, never on active-concurrency peaks.",
            HandleGrowthTolerance = _manifest.Settings.HandleGrowthTolerance,
            DiagnosticsProcessSeparated = true,
            HandleGrowthClassifications = [normalClassification, mixClassification],
            DiagnosticProcesses = Array.Empty<DiagnosticProcessEvidence>()
        };
    }

    private async Task<ProofSummaryEvidence> RunIsolationDiagnosticProcessAsync(CancellationToken cancellationToken)
    {
        DateTimeOffset startedAtUtc = DateTimeOffset.UtcNow;
        string runId = $"sp00-t02-diag-isolation-{startedAtUtc:yyyyMMddTHHmmssZ}-{Guid.NewGuid():N}";
        List<string> globalFailures = new();
        List<string> limitations = BuildLimitations();
        limitations.Add("Standard-handle isolation regression runs as a diagnostic process; its markers must be absent from every redirected parent stream outside ConPTY capture.");

        Console.WriteLine($"SP00-T02 standard-handle isolation diagnostic run {runId} pid={_environment.ProcessId}");

        IsolationRegressionEvidence regression = await RunIsolationRegressionAsync(cancellationToken).ConfigureAwait(false);
        await _evidence.WriteIsolationRegressionAsync(regression, cancellationToken).ConfigureAwait(false);
        if (!regression.Passed)
        {
            globalFailures.Add($"Standard-handle isolation regression failed for probes: {string.Join(", ", regression.Probes.Where(probe => !probe.Passed).Select(probe => probe.Mode))}");
        }

        bool technicalPass = globalFailures.Count == 0;
        return new ProofSummaryEvidence
        {
            SchemaVersion = "1.0",
            TaskId = "SP00-T02",
            RunId = runId,
            StartedAtUtc = startedAtUtc,
            CompletedAtUtc = DateTimeOffset.UtcNow,
            Status = technicalPass ? "DIAGNOSTIC_PASS" : "FAIL",
            AcceptanceEligible = false,
            Mode = "diag-isolation",
            EffectiveRepeatEach = 0,
            EffectiveScaleRepeatEach = 0,
            SessionCounts = _manifest.SessionCounts,
            Scenarios = Array.Empty<string>(),
            WarmupRuns = 0,
            ReliabilityRuns = 0,
            ScaleSessionRuns = 0,
            FailedRuns = 0,
            ScenarioSummaries = Array.Empty<ScenarioSummaryEvidence>(),
            ScaleGroups = Array.Empty<ScaleGroupEvidence>(),
            HandleCheckpoints = Array.Empty<HandleCheckpointEvidence>(),
            OwnerCrashProbe = CreateSkippedProbe("Isolation diagnostic process does not run the canonical owner-crash probe."),
            GlobalFailures = globalFailures,
            Limitations = limitations,
            EvidenceDirectory = _evidence.DirectoryPath,
            ProcessId = _environment.ProcessId,
            ProcessStartUtcTicks = _environment.ProcessStartUtcTicks,
            BaselineCheckpointName = string.Empty,
            StableCheckpointPolicy = "Diagnostic process: no canonical handle baseline; isolation markers must never escape to a redirected parent stream.",
            HandleGrowthTolerance = _manifest.Settings.HandleGrowthTolerance,
            DiagnosticsProcessSeparated = true,
            IsolationRegression = regression,
            DiagnosticProcesses = Array.Empty<DiagnosticProcessEvidence>()
        };
    }

    private async Task<int> RunRetentionReproducerAsync(
        List<HandleCheckpointEvidence> checkpoints,
        int baseline,
        CancellationToken cancellationToken)
    {
        string probeDir = Path.Combine(_evidence.DirectoryPath, "retention-probe");
        Directory.CreateDirectory(probeDir);
        ProofOptions probeOptions = _options with { EvidenceDirectory = probeDir };
        OwnerCrashProbeEvidence probe = await OwnerCrashProbe.ExecuteAsync(probeOptions, _manifest, cancellationToken).ConfigureAwait(false);
        if (!probe.Passed)
        {
            throw new ProofAssertionException($"Retention reproducer probe failed: {probe.Failure} stage={probe.FailureStage}");
        }

        string retained = DescribeRetainedResources(probe);
        if (retained.Length != 0)
        {
            throw new ProofAssertionException($"Retention reproducer probe left owned resources: {retained}.");
        }

        int immediate = GetCurrentHandleCount();
        checkpoints.Add(CaptureExtendedCheckpoint("reproducer-immediate", CheckpointPhase.ImmediatePostDisposal, immediate, baseline));
        int postGc = await StabilizeAndGetHandleCountAsync(cancellationToken).ConfigureAwait(false);
        checkpoints.Add(CaptureExtendedCheckpoint("reproducer-post-gc", CheckpointPhase.PostGc, postGc, baseline));
        await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken).ConfigureAwait(false);
        checkpoints.Add(await CaptureExtendedCheckpointAsync("reproducer-250ms", CheckpointPhase.Quiescent250Ms, baseline, cancellationToken).ConfigureAwait(false));
        await Task.Delay(TimeSpan.FromMilliseconds(1750), cancellationToken).ConfigureAwait(false);
        checkpoints.Add(await CaptureExtendedCheckpointAsync("reproducer-2s", CheckpointPhase.Quiescent2S, baseline, cancellationToken).ConfigureAwait(false));
        await Task.Delay(TimeSpan.FromMilliseconds(8000), cancellationToken).ConfigureAwait(false);
        checkpoints.Add(await CaptureExtendedCheckpointAsync("reproducer-10s", CheckpointPhase.Quiescent10S, baseline, cancellationToken).ConfigureAwait(false));
        return immediate;
    }

    private async Task<IReadOnlyList<OwnerCrashProbeEvidence>> RunProbeSeriesAsync(
        string phase,
        int repetitions,
        int baseline,
        List<HandleCheckpointEvidence> checkpoints,
        List<string> failures,
        CancellationToken cancellationToken)
    {
        List<OwnerCrashProbeEvidence> probes = new();
        int passed = 0;
        int failed = 0;
        for (int i = 1; i <= repetitions; i++)
        {
            string probeDir = Path.Combine(_evidence.DirectoryPath, $"owner-crash-diag-{phase}-probe-{i:D2}");
            Directory.CreateDirectory(probeDir);
            ProofOptions probeOptions = _options with { EvidenceDirectory = probeDir };
            OwnerCrashProbeEvidence probe = await OwnerCrashProbe.ExecuteAsync(probeOptions, _manifest, cancellationToken).ConfigureAwait(false);
            probes.Add(probe);
            string retained = DescribeRetainedResources(probe);
            if (retained.Length != 0)
            {
                failures.Add($"Owner-crash {phase} probe {i} left owned TerminalProof resources: {retained}.");
            }

            if (probe.Passed)
            {
                passed++;
            }
            else
            {
                failed++;
                failures.Add($"Owner-crash {phase} probe {i}/{repetitions} FAILED stage={probe.FailureStage}: {probe.Failure}");
            }

            _lastSessionCompletedAt = DateTimeOffset.UtcNow;
            int afterHandles = GetCurrentHandleCount();
            checkpoints.Add(CaptureExtendedCheckpoint(
                $"owner-crash-diag-{phase}-probe-{i}",
                CheckpointPhase.ImmediatePostDisposal,
                afterHandles,
                baseline));

            int settledHandles = await StabilizeAndGetHandleCountAsync(cancellationToken).ConfigureAwait(false);
            checkpoints.Add(CaptureExtendedCheckpoint(
                $"owner-crash-diag-{phase}-probe-{i}-settled",
                CheckpointPhase.PostGc,
                settledHandles,
                baseline));
        }

        Console.WriteLine($"Owner-crash {phase} diagnostic: passed={passed}/{repetitions}, failed={failed}/{repetitions}");
        return probes;
    }

    private async Task RunStressCycleAsync(string runId, CancellationToken cancellationToken)
    {
        await RunConcurrentSessionsAsync("normal_exit", 8, "diagnostic-stress", 1, runId, cancellationToken).ConfigureAwait(false);
    }

    private async Task RunConcurrentSessionsAsync(
        string scenario,
        int concurrency,
        string phase,
        int iteration,
        string runId,
        CancellationToken cancellationToken)
    {
        Task<SessionRunEvidence>[] pending = Enumerable.Range(1, concurrency)
            .Select(sessionOrdinal => _executor.ExecuteAsync(
                scenario,
                phase,
                iteration,
                concurrency,
                sessionOrdinal,
                runId,
                cancellationToken))
            .ToArray();
        SessionRunEvidence[] runs = await Task.WhenAll(pending).ConfigureAwait(false);
        foreach (SessionRunEvidence run in runs)
        {
            await _evidence.AppendRunAsync(run, cancellationToken).ConfigureAwait(false);
        }

        _lastSessionCompletedAt = DateTimeOffset.UtcNow;
    }

    private async Task<IsolationRegressionEvidence> RunIsolationRegressionAsync(CancellationToken cancellationToken)
    {
        string runId = Guid.NewGuid().ToString("N")[..8];
        string markerPrefix = $"CONPTY-STREAM-ISOLATION:{runId}";
        List<IsolationProbeEvidenceEntry> entries = new();

        foreach (string mode in new[] { "ordinary", "stdout-redirected", "stdout-stderr-redirected" })
        {
            entries.Add(await RunIsolationProbeAsync(mode, runId, cancellationToken).ConfigureAwait(false));
        }

        return new IsolationRegressionEvidence(markerPrefix, entries, runId);
    }

    private async Task<IsolationProbeEvidenceEntry> RunIsolationProbeAsync(string mode, string runId, CancellationToken cancellationToken)
    {
        string fixtureRunId = $"{runId}-{mode}-{Guid.NewGuid():N}";
        string stdoutMarker = $"CONPTY-STDOUT-MARKER:{fixtureRunId}";
        string stderrMarker = $"CONPTY-STDERR-MARKER:{fixtureRunId}";
        TextWriter? originalOut = null;
        TextWriter? originalError = null;
        StreamWriter? stdoutRedirect = null;
        StreamWriter? stderrRedirect = null;
        string stdoutFile = string.Empty;
        string stderrFile = string.Empty;
        IReadOnlyList<string> failures = Array.Empty<string>();
        ConPtyTerminalSession? session = null;
        int? exitCode = null;
        bool stdoutCaptured = false;
        bool stderrCaptured = false;

        try
        {
            if (mode is "stdout-redirected" or "stdout-stderr-redirected")
            {
                string captureDir = Path.Combine(_evidence.DirectoryPath, "isolation", mode);
                Directory.CreateDirectory(captureDir);
                stdoutFile = Path.Combine(captureDir, "parent-stdout.txt");
                stderrFile = Path.Combine(captureDir, "parent-stderr.txt");
                originalOut = Console.Out;
                originalError = Console.Error;
                stdoutRedirect = new StreamWriter(stdoutFile, append: false, System.Text.Encoding.UTF8) { AutoFlush = true };
                Console.SetOut(stdoutRedirect);
                if (mode == "stdout-stderr-redirected")
                {
                    stderrRedirect = new StreamWriter(stderrFile, append: false, System.Text.Encoding.UTF8) { AutoFlush = true };
                    Console.SetError(stderrRedirect);
                }
            }

            session = await ConPtyTerminalSession.StartAsync(
                    new TerminalLaunchOptions
                    {
                        ExecutablePath = _options.FixturePath,
                        WorkingDirectory = _options.WorkingDirectory,
                        Arguments =
                        [
                            "--scenario",
                            "stream_isolation",
                            "--run-id",
                            fixtureRunId
                        ],
                        InitialSize = new TerminalSize(_manifest.Settings.InitialColumns, _manifest.Settings.InitialRows),
                        CleanupTimeout = _manifest.Settings.CleanupTimeout
                    },
                    cancellationToken)
                .ConfigureAwait(false);

            exitCode = await session.WaitForExitAsync(_manifest.Settings.DefaultTimeout, cancellationToken).ConfigureAwait(false);
            await session.WaitForOutputAsync(stdoutMarker, _manifest.Settings.DefaultTimeout, cancellationToken).ConfigureAwait(false);
            await session.WaitForOutputAsync(stderrMarker, _manifest.Settings.DefaultTimeout, cancellationToken).ConfigureAwait(false);

            string conptyText = session.GetOutputSnapshot().Utf8Text;
            stdoutCaptured = conptyText.Contains(stdoutMarker, StringComparison.Ordinal);
            stderrCaptured = conptyText.Contains(stderrMarker, StringComparison.Ordinal);
            await session.ShutdownAsync(CancellationToken.None).ConfigureAwait(false);
            await session.DisposeAsync().ConfigureAwait(false);
            session = null;
            OwnedResourceCounters.AssertZero($"Isolation probe disposal boundary ({mode}, run {fixtureRunId})");
            ProofAssert.True(exitCode == 0, $"Stream isolation fixture must exit zero; got {exitCode}.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            failures = [exception.ToString()];
        }
        finally
        {
            if (session is not null)
            {
                try
                {
                    await session.ShutdownAsync(CancellationToken.None).ConfigureAwait(false);
                    await session.DisposeAsync().ConfigureAwait(false);
                }
                catch
                {
                }
            }

            if (originalOut is not null)
            {
                Console.SetOut(originalOut);
            }

            if (originalError is not null)
            {
                Console.SetError(originalError);
            }

            stdoutRedirect?.Dispose();
            stderrRedirect?.Dispose();
        }

        bool absentStdout = true;
        bool absentStderr = true;
        if (mode is "stdout-redirected" or "stdout-stderr-redirected")
        {
            string parentStdout = File.ReadAllText(stdoutFile);
            absentStdout = !parentStdout.Contains(stdoutMarker, StringComparison.Ordinal)
                && !parentStdout.Contains(stderrMarker, StringComparison.Ordinal);
            if (mode == "stdout-stderr-redirected")
            {
                string parentStderr = File.ReadAllText(stderrFile);
                absentStderr = !parentStderr.Contains(stdoutMarker, StringComparison.Ordinal)
                    && !parentStderr.Contains(stderrMarker, StringComparison.Ordinal);
            }
        }

        bool passed = failures.Count == 0
            && stdoutCaptured
            && stderrCaptured
            && absentStdout
            && absentStderr
            && exitCode == 0;
        return new IsolationProbeEvidenceEntry(mode, passed, stdoutCaptured, stderrCaptured, absentStdout, absentStderr, exitCode)
        {
            Failure = passed ? null : string.Join(" | ", failures)
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

    private string HandleGrowthToleranceForDisplay() => _manifest.Settings.HandleGrowthTolerance.ToString(CultureInfo.InvariantCulture);

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
        string phase,
        int baseline,
        CancellationToken cancellationToken)
    {
        int handles = await StabilizeAndGetHandleCountAsync(cancellationToken).ConfigureAwait(false);
        return CaptureExtendedCheckpoint(name, phase, handles, baseline);
    }

    private HandleCheckpointEvidence CaptureExtendedCheckpoint(string name, string phase, int handles, int baseline)
    {
        int growth = handles - baseline;
        IReadOnlyList<OwnedResourceCount> owned = OwnedResourceCounters.Snapshot();
        return new HandleCheckpointEvidence(
            name,
            phase,
            handles,
            growth,
            DateTimeOffset.UtcNow,
            growth <= _manifest.Settings.HandleGrowthTolerance,
            owned,
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

    private void RegisterCheckpointFailures(HandleCheckpointEvidence checkpoint, ICollection<string> failures)
    {
        if (!checkpoint.WithinTolerance)
        {
            failures.Add(
                $"Handle checkpoint '{checkpoint.Name}' ({checkpoint.Phase}) exceeded tolerance: " +
                $"growth={checkpoint.GrowthFromBaseline}, tolerance={_manifest.Settings.HandleGrowthTolerance}.");
        }

        if (!checkpoint.OwnedResourcesZero)
        {
            failures.Add(
                $"Handle checkpoint '{checkpoint.Name}' retained owned TerminalProof resources: " +
                $"{string.Join(", ", checkpoint.OwnedResourceCounts.Where(count => count.Count != 0).Select(count => $"{count.Kind}={count.Count}"))}.");
        }
    }

    private static async Task<int> StabilizeAndGetHandleCountAsync(CancellationToken cancellationToken)
    {
        // ponytail: triple-GC with growing delays. Native handles from ConPTY threads and pipes
        // defer their final release to finalizer-triggered CloseHandle; a single GC pass often
        // collects the objects but the finalizer queue hasn't been drained yet.
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken).ConfigureAwait(false);
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken).ConfigureAwait(false);
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken).ConfigureAwait(false);
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

    private static string DescribeRetainedResources(OwnerCrashProbeEvidence probe) =>
        string.Join(", ", probe.OwnedResourceCounts.Where(count => count.Count != 0).Select(count => $"{count.Kind}={count.Count}"));

    private static IReadOnlyList<DiagnosticProcessEvidence> LoadDiagnosticsReport(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return Array.Empty<DiagnosticProcessEvidence>();
        }

        try
        {
            DiagnosticsOrchestrationRecord? record = JsonSerializer.Deserialize<DiagnosticsOrchestrationRecord>(
                File.ReadAllText(path),
                ProofJson.Create(indented: false));
            return record?.DiagnosticProcesses ?? Array.Empty<DiagnosticProcessEvidence>();
        }
        catch
        {
            return Array.Empty<DiagnosticProcessEvidence>();
        }
    }

    private static OwnerCrashProbeEvidence CreateSkippedProbe(string reason) => new()
    {
        Executed = false,
        Passed = false,
        Failure = reason,
        JobProcessIds = Array.Empty<int>(),
        SurvivingProcessIds = Array.Empty<int>(),
        LivePtyReattachSupported = false,
        ReattachConclusion = reason
    };
}
