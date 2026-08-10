using System.Buffers.Binary;
using System.Text;
using System.Text.Json;
using Square.PipeProof.Protocol;
using Square.PipeProof.ServerCore;

namespace Square.PipeProof.Tests;

internal static class Program
{
    private static async Task<int> Main()
    {
        (string Name, Func<Task> Test)[] tests =
        [
            ("framing.fragmented", FramingFragmentedAsync),
            ("framing.coalesced", FramingCoalescedAsync),
            ("framing.oversized", FramingOversizedAsync),
            ("framing.malformed-json", FramingMalformedJsonAsync),
            ("framing.truncated", FramingTruncatedAsync),
            ("protocol.valid-vectors", ProtocolValidVectorsAsync),
            ("protocol.invalid-vectors", ProtocolInvalidVectorsAsync),
            ("protocol.foreign-version-hello", ProtocolForeignHelloAsync),
            ("journal.persist-and-replay", JournalPersistReplayAsync),
            ("journal.retention-truncates", JournalRetentionAsync),
            ("journal.zero-cursor-is-live", JournalZeroCursorAsync),
            ("queue.event-capacity-bounded", OutboundQueueBoundedAsync),
            ("sequence.monotonic-observation", SequenceTrackerAsync)
        ];

        int failures = 0;
        foreach ((string name, Func<Task> test) in tests)
        {
            try
            {
                await test().ConfigureAwait(false);
                Console.Out.WriteLine($"PASS {name}");
            }
            catch (Exception exception)
            {
                failures++;
                Console.Error.WriteLine($"FAIL {name}: {exception.Message}");
                Console.Error.WriteLine(exception);
            }
        }
        Console.Out.WriteLine($"Executed {tests.Length} PipeProof test(s); failures: {failures}.");
        return failures == 0 ? 0 : 1;
    }

    private static async Task FramingFragmentedAsync()
    {
        LengthFramedJsonCodec codec = new();
        RequestMessage original = new()
        {
            Id = "request-1",
            Method = "proof.echo",
            Params = ProtocolJson.ToElement(new { text = "café 東京 🙂" })
        };
        byte[] frame = codec.Encode(original);
        await using ChunkedReadStream stream = new(frame, maximumChunk: 1);
        IProtocolMessage? decoded = await codec.ReadMessageAsync(stream).ConfigureAwait(false);
        RequestMessage request = RequireType<RequestMessage>(decoded);
        Equal("café 東京 🙂", request.Params.GetProperty("text").GetString());
    }

    private static async Task FramingCoalescedAsync()
    {
        LengthFramedJsonCodec codec = new();
        byte[] first = codec.Encode(new RequestMessage
        {
            Id = "request-1",
            Method = "proof.echo",
            Params = ProtocolJson.ToElement(new { n = 1 })
        });
        byte[] second = codec.Encode(new RequestMessage
        {
            Id = "request-2",
            Method = "proof.echo",
            Params = ProtocolJson.ToElement(new { n = 2 })
        });
        await using MemoryStream stream = new([.. first, .. second]);
        RequestMessage one = RequireType<RequestMessage>(await codec.ReadMessageAsync(stream).ConfigureAwait(false));
        RequestMessage two = RequireType<RequestMessage>(await codec.ReadMessageAsync(stream).ConfigureAwait(false));
        Equal(1, one.Params.GetProperty("n").GetInt32());
        Equal(2, two.Params.GetProperty("n").GetInt32());
        True(await codec.ReadMessageAsync(stream).ConfigureAwait(false) is null, "Expected clean end of stream.");
    }

    private static async Task FramingOversizedAsync()
    {
        byte[] prefix = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(prefix, 1025);
        await using MemoryStream stream = new(prefix);
        LengthFramedJsonCodec codec = new(maximumPayloadBytes: 1024);
        await ThrowsAsync<FrameSizeException>(() => codec.ReadMessageAsync(stream).AsTask()).ConfigureAwait(false);
    }

    private static async Task FramingMalformedJsonAsync()
    {
        LengthFramedJsonCodec codec = new();
        byte[] frame = codec.EncodePayload("{broken"u8);
        await using MemoryStream stream = new(frame);
        await ThrowsAsync<InvalidDataException>(() => codec.ReadMessageAsync(stream).AsTask()).ConfigureAwait(false);
    }

