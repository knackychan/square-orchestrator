using System.Runtime.InteropServices;

namespace Square.TerminalProof.Native;

internal sealed class ProcThreadAttributeList : IDisposable
{
    private nint _memory;

    private ProcThreadAttributeList(nint memory)
    {
        _memory = memory;
    }

    internal nint Pointer => _memory != nint.Zero
        ? _memory
        : throw new ObjectDisposedException(nameof(ProcThreadAttributeList));

    internal static ProcThreadAttributeList CreateForPseudoConsole(nint pseudoConsole)
    {
        if (pseudoConsole == nint.Zero)
        {
            throw new ArgumentException("The pseudoconsole handle cannot be null.", nameof(pseudoConsole));
        }

        nuint bytes = 0;
        _ = NativeMethods.InitializeProcThreadAttributeList(nint.Zero, 1, 0, ref bytes);
        if (bytes == 0)
        {
            Win32Error.ThrowLastError("InitializeProcThreadAttributeList(size query)");
        }

        nint memory = Marshal.AllocHGlobal(checked((nint)bytes));
        bool initialized = false;
        try
        {
            if (!NativeMethods.InitializeProcThreadAttributeList(memory, 1, 0, ref bytes))
            {
                Win32Error.ThrowLastError("InitializeProcThreadAttributeList");
            }

            initialized = true;
            // PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE takes the HPCON value directly as lpValue,
            // not the address of a separate cell containing that handle.
            if (!NativeMethods.UpdateProcThreadAttribute(
                    memory,
                    0,
                    NativeMethods.ProcThreadAttributePseudoConsole,
                    pseudoConsole,
                    (nuint)nint.Size,
                    nint.Zero,
                    nint.Zero))
            {
                Win32Error.ThrowLastError("UpdateProcThreadAttribute(PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE)");
            }

            return new ProcThreadAttributeList(memory);
        }
        catch
        {
            if (initialized)
            {
                NativeMethods.DeleteProcThreadAttributeList(memory);
            }

            Marshal.FreeHGlobal(memory);
            throw;
        }
    }

    public void Dispose()
    {
        nint memory = Interlocked.Exchange(ref _memory, nint.Zero);
        if (memory != nint.Zero)
        {
            NativeMethods.DeleteProcThreadAttributeList(memory);
            Marshal.FreeHGlobal(memory);
        }
    }
}
