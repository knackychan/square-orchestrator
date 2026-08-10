namespace Square.PipeProof.ServerCore;

public interface IConnectionListener : IAsyncDisposable
{
    string Endpoint { get; }
    ValueTask<Stream> AcceptAsync(CancellationToken cancellationToken);
}
