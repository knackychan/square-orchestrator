namespace Square.TerminalProof.Native;

public static class WindowsProcessEnvironment
{
    public static bool IsCurrentProcessInJob()
    {
        nint hCurrentProcess = unchecked((nint)(-1)); // GetCurrentProcess pseudo-handle
        if (!NativeMethods.IsProcessInJobRaw(hCurrentProcess, nint.Zero, out bool result))
        {
            Win32Error.ThrowLastError("IsProcessInJob(current process)");
        }

        return result;
    }
}
