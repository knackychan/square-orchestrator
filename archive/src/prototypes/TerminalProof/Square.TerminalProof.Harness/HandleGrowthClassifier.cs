namespace Square.TerminalProof.Harness;

/// <summary>
/// Versioned, deterministic classification of a stable handle series. It operates only on
/// stabilized post-quiescence readings, never on active-concurrency peaks, and exposes the
/// raw values, first differences, last-N net change, min/max/range, regression slope, the rule
/// version, and the exact rule that matched. The machine output and the human report must read
/// the same generated classification; no manual override is permitted.
/// </summary>
internal static class HandleGrowthClassifier
{
    internal const string RuleVersion = "sp00-t02-fix03-classifier-v1";

    internal const string NoGrowth = "NO_GROWTH";
    internal const string Plateau = "PLATEAU";
    internal const string LinearGrowth = "LINEAR_GROWTH";
    internal const string DelayedRelease = "DELAYED_RELEASE";
    internal const string Unresolved = "UNRESOLVED";

    /// <summary>Measurement noise band in handles for the classification rules.</summary>
    internal const int MeasurementNoiseBand = 3;

    /// <summary>Stable tail window used for the plateau/trend judgement.</summary>
    internal const int FinalWindowSize = 5;

    internal const int MinPointsForStableWindow = 3;

    internal static HandleGrowthClassificationEvidence Classify(string seriesName, IReadOnlyList<int> values)
    {
        ArgumentNullException.ThrowIfNull(seriesName);
        ArgumentNullException.ThrowIfNull(values);

        int[] raw = values.ToArray();
        int[] firstDifferences = new int[Math.Max(0, raw.Length - 1)];
        for (int index = 1; index < raw.Length; index++)
        {
            firstDifferences[index - 1] = raw[index] - raw[index - 1];
        }

        int minimum = raw.Length == 0 ? 0 : raw.Min();
        int maximum = raw.Length == 0 ? 0 : raw.Max();
        int range = maximum - minimum;

        int window = Math.Min(FinalWindowSize, raw.Length);
        int lastNet = raw.Length == 0 ? 0 : raw[^1] - raw[Math.Max(0, raw.Length - window)];

        double slopeAll = raw.Length < 2 ? 0 : LeastSquaresSlope(raw, start: 0, count: raw.Length);
        double slopeTail = raw.Length < 2 ? 0 : LeastSquaresSlope(raw, start: raw.Length - window, count: window);

        (string classification, string matchedRule) = raw.Length < MinPointsForStableWindow
            ? (Unresolved, "fewer than three stable readings are available for a stable-window rule.")
            : Evaluate(raw, maximum, range, lastNet, slopeTail);

        return new HandleGrowthClassificationEvidence(
            seriesName,
            RuleVersion,
            classification,
            matchedRule,
            raw,
            firstDifferences,
            lastNet,
            minimum,
            maximum,
            range,
            slopeAll,
            window,
            MeasurementNoiseBand);
    }

    private static (string Classification, string MatchedRule) Evaluate(
        int[] values,
        int maximum,
        int range,
        int lastNet,
        double slopeTail)
    {
        int last = values[^1];
        int window = Math.Min(FinalWindowSize, values.Length);
        // A tail trend is "material" only when it accumulates at least one full noise band
        // across the final stable window; a wobble smaller than that reads as measurement noise.
        double materialSlope = MeasurementNoiseBand / (double)window;
        bool stableTailWithinNoise = Math.Abs(lastNet) <= MeasurementNoiseBand;

        if (values.Length >= 4 && last < maximum - MeasurementNoiseBand && range > 2 * MeasurementNoiseBand)
        {
            return (DelayedRelease,
                $"values decline below the earlier peak ({maximum}) by more than the noise band ({MeasurementNoiseBand}) in the final stable window.");
        }

        if (lastNet > MeasurementNoiseBand && slopeTail >= materialSlope)
        {
            return (LinearGrowth,
                $"the final stable window keeps adding handles: last-{window} net change {lastNet} exceeds the noise band ({MeasurementNoiseBand}) and the tail slope {slopeTail:F4} reaches the material trend threshold {materialSlope:F4}.");
        }

        if (range > MeasurementNoiseBand && stableTailWithinNoise && Math.Abs(slopeTail) <= materialSlope)
        {
            return (Plateau,
                $"initial expansion is observed (range {range} > noise {MeasurementNoiseBand}) while the final stable window stays bounded with no material positive trend (last-{window} net {lastNet}, tail slope {slopeTail:F4}, threshold {materialSlope:F4}).");
        }

        if (range <= MeasurementNoiseBand && stableTailWithinNoise)
        {
            return (NoGrowth,
                $"all stable values remain inside the declared measurement-noise band (range {range} <= {MeasurementNoiseBand}) with no positive persistent slope.");
        }

        return (Unresolved,
            "the evidence does not satisfy a stable rule: the series is neither flat, plateau-like, linearly growing, nor delayed-release under classifier " + RuleVersion + ".");
    }

    private static double LeastSquaresSlope(IReadOnlyList<int> values, int start, int count)
    {
        if (count < 2)
        {
            return 0;
        }

        double sumX = 0;
        double sumY = 0;
        double sumXy = 0;
        double sumXX = 0;
        for (int offset = 0; offset < count; offset++)
        {
            double x = start + offset;
            double y = values[start + offset];
            sumX += x;
            sumY += y;
            sumXy += x * y;
            sumXX += x * x;
        }

        double denominator = (count * sumXX) - (sumX * sumX);
        if (denominator == 0)
        {
            return 0;
        }

        return ((count * sumXy) - (sumX * sumY)) / denominator;
    }
}
