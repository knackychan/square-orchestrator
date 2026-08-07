using System.Text.Json;
using Square.PipeProof.Protocol;

namespace Square.PipeProof.ServerCore;

internal sealed record SubscriptionRegistration(
    EventSubscription Subscription,
    EventReplayBatch Replay);

public sealed class EventHub : IAsyncDisposable
{
    private readonly DurableEventJournal _journal;
    private readonly ProofServerOptions _options;
    private readonly ServerMetrics _metrics;
    private readonly SemaphoreSlim _publishGate = new(1, 1);
    private readonly Dictionary<string, EventSubscription> _subscriptions = new(StringComparer.Ordinal);
    private int _disposed;

    public EventHub(DurableEventJournal journal, ProofServerOptions options, ServerMetrics metrics)
    {
        _journal = journal ?? throw new ArgumentNullException(nameof(journal));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
    }

    public EventJournalBounds Bounds => _journal.GetBounds();

    public async Task<DurableEventRecord> PublishAsync(
        string topic,
        string eventType,
        JsonElement payload,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        List<EventSubscription> failed = [];
        DurableEventRecord record;
        await _publishGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            record = await _journal.AppendAsync(topic, eventType, payload, cancellationToken)
                .ConfigureAwait(false);
            _metrics.EventPublished();
            foreach (EventSubscription subscription in _subscriptions.Values)
            {
                if (string.Equals(subscription.Topic, topic, StringComparison.Ordinal)
                    && !subscription.TryEnqueue(record))
                {
                    failed.Add(subscription);
                }
            }
            foreach (EventSubscription subscription in failed)
            {
                RemoveUnderGate(subscription.Id);
            }
        }
        finally
        {
            _publishGate.Release();
        }

        foreach (EventSubscription subscription in failed)
        {
            ScheduleBackpressureNotification(subscription);
        }
        return record;
    }

    internal async Task<SubscriptionRegistration> RegisterAsync(
        string topic,
        long fromSequence,
        Func<EventMessage, bool> sendEvent,
        Func<EventSubscription, long, Task> notifyConnection,
        Func<SubscriptionRegistration, CancellationToken, Task<bool>> activate,
        CancellationToken cancellationToken)
    {
        await _publishGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EventReplayBatch replay = _journal.Replay(topic, fromSequence, _options.MaximumReplayEvents);
            EventSubscription subscription = new(
                $"subscription-{Guid.NewGuid():N}",
                topic,
                _options.SubscriptionQueueCapacity,
                fromSequence == 0 ? replay.LatestSequence : fromSequence,
                sendEvent,
                (source, resumeFromSequence) => HandleBackpressureAsync(
                    source,
                    resumeFromSequence,
                    notifyConnection),
                _metrics.ObserveSubscriptionQueueDepth);
            foreach (DurableEventRecord record in replay.Events)
            {
                if (!subscription.TryEnqueue(record))
                {
                    await subscription.DisposeAsync().ConfigureAwait(false);
                    throw new InvalidOperationException("Replay did not fit the declared subscription queue.");
                }
            }
            _subscriptions.Add(subscription.Id, subscription);
            _metrics.SubscriptionCreated();
            _metrics.EventsReplayed(replay.Events.Count);
            SubscriptionRegistration registration = new(subscription, replay);
            try
            {
                if (!await activate(registration, cancellationToken).ConfigureAwait(false))
                {
                    throw new IOException("The subscription could not be activated on the bounded connection queue.");
                }
            }
            catch
            {
                if (_subscriptions.Remove(subscription.Id))
                {
                    _metrics.SubscriptionRemoved();
                }
                await subscription.DisposeAsync().ConfigureAwait(false);
                throw;
            }
            return registration;
        }
        finally
        {
            _publishGate.Release();
        }
    }

    internal async Task<bool> UnregisterAsync(string subscriptionId)
    {
        EventSubscription? subscription;
        await _publishGate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (!_subscriptions.Remove(subscriptionId, out subscription))
            {
                return false;
            }
            _metrics.SubscriptionRemoved();
        }
        finally
        {
            _publishGate.Release();
        }
        await subscription.DisposeAsync().ConfigureAwait(false);
        return true;
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }
        List<EventSubscription> subscriptions;
        await _publishGate.WaitAsync().ConfigureAwait(false);
        try
        {
            subscriptions = _subscriptions.Values.ToList();
            _subscriptions.Clear();
        }
        finally
        {
            _publishGate.Release();
        }
        foreach (EventSubscription subscription in subscriptions)
        {
            await subscription.DisposeAsync().ConfigureAwait(false);
        }
        _publishGate.Dispose();
        await _journal.DisposeAsync().ConfigureAwait(false);
    }

    private async Task HandleBackpressureAsync(
        EventSubscription subscription,
        long resumeFromSequence,
        Func<EventSubscription, long, Task> notifyConnection)
    {
        await _publishGate.WaitAsync().ConfigureAwait(false);
        try
        {
            RemoveUnderGate(subscription.Id);
        }
        finally
        {
            _publishGate.Release();
        }
        await notifyConnection(subscription, resumeFromSequence).ConfigureAwait(false);
        _ = Task.Run(async () => await subscription.DisposeAsync().ConfigureAwait(false));
    }

    private void RemoveUnderGate(string subscriptionId)
    {
        if (_subscriptions.Remove(subscriptionId))
        {
            _metrics.SubscriptionRemoved();
            _metrics.SlowSubscriberDisconnected();
        }
    }

    private static void ScheduleBackpressureNotification(EventSubscription subscription)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await subscription.NotifyBackpressureAsync().ConfigureAwait(false);
            }
            finally
            {
                await subscription.DisposeAsync().ConfigureAwait(false);
            }
        });
    }
}
