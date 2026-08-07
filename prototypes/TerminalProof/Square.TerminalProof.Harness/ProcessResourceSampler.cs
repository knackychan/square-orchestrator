using System.Diagnostics;
using Square.TerminalProof.Native;

namespace Square.TerminalProof.Harness;

internal sealed class ProcessResourceSampler : IAsyncDisposable
{
    private readonly object _sync = new();
    private readonly ConPtyTerminalSession _session;
    private readonly TimeSpan _sampleInterval;
    private readonly CancellationTokenSource _stop = new();
    private readonly Dictionary<int, ObservedProcessIdentity> _observed = new();
    private readonly Task _samplingTask;
    private long _peakWorkingSetBytes;
    private int _peakActiveProcessCount;
    private int _peakCombinedProcessHandleCount;

    internal ProcessResourceSampler(ConPtyTerminalSession session, TimeSpan sampleInterval)
    {
        _session = session;
        _sampleInterval = sampleInterval;
        _samplingTask = SampleLoopAsync();
    }

    internal ProcessSampleSnapshot Snapshot()
    {
        lock (_sync)
        {
            return new ProcessSampleSnapshot(
                _peakWorkingSetBytes,
                _peakActiveProcessCount,
                _peakCombinedProcessHandleCount,
                _observed.Values.OrderBy(item => item.ProcessId).ToArray());
        }
    }

    internal async Task<IReadOnlyList<int>> FindLeakedProcessesAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        ProcessSampleSnapshot snapshot = Snapshot();
        Stopwatch stopwatch = Stopwatch.StartNew();
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<int> survivors = snapshot.ObservedProcesses
                .Where(IsSameProcessStillRunning)
                .Select(identity => identity.ProcessId)
                .Order()
                .ToArray();
            if (survivors.Count == 0 || stopwatch.Elapsed >= timeout)
            {
                return survivors;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(25), cancellationToken).ConfigureAwait(false);
        }
    }

    public async ValueTask DisposeAsync()
    {
        _stop.Cancel();
        try
        {
            await _samplingTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (_stop.IsCancellationRequested)
        {
        }
        finally
        {
            _stop.Dispose();
        }
    }

    private async Task SampleLoopAsync()
    {
        while (!_stop.IsCancellationRequested)
        {
            CaptureOneSample();
            await Task.Delay(_sampleInterval, _stop.Token).ConfigureAwait(false);
        }
    }

    private void CaptureOneSample()
    {
        IReadOnlyList<int> processIds;
        try
        {
            processIds = _session.GetActiveProcessIds();
        }
        catch (ObjectDisposedException)
        {
            return;
        }

        long totalWorkingSet = 0;
        int totalHandles = 0;
        foreach (int processId in processIds)
        {
            try
            {
                using Process process = Process.GetProcessById(processId);
                process.Refresh();
                if (process.HasExited)
                {
                    continue;
                }

                totalWorkingSet = checked(totalWorkingSet + process.WorkingSet64);
                totalHandles = checked(totalHandles + process.HandleCount);
                long? startTicks = TryGetStartTimeUtcTicks(process);
                lock (_sync)
                {
                    _observed.TryAdd(processId, new ObservedProcessIdentity(processId, startTicks));
                }
            }
            catch (ArgumentException)
            {
                // Process exited between the Job Object query and Process.GetProcessById.
            }
            catch (InvalidOperationException)
            {
                // Process exited while metrics were being sampled.
            }
        }

        lock (_sync)
        {
            _peakWorkingSetBytes = Math.Max(_peakWorkingSetBytes, totalWorkingSet);
            _peakActiveProcessCount = Math.Max(_peakActiveProcessCount, processIds.Count);
            _peakCombinedProcessHandleCount = Math.Max(_peakCombinedProcessHandleCount, totalHandles);
        }
    }

    private static long? TryGetStartTimeUtcTicks(Process process)
    {
        try
        {
            return process.StartTime.ToUniversalTime().Ticks;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return null;
        }
    }

    private static bool IsSameProcessStillRunning(ObservedProcessIdentity identity)
    {
        try
        {
            using Process process = Process.GetProcessById(identity.ProcessId);
            process.Refresh();
            if (process.HasExited)
            {
                return false;
            }

            if (identity.StartTimeUtcTicks is null)
            {
                return true;
            }

            return process.StartTime.ToUniversalTime().Ticks == identity.StartTimeUtcTicks.Value;
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
}
