using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Square.PipeProof.Transport.Windows;

public static class RestrictedTokenAccessProbe
{
    public static RestrictedTokenProbeResult Execute(string pipePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pipePath);
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("The restricted-token named-pipe probe requires Windows.");
        }
        if (!NativeMethods.ImpersonateAnonymousToken(NativeMethods.GetCurrentThread()))
        {
            throw new Win32Exception(
                Marshal.GetLastWin32Error(),
                "Could not impersonate the anonymous token for the negative ACL probe.");
        }

        int error;
        bool connected;
        try
        {
            using SafeFileHandle handle = NativeMethods.CreateFileW(
                pipePath,
                NativeMethods.GenericRead | NativeMethods.GenericWrite,
                0,
                IntPtr.Zero,
                NativeMethods.OpenExisting,
                0,
                IntPtr.Zero);
            connected = !handle.IsInvalid;
            error = connected ? 0 : Marshal.GetLastWin32Error();
        }
        finally
        {
            if (!NativeMethods.RevertToSelf())
            {
                Environment.FailFast(
                    "RevertToSelf failed after the PipeProof anonymous-token ACL probe.",
                    new Win32Exception(Marshal.GetLastWin32Error()));
            }
        }

        bool denied = !connected && error == NativeMethods.ErrorAccessDenied;
        return new(
            Attempted: true,
            AccessDenied: denied,
            Win32Error: error,
            ProbeIdentity: "anonymous-thread-token",
            PipePath: pipePath,
            Outcome: connected
                ? "unexpected_connection_succeeded"
                : denied
                    ? "access_denied"
                    : $"connection_failed_with_unexpected_error_{error}");
    }
}
