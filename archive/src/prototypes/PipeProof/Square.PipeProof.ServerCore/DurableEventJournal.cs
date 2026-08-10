using System.Text;
using System.Text.Json;
using Square.PipeProof.Protocol;

namespace Square.PipeProof.ServerCore;

public sealed record EventJournalBounds(long MinimumAvailableSequence, long LatestSequence);

public sealed record EventReplayBatch(
    IReadOnlyList<DurableEventRecord> Events,
    long MinimumAvailableSequence,
    long LatestSequence,
    long ReplayedThroughSequence);

public sealed class ReplayUnavailableException(string code, string message, object dataObject)
    : InvalidOperationException(message)
{
    public string Code { get; } = code;
    public object DataObject { get; } = dataObject;
}

public sealed class DurableEventJournal : IAsyncDisposable
{
    private static readonly byte[] NewLine = "\n"u8.ToArray();
    private readonly string _path;
    private readonly int _retentionCapacity;
    private readonly SemaphoreSlim _appendGate = new(1, 1);
    private readonly object _stateGate = new();
    private readonly LinkedList<DurableEventRecord> _retained = [];
    private readonly FileStream _stream;
    private long _latestSequence;
    private int _disposed;

    private DurableEventJournal(string path, int retentionCapacity, FileStream stream)
    {
        _path = path;
        _retentionCapacity = retentionCapacity;
        _stream = stream;
    }

    public string Path => _path;

    public static async Task<DurableEventJournal> OpenAsync(
        string path,
        int retentionCapacity,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(retentionCapacity);
        string fullPath = System.IO.Path.GetFullPath(path);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullPath)!);

        DurableEventJournal journal = new(
            fullPath,
            retentionCapacity,
            new FileStream(
                fullPath,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.Read,
                16_384,
                FileOptions.Asynchronous | FileOptions.WriteThrough));
        try
        {
            await journal.LoadAsync(cancellationToken).ConfigureAwait(false);
            journal._stream.Seek(0, SeekOrigin.End);
            return journal;
        }
        catch
        {
            await journal.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    public EventJournalBounds GetBounds()
    {
        lock (_stateGate)
        {
            long minimum = _retained.First?.Value.Sequence ?? _latestSequence + 1;
            return new(minimum, _latestSequence);
        }
    }

    public async Task<DurableEventRecord> AppendAsync(
        string topic,
        string eventType,
        JsonElement payload,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        if (payload.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("Event payload must be a JSON object.", nameof(payload));
        }

        await _appendGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            long sequence;
            lock (_stateGate)
            {
                sequence = _latestSequence + 1;
            }
            DurableEventRecord record = new(
                sequence,
                topic,
                eventType,
                payload.Clone(),
                DateTimeOffset.UtcNow);
            byte[] json = ProtocolJson.Serialize(record);
            await _stream.WriteAsync(json, cancellationToken).ConfigureAwait(false);
            await _stream.WriteAsync(NewLine, cancellationToken).ConfigureAwait(false);
            await _stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            _stream.Flush(flushToDisk: true);

            lock (_stateGate)
            {
                _latestSequence = sequence;
                _retained.AddLast(record);
                while (_retained.Count > _retentionCapacity)
                {
                    _retained.RemoveFirst();
                }
            }
            return record;
        }
        finally
        {
            _appendGate.Release();
        }
    }

    public EventReplayBatch Replay(string topic, long fromSequence, int maximumEvents)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentOutOfRangeException.ThrowIfNegative(fromSequence);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumEvents);
        lock (_stateGate)
        {
            long minimum = _retained.First?.Value.Sequence ?? _latestSequence + 1;
            if (fromSequence == 0)
            {
                return new([], minimum, _latestSequence, _latestSequence);
            }
            if (fromSequence < minimum - 1)
            {
                throw new ReplayUnavailableException(
                    ProtocolErrorCodes.ReplayTruncated,
                    $"Sequence {fromSequence} is older than the retained replay window.",
                    new
                    {
                        from_sequence = fromSequence,
                        minimum_available_sequence = minimum,
                        latest_sequence = _latestSequence
                    });
            }

            List<DurableEventRecord> events = _retained
                .Where(record => record.Sequence > fromSequence && string.Equals(record.Topic, topic, StringComparison.Ordinal))
                .ToList();
            if (events.Count > maximumEvents)
            {
                throw new ReplayUnavailableException(
                    ProtocolErrorCodes.ReplayLimitExceeded,
                    $"Replay requires {events.Count} events, exceeding the limit {maximumEvents}.",
                    new
                    {
                        requested_events = events.Count,
                        maximum_events = maximumEvents,
                        latest_sequence = _latestSequence
                    });
            }
            long through = events.Count == 0 ? fromSequence : events[^1].Sequence;
            return new(events, minimum, _latestSequence, through);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }
        await _appendGate.WaitAsync().ConfigureAwait(false);
        try
        {
            await _stream.FlushAsync().ConfigureAwait(false);
            _stream.Flush(flushToDisk: true);
            await _stream.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            _appendGate.Release();
            _appendGate.Dispose();
        }
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        _stream.Seek(0, SeekOrigin.Begin);
        using StreamReader reader = new(
            _stream,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true),
            detectEncodingFromByteOrderMarks: false,
            bufferSize: 16_384,
            leaveOpen: true);
        long expected = 1;
        while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }
            DurableEventRecord record;
            try
            {
                record = ProtocolJson.DeserializeText<DurableEventRecord>(line);
            }
            catch (JsonException exception)
            {
                throw new InvalidDataException($"Event journal '{_path}' contains invalid JSON.", exception);
            }
            if (record.Sequence != expected)
            {
                throw new InvalidDataException(
                    $"Event journal '{_path}' is not contiguous: expected {expected}, received {record.Sequence}.");
            }
            expected++;
            _latestSequence = record.Sequence;
            _retained.AddLast(record);
            while (_retained.Count > _retentionCapacity)
            {
                _retained.RemoveFirst();
            }
        }
    }
}
