namespace Square.PipeProof.Protocol;

public sealed class RemoteProtocolException(
    ProtocolError error,
    IReadOnlyList<string>? supportedVersions = null) : Exception(error.Message)
{
    public ProtocolError Error { get; } = error;
    public IReadOnlyList<string> SupportedVersions { get; } = supportedVersions ?? [];
}

public sealed class ProtocolDisconnectedException(string message, Exception? innerException = null)
    : IOException(message, innerException);
