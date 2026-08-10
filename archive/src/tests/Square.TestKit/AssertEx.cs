namespace Square.TestKit;

public static class AssertEx
{
    public static void True(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    public static void False(bool condition, string message) => True(!condition, message);
    public static void Equal<T>(T expected, T actual, string? message = null) where T : notnull
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new InvalidOperationException(message ?? $"Expected '{expected}', received '{actual}'.");
    }
    public static TException Throws<TException>(Action action) where TException : Exception
    {
        try { action(); } catch (TException exception) { return exception; }
        throw new InvalidOperationException($"Expected {typeof(TException).Name} to be thrown.");
    }
}
