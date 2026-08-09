using System.Text;
using System.Text.Json;

namespace Square.Application.Authority;

/// <summary>
/// Canonical JSON serialization matching the proven M1 contract: compact separators, sorted
/// object keys at every depth, no trailing whitespace.
/// </summary>
public static class ManifestJson
{
    public static string Serialize(object? value)
    {
        var builder = new StringBuilder();
        WriteValue(builder, value);
        return builder.ToString();
    }

    private static void WriteValue(StringBuilder builder, object? value)
    {
        switch (value)
        {
            case null:
                builder.Append("null");
                break;
            case bool boolean:
                builder.Append(boolean ? "true" : "false");
                break;
            case string text:
                WriteString(builder, text);
                break;
            case long integer:
                builder.Append(integer.ToString(System.Globalization.CultureInfo.InvariantCulture));
                break;
            case double real:
                builder.Append(FormatDouble(real));
                break;
            case int integer:
                builder.Append(integer.ToString(System.Globalization.CultureInfo.InvariantCulture));
                break;
            case IReadOnlyDictionary<string, object?> dictionary:
                WriteObject(builder, dictionary);
                break;
            case IDictionary<string, object?> dictionary:
                WriteObject(builder, new Dictionary<string, object?>(dictionary, StringComparer.Ordinal));
                break;
            case System.Collections.IEnumerable enumerable:
                WriteArray(builder, enumerable);
                break;
            default:
                // Serialize unknown types through System.Text.Json (e.g. records) then re-emit canonically.
                string json = JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = false });
                builder.Append(json);
                break;
        }
    }

    private static void WriteObject(StringBuilder builder, IReadOnlyDictionary<string, object?> dictionary)
    {
        builder.Append('{');
        bool first = true;
        foreach (KeyValuePair<string, object?> pair in dictionary.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            if (!first) builder.Append(',');
            first = false;
            WriteString(builder, pair.Key);
            builder.Append(':');
            WriteValue(builder, pair.Value);
        }
        builder.Append('}');
    }

    private static void WriteArray(StringBuilder builder, System.Collections.IEnumerable enumerable)
    {
        builder.Append('[');
        bool first = true;
        foreach (object? item in enumerable)
        {
            if (!first) builder.Append(',');
            first = false;
            WriteValue(builder, item);
        }
        builder.Append(']');
    }

    private static void WriteString(StringBuilder builder, string value)
    {
        builder.Append('"');
        foreach (char c in value)
        {
            switch (c)
            {
                case '"': builder.Append("\\\""); break;
                case '\\': builder.Append("\\\\"); break;
                case '\b': builder.Append("\\b"); break;
                case '\f': builder.Append("\\f"); break;
                case '\n': builder.Append("\\n"); break;
                case '\r': builder.Append("\\r"); break;
                case '\t': builder.Append("\\t"); break;
                default:
                    if (c < 0x20)
                        builder.Append("\\u").Append(((int)c).ToString("x4", System.Globalization.CultureInfo.InvariantCulture));
                    else
                        builder.Append(c);
                    break;
            }
        }
        builder.Append('"');
    }

    private static string FormatDouble(double value)
    {
        // Python's json.dumps emits shortest round-trip representation without exponent when integral.
        if (value == Math.Floor(value) && !double.IsInfinity(value) && Math.Abs(value) < 1e15)
            return ((long)value).ToString(System.Globalization.CultureInfo.InvariantCulture);
        string text = value.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        return text.Contains('.') ? text : text + ".0";
    }
}
