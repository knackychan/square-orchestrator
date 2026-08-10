using Square.Domain.Primitives;

namespace Square.Domain.Terminals;

public static class TerminalLifecycleReducer
{
    private static readonly HashSet<TerminalLifecycleState> FinalStates = new()
    {
        TerminalLifecycleState.Succeeded,
        TerminalLifecycleState.Failed,
        TerminalLifecycleState.Cancelled,
        TerminalLifecycleState.HardStopped,
        TerminalLifecycleState.LostProcess
    };

    public static Result<TerminalSnapshot> Apply(TerminalSnapshot current, ITerminalLifecycleEvent lifecycleEvent)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(lifecycleEvent);
        if (current.AppliedEventIds.Contains(lifecycleEvent.EventId)) return Result<TerminalSnapshot>.Success(current);
        if (lifecycleEvent.OccurredAt.CompareTo(current.LastTransitionAt) < 0)
            return Denied(current, lifecycleEvent, "terminal.event_out_of_order", "Event time precedes the last accepted transition.");
        if (FinalStates.Contains(current.State))
            return Denied(current, lifecycleEvent, "terminal.final_state", "A final terminal state cannot transition.");

        TerminalLifecycleState? next = ResolveNextState(current.State, lifecycleEvent);
        if (next is null)
            return Denied(current, lifecycleEvent, "terminal.invalid_transition", "The event is not legal from the current terminal state.");

        List<EventId> applied = new(current.AppliedEventIds) { lifecycleEvent.EventId };
        return Result<TerminalSnapshot>.Success(current with
        {
            State = next.Value,
            LastTransitionAt = lifecycleEvent.OccurredAt,
            AppliedEventIds = applied.AsReadOnly()
        });
    }

    private static TerminalLifecycleState? ResolveNextState(TerminalLifecycleState current, ITerminalLifecycleEvent lifecycleEvent) => lifecycleEvent switch
    {
        TerminalLaunchRequested when current == TerminalLifecycleState.Created => TerminalLifecycleState.Starting,
        TerminalProcessStarted when current == TerminalLifecycleState.Starting => TerminalLifecycleState.Starting,
        TerminalStartupConfirmed when current == TerminalLifecycleState.Starting => TerminalLifecycleState.Running,
        TerminalActivityObserved when current is TerminalLifecycleState.Running or TerminalLifecycleState.QuietActive => TerminalLifecycleState.Running,
        TerminalActivityObserved when IsInteractionState(current) => current,
        TerminalQuietObserved when current == TerminalLifecycleState.Running => TerminalLifecycleState.QuietActive,
        TerminalQuestionDetected when IsWorkingState(current) => TerminalLifecycleState.WaitingForInput,
        TerminalApprovalDetected when IsWorkingState(current) => TerminalLifecycleState.WaitingForApproval,
        TerminalAuthenticationDetected when IsOperationalState(current) => TerminalLifecycleState.AuthRequired,
        TerminalBlockedDetected when IsOperationalState(current) => TerminalLifecycleState.Blocked,
        TerminalStallSuspected when IsWorkingState(current) => TerminalLifecycleState.SuspectedStall,
        TerminalInteractionResolved when IsInteractionState(current) => TerminalLifecycleState.Running,
        TerminalCompletionObserved when IsOperationalState(current) => TerminalLifecycleState.Completing,
        TerminalSucceeded when current is TerminalLifecycleState.Completing or TerminalLifecycleState.Running or TerminalLifecycleState.QuietActive => TerminalLifecycleState.Succeeded,
        TerminalFailed when IsOperationalState(current) => TerminalLifecycleState.Failed,
        TerminalCancellationRequested when IsOperationalState(current) => TerminalLifecycleState.Cancelling,
        TerminalCancelled when current == TerminalLifecycleState.Cancelling => TerminalLifecycleState.Cancelled,
        TerminalHardStopped when IsOperationalState(current) => TerminalLifecycleState.HardStopped,
        TerminalProcessLost when IsOperationalState(current) => TerminalLifecycleState.LostProcess,
        _ => null
    };

    private static bool IsWorkingState(TerminalLifecycleState state) => state is TerminalLifecycleState.Running or TerminalLifecycleState.QuietActive;
    private static bool IsInteractionState(TerminalLifecycleState state) => state is TerminalLifecycleState.WaitingForInput or TerminalLifecycleState.WaitingForApproval or TerminalLifecycleState.AuthRequired or TerminalLifecycleState.Blocked or TerminalLifecycleState.SuspectedStall;
    private static bool IsOperationalState(TerminalLifecycleState state) => state != TerminalLifecycleState.Created && !FinalStates.Contains(state);

    private static Result<TerminalSnapshot> Denied(TerminalSnapshot current, ITerminalLifecycleEvent lifecycleEvent, string code, string message) =>
        Result<TerminalSnapshot>.Failure(new DomainProblem(code, message, new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["terminal_id"] = current.TerminalId.ToString(),
            ["state"] = current.State.ToString(),
            ["event_type"] = lifecycleEvent.GetType().Name,
            ["event_id"] = lifecycleEvent.EventId.ToString()
        }));
}