    private static async Task FramingTruncatedAsync()
    {
        LengthFramedJsonCodec codec = new();
        byte[] frame = codec.Encode(new RequestMessage
        {
            Id = "request-1",
            Method = "proof.echo",
            Params = ProtocolJson.ToElement(new { })
        });
        await using MemoryStream stream = new(frame[..^1]);
        await ThrowsAsync<EndOfStreamException>(() => codec.ReadMessageAsync(stream).AsTask()).ConfigureAwait(false);
    }

    private static Task ProtocolValidVectorsAsync()
    {
        ProtocolVectors vectors = LoadVectors();
        foreach (ProtocolVector vector in vectors.ValidMessages)
        {
            IProtocolMessage decoded = ProtocolMessageCodec.Deserialize(Encoding.UTF8.GetBytes(vector.Json));
            byte[] encoded = ProtocolMessageCodec.Serialize(decoded);
            IProtocolMessage roundTrip = ProtocolMessageCodec.Deserialize(encoded);
            Equal(decoded.Kind, roundTrip.Kind, vector.Name);
        }
        return Task.CompletedTask;
    }

    private static async Task ProtocolInvalidVectorsAsync()
    {
        ProtocolVectors vectors = LoadVectors();
        foreach (ProtocolVector vector in vectors.InvalidMessages)
        {
            await ThrowsAsync<InvalidDataException>(() => Task.Run(
                () => ProtocolMessageCodec.Deserialize(Encoding.UTF8.GetBytes(vector.Json)))).ConfigureAwait(false);
        }
    }

    private static Task ProtocolForeignHelloAsync()
    {
        HelloMessage message = new()
        {
            Version = "999.0",
            Id = "hello-foreign",
            Client = new("fixture", "0.1.0", "client-foreign"),
            Capabilities = ["request"]
        };
        byte[] encoded = ProtocolMessageCodec.Serialize(message);
        HelloMessage decoded = RequireType<HelloMessage>(ProtocolMessageCodec.Deserialize(encoded));
        Equal("999.0", decoded.Version);
        return Task.CompletedTask;
    }

