using Square.Domain.Primitives;

namespace Square.Application.Primitives;

public sealed class SystemClock : IClock
{
    public UtcInstant UtcNow => new(DateTimeOffset.UtcNow);
}
