using Square.PipeProof.Protocol;

namespace Square.PipeProof.Client;

public sealed record ProtocolClientOptions
{
    public string ClientKind { get; init; } = "dotnet-proof-client";
    public string ClientVersion { get; init; } = "0.1.0";
    public string ClientInstanceId { get; init; } = $"client-{Guid.NewGuid():N}";
    public string RequestedProtocol { get; init; } = ProtocolConstants.ProtocolName;
    public string RequestedVersion { get; init; } = ProtocolConstants.CurrentVersion;
    public int MaximumPayloadBytes { get; init; } = ProtocolConstants.DefaultMaximumPayloadBytes;
    public int MaximumWriteChunkBytes { get; init; } = int.MaxValue;
    public int LocalSubscriptionCapacity { get; init; } = 256;
}
