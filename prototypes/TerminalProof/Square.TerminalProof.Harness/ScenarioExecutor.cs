using System.Diagnostics;
using System.Globalization;
using Square.TerminalProof.Native;

namespace Square.TerminalProof.Harness;

internal sealed class ScenarioExecutor
{
    private const uint HardStopExitCode = 0x534F00FF;
    private readonly ProofManifest _manifest;
    private readonly string _fixturePath;
    private readonly string _workingDirectory;

    internal ScenarioExecutor(ProofManifest manifest, string fixturePath, string workingDirectory)
    {
        _manifest = manifest;
        _fixturePath = fixturePath;
        _workingDirectory = workingDirectory;
    }

    internal async Task<SessionRunEvidence> ExecuteAsync(
        string scenario,
        string phase,
        int iteration,
        int concurrency,
        int sessionOrdinal,
        string proofRunId,
        CancellationToken cancellationToken)
    {
        string runId = $"{phase}-{scenario}-{iteration:D4}-{concurrency:D2}-{sessionOrdinal:D2}-{Guid.NewGuid():N}";
        DateTimeOffset startedAt = DateTimeOffset.UtcNow;
        Stopwatch duration = Stopwatch.StartNew();
        int handleCountBefore = GetCurrentHandleCount();
        ConPtyTerminalSession? session = null;
        ProcessResourceSampler? sampler = null;
        TerminalOutputSnapshot output = new(Array.Empty<byte>(), null);
        TerminalAccountingSnapshot accounting = EmptyAccounting();
        ProcessSampleSnapshot processSample = new(0, 0, 0, Array.Empty<ObservedProcessIdentity>());
        IReadOnlyList<int> leakedProcessIds = Array.Empty<int>();
        int? exitCode = null;
        string? failure = null;

        try
        {
            session = await ConPtyTerminalSession.StartAsync(
                new TerminalLaunchOptions
                {
                    ExecutablePath = _fixturePath,
                    WorkingDirectory = _workingDirectory,
                    Arguments = BuildArguments(scenario),
                    InitialSize = new TerminalSize(_manifest.Settings.InitialColumns, _manifest.Settings.InitialRows),
                    CleanupTimeout = _manifest.Settings.CleanupTimeout
                },
                cancellationToken).ConfigureAwait(false);
            sampler = new ProcessResourceSampler(session, _manifest.Settings.SampleInterval);
            exitCode = await ExecuteScenarioAsync(scenario, session, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            failure = exception.ToString();
        }
        finally
        {
            if (session is not null)
            {
                try
                {
                    if (session.GetAccounting().ActiveProcesses != 0)
                    {
                        await session.HardStopAsync(HardStopExitCode, CancellationToken.None).ConfigureAwait(false);
                    }
                }
                catch (Exception cleanupException) when (cleanupException is not OperationCanceledException)
                {
                    failure = AppendFailure(failure, "Hard-stop cleanup failed", cleanupException);
                }

                if (sampler is not null)
                {
                    try
                    {
                        await sampler.DisposeAsync().ConfigureAwait(false);
                        processSample = sampler.Snapshot();
                    }
                    catch (Exception samplingException) when (samplingException is not OperationCanceledException)
                    {
                        failure = AppendFailure(failure, "Resource sampler shutdown failed", samplingException);
                    }
                }

                try
                {
                    await session.ShutdownAsync(CancellationToken.None).ConfigureAwait(false);
                    accounting = session.GetAccounting();
                    output = session.GetOutputSnapshot();
                }
                catch (Exception shutdownException) when (shutdownException is not OperationCanceledException)
                {
                    failure = AppendFailure(failure, "ConPTY shutdown failed", shutdownException);
                    try
                    {
                        output = session.GetOutputSnapshot();
                        accounting = session.GetAccounting();
                    }
                    catch (Exception snapshotException)
                    {
                        failure = AppendFailure(failure, "Post-failure snapshot failed", snapshotException);
                    }
                }

                try
                {
                    await session.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception disposeException)
                {
                    failure = AppendFailure(failure, "Session disposal failed", disposeException);
                }

                if (sampler is not null)
                {
                    try
                    {
                        leakedProcessIds = await sampler.FindLeakedProcessesAsync(
                            _manifest.Settings.DescendantExitTimeout,
                            CancellationToken.None).ConfigureAwait(false);
                        if (leakedProcessIds.Count != 0)
                        {
                            failure = AppendFailure(
                                failure,
                                "Leaked descendants",
                                new ProofAssertionException($"Observed process IDs still running: {string.Join(", ", leakedProcessIds)}"));
                        }
                    }
                    catch (Exception leakException) when (leakException is not OperationCanceledException)
                    {
                        failure = AppendFailure(failure, "Leak verification failed", leakException);
                    }
                }
            }
        }

        duration.Stop();
        int handleCountAfter = GetCurrentHandleCount();
        return new SessionRunEvidence
        {
            SchemaVersion = "1.0",
            ProofRunId = proofRunId,
            RunId = runId,
            Phase = phase,
            Scenario = scenario,
            Iteration = iteration,
            Concurrency = concurrency,
            SessionOrdinal = sessionOrdinal,
            StartedAtUtc = startedAt,
            DurationMilliseconds = duration.Elapsed.TotalMilliseconds,
            Passed = failure is null,
            Failure = failure,
            RootProcessId = session?.ProcessId,
            ExitCode = exitCode,
            OutputBytes = output.Length,
            OutputSha256 = output.Bytes.Length == 0 ? null : Hashing.Sha256(output.Bytes),
            OutputExcerpt = SanitizeExcerpt(output.Utf8Text, 768),
            FirstOutputLatencyMilliseconds = output.FirstByteLatency?.TotalMilliseconds,
            JobCpuMilliseconds = accounting.TotalCpuTime.TotalMilliseconds,
            JobReadTransferBytes = accounting.ReadTransferBytes,
            JobWriteTransferBytes = accounting.WriteTransferBytes,
            PeakWorkingSetBytes = processSample.PeakWorkingSetBytes,
            PeakActiveProcessCount = processSample.PeakActiveProcessCount,
            PeakCombinedProcessHandleCount = processSample.PeakCombinedProcessHandleCount,
            ObservedProcessIds = processSample.ObservedProcesses.Select(item => item.ProcessId).Order().ToArray(),
            LeakedProcessIds = leakedProcessIds,
            HarnessHandleCountBefore = handleCountBefore,
            HarnessHandleCountAfter = handleCountAfter
        };
    }

    private async Task<int> ExecuteScenarioAsync(
        string scenario,
        ConPtyTerminalSession session,
        CancellationToken cancellationToken)
    {
        TimeSpan timeout = _manifest.Settings.DefaultTimeout;
        int exitCode;
        switch (scenario)
        {
            case "unicode":
                exitCode = await session.WaitForExitAsync(timeout, cancellationToken).ConfigureAwait(false);
                ProofAssert.Equal(0, exitCode, "Unicode fixture must exit normally.");
                await session.WaitForOutputAsync("UNICODE:café|漢字|Ελληνικά|😀", timeout, cancellationToken).ConfigureAwait(false);
                break;

            case "ansi":
                exitCode = await session.WaitForExitAsync(timeout, cancellationToken).ConfigureAwait(false);
                ProofAssert.Equal(0, exitCode, "ANSI fixture must exit normally.");
                await session.WaitForOutputAsync("ANSI-RED", timeout, cancellationToken).ConfigureAwait(false);
                TerminalOutputSnapshot ansiOutput = session.GetOutputSnapshot();
                ProofAssert.True(ansiOutput.Bytes.Contains((byte)0x1B), "ANSI/ConPTY output must contain at least one escape byte.");
                break;

            case "large_burst":
                await session.WaitForOutputAsync("BURST-BEGIN", timeout, cancellationToken).ConfigureAwait(false);
                exitCode = await session.WaitForExitAsync(timeout, cancellationToken).ConfigureAwait(false);
                ProofAssert.Equal(0, exitCode, "Large burst fixture must exit normally.");
                await session.WaitForOutputAsync("BURST-END", timeout, cancellationToken).ConfigureAwait(false);
                ProofAssert.True(
                    session.GetOutputSnapshot().Length >= _manifest.Settings.LargeBurstBytes,
                    "Captured output must contain at least the requested burst payload.");
                break;

            case "quiet_child":
                await session.WaitForOutputAsync("QUIET-READY", timeout, cancellationToken).ConfigureAwait(false);
                await WaitForActiveProcessCountAsync(session, 2, timeout, cancellationToken).ConfigureAwait(false);
                await Task.Delay(TimeSpan.FromMilliseconds(75), cancellationToken).ConfigureAwait(false);
                long beforeQuiet = session.GetOutputSnapshot().Length;
                await Task.Delay(TimeSpan.FromMilliseconds(_manifest.Settings.QuietObservationMilliseconds), cancellationToken).ConfigureAwait(false);
                ProofAssert.True(session.IsRunning, "Quiet fixture root must remain running during the observation window.");
                ProofAssert.True(session.GetAccounting().ActiveProcesses >= 2, "Quiet child must remain contained and active during the observation window.");
                long afterQuiet = session.GetOutputSnapshot().Length;
                ProofAssert.Equal(beforeQuiet, afterQuiet, "Quiet fixture emitted output during the declared quiet observation window.");
                exitCode = await session.WaitForExitAsync(timeout, cancellationToken).ConfigureAwait(false);
                ProofAssert.Equal(0, exitCode, "Quiet child fixture must eventually exit normally.");
                break;

            case "stdin_question":
                await session.WaitForOutputAsync("QUESTION:enter-square-proof-token>", timeout, cancellationToken).ConfigureAwait(false);
                await session.WriteTextAsync("square-proof-answer\r", cancellationToken).ConfigureAwait(false);
                await session.WaitForOutputAsync("ANSWER:square-proof-answer", timeout, cancellationToken).ConfigureAwait(false);
                exitCode = await session.WaitForExitAsync(timeout, cancellationToken).ConfigureAwait(false);
                ProofAssert.Equal(0, exitCode, "Question fixture must accept the expected input.");
                break;

            case "resize":
                await session.WaitForOutputAsync("RESIZE-READY", timeout, cancellationToken).ConfigureAwait(false);
                session.Resize(new TerminalSize(_manifest.Settings.ResizeColumns, _manifest.Settings.ResizeRows));
                await Task.Delay(TimeSpan.FromMilliseconds(75), cancellationToken).ConfigureAwait(false);
                await session.WriteTextAsync("continue\r", cancellationToken).ConfigureAwait(false);
                await session.WaitForOutputAsync(
                    $"SIZE-AFTER:{_manifest.Settings.ResizeColumns}x{_manifest.Settings.ResizeRows}",
                    timeout,
                    cancellationToken).ConfigureAwait(false);
                exitCode = await session.WaitForExitAsync(timeout, cancellationToken).ConfigureAwait(false);
                ProofAssert.Equal(0, exitCode, "Resize fixture must exit normally.");
                break;

            case "normal_exit":
                await session.WaitForOutputAsync("NORMAL-EXIT:0", timeout, cancellationToken).ConfigureAwait(false);
                exitCode = await session.WaitForExitAsync(timeout, cancellationToken).ConfigureAwait(false);
                ProofAssert.Equal(0, exitCode, "Normal fixture must exit with code zero.");
                break;

            case "crash":
                await session.WaitForOutputAsync("CRASH-READY", timeout, cancellationToken).ConfigureAwait(false);
                exitCode = await session.WaitForExitAsync(timeout, cancellationToken).ConfigureAwait(false);
                ProofAssert.True(exitCode != 0, "Crash fixture must produce a non-zero exit code.");
                break;

            case "graceful_cancel":
                await session.WaitForOutputAsync("CANCEL-READY", timeout, cancellationToken).ConfigureAwait(false);
                await session.SendCtrlCAsync(cancellationToken).ConfigureAwait(false);
                await session.WaitForOutputAsync("CANCEL-ACK", timeout, cancellationToken).ConfigureAwait(false);
                exitCode = await session.WaitForExitAsync(timeout, cancellationToken).ConfigureAwait(false);
                ProofAssert.Equal(0, exitCode, "Graceful Ctrl+C fixture must acknowledge cancellation and exit zero.");
                break;

            case "forced_termination":
                await session.WaitForOutputAsync("FORCE-READY", timeout, cancellationToken).ConfigureAwait(false);
                await session.SendCtrlCAsync(cancellationToken).ConfigureAwait(false);
                await session.WaitForOutputAsync("FORCE-CANCEL-IGNORED", timeout, cancellationToken).ConfigureAwait(false);
                ProofAssert.True(session.IsRunning, "Forced-termination fixture must remain active after ignoring Ctrl+C.");
                await session.HardStopAsync(HardStopExitCode, cancellationToken).ConfigureAwait(false);
                exitCode = await session.WaitForExitAsync(timeout, cancellationToken).ConfigureAwait(false);
                ProofAssert.True(exitCode != 0, "Hard-stopped fixture must not report a successful exit.");
                break;

            case "nested_children":
                await session.WaitForOutputAsync("TREE-READY", timeout, cancellationToken).ConfigureAwait(false);
                await WaitForActiveProcessCountAsync(
                    session,
                    _manifest.Settings.NestedChildCount + 1,
                    timeout,
                    cancellationToken).ConfigureAwait(false);
                await session.HardStopAsync(HardStopExitCode, cancellationToken).ConfigureAwait(false);
                exitCode = await session.WaitForExitAsync(timeout, cancellationToken).ConfigureAwait(false);
                ProofAssert.True(exitCode != 0, "Nested process tree root must be terminated by the Job Object hard stop.");
                ProofAssert.Equal(0u, session.GetAccounting().ActiveProcesses, "Job Object must be empty after nested tree termination.");
                break;

            default:
                throw new InvalidDataException($"No executor exists for scenario '{scenario}'.");
        }

        ProofAssert.True(session.GetOutputSnapshot().FirstByteLatency is not null, "Every scenario must produce measurable output latency.");
        return exitCode;
    }

    private IReadOnlyList<string> BuildArguments(string scenario)
    {
        List<string> arguments = new() { "--scenario", scenario };
        switch (scenario)
        {
            case "large_burst":
                arguments.Add("--payload-bytes");
                arguments.Add(_manifest.Settings.LargeBurstBytes.ToString(CultureInfo.InvariantCulture));
                break;
            case "quiet_child":
                arguments.Add("--quiet-ms");
                arguments.Add(_manifest.Settings.QuietDurationMilliseconds.ToString(CultureInfo.InvariantCulture));
                break;
            case "nested_children":
                arguments.Add("--child-count");
                arguments.Add(_manifest.Settings.NestedChildCount.ToString(CultureInfo.InvariantCulture));
                break;
        }

        return arguments;
    }

    private static async Task WaitForActiveProcessCountAsync(
        ConPtyTerminalSession session,
        int minimumCount,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        while (session.GetAccounting().ActiveProcesses < minimumCount)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (stopwatch.Elapsed >= timeout)
            {
                throw new TimeoutException($"Expected at least {minimumCount} active Job Object processes within {timeout}.");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(15), cancellationToken).ConfigureAwait(false);
        }
    }

    private static string? SanitizeExcerpt(string text, int maximumCharacters)
    {
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        string normalized = text
            .Replace("\u001b", "<ESC>", StringComparison.Ordinal)
            .Replace("\r", "<CR>", StringComparison.Ordinal)
            .Replace("\n", "<LF>", StringComparison.Ordinal);
        return normalized.Length <= maximumCharacters
            ? normalized
            : normalized[..maximumCharacters] + "…";
    }

    private static string AppendFailure(string? existing, string label, Exception exception) =>
        string.IsNullOrEmpty(existing)
            ? $"{label}: {exception}"
            : $"{existing}{Environment.NewLine}{label}: {exception}";

    private static int GetCurrentHandleCount()
    {
        using Process current = Process.GetCurrentProcess();
        return current.HandleCount;
    }

    private static TerminalAccountingSnapshot EmptyAccounting() => new(
        TimeSpan.Zero,
        TimeSpan.Zero,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        0);
}
