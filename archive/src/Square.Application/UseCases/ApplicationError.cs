namespace Square.Application.UseCases;

/// <summary>An application-level error carrying the M1 exit code and machine-readable code.</summary>
public sealed class ApplicationError : Exception
{
    public ApplicationError(string code, string message, int exitCode)
        : base(message)
    {
        Code = code;
        ExitCode = exitCode;
    }

    public string Code { get; }
    public int ExitCode { get; }
}
