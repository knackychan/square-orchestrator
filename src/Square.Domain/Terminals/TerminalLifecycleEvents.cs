using Square.Domain.Primitives;

namespace Square.Domain.Terminals;

public interface ITerminalLifecycleEvent
{
    EventId EventId { get; }
    UtcInstant OccurredAt { get; }
}

public sealed record TerminalLaunchRequested(EventId EventId, UtcInstant OccurredAt) : ITerminalLifecycleEvent;
public sealed record TerminalProcessStarted(EventId EventId, UtcInstant OccurredAt) : ITerminalLifecycleEvent;
public sealed record TerminalStartupConfirmed(EventId EventId, UtcInstant OccurredAt) : ITerminalLifecycleEvent;
public sealed record TerminalActivityObserved(EventId EventId, UtcInstant OccurredAt) : ITerminalLifecycleEvent;
public sealed record TerminalQuietObserved(EventId EventId, UtcInstant OccurredAt) : ITerminalLifecycleEvent;
public sealed record TerminalQuestionDetected(EventId EventId, UtcInstant OccurredAt) : ITerminalLifecycleEvent;
public sealed record TerminalApprovalDetected(EventId EventId, UtcInstant OccurredAt) : ITerminalLifecycleEvent;
public sealed record TerminalAuthenticationDetected(EventId EventId, UtcInstant OccurredAt) : ITerminalLifecycleEvent;
public sealed record TerminalBlockedDetected(EventId EventId, UtcInstant OccurredAt) : ITerminalLifecycleEvent;
public sealed record TerminalStallSuspected(EventId EventId, UtcInstant OccurredAt) : ITerminalLifecycleEvent;
public sealed record TerminalInteractionResolved(EventId EventId, UtcInstant OccurredAt) : ITerminalLifecycleEvent;
public sealed record TerminalCompletionObserved(EventId EventId, UtcInstant OccurredAt) : ITerminalLifecycleEvent;
public sealed record TerminalSucceeded(EventId EventId, UtcInstant OccurredAt) : ITerminalLifecycleEvent;
public sealed record TerminalFailed(EventId EventId, UtcInstant OccurredAt) : ITerminalLifecycleEvent;
public sealed record TerminalCancellationRequested(EventId EventId, UtcInstant OccurredAt) : ITerminalLifecycleEvent;
public sealed record TerminalCancelled(EventId EventId, UtcInstant OccurredAt) : ITerminalLifecycleEvent;
public sealed record TerminalHardStopped(EventId EventId, UtcInstant OccurredAt) : ITerminalLifecycleEvent;
public sealed record TerminalProcessLost(EventId EventId, UtcInstant OccurredAt) : ITerminalLifecycleEvent;
