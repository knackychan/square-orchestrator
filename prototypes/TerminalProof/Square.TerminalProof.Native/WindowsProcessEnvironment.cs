using System.Diagnostics;

namespace Square.TerminalProof.Native;

public static class WindowsProcessEnvironment
{
    public static bool IsCurrentProcessInJob()
    {
        using Process current = Process.GetCurrentProcess();
        if (!NativeMethods.IsProcessInJob(current.SafeHandle, null, out bool result))
        {
            Win32Error.ThrowLastError("IsProcessInJob(current process)");
        }

        return result;
    }
}
