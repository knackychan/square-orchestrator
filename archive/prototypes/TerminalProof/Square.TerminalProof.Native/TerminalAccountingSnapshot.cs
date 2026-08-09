namespace Square.TerminalProof.Native;

public sealed record TerminalAccountingSnapshot(
    TimeSpan TotalUserTime,
    TimeSpan TotalKernelTime,
    uint TotalProcesses,
    uint ActiveProcesses,
    uint TotalTerminatedProcesses,
    ulong ReadOperationCount,
    ulong WriteOperationCount,
    ulong OtherOperationCount,
    ulong ReadTransferBytes,
    ulong WriteTransferBytes,
    ulong OtherTransferBytes)
{
    public TimeSpan TotalCpuTime => TotalUserTime + TotalKernelTime;
}
