using System.Text.Json;

namespace Square.PipeProof.Protocol;

public static class ProtocolMessageCodec
{
    public static byte[] Serialize(IProtocolMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        ProtocolMessageValidator.Validate(message);
        return message switch
        {
            HelloMessage value => ProtocolJson.Serialize(value),
            HelloAckMessage value => ProtocolJson.Serialize(value),
            RequestMessage value => ProtocolJson.Serialize(value),
            ResponseMessage value => ProtocolJson.Serialize(value),
            CancelMessage value => ProtocolJson.Serialize(value),
            SubscribeMessage value => ProtocolJson.Serialize(value),
            SubscribedMessage value => ProtocolJson.Serialize(value),
            UnsubscribeMessage value => ProtocolJson.Serialize(value),
            EventMessage value => ProtocolJson.Serialize(value),
            SubscriptionClosedMessage value => ProtocolJson.Serialize(value),
            ProtocolErrorMessage value => ProtocolJson.Serialize(value),
            ServerGoingAwayMessage value => ProtocolJson.Serialize(value),
            _ => throw new NotSupportedException($"Unsupported protocol message type {message.GetType().FullName}.")
        };
    }

    public static IProtocolMessage Deserialize(ReadOnlySpan<byte> utf8Json)
    {
        string kind;
        try
        {
            using JsonDocument document = JsonDocument.Parse(utf8Json.ToArray());
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidDataException("Protocol message root must be a JSON object.");
            }
            if (!root.TryGetProperty("kind", out JsonElement kindElement)
                || kindElement.ValueKind != JsonValueKind.String
                || string.IsNullOrWhiteSpace(kindElement.GetString()))
            {
                throw new InvalidDataException("Protocol message requires a non-empty string 'kind'.");
            }
            kind = kindElement.GetString()!;
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Protocol message is not valid UTF-8 JSON.", exception);
        }

        IProtocolMessage message;
        try
        {
            message = kind switch
            {
                ProtocolMessageKinds.Hello => ProtocolJson.Deserialize<HelloMessage>(utf8Json),
                ProtocolMessageKinds.HelloAck => ProtocolJson.Deserialize<HelloAckMessage>(utf8Json),
                ProtocolMessageKinds.Request => ProtocolJson.Deserialize<RequestMessage>(utf8Json),
                ProtocolMessageKinds.Response => ProtocolJson.Deserialize<ResponseMessage>(utf8Json),
                ProtocolMessageKinds.Cancel => ProtocolJson.Deserialize<CancelMessage>(utf8Json),
                ProtocolMessageKinds.Subscribe => ProtocolJson.Deserialize<SubscribeMessage>(utf8Json),
                ProtocolMessageKinds.Subscribed => ProtocolJson.Deserialize<SubscribedMessage>(utf8Json),
                ProtocolMessageKinds.Unsubscribe => ProtocolJson.Deserialize<UnsubscribeMessage>(utf8Json),
                ProtocolMessageKinds.Event => ProtocolJson.Deserialize<EventMessage>(utf8Json),
                ProtocolMessageKinds.SubscriptionClosed => ProtocolJson.Deserialize<SubscriptionClosedMessage>(utf8Json),
                ProtocolMessageKinds.ProtocolError => ProtocolJson.Deserialize<ProtocolErrorMessage>(utf8Json),
                ProtocolMessageKinds.ServerGoingAway => ProtocolJson.Deserialize<ServerGoingAwayMessage>(utf8Json),
                _ => throw new InvalidDataException($"Unknown protocol message kind '{kind}'.")
            };
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Protocol message does not match its strict schema.", exception);
        }

        ProtocolMessageValidator.Validate(message);
        return message;
    }
}
