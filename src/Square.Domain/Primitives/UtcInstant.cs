using System.Globalization;

namespace Square.Domain.Primitives;

public readonly record struct UtcInstant : IComparable<UtcInstant>
{
    private const string CanonicalFormat = "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'";

    public UtcInstant(DateTimeOffset value)
    {
        Value = value.ToUniversalTime();
    }

    public DateTimeOffset Value { get; }

    public static UtcInstant Parse(string text)
    {
        if (!DateTimeOffset.TryParseExact(text, CanonicalFormat, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTimeOffset value))
        {
            throw new FormatException($"Invalid canonical UTC instant '{text}'.");
        }
        return new UtcInstant(value);
    }

    public int CompareTo(UtcInstant other) => Value.CompareTo(other.Value);
    public override string ToString() => Value.UtcDateTime.ToString(CanonicalFormat, CultureInfo.InvariantCulture);
}
