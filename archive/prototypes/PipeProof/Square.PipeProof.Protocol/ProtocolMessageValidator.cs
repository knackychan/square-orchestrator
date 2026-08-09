using System.Text.Json;

namespace Square.PipeProof.Protocol;

public static class ProtocolMessageValidator
{
    public static void Validate(IProtocolMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        RequireNonEmpty(message.Protocol, nameof(message.Protocol));
        RequireNonEmpty(message.Version, nameof(message.Version));
        RequireNonEmpty(message.Kind, nameof(message.Kind));
        if (message is not HelloMessage)
        {
            RequireEqual(ProtocolConstants.ProtocolName, message.Protocol, nameof(message.Protocol));
            RequireEqual(ProtocolConstants.CurrentVersion, message.Version, nameof(message.Version));
        }

        switch (message)
        {
            case HelloMessage value:
                RequireKind(value, ProtocolMessageKinds.Hello);
                RequireId(value.Id, nameof(value.Id));
                ValidateClient(value.Client);
                ValidateCapabilities(value.Capabilities);
                break;
            case HelloAckMessage value:
                RequireKind(value, ProtocolMessageKinds.HelloAck);
                RequireId(value.ReplyTo, nameof(value.ReplyTo));
                ValidateServer(value.Server);
                ValidateCapabilities(value.Capabilities);
                ValidateLimits(value.Limits);
                RequirePositive(value.MinimumAvailableSequence, nameof(value.MinimumAvailableSequence));
                RequireNonNegative(value.LatestSequence, nameof(value.LatestSequence));
                if (value.MinimumAvailableSequence > value.LatestSequence + 1)
                {
                    throw new InvalidDataException("Minimum available sequence cannot exceed latest sequence plus one.");
                }
                break;
            case RequestMessage value:
                RequireKind(value, ProtocolMessageKinds.Request);
                RequireId(value.Id, nameof(value.Id));
                RequireBounded(value.Method, ProtocolConstants.MaximumMethodLength, nameof(value.Method));
                RequireObject(value.Params, nameof(value.Params));
                break;
            case ResponseMessage value:
                RequireKind(value, ProtocolMessageKinds.Response);
                RequireId(value.ReplyTo, nameof(value.ReplyTo));
                if ((value.Result is null) == (value.Error is null))
                {
                    throw new InvalidDataException("A response must contain exactly one of result or error.");
                }
                if (value.Error is not null)
                {
                    ValidateError(value.Error);
                }
                break;
            case CancelMessage value:
                RequireKind(value, ProtocolMessageKinds.Cancel);
                RequireId(value.Id, nameof(value.Id));
                RequireId(value.TargetRequestId, nameof(value.TargetRequestId));
                break;
            case SubscribeMessage value:
                RequireKind(value, ProtocolMessageKinds.Subscribe);
                RequireId(value.Id, nameof(value.Id));
                RequireTopic(value.Topic);
                RequireNonNegative(value.FromSequence, nameof(value.FromSequence));
                break;
            case SubscribedMessage value:
                RequireKind(value, ProtocolMessageKinds.Subscribed);
                RequireId(value.ReplyTo, nameof(value.ReplyTo));
                RequireId(value.SubscriptionId, nameof(value.SubscriptionId));
                RequireTopic(value.Topic);
                RequireNonNegative(value.FromSequence, nameof(value.FromSequence));
                RequireNonNegative(value.ReplayedThroughSequence, nameof(value.ReplayedThroughSequence));
                RequirePositive(value.LiveFromSequence, nameof(value.LiveFromSequence));
                RequirePositive(value.MinimumAvailableSequence, nameof(value.MinimumAvailableSequence));
                RequireNonNegative(value.LatestSequence, nameof(value.LatestSequence));
                break;
            case UnsubscribeMessage value:
                RequireKind(value, ProtocolMessageKinds.Unsubscribe);
                RequireId(value.Id, nameof(value.Id));
                RequireId(value.SubscriptionId, nameof(value.SubscriptionId));
                break;
            case EventMessage value:
                RequireKind(value, ProtocolMessageKinds.Event);
                RequireId(value.SubscriptionId, nameof(value.SubscriptionId));
                RequireTopic(value.Topic);
                RequirePositive(value.Sequence, nameof(value.Sequence));
                RequireBounded(value.EventType, ProtocolConstants.MaximumMethodLength, nameof(value.EventType));
                RequireObject(value.Payload, nameof(value.Payload));
                break;
            case SubscriptionClosedMessage value:
                RequireKind(value, ProtocolMessageKinds.SubscriptionClosed);
                RequireId(value.SubscriptionId, nameof(value.SubscriptionId));
                RequireBounded(value.Code, ProtocolConstants.MaximumIdentifierLength, nameof(value.Code));
                RequireNonEmpty(value.Message, nameof(value.Message));
                RequireNonNegative(value.ResumeFromSequence, nameof(value.ResumeFromSequence));
                break;
            case ProtocolErrorMessage value:
                RequireKind(value, ProtocolMessageKinds.ProtocolError);
                if (value.ReplyTo is not null)
                {
                    RequireId(value.ReplyTo, nameof(value.ReplyTo));
                }
                ValidateError(value.Error);
                if (value.SupportedVersions is not null)
                {
                    ValidateCapabilities(value.SupportedVersions);
                }
                break;
            case ServerGoingAwayMessage value:
                RequireKind(value, ProtocolMessageKinds.ServerGoingAway);
                RequireNonEmpty(value.Reason, nameof(value.Reason));
                RequireNonNegative(value.ReconnectDelayMilliseconds, nameof(value.ReconnectDelayMilliseconds));
                break;
            default:
                throw new NotSupportedException($"Unsupported protocol message type {message.GetType().FullName}.");
        }
    }

