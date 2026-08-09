using System.Security.Cryptography;

namespace Square.Domain.Primitives;

public readonly record struct ContentHash : IComparable<ContentHash>
{
    private const string Prefix = "sha256:";
    private const int HexLength = 64;
    private readonly string? value;

    private ContentHash(string value) { this.value = value; }
    public string Value => value ?? string.Empty;

    public static ContentHash Compute(ReadOnlySpan<byte> content)
    {
        Span<byte> digest = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.HashData(content, digest);
        return new ContentHash(Prefix + Convert.ToHexString(digest).ToLowerInvariant());
    }

    public static ContentHash Parse(string text)
    {
        if (text is null || text.Length != Prefix.Length + HexLength || !text.StartsWith(Prefix, StringComparison.Ordinal))
            throw new FormatException($"Invalid SHA-256 content hash '{text}'.");
        foreach (char character in text.AsSpan(Prefix.Length))
        {
            bool valid = character is >= '0' and <= '9' or >= 'a' and <= 'f';
            if (!valid) throw new FormatException("SHA-256 hashes must use canonical lowercase hexadecimal text.");
        }
        return new ContentHash(text);
    }

    public int CompareTo(ContentHash other) => StringComparer.Ordinal.Compare(Value, other.Value);
    public override string ToString() => Value;
}
