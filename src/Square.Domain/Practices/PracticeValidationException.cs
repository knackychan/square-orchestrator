namespace Square.Domain.Practices;

/// <summary>A fail-closed domain error carrying the M1 practice validation code.</summary>
public sealed class PracticeValidationException : Exception
{
    public PracticeValidationException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}