    private static void ValidateClient(ClientDescriptor client)
    {
        ArgumentNullException.ThrowIfNull(client);
        RequireBounded(client.Kind, ProtocolConstants.MaximumIdentifierLength, nameof(client.Kind));
        RequireBounded(client.Version, ProtocolConstants.MaximumIdentifierLength, nameof(client.Version));
        RequireId(client.InstanceId, nameof(client.InstanceId));
    }

    private static void ValidateServer(ServerDescriptor server)
    {
        ArgumentNullException.ThrowIfNull(server);
        RequireBounded(server.Kind, ProtocolConstants.MaximumIdentifierLength, nameof(server.Kind));
        RequireBounded(server.Version, ProtocolConstants.MaximumIdentifierLength, nameof(server.Version));
        RequireId(server.InstanceId, nameof(server.InstanceId));
        RequirePositive(server.Epoch, nameof(server.Epoch));
    }

    private static void ValidateCapabilities(string[] capabilities)
    {
        ArgumentNullException.ThrowIfNull(capabilities);
        HashSet<string> distinct = new(StringComparer.Ordinal);
        foreach (string capability in capabilities)
        {
            RequireBounded(capability, ProtocolConstants.MaximumIdentifierLength, "capability");
            if (!distinct.Add(capability))
            {
                throw new InvalidDataException($"Capability '{capability}' appears more than once.");
            }
        }
    }

    private static void ValidateLimits(ProtocolLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits);
        RequirePositive(limits.MaximumPayloadBytes, nameof(limits.MaximumPayloadBytes));
        RequirePositive(limits.ControlQueueCapacity, nameof(limits.ControlQueueCapacity));
        RequirePositive(limits.EventQueueCapacity, nameof(limits.EventQueueCapacity));
        RequirePositive(limits.SubscriptionQueueCapacity, nameof(limits.SubscriptionQueueCapacity));
        RequirePositive(limits.MaximumReplayEvents, nameof(limits.MaximumReplayEvents));
        RequirePositive(limits.MaximumInFlightRequests, nameof(limits.MaximumInFlightRequests));
        RequirePositive(limits.WriteTimeoutMilliseconds, nameof(limits.WriteTimeoutMilliseconds));
    }

    private static void ValidateError(ProtocolError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        RequireBounded(error.Code, ProtocolConstants.MaximumIdentifierLength, nameof(error.Code));
        RequireNonEmpty(error.Message, nameof(error.Message));
    }

    private static void RequireKind(IProtocolMessage message, string expected) =>
        RequireEqual(expected, message.Kind, nameof(message.Kind));

    private static void RequireTopic(string topic) =>
        RequireBounded(topic, ProtocolConstants.MaximumTopicLength, nameof(topic));

    private static void RequireId(string value, string name) =>
        RequireBounded(value, ProtocolConstants.MaximumIdentifierLength, name);

    private static void RequireObject(JsonElement value, string name)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException($"{name} must be a JSON object.");
        }
    }

    private static void RequireBounded(string value, int maximumLength, string name)
    {
        RequireNonEmpty(value, name);
        if (value.Length > maximumLength)
        {
            throw new InvalidDataException($"{name} exceeds {maximumLength} characters.");
        }
    }

    private static void RequireNonEmpty(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidDataException($"{name} must not be empty.");
        }
    }

    private static void RequireEqual(string expected, string actual, string name)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"{name} must be '{expected}'.");
        }
    }

    private static void RequirePositive(long value, string name)
    {
        if (value <= 0)
        {
            throw new InvalidDataException($"{name} must be greater than zero.");
        }
    }

    private static void RequireNonNegative(long value, string name)
    {
        if (value < 0)
        {
            throw new InvalidDataException($"{name} must not be negative.");
        }
    }
}
