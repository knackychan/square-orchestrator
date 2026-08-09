using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Square.TerminalProof.Fixture;

internal static class FixtureProgram
{
    private const uint StdOutputHandle = unchecked((uint)(-11));

    [StructLayout(LayoutKind.Sequential)]
    private struct Coord { internal short X; internal short Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct SmallRect { internal short Left; internal short Top; internal short Right; internal short Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct ConsoleScreenBufferInfo
    {
        internal Coord Size;
        internal Coord CursorPosition;
        internal ushort Attributes;
        internal SmallRect Window;
        internal Coord MaximumWindowSize;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nint GetStdHandle(uint nStdHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetConsoleScreenBufferInfo(nint hConsoleOutput, out ConsoleScreenBufferInfo info);
    private const string UnicodeMarker = "UNICODE:café|漢字|Ελληνικά|😀";
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    internal static async Task<int> RunAsync(string[] args)
    {
        Console.OutputEncoding = Utf8NoBom;
        Console.InputEncoding = Utf8NoBom;

        FixtureArguments parsed = FixtureArguments.Parse(args);
        string? scenario = TryGetMode(args, "--scenario");
        string? helper = TryGetMode(args, "--helper");
        if ((scenario is null) == (helper is null))
        {
            throw new ArgumentException("Specify exactly one of --scenario or --helper.");
        }

        return scenario is not null
            ? await RunScenarioAsync(scenario, parsed).ConfigureAwait(false)
            : await RunHelperAsync(helper!, parsed).ConfigureAwait(false);
    }

    private static async Task<int> RunScenarioAsync(string scenario, FixtureArguments arguments) => scenario switch
    {
        "unicode" => RunUnicode(),
        "ansi" => RunAnsi(),
        "large_burst" => await RunLargeBurstAsync(arguments.GetInt32("--payload-bytes", 1_048_576, 1)).ConfigureAwait(false),
        "quiet_child" => await RunQuietChildAsync(arguments.GetInt32("--quiet-ms", 750, 50)).ConfigureAwait(false),
        "stdin_question" => RunStdinQuestion(),
        "resize" => await RunResizeAsync().ConfigureAwait(false),
        "normal_exit" => RunNormalExit(),
        "crash" => RunCrash(),
        "graceful_cancel" => await RunGracefulCancelAsync().ConfigureAwait(false),
        "forced_termination" => await RunForcedTerminationAsync().ConfigureAwait(false),
        "nested_children" => await RunNestedChildrenAsync(arguments.GetInt32("--child-count", 3, 1)).ConfigureAwait(false),
        "stream_isolation" => RunStreamIsolation(arguments.GetString("--run-id") ?? Guid.NewGuid().ToString("N")[..8]),
        _ => throw new ArgumentException($"Unknown scenario '{scenario}'.")
    };

    private static async Task<int> RunHelperAsync(string helper, FixtureArguments arguments) => helper switch
    {
        "quiet" => await RunQuietHelperAsync(arguments.GetInt32("--duration-ms", 750, 1)).ConfigureAwait(false),
        "nested" => await RunNestedHelperAsync(arguments.GetInt32("--remaining", 1, 1)).ConfigureAwait(false),
        _ => throw new ArgumentException($"Unknown helper '{helper}'.")
    };

    private static int RunUnicode()
    {
        Console.WriteLine(UnicodeMarker);
        Console.Out.Flush();
        return 0;
    }

    private static int RunAnsi()
    {
        Console.Write("\u001b[31mANSI-RED\u001b[0m|");
        Console.Write("\u001b[1;4mANSI-BOLD-UNDERLINE\u001b[0m");
        Console.WriteLine();
        Console.Out.Flush();
        return 0;
    }

    private static async Task<int> RunLargeBurstAsync(int payloadBytes)
    {
        byte[] line = Utf8NoBom.GetBytes("BURST-DATA:0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz\r\n");
        Stream stdout = Console.OpenStandardOutput();
        byte[] begin = Utf8NoBom.GetBytes($"BURST-BEGIN:{payloadBytes}\r\n");
        await stdout.WriteAsync(begin).ConfigureAwait(false);

        int remaining = payloadBytes;
        while (remaining > 0)
        {
            int length = Math.Min(remaining, line.Length);
            await stdout.WriteAsync(line.AsMemory(0, length)).ConfigureAwait(false);
            remaining -= length;
        }

        byte[] end = Utf8NoBom.GetBytes("\r\nBURST-END\r\n");
        await stdout.WriteAsync(end).ConfigureAwait(false);
        await stdout.FlushAsync().ConfigureAwait(false);
        return 0;
    }

    private static async Task<int> RunQuietChildAsync(int quietMilliseconds)
    {
        using Process child = StartSelf("--helper", "quiet", "--duration-ms", quietMilliseconds.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Console.WriteLine($"QUIET-READY:child={child.Id};duration_ms={quietMilliseconds}");
        Console.Out.Flush();
        await child.WaitForExitAsync().ConfigureAwait(false);
        Console.WriteLine($"QUIET-DONE:child_exit={child.ExitCode}");
        Console.Out.Flush();
        return child.ExitCode;
    }

    private static int RunStdinQuestion()
    {
        Console.Write("QUESTION:enter-square-proof-token>");
        Console.Out.Flush();
        string? answer = Console.ReadLine();
        if (answer is null)
        {
            Console.WriteLine("ANSWER:EOF");
            return 41;
        }

        Console.WriteLine($"ANSWER:{answer}");
        Console.Out.Flush();
        return string.Equals(answer, "square-proof-answer", StringComparison.Ordinal) ? 0 : 42;
    }

    private static async Task<int> RunResizeAsync()
    {
        string sizeBefore = GetConsoleSize();
        if (sizeBefore == "unknown")
        {
            Console.Error.WriteLine($"RESIZE-FAILED:before-size-unknown");
            return 43;
        }

        Console.WriteLine($"SIZE-BEFORE:{sizeBefore}");
        Console.WriteLine("RESIZE-READY");
        Console.Out.Flush();
        string? command = Console.ReadLine();
        await Task.Delay(TimeSpan.FromMilliseconds(50)).ConfigureAwait(false);
        string sizeAfter = GetConsoleSize();
        if (sizeAfter == "unknown")
        {
            Console.Error.WriteLine($"RESIZE-FAILED:after-size-unknown");
            return 43;
        }

        Console.WriteLine($"SIZE-AFTER:{sizeAfter};command={command}");
        Console.Out.Flush();
        return string.Equals(command, "continue", StringComparison.Ordinal) ? 0 : 43;
    }

    private static string GetConsoleSize()
    {
        nint hStdOut = GetStdHandle(StdOutputHandle);
        if (hStdOut == nint.Zero || hStdOut == unchecked((nint)(-1)))
        {
            return "unknown";
        }

        if (!GetConsoleScreenBufferInfo(hStdOut, out ConsoleScreenBufferInfo info))
        {
            return "unknown";
        }

        int width = info.Window.Right - info.Window.Left + 1;
        int height = info.Window.Bottom - info.Window.Top + 1;
        return $"{width}x{height}";
    }

    private static int RunNormalExit()
    {
        Console.WriteLine("NORMAL-EXIT:0");
        Console.Out.Flush();
        return 0;
    }

    private static int RunCrash()
    {
        Console.WriteLine("CRASH-READY");
        Console.Out.Flush();
        throw new InvalidOperationException("Intentional SP00-T02 crash fixture.");
    }

    private static async Task<int> RunGracefulCancelAsync()
    {
        TaskCompletionSource<bool> cancellation = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ConsoleCancelEventHandler handler = (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellation.TrySetResult(true);
        };

        Console.CancelKeyPress += handler;
        try
        {
            Console.WriteLine("CANCEL-READY");
            Console.Out.Flush();
            await cancellation.Task.WaitAsync(TimeSpan.FromSeconds(30)).ConfigureAwait(false);
            Console.WriteLine("CANCEL-ACK");
            Console.Out.Flush();
            return 0;
        }
        finally
        {
            Console.CancelKeyPress -= handler;
        }
    }

    private static async Task<int> RunForcedTerminationAsync()
    {
        ConsoleCancelEventHandler handler = (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            Console.WriteLine("FORCE-CANCEL-IGNORED");
            Console.Out.Flush();
        };

        Console.CancelKeyPress += handler;
        try
        {
            Console.WriteLine("FORCE-READY");
            Console.Out.Flush();
            await Task.Delay(Timeout.InfiniteTimeSpan).ConfigureAwait(false);
            return 0;
        }
        finally
        {
            Console.CancelKeyPress -= handler;
        }
    }

    private static async Task<int> RunNestedChildrenAsync(int childCount)
    {
        using Process child = StartSelf("--helper", "nested", "--remaining", childCount.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Console.WriteLine($"TREE-READY:root={Environment.ProcessId};first_child={child.Id};child_count={childCount}");
        Console.Out.Flush();
        await child.WaitForExitAsync().ConfigureAwait(false);
        return child.ExitCode;
    }

    private static async Task<int> RunQuietHelperAsync(int durationMilliseconds)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(durationMilliseconds)).ConfigureAwait(false);
        return 0;
    }

    private static async Task<int> RunNestedHelperAsync(int remaining)
    {
        Process? child = null;
        if (remaining > 1)
        {
            child = StartSelf("--helper", "nested", "--remaining", (remaining - 1).ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        using (child)
        {
            Console.WriteLine($"TREE-NODE:pid={Environment.ProcessId};remaining={remaining};child={(child is null ? 0 : child.Id)}");
            Console.Out.Flush();
            if (child is null)
            {
                await Task.Delay(Timeout.InfiniteTimeSpan).ConfigureAwait(false);
            }
            else
            {
                await child.WaitForExitAsync().ConfigureAwait(false);
                return child.ExitCode;
            }
        }

        return 0;
    }

    private static Process StartSelf(params string[] arguments)
    {
        string executable = Environment.ProcessPath
            ?? throw new InvalidOperationException("Environment.ProcessPath is unavailable for the fixture executable.");
        ProcessStartInfo startInfo = new(executable)
        {
            UseShellExecute = false,
            WorkingDirectory = Environment.CurrentDirectory
        };
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start the fixture child process.");
    }

    private static int RunStreamIsolation(string runId)
    {
        Console.WriteLine($"CONPTY-STDOUT-MARKER:{runId}");
        Console.Out.Flush();
        Console.Error.WriteLine($"CONPTY-STDERR-MARKER:{runId}");
        Console.Error.Flush();
        return 0;
    }

    private static string? TryGetMode(IReadOnlyList<string> args, string name)
    {
        for (int index = 0; index < args.Count - 1; index++)
        {
            if (string.Equals(args[index], name, StringComparison.Ordinal))
            {
                return args[index + 1];
            }
        }

        return null;
    }
}
