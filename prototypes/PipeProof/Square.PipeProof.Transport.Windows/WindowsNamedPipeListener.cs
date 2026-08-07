using System.ComponentModel;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Square.PipeProof.ServerCore;

namespace Square.PipeProof.Transport.Windows;

public sealed class WindowsNamedPipeListener : IConnectionListener
{
    private readonly int _maximumInstances;
    private readonly PipeSecurityDescriptor _securityDescriptor;
    private readonly SemaphoreSlim _acceptGate = new(1, 1);
    private readonly CancellationTokenSource _lifetime = new();
    private NamedPipeServerStream? _pending;
    private int _disposed;

    public WindowsNamedPipeListener(string pipeName, int maximumInstances = 32)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("The named-pipe proof transport requires Windows.");
        }
        ValidatePipeName(pipeName);
        if (maximumInstances is < 1 or > 254)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumInstances),
                maximumInstances,
                "Maximum instances must be in the range 1..254.");
        }

        PipeName = pipeName;
        Endpoint = $@"\\.\pipe\{pipeName}";
        _maximumInstances = maximumInstances;
        _securityDescriptor = new PipeSecurityDescriptor();
        try
        {
            _pending = CreateServerStream(firstInstance: true);
            PipeAcl = _securityDescriptor.Inspect(_pending.SafePipeHandle);
            if (!PipeAcl.GrantsOnlyCurrentUserAndSystem)
            {
                throw new InvalidDataException(
                    "The live named-pipe DACL did not match the protected current-user-and-SYSTEM policy.");
            }
        }
        catch
        {
            _pending?.Dispose();
            _securityDescriptor.Dispose();
            _acceptGate.Dispose();
            _lifetime.Dispose();
            throw;
        }
    }

    public string Endpoint { get; }
    public string PipeName { get; }
    public PipeAclEvidence PipeAcl { get; }

    public async ValueTask<Stream> AcceptAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _lifetime.Token);
        await _acceptGate.WaitAsync(linked.Token).ConfigureAwait(false);
        NamedPipeServerStream? accepted = null;
        try
        {
            accepted = _pending
                ?? throw new InvalidOperationException("No pending named-pipe server instance exists.");
            _pending = null;
            await accepted.WaitForConnectionAsync(linked.Token).ConfigureAwait(false);
            if (Volatile.Read(ref _disposed) == 0)
            {
                _pending = CreateServerStream(firstInstance: false);
            }
            NamedPipeServerStream result = accepted;
            accepted = null;
            return result;
        }
        catch
        {
            accepted?.Dispose();
            if (Volatile.Read(ref _disposed) == 0 && _pending is null)
            {
                _pending = CreateServerStream(firstInstance: false);
            }
            throw;
        }
        finally
        {
            _acceptGate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }
        _lifetime.Cancel();
        await _acceptGate.WaitAsync().ConfigureAwait(false);
        try
        {
            _pending?.Dispose();
            _pending = null;
            _securityDescriptor.Dispose();
        }
        finally
        {
            _acceptGate.Release();
            _acceptGate.Dispose();
            _lifetime.Dispose();
        }
    }

    private NamedPipeServerStream CreateServerStream(bool firstInstance)
    {
        NativeMethods.SecurityAttributes attributes = _securityDescriptor.CreateAttributes();
        uint openMode = NativeMethods.PipeAccessDuplex | NativeMethods.FileFlagOverlapped;
        if (firstInstance)
        {
            openMode |= NativeMethods.FileFlagFirstPipeInstance;
        }
        SafePipeHandle handle = NativeMethods.CreateNamedPipeW(
            Endpoint,
            openMode,
            NativeMethods.PipeTypeByte
                | NativeMethods.PipeReadModeByte
                | NativeMethods.PipeWait
                | NativeMethods.PipeRejectRemoteClients,
            checked((uint)_maximumInstances),
            65_536,
            65_536,
            0,
            ref attributes);
        if (handle.IsInvalid)
        {
            int error = Marshal.GetLastWin32Error();
            handle.Dispose();
            throw new Win32Exception(error, $"CreateNamedPipeW failed for '{Endpoint}'.");
        }
        try
        {
            return new NamedPipeServerStream(
                PipeDirection.InOut,
                isAsync: true,
                isConnected: false,
                handle);
        }
        catch
        {
            handle.Dispose();
            throw;
        }
    }

    private static void ValidatePipeName(string pipeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pipeName);
        if (pipeName.Length > 200 || pipeName.Contains('\\') || pipeName.Contains('/'))
        {
            throw new ArgumentException(
                "Pipe name must be a short local name without path separators.",
                nameof(pipeName));
        }
    }
}
