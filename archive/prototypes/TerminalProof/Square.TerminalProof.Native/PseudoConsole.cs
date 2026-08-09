namespace Square.TerminalProof.Native;

internal sealed class PseudoConsole : IDisposable
{
    private nint _handle;

    private PseudoConsole(nint handle)
    {
        _handle = handle;
    }

    internal nint Handle => _handle != nint.Zero
        ? _handle
        : throw new ObjectDisposedException(nameof(PseudoConsole));

    internal static PseudoConsole Create(TerminalSize size, Microsoft.Win32.SafeHandles.SafeFileHandle input, Microsoft.Win32.SafeHandles.SafeFileHandle output)
    {
        int hresult = NativeMethods.CreatePseudoConsole(size.ToNative(), input, output, 0, out nint handle);
        Win32Error.ThrowIfHResultFailed(hresult, "CreatePseudoConsole");
        if (handle == nint.Zero)
        {
            throw new InvalidOperationException("CreatePseudoConsole returned a null handle after reporting success.");
        }

        OwnedResourceCounters.Increment(OwnedResourceKind.PseudoConsole);
        return new PseudoConsole(handle);
    }

    internal void Resize(TerminalSize size)
    {
        int hresult = NativeMethods.ResizePseudoConsole(Handle, size.ToNative());
        Win32Error.ThrowIfHResultFailed(hresult, "ResizePseudoConsole");
    }

    internal async Task CloseAsync(TimeSpan timeout)
    {
        nint handle = Interlocked.Exchange(ref _handle, nint.Zero);
        if (handle == nint.Zero)
        {
            return;
        }

        TaskCompletionSource closeCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Thread closeThread = new(() =>
        {
            OwnedResourceCounters.Increment(OwnedResourceKind.ClosePseudoConsoleThread);
            Exception? failure = null;
            try
            {
                NativeMethods.ClosePseudoConsole(handle);
            }
            catch (Exception ex)
            {
                failure = ex;
            }
            finally
            {
                OwnedResourceCounters.Decrement(OwnedResourceKind.ClosePseudoConsoleThread);
                OwnedResourceCounters.Decrement(OwnedResourceKind.PseudoConsole);
            }

            if (failure is null)
            {
                closeCompletion.TrySetResult();
            }
            else
            {
                closeCompletion.TrySetException(failure);
            }
        })
        {
            Name = "ConPtyCloseThread",
            IsBackground = true
        };
        closeThread.Start();

        await closeCompletion.Task.WaitAsync(timeout).ConfigureAwait(false);

        if (!closeCompletion.Task.IsCompleted)
        {
            throw new TimeoutException($"ClosePseudoConsole did not complete within {timeout}.");
        }
    }

    public void Dispose()
    {
        nint handle = Interlocked.Exchange(ref _handle, nint.Zero);
        if (handle != nint.Zero)
        {
            try
            {
                NativeMethods.ClosePseudoConsole(handle);
            }
            finally
            {
                OwnedResourceCounters.Decrement(OwnedResourceKind.PseudoConsole);
            }
        }
    }
}
