namespace Square.PipeProof.Protocol;

public static class ProtocolConstants
{
    public const string ProtocolName = "square.rpc";
    public const string CurrentVersion = "1.0";
    public const int DefaultMaximumPayloadBytes = 1_048_576;
    public const int MaximumIdentifierLength = 128;
    public const int MaximumMethodLength = 128;
    public const int MaximumTopicLength = 256;

    public static readonly string[] SupportedVersions = [CurrentVersion];

    public static readonly string[] ServerCapabilities =
    [
        "request",
        "cancel",
        "subscribe",
        "replay",
        "bounded_backpressure"
    ];
}

public static class ProtocolMessageKinds
{
    public const string Hello = "hello";
    public const string HelloAck = "hello_ack";
    public const string Request = "request";
    public const string Response = "response";
    public const string Cancel = "cancel";
    public const string Subscribe = "subscribe";
    public const string Subscribed = "subscribed";
    public const string Unsubscribe = "unsubscribe";
    public const string Event = "event";
    public const string SubscriptionClosed = "subscription_closed";
    public const string ProtocolError = "protocol_error";
    public const string ServerGoingAway = "server_going_away";
}

public static class ProtocolErrorCodes
{
    public const string MalformedMessage = "protocol.malformed_message";
    public const string HandshakeRequired = "protocol.handshake_required";
    public const string IncompatibleProtocol = "protocol.incompatible_protocol";
    public const string IncompatibleVersion = "protocol.incompatible_version";
    public const string UnexpectedMessage = "protocol.unexpected_message";
    public const string FrameTooLarge = "protocol.frame_too_large";
    public const string DuplicateRequest = "request.duplicate_id";
    public const string RequestCancelled = "request.cancelled";
    public const string RequestInvalid = "request.invalid";
    public const string MethodNotFound = "request.method_not_found";
    public const string ServerBusy = "request.server_busy";
    public const string ReplayTruncated = "subscription.replay_truncated";
    public const string ReplayLimitExceeded = "subscription.replay_limit_exceeded";
    public const string SubscriptionNotFound = "subscription.not_found";
    public const string BackpressureExceeded = "subscription.backpressure_exceeded";
    public const string InternalError = "server.internal_error";
}
