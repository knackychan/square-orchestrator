namespace Square.TerminalProof.Harness;

internal static class Statistics
{
    internal static double? Percentile(IEnumerable<double?> source, double percentile)
    {
        ArgumentNullException.ThrowIfNull(source);
        return Percentile(source.Where(value => value.HasValue).Select(value => value!.Value), percentile);
    }

    internal static double? Percentile(IEnumerable<double> source, double percentile)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (percentile is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(percentile), percentile, "Percentile must be between zero and one.");
        }

        double[] values = source.Order().ToArray();
        if (values.Length == 0)
        {
            return null;
        }

        if (values.Length == 1)
        {
            return values[0];
        }

        double position = percentile * (values.Length - 1);
        int lower = (int)Math.Floor(position);
        int upper = (int)Math.Ceiling(position);
        if (lower == upper)
        {
            return values[lower];
        }

        double fraction = position - lower;
        return values[lower] + ((values[upper] - values[lower]) * fraction);
    }
}
