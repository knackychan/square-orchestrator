using System.IO.Pipes;

namespace Square.PipeProof.Transport.Windows;

public static class NamedPipeConnector
{
    public static async Task<NamedPipeClientStream> ConnectAsync(
        string pipeName,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pipeName);
        if (timeout <= TimeSpan.Zero || timeout.TotalMilliseconds > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "Timeout is outside the supported range.");
        }

        NamedPipeClientStream stream = new(
            ".",
            pipeName,
            PipeDirection.InOut,
            PipeOptions.Asynchronous | PipeOptions.WriteThrough);
        try
        {
            await stream.ConnectAsync(
                checked((int)Math.Ceiling(timeout.TotalMilliseconds)),
                cancellationToken).ConfigureAwait(false);
            stream.ReadMode = PipeTransmissionMode.Byte;
            return stream;
        }
        catch
        {
            await stream.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }
}
