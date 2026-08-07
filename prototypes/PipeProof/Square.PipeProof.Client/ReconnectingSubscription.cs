using System.Threading.Channels;
using Square.PipeProof.Protocol;

namespace Square.PipeProof.Client;

public sealed record ReconnectingSubscriptionOptions
{
    public required Func<CancellationToken, Task<ProtocolClientConnection>> ConnectAsync { get; init; }
    public required string Topic { get; init; }
    public long FromSequence { get; init; }
    public int OutputCapacity { get; init; } = 256;
    public int MinimumReconnectDelayMilliseconds { get; init; } = 50;
    public int MaximumReconnectDelayMilliseconds { get; init; } = 1_000;
}

public sealed class ReconnectingSubscription : IAsyncDisposable
{
    private readonly ReconnectingSubscriptionOptions _options;
    private readonly Channel<EventMessage> _events;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly Task _pump;
    private long _lastSequence;
    private int _successfulConnections;
    private int _disposed;

    public ReconnectingSubscription(ReconnectingSubscriptionOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Topic);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.OutputCapacity);
        ArgumentOutOfRangeException.ThrowIfNegative(options.FromSequence);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.MinimumReconnectDelayMilliseconds);
        if (options.MaximumReconnectDelayMilliseconds < options.MinimumReconnectDelayMilliseconds)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.MaximumReconnectDelayMilliseconds),
                "Maximum reconnect delay must not be lower than the minimum.");
        }
        _lastSequence = options.FromSequence;
        _events = Channel.CreateBounded<EventMessage>(new BoundedChannelOptions(options.OutputCapacity)
        {
            SingleReader = false,
            SingleWriter = true,
            AllowSynchronousContinuations = false,
            FullMode = BoundedChannelFullMode.Wait
        });
        _pump = Task.Run(PumpAsync);
    }

    public ChannelReader<EventMessage> Events => _events.Reader;
    public long LastSequence => Interlocked.Read(ref _lastSequence);
    public int SuccessfulConnections => Volatile.Read(ref _successfulConnections);

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }
        _lifetime.Cancel();
        try
        {
            await _pump.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        _lifetime.Dispose();
    }

    private bool ObserveSequence(long sequence)
    {
        while (true)
        {
            long previous = Interlocked.Read(ref _lastSequence);
            if (sequence <= previous)
            {
                return false;
            }
            if (Interlocked.CompareExchange(ref _lastSequence, sequence, previous) == previous)
            {
                return true;
            }
        }
    }

    private async Task PumpAsync()
    {
        int delay = _options.MinimumReconnectDelayMilliseconds;
        try
        {
            while (!_lifetime.IsCancellationRequested)
            {
                try
                {
                    await using ProtocolClientConnection connection = await _options.ConnectAsync(_lifetime.Token)
                        .ConfigureAwait(false);
                    Interlocked.Increment(ref _successfulConnections);
                    delay = _options.MinimumReconnectDelayMilliseconds;
                    await using ProtocolSubscription subscription = await connection.SubscribeAsync(
                        _options.Topic,
                        LastSequence,
                        _lifetime.Token).ConfigureAwait(false);
                    ObserveSequence(subscription.Accepted.LiveFromSequence - 1);
                    await foreach (EventMessage message in subscription.Events.ReadAllAsync(_lifetime.Token)
                        .ConfigureAwait(false))
                    {
                        if (!ObserveSequence(message.Sequence))
                        {
                            continue;
                        }
                        await _events.Writer.WriteAsync(message, _lifetime.Token).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception) when (
                    exception is IOException
                    or RemoteProtocolException
                    or InvalidDataException
                    or System.Threading.Channels.ChannelClosedException)
                {
                    await Task.Delay(delay, _lifetime.Token).ConfigureAwait(false);
                    delay = Math.Min(delay * 2, _options.MaximumReconnectDelayMilliseconds);
                }
            }
            _events.Writer.TryComplete();
        }
        catch (OperationCanceledException)
        {
            _events.Writer.TryComplete();
        }
        catch (Exception exception)
        {
            _events.Writer.TryComplete(exception);
        }
    }
}
