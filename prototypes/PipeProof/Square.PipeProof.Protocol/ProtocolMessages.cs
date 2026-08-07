using System.Text.Json;
using System.Text.Json.Serialization;

namespace Square.PipeProof.Protocol;

public interface IProtocolMessage
{
    string Kind { get; }
    string Protocol { get; }
    string Version { get; }
}

public abstract record ProtocolMessage : IProtocolMessage
{
    [JsonRequired]
    [JsonPropertyOrder(-3)]
    public string Kind { get; init; } = string.Empty;

    [JsonRequired]
    [JsonPropertyOrder(-2)]
    public string Protocol { get; init; } = ProtocolConstants.ProtocolName;

    [JsonRequired]
    [JsonPropertyOrder(-1)]
    public string Version { get; init; } = ProtocolConstants.CurrentVersion;
}

public sealed record ClientDescriptor(
    string Kind,
    string Version,
    string InstanceId);

public sealed record ServerDescriptor(
    string Kind,
    string Version,
    string InstanceId,
    long Epoch);

public sealed record ProtocolLimits(
    int MaximumPayloadBytes,
    int ControlQueueCapacity,
    int EventQueueCapacity,
    int SubscriptionQueueCapacity,
    int MaximumReplayEvents,
    int MaximumInFlightRequests,
    int WriteTimeoutMilliseconds);

public sealed record ProtocolError(
    string Code,
    string Message,
    JsonElement? Data = null);

public sealed record HelloMessage : ProtocolMessage
{
    public HelloMessage() => Kind = ProtocolMessageKinds.Hello;

    public required string Id { get; init; }
    public required ClientDescriptor Client { get; init; }
    public required string[] Capabilities { get; init; }
}

public sealed record HelloAckMessage : ProtocolMessage
{
    public HelloAckMessage() => Kind = ProtocolMessageKinds.HelloAck;

    public required string ReplyTo { get; init; }
    public required ServerDescriptor Server { get; init; }
    public required string[] Capabilities { get; init; }
    public required ProtocolLimits Limits { get; init; }
    public required long MinimumAvailableSequence { get; init; }
    public required long LatestSequence { get; init; }
}

public sealed record RequestMessage : ProtocolMessage
{
    public RequestMessage() => Kind = ProtocolMessageKinds.Request;

    public required string Id { get; init; }
    public required string Method { get; init; }
    public required JsonElement Params { get; init; }
}

public sealed record ResponseMessage : ProtocolMessage
{
    public ResponseMessage() => Kind = ProtocolMessageKinds.Response;

    public required string ReplyTo { get; init; }
    public JsonElement? Result { get; init; }
    public ProtocolError? Error { get; init; }
}

public sealed record CancelMessage : ProtocolMessage
{
    public CancelMessage() => Kind = ProtocolMessageKinds.Cancel;

    public required string Id { get; init; }
    public required string TargetRequestId { get; init; }
}

public sealed record SubscribeMessage : ProtocolMessage
{
    public SubscribeMessage() => Kind = ProtocolMessageKinds.Subscribe;

    public required string Id { get; init; }
    public required string Topic { get; init; }
    public required long FromSequence { get; init; }
}

public sealed record SubscribedMessage : ProtocolMessage
{
    public SubscribedMessage() => Kind = ProtocolMessageKinds.Subscribed;

    public required string ReplyTo { get; init; }
    public required string SubscriptionId { get; init; }
    public required string Topic { get; init; }
    public required long FromSequence { get; init; }
    public required long ReplayedThroughSequence { get; init; }
    public required long LiveFromSequence { get; init; }
    public required long MinimumAvailableSequence { get; init; }
    public required long LatestSequence { get; init; }
}

public sealed record UnsubscribeMessage : ProtocolMessage
{
    public UnsubscribeMessage() => Kind = ProtocolMessageKinds.Unsubscribe;

    public required string Id { get; init; }
    public required string SubscriptionId { get; init; }
}

public sealed record EventMessage : ProtocolMessage
{
    public EventMessage() => Kind = ProtocolMessageKinds.Event;

    public required string SubscriptionId { get; init; }
    public required string Topic { get; init; }
    public required long Sequence { get; init; }
    public required string EventType { get; init; }
    public required JsonElement Payload { get; init; }
}

public sealed record SubscriptionClosedMessage : ProtocolMessage
{
    public SubscriptionClosedMessage() => Kind = ProtocolMessageKinds.SubscriptionClosed;

    public required string SubscriptionId { get; init; }
    public required string Code { get; init; }
    public required string Message { get; init; }
    public required long ResumeFromSequence { get; init; }
}

public sealed record ProtocolErrorMessage : ProtocolMessage
{
    public ProtocolErrorMessage() => Kind = ProtocolMessageKinds.ProtocolError;

    public string? ReplyTo { get; init; }
    public required ProtocolError Error { get; init; }
    public string[]? SupportedVersions { get; init; }
}

public sealed record ServerGoingAwayMessage : ProtocolMessage
{
    public ServerGoingAwayMessage() => Kind = ProtocolMessageKinds.ServerGoingAway;

    public required string Reason { get; init; }
    public required int ReconnectDelayMilliseconds { get; init; }
}

public sealed record DurableEventRecord(
    long Sequence,
    string Topic,
    string EventType,
    JsonElement Payload,
    DateTimeOffset PublishedAtUtc);
