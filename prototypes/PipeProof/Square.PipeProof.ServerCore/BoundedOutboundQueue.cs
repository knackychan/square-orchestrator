using System.Threading.Channels;
using Square.PipeProof.Protocol;

namespace Square.PipeProof.ServerCore;

internal sealed class BoundedOutboundQueue
{
    private readonly Channel<IProtocolMessage> _control;
    private readonly Channel<IProtocolMessage> _events;
    private readonly SemaphoreSlim _available = new(0);
    private readonly TimeSpan _controlTimeout;
    private readonly ServerMetrics _metrics;
    private readonly ConnectionMetrics _connection;
    private int _controlDepth;
    private int _eventDepth;
    private int _completed;

    internal BoundedOutboundQueue(
        ProofServerOptions options,
        ServerMetrics metrics,
        ConnectionMetrics connection)
    {
        _metrics = metrics;
        _connection = connection;
        _controlTimeout = TimeSpan.FromMilliseconds(options.WriteTimeoutMilliseconds);
        _control = Create(options.ControlQueueCapacity);
        _events = Create(options.EventQueueCapacity);
        ObserveDepths();
    }

    internal int ControlDepth => Volatile.Read(ref _controlDepth);
    internal int EventDepth => Volatile.Read(ref _eventDepth);

    internal async ValueTask<bool> EnqueueControlAsync(
        IProtocolMessage message,
        CancellationToken cancellationToken)
    {
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_controlTimeout);
        try
        {
            await _control.Writer.WriteAsync(message, timeout.Token).ConfigureAwait(false);
            Interlocked.Increment(ref _controlDepth);
            ObserveDepths();
            _available.Release();
            return true;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
        catch (ChannelClosedException)
        {
            return false;
        }
    }

    internal bool TryEnqueueEvent(IProtocolMessage message)
    {
        if (!_events.Writer.TryWrite(message))
        {
            _metrics.EventFrameDropped(_connection);
            return false;
        }
        Interlocked.Increment(ref _eventDepth);
        ObserveDepths();
        _available.Release();
        return true;
    }

    internal async ValueTask<IProtocolMessage?> ReadAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            if (_control.Reader.TryRead(out IProtocolMessage? control))
            {
                Interlocked.Decrement(ref _controlDepth);
                ObserveDepths();
                return control;
            }
            if (_events.Reader.TryRead(out IProtocolMessage? eventMessage))
            {
                Interlocked.Decrement(ref _eventDepth);
                ObserveDepths();
                return eventMessage;
            }
            if (Volatile.Read(ref _completed) != 0)
            {
                return null;
            }
            await _available.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    internal void Complete(Exception? exception = null)
    {
        if (Interlocked.Exchange(ref _completed, 1) != 0)
        {
            return;
        }
        _control.Writer.TryComplete(exception);
        _events.Writer.TryComplete(exception);
        _available.Release();
    }

    private void ObserveDepths() =>
        _metrics.ObserveQueueDepth(_connection, ControlDepth, EventDepth);

    private static Channel<IProtocolMessage> Create(int capacity) =>
        Channel.CreateBounded<IProtocolMessage>(new BoundedChannelOptions(capacity)
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false,
            FullMode = BoundedChannelFullMode.Wait
        });
}
