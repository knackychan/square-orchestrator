namespace Square.Domain.Primitives;

public interface IClock
{
    UtcInstant UtcNow { get; }
}
