namespace Square.TerminalProof.Fixture;

internal sealed class FixtureArguments
{
    private readonly Dictionary<string, string> _values;

    private FixtureArguments(Dictionary<string, string> values)
    {
        _values = values;
    }

    internal static FixtureArguments Parse(string[] args)
    {
        Dictionary<string, string> values = new(StringComparer.Ordinal);
        for (int index = 0; index < args.Length; index++)
        {
            string key = args[index];
            if (!key.StartsWith("--", StringComparison.Ordinal))
            {
                throw new ArgumentException($"Unexpected positional argument '{key}'.");
            }

            if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
            {
                throw new ArgumentException($"Argument '{key}' requires a value.");
            }

            if (!values.TryAdd(key, args[++index]))
            {
                throw new ArgumentException($"Argument '{key}' was specified more than once.");
            }
        }

        return new FixtureArguments(values);
    }

    internal string Required(string name) => _values.TryGetValue(name, out string? value)
        ? value
        : throw new ArgumentException($"Required argument '{name}' was not supplied.");

    internal string? GetString(string name) => _values.TryGetValue(name, out string? value) ? value : null;

    internal int GetInt32(string name, int defaultValue, int minimum = 0)
    {
        if (!_values.TryGetValue(name, out string? text))
        {
            return defaultValue;
        }

        if (!int.TryParse(text, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int value)
            || value < minimum)
        {
            throw new ArgumentException($"Argument '{name}' must be an integer greater than or equal to {minimum}.");
        }

        return value;
    }
}
