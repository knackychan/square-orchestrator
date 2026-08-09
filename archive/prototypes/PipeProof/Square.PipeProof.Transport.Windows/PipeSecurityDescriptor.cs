using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;

namespace Square.PipeProof.Transport.Windows;

internal sealed class PipeSecurityDescriptor : IDisposable
{
    private IntPtr _descriptor;
    private int _disposed;

    internal PipeSecurityDescriptor()
    {
        using WindowsIdentity identity = WindowsIdentity.GetCurrent(TokenAccessLevels.Query);
        CurrentUserSid = identity.User?.Value
            ?? throw new InvalidOperationException("The current Windows identity has no user SID.");
        SystemSid = "S-1-5-18";
        RequestedSddl = $"D:P(A;;GA;;;{SystemSid})(A;;GA;;;{CurrentUserSid})";
        if (!NativeMethods.ConvertStringSecurityDescriptorToSecurityDescriptorW(
            RequestedSddl,
            NativeMethods.SddlRevision1,
            out _descriptor,
            out _))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    internal string CurrentUserSid { get; }
    internal string SystemSid { get; }
    internal string RequestedSddl { get; }

    internal NativeMethods.SecurityAttributes CreateAttributes()
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        return new()
        {
            Length = checked((uint)Marshal.SizeOf<NativeMethods.SecurityAttributes>()),
            SecurityDescriptor = _descriptor,
            InheritHandle = false
        };
    }

    internal PipeAclEvidence Inspect(SafePipeHandle handle)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        ArgumentNullException.ThrowIfNull(handle);
        uint result = NativeMethods.GetSecurityInfo(
            handle,
            NativeMethods.SeKernelObject,
            NativeMethods.DaclSecurityInformation,
            out _,
            out _,
            out IntPtr dacl,
            out _,
            out IntPtr securityDescriptor);
        if (result != 0)
        {
            throw new Win32Exception(checked((int)result));
        }

        try
        {
            if (!NativeMethods.GetSecurityDescriptorControl(securityDescriptor, out ushort control, out _))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            bool daclProtected = (control & NativeMethods.SeDaclProtected) != 0;
            List<PipeAceEvidence> aces = dacl == IntPtr.Zero ? [] : ReadAces(dacl);
            string[] allowed = aces
                .Where(static ace => ace.AceType == NativeMethods.AccessAllowedAceType)
                .Select(static ace => ace.Sid)
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToArray();
            string[] expected = [CurrentUserSid, SystemSid];
            Array.Sort(expected, StringComparer.Ordinal);
            bool grantsOnlyExpected = dacl != IntPtr.Zero
                && daclProtected
                && aces.Count == 2
                && aces.All(static ace =>
                    ace.AceType == NativeMethods.AccessAllowedAceType
                    && !ace.Inherited
                    && ace.GrantsFullControl)
                && allowed.SequenceEqual(expected, StringComparer.Ordinal);
            return new(
                CurrentUserSid,
                SystemSid,
                RequestedSddl,
                ConvertSecurityDescriptor(securityDescriptor),
                DaclPresent: dacl != IntPtr.Zero,
                DaclProtected: daclProtected,
                allowed,
                aces,
                grantsOnlyExpected);
        }
        finally
        {
            _ = NativeMethods.LocalFree(securityDescriptor);
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }
        if (_descriptor != IntPtr.Zero)
        {
            _ = NativeMethods.LocalFree(_descriptor);
            _descriptor = IntPtr.Zero;
        }
    }

    private static List<PipeAceEvidence> ReadAces(IntPtr dacl)
    {
        if (!NativeMethods.GetAclInformation(
            dacl,
            out NativeMethods.AclSizeInformationData information,
            checked((uint)Marshal.SizeOf<NativeMethods.AclSizeInformationData>()),
            NativeMethods.AclSizeInformation))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        List<PipeAceEvidence> aces = [];
        for (uint index = 0; index < information.AceCount; index++)
        {
            if (!NativeMethods.GetAce(dacl, index, out IntPtr ace))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            byte aceType = Marshal.ReadByte(ace, 0);
            byte aceFlags = Marshal.ReadByte(ace, 1);
            uint accessMask = unchecked((uint)Marshal.ReadInt32(ace, 4));
            string sid = ConvertSid(IntPtr.Add(ace, 8));
            bool grantsFullControl = (accessMask & NativeMethods.GenericAll) != 0
                || (accessMask & NativeMethods.FileAllAccess) == NativeMethods.FileAllAccess;
            aces.Add(new(
                sid,
                accessMask,
                aceType,
                aceFlags,
                Inherited: (aceFlags & NativeMethods.InheritedAce) != 0,
                grantsFullControl));
        }
        return aces;
    }

    private static string ConvertSid(IntPtr sid)
    {
        if (!NativeMethods.ConvertSidToStringSidW(sid, out IntPtr text))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
        try
        {
            return Marshal.PtrToStringUni(text)
                ?? throw new InvalidOperationException("SID conversion returned an empty string.");
        }
        finally
        {
            _ = NativeMethods.LocalFree(text);
        }
    }

    private static string ConvertSecurityDescriptor(IntPtr descriptor)
    {
        if (!NativeMethods.ConvertSecurityDescriptorToStringSecurityDescriptorW(
            descriptor,
            NativeMethods.SddlRevision1,
            NativeMethods.DaclSecurityInformation,
            out IntPtr text,
            out _))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
        try
        {
            return Marshal.PtrToStringUni(text)
                ?? throw new InvalidOperationException("Security descriptor conversion returned an empty string.");
        }
        finally
        {
            _ = NativeMethods.LocalFree(text);
        }
    }
}
