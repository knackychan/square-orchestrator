using System.Text.Json;
using Square.Domain.Primitives;

namespace Square.Contracts.Rpc;

public sealed record RpcClientDescriptor(string Kind, string Version, string InstanceId);
public sealed record RpcRequestEnvelope(string Protocol, SchemaVersion Version, CorrelationId Id, string Method, string IdempotencyKey, JsonElement Params, RpcClientDescriptor Client);
public sealed record RpcError(string Code, string Message, JsonElement? Details = null);
public sealed record RpcResponseEnvelope<T>(string Protocol, SchemaVersion Version, CorrelationId Id, T? Result, RpcError? Error);
public sealed record RpcEventEnvelope<T>(string Protocol, SchemaVersion Version, string SubscriptionId, long Sequence, T Event);
