namespace Square.TerminalProof.Harness;

internal static class ProofAssert
{
    internal static void True(bool condition, string message)
    {
        if (!condition)
        {
            throw new ProofAssertionException(message);
        }
    }

    internal static void Equal<T>(T expected, T actual, string message)
        where T : notnull
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new ProofAssertionException($"{message} Expected '{expected}', received '{actual}'.");
        }
    }
}

internal sealed class ProofAssertionException : Exception
{
    internal ProofAssertionException(string message)
        : base(message)
    {
    }
}
