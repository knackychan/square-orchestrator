using Square.Domain.Primitives;

namespace Square.Contracts;

public static class ContractVersions
{
    public static SchemaVersion DraftV1 { get; } = new(1, 0);
    public const string RpcProtocol = "square.rpc";
}
