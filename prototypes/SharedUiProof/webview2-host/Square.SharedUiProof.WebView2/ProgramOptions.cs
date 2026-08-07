namespace Square.SharedUiProof.WebView2;

internal sealed record ProgramOptions(
    bool Autorun,
    bool AcceptanceRun,
    string EvidencePath,
    string UserDataDirectory)
{
    public static ProgramOptions Parse(IReadOnlyList<string> arguments)
    {
        var autorun = false;
        var acceptanceRun = false;
        string? evidencePath = null;
        string? userDataDirectory = null;
        for (var index = 0; index < arguments.Count; index++)
        {
            switch (arguments[index])
            {
                case "--autorun":
                    autorun = true;
                    break;
                case "--acceptance":
                    acceptanceRun = true;
                    break;
                case "--evidence":
                    evidencePath = RequiredPath(arguments, ref index, "--evidence");
                    break;
                case "--user-data":
                    userDataDirectory = RequiredPath(arguments, ref index, "--user-data");
                    break;
                default:
                    throw new ArgumentException($"Unknown argument '{arguments[index]}'.", nameof(arguments));
            }
        }

        if (acceptanceRun && !autorun)
        {
            throw new ArgumentException("--acceptance requires --autorun.", nameof(arguments));
        }
        evidencePath ??= Path.Combine(Environment.CurrentDirectory, "evidence", "sp00-t04-webview2.json");
        userDataDirectory ??= Path.Combine(
            Path.GetTempPath(),
            "SquareOrchestrator",
            "SharedUiProof",
            $"webview2-{Guid.NewGuid():N}");
        return new ProgramOptions(
            autorun,
            acceptanceRun,
            Path.GetFullPath(evidencePath),
            Path.GetFullPath(userDataDirectory));
    }

    private static string RequiredPath(IReadOnlyList<string> arguments, ref int index, string option)
    {
        if (++index >= arguments.Count || string.IsNullOrWhiteSpace(arguments[index]))
        {
            throw new ArgumentException($"{option} requires a path.", nameof(arguments));
        }
        return Path.GetFullPath(arguments[index]);
    }
}
