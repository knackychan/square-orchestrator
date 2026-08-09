using Square.Domain.Primitives;

namespace Square.Domain.Terminals;

public sealed record TerminalSnapshot(
    TerminalId TerminalId,
    TerminalLifecycleState State,
    UtcInstant LastTransitionAt,
    IReadOnlyList<EventId> AppliedEventIds)
{
    public static TerminalSnapshot Create(TerminalId terminalId, UtcInstant createdAt) =>
        new(terminalId, TerminalLifecycleState.Created, createdAt, Array.Empty<EventId>());
}
