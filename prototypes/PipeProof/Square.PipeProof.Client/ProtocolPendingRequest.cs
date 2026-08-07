using Square.PipeProof.Protocol;

namespace Square.PipeProof.Client;

public sealed class ProtocolPendingRequest(
    string id,
    Task<ResponseMessage> response)
{
    public string Id { get; } = id;
    public Task<ResponseMessage> Response { get; } = response;
}
