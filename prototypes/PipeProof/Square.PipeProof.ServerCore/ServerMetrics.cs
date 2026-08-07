using System.Collections.Concurrent;

namespace Square.PipeProof.ServerCore;

public sealed record QueueMetricsSnapshot(
    int CurrentControlDepth,
    int CurrentEventDepth,
    int CurrentTotalDepth,
    int PeakTotalDepth,
    long DroppedEvents,
    int EventCapacity,
    int ControlCapacity);

public sealed record ConnectionMetricsSnapshot(
    string ConnectionId,
    string? ClientKind,
    string? ClientInstanceId,
    DateTimeOffset ConnectedAtUtc,
    DateTimeOffset? ClosedAtUtc,
    bool HandshakeComplete,
    int ActiveRequests,
    int ActiveSubscriptions,
    QueueMetricsSnapshot Queue);

public sealed record ServerMetricsSnapshot(
    string SchemaVersion,
    DateTimeOffset CapturedAtUtc,
    string ServerInstanceId,
    long ServerEpoch,
    long AcceptedConnections,
    int ActiveConnections,
    long ClosedConnections,
    long CompletedHandshakes,
    long RejectedHandshakes,
    long ProtocolViolations,
    long OversizedFrames,
    long MalformedFrames,
    long InvalidUtf8Frames,
    long RequestsStarted,
    long RequestsCompleted,
    long RequestsCancelled,
    long SubscriptionsCreated,
    long SubscriptionsRemoved,
    long EventsPublished,
    long EventsReplayed,
    long EventFramesDropped,
    long SlowSubscriberDisconnects,
    int MaximumObservedQueueDepth,
    int MaximumObservedSubscriptionQueueDepth,
    int CurrentQueuedFrames,
    long LatestEventSequence,
    long MinimumAvailableEventSequence,
    long ProcessWorkingSetBytes,
    int ProcessHandleCount,
    IReadOnlyList<ConnectionMetricsSnapshot> Connections);

internal sealed class ConnectionMetrics(
    string connectionId,
    DateTimeOffset connectedAtUtc,
    int controlCapacity,
    int eventCapacity)
{
    private int _handshakeComplete;
    private int _activeRequests;
    private int _activeSubscriptions;
    private int _controlDepth;
    private int _eventDepth;
    private int _peakTotalDepth;
    private long _droppedEvents;
    private long _closedAtUtcTicks;
    private string? _clientKind;
    private string? _clientInstanceId;

    internal string ConnectionId { get; } = connectionId;

    internal void SetClient(string clientKind, string clientInstanceId)
    {
        _clientKind = clientKind;
        _clientInstanceId = clientInstanceId;
        Volatile.Write(ref _handshakeComplete, 1);
    }

    internal void RequestStarted() => Interlocked.Increment(ref _activeRequests);
    internal void RequestCompleted() => Interlocked.Decrement(ref _activeRequests);
    internal void SubscriptionAdded() => Interlocked.Increment(ref _activeSubscriptions);
    internal void SubscriptionRemoved() => Interlocked.Decrement(ref _activeSubscriptions);

    internal void SetQueueDepths(int controlDepth, int eventDepth)
    {
        Volatile.Write(ref _controlDepth, controlDepth);
        Volatile.Write(ref _eventDepth, eventDepth);
        ObserveMaximum(ref _peakTotalDepth, controlDepth + eventDepth);
    }

    internal void EventDropped() => Interlocked.Increment(ref _droppedEvents);

    internal void Close(DateTimeOffset closedAtUtc) =>
        Interlocked.CompareExchange(ref _closedAtUtcTicks, closedAtUtc.UtcDateTime.Ticks, 0);

    internal ConnectionMetricsSnapshot Snapshot()
    {
        long closedTicks = Interlocked.Read(ref _closedAtUtcTicks);
        int controlDepth = Volatile.Read(ref _controlDepth);
        int eventDepth = Volatile.Read(ref _eventDepth);
        return new(
            ConnectionId,
            _clientKind,
            _clientInstanceId,
            connectedAtUtc,
            closedTicks == 0 ? null : new DateTimeOffset(closedTicks, TimeSpan.Zero),
            Volatile.Read(ref _handshakeComplete) != 0,
            Volatile.Read(ref _activeRequests),
            Volatile.Read(ref _activeSubscriptions),
            new(
                controlDepth,
                eventDepth,
                controlDepth + eventDepth,
                Volatile.Read(ref _peakTotalDepth),
                Interlocked.Read(ref _droppedEvents),
                eventCapacity,
                controlCapacity));
    }

    private static void ObserveMaximum(ref int location, int value)
    {
        while (true)
        {
            int current = Volatile.Read(ref location);
            if (value <= current || Interlocked.CompareExchange(ref location, value, current) == current)
            {
                return;
            }
        }
    }
}

public sealed class ServerMetrics(string serverInstanceId, long serverEpoch)
{
    private readonly ConcurrentDictionary<string, ConnectionMetrics> _connections = new(StringComparer.Ordinal);
    private int _activeConnections;
    private long _acceptedConnections;
    private long _closedConnections;
    private long _completedHandshakes;
    private long _rejectedHandshakes;
    private long _protocolViolations;
    private long _oversizedFrames;
    private long _malformedFrames;
    private long _invalidUtf8Frames;
    private long _requestsStarted;
    private long _requestsCompleted;
    private long _requestsCancelled;
    private long _subscriptionsCreated;
    private long _subscriptionsRemoved;
    private long _eventsPublished;
    private long _eventsReplayed;
    private long _eventFramesDropped;
    private long _slowSubscriberDisconnects;
    private int _maximumObservedQueueDepth;
    private int _maximumObservedSubscriptionQueueDepth;

