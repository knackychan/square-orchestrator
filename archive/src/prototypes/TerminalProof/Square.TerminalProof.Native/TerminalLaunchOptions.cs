namespace Square.TerminalProof.Native;

public sealed record TerminalLaunchOptions
{
    public required string ExecutablePath { get; init; }

    public IReadOnlyList<string> Arguments { get; init; } = Array.Empty<string>();

    public required string WorkingDirectory { get; init; }

    public TerminalSize InitialSize { get; init; } = new(100, 30);

    public TimeSpan CleanupTimeout { get; init; } = TimeSpan.FromSeconds(10);

    internal void Validate()
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763))
        {
            throw new PlatformNotSupportedException("ConPTY requires Windows 10 version 1809 (build 17763) or later.");
        }

        if (string.IsNullOrWhiteSpace(ExecutablePath))
        {
            throw new ArgumentException("An executable path is required.", nameof(ExecutablePath));
        }

        string resolvedExecutable = Path.GetFullPath(ExecutablePath);
        if (!File.Exists(resolvedExecutable))
        {
            throw new FileNotFoundException("The terminal child executable does not exist.", resolvedExecutable);
        }

        if (string.IsNullOrWhiteSpace(WorkingDirectory))
        {
            throw new ArgumentException("A working directory is required.", nameof(WorkingDirectory));
        }

        string resolvedWorkingDirectory = Path.GetFullPath(WorkingDirectory);
        if (!Directory.Exists(resolvedWorkingDirectory))
        {
            throw new DirectoryNotFoundException($"The terminal working directory does not exist: {resolvedWorkingDirectory}");
        }


        if (CleanupTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(CleanupTimeout), CleanupTimeout, "Cleanup timeout must be positive.");
        }
    }
}
