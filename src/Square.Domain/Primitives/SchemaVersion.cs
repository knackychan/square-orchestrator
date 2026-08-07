using System.Globalization;

namespace Square.Domain.Primitives;

public readonly record struct SchemaVersion : IComparable<SchemaVersion>
{
    public SchemaVersion(int major, int minor)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(major);
        ArgumentOutOfRangeException.ThrowIfNegative(minor);
        Major = major;
        Minor = minor;
    }
    public int Major { get; }
    public int Minor { get; }

    public static SchemaVersion Parse(string text)
    {
        string[] parts = text.Split('.', StringSplitOptions.None);
        if (parts.Length != 2 ||
            !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out int major) ||
            !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out int minor) || major < 0 || minor < 0)
            throw new FormatException($"Invalid schema version '{text}'. Expected '<major>.<minor>'.");
        return new SchemaVersion(major, minor);
    }
    public int CompareTo(SchemaVersion other)
    {
        int major = Major.CompareTo(other.Major);
        return major != 0 ? major : Minor.CompareTo(other.Minor);
    }
    public override string ToString() => Major.ToString(CultureInfo.InvariantCulture) + "." + Minor.ToString(CultureInfo.InvariantCulture);
}
