using System.Text;

namespace Square.TerminalProof.Native;

public static class WindowsCommandLine
{
    public static string Build(string executablePath, IEnumerable<string> arguments)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);
        ArgumentNullException.ThrowIfNull(arguments);

        StringBuilder commandLine = new();
        AppendArgument(commandLine, executablePath);
        foreach (string argument in arguments)
        {
            commandLine.Append(' ');
            AppendArgument(commandLine, argument ?? throw new ArgumentException("Arguments cannot contain null values.", nameof(arguments)));
        }

        return commandLine.ToString();
    }

    public static string Quote(string argument)
    {
        ArgumentNullException.ThrowIfNull(argument);
        StringBuilder result = new();
        AppendArgument(result, argument);
        return result.ToString();
    }

    private static void AppendArgument(StringBuilder destination, string argument)
    {
        bool requiresQuotes = argument.Length == 0 || argument.Any(character => char.IsWhiteSpace(character) || character == '"');
        if (!requiresQuotes)
        {
            destination.Append(argument);
            return;
        }

        destination.Append('"');
        int backslashCount = 0;
        foreach (char character in argument)
        {
            if (character == '\\')
            {
                backslashCount++;
                continue;
            }

            if (character == '"')
            {
                destination.Append('\\', (backslashCount * 2) + 1);
                destination.Append('"');
                backslashCount = 0;
                continue;
            }

            destination.Append('\\', backslashCount);
            backslashCount = 0;
            destination.Append(character);
        }

        destination.Append('\\', backslashCount * 2);
        destination.Append('"');
    }
}
