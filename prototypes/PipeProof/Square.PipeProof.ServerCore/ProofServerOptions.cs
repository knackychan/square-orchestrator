using Square.PipeProof.Protocol;

namespace Square.PipeProof.ServerCore;

public sealed record ProofServerOptions
{
    public int MaximumPayloadBytes { get; init; } = ProtocolConstants.DefaultMaximumPayloadBytes;
    public int ControlQueueCapacity { get; init; } = 16;
    public int EventQueueCapacity { get; init; } = 16;
    public int SubscriptionQueueCapacity { get; init; } = 32;
    public int JournalRetentionCapacity { get; init; } = 256;
    public int MaximumReplayEvents { get; init; } = 32;
    public int MaximumInFlightRequests { get; init; } = 16;
    public int MaximumConnections { get; init; } = 32;
    public int WriteTimeoutMilliseconds { get; init; } = 750;
    public int MaximumWriteChunkBytes { get; init; } = int.MaxValue;
    public int MaximumPublishCount { get; init; } = 20_000;
    public int MaximumPublishedPayloadBytes { get; init; } = 65_536;
    public TimeSpan HandshakeTimeout { get; init; } = TimeSpan.FromSeconds(5);
    public TimeSpan ShutdownDrainTimeout { get; init; } = TimeSpan.FromSeconds(3);

    public ProtocolLimits ToLimits() => new(
        MaximumPayloadBytes,
        ControlQueueCapacity,
        EventQueueCapacity,
        SubscriptionQueueCapacity,
        MaximumReplayEvents,
        MaximumInFlightRequests,
        WriteTimeoutMilliseconds);

    public void Validate()
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumPayloadBytes);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ControlQueueCapacity);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(EventQueueCapacity);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(SubscriptionQueueCapacity);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(JournalRetentionCapacity);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumReplayEvents);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumInFlightRequests);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumConnections);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(WriteTimeoutMilliseconds);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumWriteChunkBytes);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumPublishCount);
        ArgumentOutOfRangeException.ThrowIfNegative(MaximumPublishedPayloadBytes);
        if (MaximumReplayEvents > SubscriptionQueueCapacity)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MaximumReplayEvents),
                "Replay capacity must not exceed the per-subscription queue capacity.");
        }
        if (MaximumReplayEvents > JournalRetentionCapacity)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MaximumReplayEvents),
                "Replay capacity must not exceed journal retention.");
        }
        if (HandshakeTimeout <= TimeSpan.Zero || ShutdownDrainTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(HandshakeTimeout), "Timeouts must be positive.");
        }
    }
}
