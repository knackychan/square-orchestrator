using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Square.TerminalProof.Native;

internal static class Win32Error
{
    internal static void ThrowLastError(string operation)
    {
        int error = Marshal.GetLastWin32Error();
        throw new Win32Exception(error, $"{operation} failed with Win32 error {error}.");
    }

    internal static void ThrowIfHResultFailed(int hresult, string operation)
    {
        if (hresult >= 0)
        {
            return;
        }

        Exception? exception = Marshal.GetExceptionForHR(hresult);
        throw new InvalidOperationException($"{operation} failed with HRESULT 0x{hresult:X8}.", exception);
    }
}