    public string ServerInstanceId { get; } = serverInstanceId;
    public long ServerEpoch { get; } = serverEpoch;

    internal ConnectionMetrics ConnectionAccepted(int controlCapacity, int eventCapacity)
    {
        string id = $"connection-{Guid.NewGuid():N}";
        ConnectionMetrics connection = new(id, DateTimeOffset.UtcNow, controlCapacity, eventCapacity);
        if (!_connections.TryAdd(id, connection))
        {
            throw new InvalidOperationException("Generated a duplicate connection identifier.");
        }
        Interlocked.Increment(ref _acceptedConnections);
        Interlocked.Increment(ref _activeConnections);
        return connection;
    }

    internal void ConnectionClosed(ConnectionMetrics connection)
    {
        connection.SetQueueDepths(0, 0);
        connection.Close(DateTimeOffset.UtcNow);
        Interlocked.Increment(ref _closedConnections);
        Interlocked.Decrement(ref _activeConnections);
    }

    internal void HandshakeCompleted() => Interlocked.Increment(ref _completedHandshakes);
    internal void HandshakeRejected() => Interlocked.Increment(ref _rejectedHandshakes);
    internal void ProtocolViolation() => Interlocked.Increment(ref _protocolViolations);
    internal void OversizedFrame() => Interlocked.Increment(ref _oversizedFrames);
    internal void MalformedFrame() => Interlocked.Increment(ref _malformedFrames);
    internal void InvalidUtf8Frame() => Interlocked.Increment(ref _invalidUtf8Frames);
    internal void RequestStarted() => Interlocked.Increment(ref _requestsStarted);
    internal void RequestCompleted() => Interlocked.Increment(ref _requestsCompleted);
    internal void RequestCancelled() => Interlocked.Increment(ref _requestsCancelled);
    internal void SubscriptionCreated() => Interlocked.Increment(ref _subscriptionsCreated);
    internal void SubscriptionRemoved() => Interlocked.Increment(ref _subscriptionsRemoved);
    internal void EventPublished() => Interlocked.Increment(ref _eventsPublished);
    internal void EventsReplayed(int count) => Interlocked.Add(ref _eventsReplayed, count);

    internal void EventFrameDropped(ConnectionMetrics connection)
    {
        connection.EventDropped();
        Interlocked.Increment(ref _eventFramesDropped);
    }

    internal void SlowSubscriberDisconnected() => Interlocked.Increment(ref _slowSubscriberDisconnects);

    internal void ObserveSubscriptionQueueDepth(int depth) =>
        ObserveMaximum(ref _maximumObservedSubscriptionQueueDepth, depth);

    internal void ObserveQueueDepth(ConnectionMetrics connection, int controlDepth, int eventDepth)
    {
        connection.SetQueueDepths(controlDepth, eventDepth);
        ObserveMaximum(ref _maximumObservedQueueDepth, controlDepth + eventDepth);
    }

    public ServerMetricsSnapshot Snapshot(EventJournalBounds bounds)
    {
        ConnectionMetricsSnapshot[] connections = _connections.Values
            .Select(connection => connection.Snapshot())
            .OrderBy(connection => connection.ConnectedAtUtc)
            .ToArray();
        int queued = connections.Sum(connection => connection.Queue.CurrentTotalDepth);
        using System.Diagnostics.Process process = System.Diagnostics.Process.GetCurrentProcess();
        int handleCount = OperatingSystem.IsWindows() ? process.HandleCount : -1;
        return new(
            "1.0",
            DateTimeOffset.UtcNow,
            ServerInstanceId,
            ServerEpoch,
            Interlocked.Read(ref _acceptedConnections),
            Volatile.Read(ref _activeConnections),
            Interlocked.Read(ref _closedConnections),
            Interlocked.Read(ref _completedHandshakes),
            Interlocked.Read(ref _rejectedHandshakes),
            Interlocked.Read(ref _protocolViolations),
            Interlocked.Read(ref _oversizedFrames),
            Interlocked.Read(ref _malformedFrames),
            Interlocked.Read(ref _invalidUtf8Frames),
            Interlocked.Read(ref _requestsStarted),
            Interlocked.Read(ref _requestsCompleted),
            Interlocked.Read(ref _requestsCancelled),
            Interlocked.Read(ref _subscriptionsCreated),
            Interlocked.Read(ref _subscriptionsRemoved),
            Interlocked.Read(ref _eventsPublished),
            Interlocked.Read(ref _eventsReplayed),
            Interlocked.Read(ref _eventFramesDropped),
            Interlocked.Read(ref _slowSubscriberDisconnects),
            Volatile.Read(ref _maximumObservedQueueDepth),
            Volatile.Read(ref _maximumObservedSubscriptionQueueDepth),
            queued,
            bounds.LatestSequence,
            bounds.MinimumAvailableSequence,
            process.WorkingSet64,
            handleCount,
            connections);
    }

    private static void ObserveMaximum(ref int location, int value)
    {
        while (true)
        {
            int current = Volatile.Read(ref location);
            if (value <= current || Interlocked.CompareExchange(ref location, value, current) == current)
            {
                return;
            }
        }
    }
}
