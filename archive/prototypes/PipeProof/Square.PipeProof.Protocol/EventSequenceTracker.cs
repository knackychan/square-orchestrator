namespace Square.PipeProof.Protocol;

public readonly record struct EventSequenceObservation(
    long PreviousSequence,
    long CurrentSequence,
    bool IsDuplicate,
    bool HasGap);

public sealed class EventSequenceTracker(long initialSequence = 0)
{
    private long _lastSequence = initialSequence;

    public long LastSequence => Interlocked.Read(ref _lastSequence);

    public EventSequenceObservation Observe(long sequence)
    {
        if (sequence <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence), sequence, "Sequence must be positive.");
        }

        while (true)
        {
            long previous = Interlocked.Read(ref _lastSequence);
            if (sequence <= previous)
            {
                return new(previous, sequence, IsDuplicate: true, HasGap: false);
            }
            if (Interlocked.CompareExchange(ref _lastSequence, sequence, previous) == previous)
            {
                return new(previous, sequence, IsDuplicate: false, HasGap: sequence > previous + 1);
            }
        }
    }
}
