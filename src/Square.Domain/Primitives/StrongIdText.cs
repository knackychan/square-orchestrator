namespace Square.Domain.Primitives;

internal static class StrongIdText
{
    private const int PayloadLength = 26;
    private const string CrockfordAlphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    public static bool TryNormalize(string? text, string prefix, out string canonical)
    {
        canonical = string.Empty;
        if (string.IsNullOrWhiteSpace(text)) return false;
        string trimmed = text.Trim();
        if (trimmed.Length != prefix.Length + 1 + PayloadLength ||
            !trimmed.StartsWith(prefix + "_", StringComparison.OrdinalIgnoreCase)) return false;

        ReadOnlySpan<char> payload = trimmed.AsSpan(prefix.Length + 1);
        Span<char> normalized = stackalloc char[PayloadLength];
        for (int index = 0; index < payload.Length; index++)
        {
            char value = char.ToUpperInvariant(payload[index]);
            if (CrockfordAlphabet.IndexOf(value) < 0) return false;
            normalized[index] = value;
        }
        canonical = string.Concat(prefix, "_", new string(normalized));
        return true;
    }
}
