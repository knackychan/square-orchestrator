namespace Square.Domain.Terminals;

public enum TerminalLifecycleState
{
    Created,
    Starting,
    Running,
    QuietActive,
    WaitingForInput,
    WaitingForApproval,
    AuthRequired,
    Blocked,
    SuspectedStall,
    Completing,
    Cancelling,
    Succeeded,
    Failed,
    Cancelled,
    HardStopped,
    LostProcess
}
