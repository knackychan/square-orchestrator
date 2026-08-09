using System.Threading.Channels;
using Square.PipeProof.Protocol;

namespace Square.PipeProof.ServerCore;

internal sealed class EventSubscription : IAsyncDisposable
{
    private readonly Channel<DurableEventRecord> _queue;
    private readonly Func<EventMessage, bool> _sendEvent;
    private readonly Func<EventSubscription, long, Task> _backpressure;
    private readonly Action<int> _observeDepth;
    private readonly CancellationTokenSource _lifetime = new();
    private Task? _pump;
    private int _depth;
    private long _lastDeliveredSequence;
    private int _backpressureSignaled;
    private int _started;
    private int _disposed;

    internal EventSubscription(
        string id,
        string topic,
        int capacity,
        long initialResumeSequence,
        Func<EventMessage, bool> sendEvent,
        Func<EventSubscription, long, Task> backpressure,
        Action<int> observeDepth)
    {
        Id = id;
        Topic = topic;
        ArgumentOutOfRangeException.ThrowIfNegative(initialResumeSequence);
        _lastDeliveredSequence = initialResumeSequence;
        _sendEvent = sendEvent;
        _backpressure = backpressure;
        _observeDepth = observeDepth;
        _queue = Channel.CreateBounded<DurableEventRecord>(new BoundedChannelOptions(capacity)
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false,
            FullMode = BoundedChannelFullMode.Wait
        });
        _observeDepth(0);
    }

    internal string Id { get; }
    internal string Topic { get; }
    internal long LastDeliveredSequence => Interlocked.Read(ref _lastDeliveredSequence);
    internal bool IsStarted => Volatile.Read(ref _started) != 0;

    internal bool TryEnqueue(DurableEventRecord record)
    {
        if (!_queue.Writer.TryWrite(record))
        {
            return false;
        }
        int depth = Interlocked.Increment(ref _depth);
        _observeDepth(depth);
        return true;
    }

    internal async Task NotifyBackpressureAsync()
    {
        if (Interlocked.Exchange(ref _backpressureSignaled, 1) != 0)
        {
            return;
        }
        await _backpressure(this, LastDeliveredSequence).ConfigureAwait(false);
    }

    internal void Start()
    {
        if (Interlocked.Exchange(ref _started, 1) != 0)
        {
            throw new InvalidOperationException("Subscription pump was already started.");
        }
        _pump = Task.Run(PumpAsync);
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }
        _lifetime.Cancel();
        _queue.Writer.TryComplete();
        if (_pump is not null)
        {
            try
            {
                await _pump.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }
        _lifetime.Dispose();
    }

    private async Task PumpAsync()
    {
        try
        {
            await foreach (DurableEventRecord record in _queue.Reader.ReadAllAsync(_lifetime.Token)
                .ConfigureAwait(false))
            {
                int depth = Interlocked.Decrement(ref _depth);
                _observeDepth(depth);
                EventMessage message = new()
                {
                    SubscriptionId = Id,
                    Topic = Topic,
                    Sequence = record.Sequence,
                    EventType = record.EventType,
                    Payload = record.Payload
                };
                if (!_sendEvent(message))
                {
                    await NotifyBackpressureAsync().ConfigureAwait(false);
                    return;
                }
                Interlocked.Exchange(ref _lastDeliveredSequence, record.Sequence);
            }
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
            await NotifyBackpressureAsync().ConfigureAwait(false);
        }
    }
}
