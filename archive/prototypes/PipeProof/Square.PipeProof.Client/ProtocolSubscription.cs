using System.Threading.Channels;
using Square.PipeProof.Protocol;

namespace Square.PipeProof.Client;

public sealed class ProtocolSubscription : IAsyncDisposable
{
    private readonly ProtocolClientConnection _connection;
    private readonly Channel<EventMessage> _events;
    private int _disposed;

    internal ProtocolSubscription(
        ProtocolClientConnection connection,
        SubscribedMessage accepted,
        int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _connection = connection;
        Accepted = accepted;
        _events = Channel.CreateBounded<EventMessage>(new BoundedChannelOptions(capacity)
        {
            SingleReader = false,
            SingleWriter = true,
            AllowSynchronousContinuations = false,
            FullMode = BoundedChannelFullMode.Wait
        });
    }

    public SubscribedMessage Accepted { get; }
    public string Id => Accepted.SubscriptionId;
    public string Topic => Accepted.Topic;
    public ChannelReader<EventMessage> Events => _events.Reader;

    internal bool TryPublish(EventMessage message) => _events.Writer.TryWrite(message);

    internal void Complete(Exception? exception = null) => _events.Writer.TryComplete(exception);

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        try
        {
            await _connection.UnsubscribeAsync(Id).ConfigureAwait(false);
        }
        catch (Exception exception) when (
            exception is IOException
            or ObjectDisposedException
            or OperationCanceledException
            or RemoteProtocolException)
        {
        }
        finally
        {
            Complete();
        }
    }
}
