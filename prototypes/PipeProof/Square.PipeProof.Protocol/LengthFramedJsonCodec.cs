using System.Buffers.Binary;

namespace Square.PipeProof.Protocol;

public sealed class LengthFramedJsonCodec
{
    private readonly int _maximumPayloadBytes;
    private readonly int _maximumWriteChunkBytes;

    public LengthFramedJsonCodec(
        int maximumPayloadBytes = ProtocolConstants.DefaultMaximumPayloadBytes,
        int maximumWriteChunkBytes = int.MaxValue)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumPayloadBytes);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumWriteChunkBytes);
        _maximumPayloadBytes = maximumPayloadBytes;
        _maximumWriteChunkBytes = maximumWriteChunkBytes;
    }

    public int MaximumPayloadBytes => _maximumPayloadBytes;

    public byte[] Encode(IProtocolMessage message) => EncodePayload(ProtocolMessageCodec.Serialize(message));

    public byte[] EncodePayload(ReadOnlySpan<byte> payload)
    {
        ValidatePayloadLength(payload.Length);
        byte[] frame = new byte[sizeof(uint) + payload.Length];
        BinaryPrimitives.WriteUInt32BigEndian(frame, checked((uint)payload.Length));
        payload.CopyTo(frame.AsSpan(sizeof(uint)));
        return frame;
    }

    public async ValueTask WriteMessageAsync(
        Stream stream,
        IProtocolMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        byte[] frame = Encode(message);
        for (int offset = 0; offset < frame.Length; offset += _maximumWriteChunkBytes)
        {
            int count = Math.Min(_maximumWriteChunkBytes, frame.Length - offset);
            await stream.WriteAsync(frame.AsMemory(offset, count), cancellationToken).ConfigureAwait(false);
        }
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<IProtocolMessage?> ReadMessageAsync(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        byte[]? payload = await ReadPayloadAsync(stream, cancellationToken).ConfigureAwait(false);
        return payload is null ? null : ProtocolMessageCodec.Deserialize(payload);
    }

    public async ValueTask<byte[]?> ReadPayloadAsync(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        byte[] prefix = new byte[sizeof(uint)];
        int prefixBytes = await ReadExactlyAsync(stream, prefix, allowCleanEndOfStream: true, cancellationToken)
            .ConfigureAwait(false);
        if (prefixBytes == 0)
        {
            return null;
        }

        uint unsignedLength = BinaryPrimitives.ReadUInt32BigEndian(prefix);
        if (unsignedLength == 0 || unsignedLength > int.MaxValue || unsignedLength > _maximumPayloadBytes)
        {
            throw new FrameSizeException(unsignedLength, _maximumPayloadBytes);
        }

        byte[] payload = new byte[(int)unsignedLength];
        _ = await ReadExactlyAsync(stream, payload, allowCleanEndOfStream: false, cancellationToken)
            .ConfigureAwait(false);
        return payload;
    }

    private void ValidatePayloadLength(int payloadLength)
    {
        if (payloadLength <= 0 || payloadLength > _maximumPayloadBytes)
        {
            throw new FrameSizeException(checked((uint)Math.Max(payloadLength, 0)), _maximumPayloadBytes);
        }
    }

    private static async ValueTask<int> ReadExactlyAsync(
        Stream stream,
        Memory<byte> buffer,
        bool allowCleanEndOfStream,
        CancellationToken cancellationToken)
    {
        int total = 0;
        while (total < buffer.Length)
        {
            int read = await stream.ReadAsync(buffer[total..], cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                if (total == 0 && allowCleanEndOfStream)
                {
                    return 0;
                }
                throw new EndOfStreamException(
                    $"The framed stream ended after {total} of {buffer.Length} required bytes.");
            }
            total += read;
        }
        return total;
    }
}

public sealed class FrameSizeException(uint declaredLength, int maximumLength)
    : IOException($"Frame payload length {declaredLength} is outside the allowed range 1..{maximumLength}.")
{
    public uint DeclaredLength { get; } = declaredLength;
    public int MaximumLength { get; } = maximumLength;
}
