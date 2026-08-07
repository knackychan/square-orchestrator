namespace Square.Domain.Authority;

/// <summary>A fail-closed domain error carrying the M1 authority error code.</summary>
public sealed class AuthorityValidationException : Exception
{
    public AuthorityValidationException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}
