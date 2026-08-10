using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Square.TerminalProof.Native;

internal sealed class JobObject : IDisposable
{
    private readonly SafeJobHandle _handle;

    private JobObject(SafeJobHandle handle)
    {
        _handle = handle;
    }

    internal static JobObject CreateKillOnClose()
    {
        SafeJobHandle handle = NativeMethods.CreateJobObjectW(nint.Zero, null);
        if (handle.IsInvalid)
        {
            handle.Dispose();
            Win32Error.ThrowLastError("CreateJobObject");
        }

        NativeMethods.JobObjectExtendedLimitInfo limits = new();
        limits.BasicLimitInformation.LimitFlags = NativeMethods.JobObjectLimitKillOnJobClose;
        if (!NativeMethods.SetInformationJobObject(
                handle,
                NativeMethods.JobObjectExtendedLimitInformation,
                ref limits,
                checked((uint)Marshal.SizeOf<NativeMethods.JobObjectExtendedLimitInfo>())))
        {
            handle.Dispose();
            Win32Error.ThrowLastError("SetInformationJobObject(KILL_ON_JOB_CLOSE)");
        }

        OwnedResourceCounters.Increment(OwnedResourceKind.JobObject);
        return new JobObject(handle);
    }

    internal void Assign(SafeProcessHandle process)
    {
        if (!NativeMethods.AssignProcessToJobObject(_handle, process))
        {
            Win32Error.ThrowLastError("AssignProcessToJobObject");
        }

        if (!NativeMethods.IsProcessInJob(process, _handle, out bool isInJob))
        {
            Win32Error.ThrowLastError("IsProcessInJob");
        }

        if (!isInJob)
        {
            throw new InvalidOperationException("The root process was not reported as a member of the proof Job Object after assignment.");
        }
    }

    internal void Terminate(uint exitCode)
    {
        if (!NativeMethods.TerminateJobObject(_handle, exitCode))
        {
            Win32Error.ThrowLastError("TerminateJobObject");
        }
    }

    internal TerminalAccountingSnapshot GetAccounting()
    {
        int size = Marshal.SizeOf<NativeMethods.JobObjectBasicAndIoAccountingInfo>();
        nint buffer = Marshal.AllocHGlobal(size);
        try
        {
            if (!NativeMethods.QueryInformationJobObject(
                    _handle,
                    NativeMethods.JobObjectBasicAndIoAccountingInformation,
                    buffer,
                    checked((uint)size),
                    out _))
            {
                Win32Error.ThrowLastError("QueryInformationJobObject(BasicAndIoAccounting)");
            }

            NativeMethods.JobObjectBasicAndIoAccountingInfo native =
                Marshal.PtrToStructure<NativeMethods.JobObjectBasicAndIoAccountingInfo>(buffer);
            return new TerminalAccountingSnapshot(
                TimeSpan.FromTicks(native.BasicInfo.TotalUserTime),
                TimeSpan.FromTicks(native.BasicInfo.TotalKernelTime),
                native.BasicInfo.TotalProcesses,
                native.BasicInfo.ActiveProcesses,
                native.BasicInfo.TotalTerminatedProcesses,
                native.IoInfo.ReadOperationCount,
                native.IoInfo.WriteOperationCount,
                native.IoInfo.OtherOperationCount,
                native.IoInfo.ReadTransferCount,
                native.IoInfo.WriteTransferCount,
                native.IoInfo.OtherTransferCount);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    internal IReadOnlyList<int> GetActiveProcessIds()
    {
        int capacity = 16;
        while (capacity <= 4096)
        {
            int bytes = checked(8 + (nint.Size * capacity));
            nint buffer = Marshal.AllocHGlobal(bytes);
            try
            {
                if (NativeMethods.QueryInformationJobObject(
                        _handle,
                        NativeMethods.JobObjectBasicProcessIdList,
                        buffer,
                        checked((uint)bytes),
                        out _))
                {
                    uint processCount = unchecked((uint)Marshal.ReadInt32(buffer, 4));
                    if (processCount > capacity)
                    {
                        capacity = checked((int)processCount + 8);
                        continue;
                    }

                    List<int> processIds = new(checked((int)processCount));
                    for (int index = 0; index < processCount; index++)
                    {
                        nint processId = Marshal.ReadIntPtr(buffer, 8 + (index * nint.Size));
                        if (processId > 0 && processId <= int.MaxValue)
                        {
                            processIds.Add(checked((int)processId));
                        }
                    }

                    return processIds;
                }

                int error = Marshal.GetLastWin32Error();
                if (error == NativeMethods.ErrorMoreData)
                {
                    capacity *= 2;
                    continue;
                }

                throw new Win32Exception(error, $"QueryInformationJobObject(BasicProcessIdList) failed with Win32 error {error}.");
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        throw new InvalidOperationException("The Job Object process list exceeded the proof harness safety bound of 4096 processes.");
    }

    internal async Task WaitForEmptyAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        while (GetAccounting().ActiveProcesses != 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (stopwatch.Elapsed >= timeout)
            {
                throw new TimeoutException($"The Job Object still contained active processes after {timeout}.");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(20), cancellationToken).ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        _handle.Dispose();
        OwnedResourceCounters.Decrement(OwnedResourceKind.JobObject);
    }
}