    private static async Task JournalPersistReplayAsync()
    {
        string directory = NewTemporaryDirectory();
        string path = Path.Combine(directory, "events.ndjson");
        try
        {
            await using (DurableEventJournal journal = await DurableEventJournal.OpenAsync(path, 8).ConfigureAwait(false))
            {
                for (int ordinal = 1; ordinal <= 3; ordinal++)
                {
                    _ = await journal.AppendAsync(
                        "reconnect",
                        "proof.event",
                        ProtocolJson.ToElement(new { ordinal })).ConfigureAwait(false);
                }
            }
            await using DurableEventJournal reopened = await DurableEventJournal.OpenAsync(path, 8).ConfigureAwait(false);
            EventReplayBatch replay = reopened.Replay("reconnect", fromSequence: 1, maximumEvents: 8);
            Equal(3L, replay.LatestSequence);
            Equal(2, replay.Events.Count);
            Equal(2, replay.Events[0].Payload.GetProperty("ordinal").GetInt32());
            Equal(3, replay.Events[1].Payload.GetProperty("ordinal").GetInt32());
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static async Task JournalRetentionAsync()
    {
        string directory = NewTemporaryDirectory();
        try
        {
            await using DurableEventJournal journal = await DurableEventJournal.OpenAsync(
                Path.Combine(directory, "events.ndjson"),
                retentionCapacity: 2).ConfigureAwait(false);
            for (int ordinal = 1; ordinal <= 4; ordinal++)
            {
                _ = await journal.AppendAsync(
                    "reconnect",
                    "proof.event",
                    ProtocolJson.ToElement(new { ordinal })).ConfigureAwait(false);
            }
            EventJournalBounds bounds = journal.GetBounds();
            Equal(3L, bounds.MinimumAvailableSequence);
            ReplayUnavailableException exception = await ThrowsAsync<ReplayUnavailableException>(
                () => Task.Run(() => journal.Replay("reconnect", fromSequence: 1, maximumEvents: 2)))
                .ConfigureAwait(false);
            Equal(ProtocolErrorCodes.ReplayTruncated, exception.Code);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static async Task JournalZeroCursorAsync()
    {
        string directory = NewTemporaryDirectory();
        try
        {
            await using DurableEventJournal journal = await DurableEventJournal.OpenAsync(
                Path.Combine(directory, "events.ndjson"),
                retentionCapacity: 2).ConfigureAwait(false);
            _ = await journal.AppendAsync("proof", "proof.event", ProtocolJson.ToElement(new { n = 1 })).ConfigureAwait(false);
            EventReplayBatch replay = journal.Replay("proof", fromSequence: 0, maximumEvents: 2);
            Equal(0, replay.Events.Count);
            Equal(1L, replay.ReplayedThroughSequence);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static Task OutboundQueueBoundedAsync()
    {
        ProofServerOptions options = new()
        {
            ControlQueueCapacity = 1,
            EventQueueCapacity = 2,
            SubscriptionQueueCapacity = 2,
            MaximumReplayEvents = 1
        };
        options.Validate();
        ServerMetrics metrics = new("server-test", 1);
        ConnectionMetrics connection = metrics.ConnectionAccepted(
            options.ControlQueueCapacity,
            options.EventQueueCapacity);
        BoundedOutboundQueue queue = new(options, metrics, connection);
        EventMessage message = new()
        {
            SubscriptionId = "subscription-1",
            Topic = "proof",
            Sequence = 1,
            EventType = "proof.event",
            Payload = ProtocolJson.ToElement(new { })
        };
        True(queue.TryEnqueueEvent(message), "First event should fit.");
        True(queue.TryEnqueueEvent(message with { Sequence = 2 }), "Second event should fit.");
        True(!queue.TryEnqueueEvent(message with { Sequence = 3 }), "Third event must be refused, not retained.");
        queue.Complete();
        return Task.CompletedTask;
    }

    private static Task SequenceTrackerAsync()
    {
        EventSequenceTracker tracker = new();
        EventSequenceObservation first = tracker.Observe(10);
        True(first.HasGap, "Jump from zero to ten should be observable as a gap.");
        EventSequenceObservation next = tracker.Observe(12);
        True(next.HasGap, "Global sequence can expose an unrelated-topic gap.");
        EventSequenceObservation duplicate = tracker.Observe(12);
        True(duplicate.IsDuplicate, "Repeated sequence should be duplicate.");
        Equal(12L, tracker.LastSequence);
        return Task.CompletedTask;
    }

    private static ProtocolVectors LoadVectors()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "protocol-vectors.json");
        return ProtocolJson.DeserializeText<ProtocolVectors>(File.ReadAllText(path));
    }

    private static string NewTemporaryDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), $"square-pipeproof-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static T RequireType<T>(object? value) where T : class =>
        value as T ?? throw new InvalidOperationException($"Expected {typeof(T).Name}, received {value?.GetType().Name ?? "null"}.");

    private static void True(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Equal<T>(T expected, T? actual, string? message = null)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(message ?? $"Expected '{expected}', received '{actual}'.");
        }
    }

    private static async Task<TException> ThrowsAsync<TException>(Func<Task> action)
        where TException : Exception
    {
        try
        {
            await action().ConfigureAwait(false);
        }
        catch (TException exception)
        {
            return exception;
        }
        throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
    }

    private sealed record ProtocolVector(string Name, string Json);

    private sealed record ProtocolVectors(
        string SchemaVersion,
        IReadOnlyList<ProtocolVector> ValidMessages,
        IReadOnlyList<ProtocolVector> InvalidMessages);

    private sealed class ChunkedReadStream(byte[] bytes, int maximumChunk) : Stream
    {
        private int _position;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => bytes.Length;
        public override long Position { get => _position; set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count)
        {
            int available = Math.Min(Math.Min(count, maximumChunk), bytes.Length - _position);
            if (available <= 0) return 0;
            Array.Copy(bytes, _position, buffer, offset, available);
            _position += available;
            return available;
        }
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int available = Math.Min(Math.Min(buffer.Length, maximumChunk), bytes.Length - _position);
            if (available <= 0) return ValueTask.FromResult(0);
            bytes.AsMemory(_position, available).CopyTo(buffer);
            _position += available;
            return ValueTask.FromResult(available);
        }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
