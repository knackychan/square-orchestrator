namespace Square.Domain.Projects;

/// <summary>A fail-closed domain error carrying the M1 project validation code.</summary>
public sealed class ProjectValidationException : Exception
{
    public ProjectValidationException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}
