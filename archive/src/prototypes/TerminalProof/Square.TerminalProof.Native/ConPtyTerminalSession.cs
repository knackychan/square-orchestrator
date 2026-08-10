using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Square.TerminalProof.Native;

public sealed class ConPtyTerminalSession : IAsyncDisposable
{
    private const uint ProofHardStopExitCode = 0x534F0002;
    private readonly object _outputLock = new();
    private readonly MemoryStream _outputCapture = new();
    private readonly SemaphoreSlim _inputLock = new(1, 1);
    private readonly SafeProcessHandle _processHandle;
    private readonly JobObject _job;
    private readonly PseudoConsole _pseudoConsole;
    private readonly FileStream _input;
    private readonly FileStream _output;
    private readonly Task _outputPump;
    private readonly long _launchTimestamp;
    private readonly TimeSpan _cleanupTimeout;
    private long _firstOutputTimestamp = -1;
    private long _closeStartedTimestamp = -1;
    private long _closeCompletedTimestamp = -1;
    private long _outputPumpCompletedTimestamp = -1;
    private int _shutdownStarted;
    private int _disposeStarted;
    private int _disposed;
    private string? _closeFailure;
    private string? _pumpFailure;
    private bool _closeTimedOut;

    private ConPtyTerminalSession(
        uint processId,
        SafeProcessHandle processHandle,
        JobObject job,
        PseudoConsole pseudoConsole,
        SafeFileHandle inputWrite,
        SafeFileHandle outputRead,
        long launchTimestamp,
        TimeSpan cleanupTimeout)
    {
        ProcessId = checked((int)processId);
        _processHandle = processHandle;
        _job = job;
        _pseudoConsole = pseudoConsole;
        // CreatePipe returns synchronous handles. The output is drained by a dedicated thread and
        // input writes use FileStream's synchronous-handle fallback; claiming overlapped I/O here
        // would be incorrect.
        _input = new FileStream(inputWrite, FileAccess.Write, bufferSize: 4096, isAsync: false);
        _output = new FileStream(outputRead, FileAccess.Read, bufferSize: 64 * 1024, isAsync: false);
        _launchTimestamp = launchTimestamp;
        _cleanupTimeout = cleanupTimeout;
        // ponytail: raw Thread instead of Task.Factory.StartNew. A LongRunning task creates a
        // dedicated ThreadPool-aligned thread whose native thread handle survives task completion
        // until GC collects the internal thread wrapper. A raw named background thread avoids
        // the ThreadPool wrapper and makes completion deterministic.
        TaskCompletionSource outputPumpCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _outputPump = outputPumpCompletion.Task;
        Thread pumpThread = new(() =>
        {
            try
            {
                PumpOutput();
            }
            finally
            {
                OwnedResourceCounters.Decrement(OwnedResourceKind.OutputPumpThread);
                outputPumpCompletion.TrySetResult();
            }
        })
        {
            Name = "ConPtyOutputPump",
            IsBackground = true
        };
        OwnedResourceCounters.Increment(OwnedResourceKind.OutputPumpThread);
        pumpThread.Start();
    }

    public int ProcessId { get; }

    public bool IsCloseTimedOut => _closeTimedOut;

    public string? CloseFailure => _closeFailure;

    public string? PumpFailure => _pumpFailure;

    public TimeSpan? CloseDuration => _closeStartedTimestamp >= 0 && _closeCompletedTimestamp >= 0
        ? Stopwatch.GetElapsedTime(_closeStartedTimestamp, _closeCompletedTimestamp)
        : null;

    public TimeSpan? OutputPumpCompletionTime => _outputPumpCompletedTimestamp >= 0
        ? Stopwatch.GetElapsedTime(_closeStartedTimestamp >= 0 ? _closeStartedTimestamp : _launchTimestamp, _outputPumpCompletedTimestamp)
        : null;

    public bool IsOutputPumpCompleted => _outputPump.IsCompleted;

    public bool IsOutputPumpFaulted => _outputPump.IsFaulted;

    public bool IsRunning
    {
        get
        {
            uint result = NativeMethods.WaitForSingleObject(_processHandle, 0);
            return result switch
            {
                NativeMethods.WaitTimeout => true,
                NativeMethods.WaitObject0 => false,
                NativeMethods.WaitFailed => throw LastWaitException(),
                _ => throw new InvalidOperationException($"WaitForSingleObject returned unexpected status 0x{result:X8}.")
            };
        }
    }

    internal sealed record TerminalLaunchPlan(
        uint Cb,
        uint Flags,
        nint HStdInput,
        nint HStdOutput,
        nint HStdError,
        bool InheritHandles,
        uint CreationFlags,
        bool HasExtendedStartupInfoPresent,
        bool HasCreateUnicodeEnvironment,
        bool HasCreateSuspended,
        bool HasPseudoConsoleAttribute,
        int InitialColumns,
        int InitialRows);

    internal static TerminalLaunchPlan BuildLaunchPlan(TerminalLaunchOptions options)
    {
        TerminalSize size = options.InitialSize;
        return new TerminalLaunchPlan(
            Cb: checked((uint)Marshal.SizeOf<NativeMethods.StartupInfoEx>()),
            Flags: NativeMethods.StartFUseStdHandles,
            HStdInput: nint.Zero,
            HStdOutput: nint.Zero,
            HStdError: nint.Zero,
            InheritHandles: false,
            CreationFlags: NativeMethods.ExtendedStartupInfoPresent
                | NativeMethods.CreateUnicodeEnvironment
                | NativeMethods.CreateSuspended,
            HasExtendedStartupInfoPresent: true,
            HasCreateUnicodeEnvironment: true,
            HasCreateSuspended: true,
            HasPseudoConsoleAttribute: true,
            InitialColumns: size.Columns,
            InitialRows: size.Rows);
    }

    public static async Task<ConPtyTerminalSession> StartAsync(
        TerminalLaunchOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        cancellationToken.ThrowIfCancellationRequested();

        string executablePath = Path.GetFullPath(options.ExecutablePath);
        string workingDirectory = Path.GetFullPath(options.WorkingDirectory);
        string commandLine = WindowsCommandLine.Build(executablePath, options.Arguments);

        SafeFileHandle? pseudoInputRead = null;
        SafeFileHandle? hostInputWrite = null;
        SafeFileHandle? hostOutputRead = null;
        SafeFileHandle? pseudoOutputWrite = null;
        PseudoConsole? pseudoConsole = null;
        ProcThreadAttributeList? attributes = null;
        JobObject? job = null;
        SafeProcessHandle? processHandle = null;
        SafeThreadHandle? threadHandle = null;
        ConPtyTerminalSession? session = null;

        try
        {
            if (!NativeMethods.CreatePipe(out pseudoInputRead, out hostInputWrite, nint.Zero, 0))
            {
                Win32Error.ThrowLastError("CreatePipe(ConPTY input)");
            }

            OwnedResourceCounters.Increment(OwnedResourceKind.ConPtySidePipeHandle);
            OwnedResourceCounters.Increment(OwnedResourceKind.HostSidePipeHandle);

            if (!NativeMethods.CreatePipe(out hostOutputRead, out pseudoOutputWrite, nint.Zero, 0))
            {
                Win32Error.ThrowLastError("CreatePipe(ConPTY output)");
            }

            OwnedResourceCounters.Increment(OwnedResourceKind.HostSidePipeHandle);
            OwnedResourceCounters.Increment(OwnedResourceKind.ConPtySidePipeHandle);

            pseudoConsole = PseudoConsole.Create(options.InitialSize, pseudoInputRead, pseudoOutputWrite);
            attributes = ProcThreadAttributeList.CreateForPseudoConsole(pseudoConsole.Handle);
            job = JobObject.CreateKillOnClose();

            NativeMethods.StartupInfoEx startupInfo = new()
            {
                StartupInfo = new NativeMethods.StartupInfo
                {
                    Cb = checked((uint)Marshal.SizeOf<NativeMethods.StartupInfoEx>()),
                    Flags = NativeMethods.StartFUseStdHandles,
                    hStdInput = nint.Zero,
                    hStdOutput = nint.Zero,
                    hStdError = nint.Zero
                },
                AttributeList = attributes.Pointer
            };

            uint creationFlags = NativeMethods.ExtendedStartupInfoPresent
                | NativeMethods.CreateUnicodeEnvironment
                | NativeMethods.CreateSuspended;
            cancellationToken.ThrowIfCancellationRequested();
            StringBuilder mutableCommandLine = new(commandLine);
            if (!NativeMethods.CreateProcessW(
                    executablePath,
                    mutableCommandLine,
                    nint.Zero,
                    nint.Zero,
                    inheritHandles: false,
                    creationFlags,
                    nint.Zero,
                    workingDirectory,
                    ref startupInfo,
                    out NativeMethods.ProcessInformation processInformation))
            {
                Win32Error.ThrowLastError("CreateProcessW(ConPTY child)");
            }

            processHandle = new SafeProcessHandle(processInformation.Process, ownsHandle: true);
            threadHandle = new SafeThreadHandle(processInformation.Thread);
            OwnedResourceCounters.Increment(OwnedResourceKind.ProcessHandle);
            OwnedResourceCounters.Increment(OwnedResourceKind.PrimaryThreadHandle);

            // The process is suspended so assignment happens before any descendant can be created.
            job.Assign(processHandle);

            // Close the host copies of the pseudoconsole-side pipe handles. The pseudoconsole
            // holds its own references after CreatePseudoConsole and CreateProcessW.
            pseudoInputRead.Dispose();
            OwnedResourceCounters.Decrement(OwnedResourceKind.ConPtySidePipeHandle);
            pseudoInputRead = null;
            pseudoOutputWrite.Dispose();
            OwnedResourceCounters.Decrement(OwnedResourceKind.ConPtySidePipeHandle);
            pseudoOutputWrite = null;
            attributes.Dispose();
            attributes = null;

            long launchTimestamp = Stopwatch.GetTimestamp();
            session = new ConPtyTerminalSession(
                processInformation.ProcessId,
                processHandle,
                job,
                pseudoConsole,
                hostInputWrite,
                hostOutputRead,
                launchTimestamp,
                options.CleanupTimeout);

            processHandle = null;
            job = null;
            pseudoConsole = null;
            hostInputWrite = null;
            hostOutputRead = null;

            cancellationToken.ThrowIfCancellationRequested();
            uint resumeResult = NativeMethods.ResumeThread(threadHandle);
            if (resumeResult == uint.MaxValue)
            {
                Win32Error.ThrowLastError("ResumeThread(ConPTY child)");
            }

            if (resumeResult != 1)
            {
                throw new InvalidOperationException(
                    $"ResumeThread reported an unexpected previous suspend count of {resumeResult}; expected exactly one.");
            }

            threadHandle.Dispose();
            OwnedResourceCounters.Decrement(OwnedResourceKind.PrimaryThreadHandle);
            threadHandle = null;
            return session;
        }
        catch (Exception launchException)
        {
            List<Exception> cleanupFailures = new();
            if (session is not null)
            {
                try
                {
                    await session.HardStopAsync(ProofHardStopExitCode, CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception cleanupException)
                {
                    cleanupFailures.Add(cleanupException);
                }

                try
                {
                    await session.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception cleanupException)
                {
                    cleanupFailures.Add(cleanupException);
                }
            }
            else if (processHandle is not null && !processHandle.IsInvalid
                && !NativeMethods.TerminateProcess(processHandle, ProofHardStopExitCode))
            {
                cleanupFailures.Add(new System.ComponentModel.Win32Exception(
                    Marshal.GetLastWin32Error(),
                    "TerminateProcess failed while cleaning up a partially launched ConPTY child."));
            }

            if (cleanupFailures.Count != 0)
            {
                launchException.Data["Square.TerminalProof.CleanupFailures"] = new AggregateException(cleanupFailures);
            }

            throw;
        }
        finally
        {
            if (threadHandle is not null)
            {
                threadHandle.Dispose();
                OwnedResourceCounters.Decrement(OwnedResourceKind.PrimaryThreadHandle);
            }

            if (processHandle is not null)
            {
                processHandle.Dispose();
                OwnedResourceCounters.Decrement(OwnedResourceKind.ProcessHandle);
            }

            attributes?.Dispose();
            if (pseudoInputRead is not null)
            {
                pseudoInputRead.Dispose();
                OwnedResourceCounters.Decrement(OwnedResourceKind.ConPtySidePipeHandle);
            }

            if (pseudoOutputWrite is not null)
            {
                pseudoOutputWrite.Dispose();
                OwnedResourceCounters.Decrement(OwnedResourceKind.ConPtySidePipeHandle);
            }

            if (hostInputWrite is not null)
            {
                hostInputWrite.Dispose();
                OwnedResourceCounters.Decrement(OwnedResourceKind.HostSidePipeHandle);
            }

            if (hostOutputRead is not null)
            {
                hostOutputRead.Dispose();
                OwnedResourceCounters.Decrement(OwnedResourceKind.HostSidePipeHandle);
            }

            pseudoConsole?.Dispose();
            job?.Dispose();
        }
    }

    public async ValueTask WriteAsync(ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await _inputLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await _input.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
            await _input.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _inputLock.Release();
        }
    }

    public ValueTask WriteTextAsync(string text, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);
        return WriteAsync(Encoding.UTF8.GetBytes(text), cancellationToken);
    }

    public ValueTask SendCtrlCAsync(CancellationToken cancellationToken = default) =>
        WriteAsync(new byte[] { 0x03 }, cancellationToken);

    public void Resize(TerminalSize size)
    {
        ThrowIfDisposed();
        _pseudoConsole.Resize(size);
    }

    public async Task WaitForOutputAsync(
        string marker,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(marker);
        byte[] markerBytes = Encoding.UTF8.GetBytes(marker);
        Stopwatch stopwatch = Stopwatch.StartNew();

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (ContainsOutput(markerBytes))
            {
                return;
            }

            if (_outputPump.IsCompleted)
            {
                await _outputPump.ConfigureAwait(false);
                throw new EndOfStreamException($"Terminal output ended before marker '{marker}' was observed. Output excerpt: {GetOutputExcerpt(512)}");
            }

            if (stopwatch.Elapsed >= timeout)
            {
                throw new TimeoutException($"Terminal output did not contain marker '{marker}' within {timeout}. Output excerpt: {GetOutputExcerpt(512)}");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(10), cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<int> WaitForExitAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            uint result = NativeMethods.WaitForSingleObject(_processHandle, 0);
            if (result == NativeMethods.WaitObject0)
            {
                if (!NativeMethods.GetExitCodeProcess(_processHandle, out uint exitCode))
                {
                    Win32Error.ThrowLastError("GetExitCodeProcess");
                }

                if (exitCode == NativeMethods.StillActive)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(5), cancellationToken).ConfigureAwait(false);
                    continue;
                }

                return unchecked((int)exitCode);
            }

            if (result == NativeMethods.WaitFailed)
            {
                throw LastWaitException();
            }

            if (result != NativeMethods.WaitTimeout)
            {
                throw new InvalidOperationException($"WaitForSingleObject returned unexpected status 0x{result:X8}.");
            }

            if (stopwatch.Elapsed >= timeout)
            {
                throw new TimeoutException($"Root process {ProcessId} did not exit within {timeout}.");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(15), cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task HardStopAsync(uint exitCode = ProofHardStopExitCode, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (_job.GetAccounting().ActiveProcesses == 0)
        {
            return;
        }

        _job.Terminate(exitCode);
        await _job.WaitForEmptyAsync(_cleanupTimeout, cancellationToken).ConfigureAwait(false);
    }

    public IReadOnlyList<int> GetActiveProcessIds()
    {
        ThrowIfDisposed();
        return _job.GetActiveProcessIds();
    }

    public TerminalAccountingSnapshot GetAccounting()
    {
        ThrowIfDisposed();
        return _job.GetAccounting();
    }

    public TerminalOutputSnapshot GetOutputSnapshot()
    {
        lock (_outputLock)
        {
            TimeSpan? firstByteLatency = _firstOutputTimestamp < 0
                ? null
                : Stopwatch.GetElapsedTime(_launchTimestamp, _firstOutputTimestamp);
            return new TerminalOutputSnapshot(_outputCapture.ToArray(), firstByteLatency);
        }
    }

    public string GetOutputExcerpt(int maximumCharacters)
    {
        if (maximumCharacters < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumCharacters));
        }

        string text = GetOutputSnapshot().Utf8Text;
        string normalized = text
            .Replace("\u001b", "<ESC>", StringComparison.Ordinal)
            .Replace("\r", "<CR>", StringComparison.Ordinal)
            .Replace("\n", "<LF>", StringComparison.Ordinal);
        return normalized.Length <= maximumCharacters
            ? normalized
            : normalized[..maximumCharacters] + "…";
    }

    public async Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (Interlocked.Exchange(ref _shutdownStarted, 1) != 0)
        {
            await _outputPump.WaitAsync(_cleanupTimeout, cancellationToken).ConfigureAwait(false);
            return;
        }

        List<Exception> failures = new();
        try
        {
            if (IsRunning || _job.GetAccounting().ActiveProcesses != 0)
            {
                await HardStopAsync(ProofHardStopExitCode, CancellationToken.None).ConfigureAwait(false);
            }
        }
        catch (Exception exception)
        {
            failures.Add(exception);
        }

        try
        {
            await _input.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            failures.Add(exception);
        }

        try
        {
            _closeStartedTimestamp = Stopwatch.GetTimestamp();
            await _pseudoConsole.CloseAsync(_cleanupTimeout).ConfigureAwait(false);
            _closeCompletedTimestamp = Stopwatch.GetTimestamp();
        }
        catch (TimeoutException)
        {
            _closeTimedOut = true;
            _closeFailure = $"ClosePseudoConsole timed out after {_cleanupTimeout}.";
            failures.Add(new TimeoutException(_closeFailure));
        }
        catch (Exception exception)
        {
            _closeFailure = $"{exception.GetType().Name}: {exception.Message}";
            failures.Add(exception);
        }

        try
        {
            await _outputPump.WaitAsync(_cleanupTimeout, CancellationToken.None).ConfigureAwait(false);
            _outputPumpCompletedTimestamp = Stopwatch.GetTimestamp();
            if (_outputPump.IsFaulted && _outputPump.Exception is not null)
            {
                _pumpFailure = $"{_outputPump.Exception.InnerException?.GetType().Name}: {_outputPump.Exception.InnerException?.Message}";
                failures.Add(new InvalidOperationException("Output pump faulted.", _outputPump.Exception));
            }
        }
        catch (Exception exception)
        {
            _pumpFailure = $"{exception.GetType().Name}: {exception.Message}";
            failures.Add(exception);
        }

        try
        {
            await _output.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            failures.Add(exception);
        }

        ThrowCleanupFailures(failures);
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposeStarted, 1) != 0)
        {
            return;
        }

        List<Exception> failures = new();
        try
        {
            await ShutdownAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            failures.Add(exception);
        }

        Volatile.Write(ref _disposed, 1);
        try
        {
            await _input.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            failures.Add(exception);
        }

        try
        {
            await _output.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            failures.Add(exception);
        }

        _inputLock.Dispose();
        _processHandle.Dispose();
        OwnedResourceCounters.Decrement(OwnedResourceKind.ProcessHandle);
        _job.Dispose();
        _pseudoConsole.Dispose();
        _outputCapture.Dispose();
        OwnedResourceCounters.Decrement(OwnedResourceKind.HostSidePipeHandle);
        OwnedResourceCounters.Decrement(OwnedResourceKind.HostSidePipeHandle);
        ThrowCleanupFailures(failures);
    }

    private void PumpOutput()
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(64 * 1024);
        try
        {
            while (true)
            {
                int read = _output.Read(buffer, 0, buffer.Length);
                if (read == 0)
                {
                    return;
                }

                lock (_outputLock)
                {
                    if (_firstOutputTimestamp < 0)
                    {
                        _firstOutputTimestamp = Stopwatch.GetTimestamp();
                    }

                    _outputCapture.Write(buffer, 0, read);
                }
            }
        }
        catch (ObjectDisposedException) when (Volatile.Read(ref _shutdownStarted) != 0)
        {
        }
        catch (IOException ex) when (Volatile.Read(ref _shutdownStarted) != 0)
        {
            int error = Marshal.GetHRForException(ex);
            if (error == -2147024785 || error == -2147024784)
            {
                return;
            }
        }
        catch (Exception ex)
        {
            lock (_outputLock)
            {
                byte[] failure = Encoding.UTF8.GetBytes(
                    $"\nPUMP-FAILED:{ex.GetType().Name}:{ex.Message}\n");
                _outputCapture.Write(failure, 0, failure.Length);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private bool ContainsOutput(ReadOnlySpan<byte> marker)
    {
        lock (_outputLock)
        {
            if (!_outputCapture.TryGetBuffer(out ArraySegment<byte> segment))
            {
                return false;
            }

            return segment.AsSpan().IndexOf(marker) >= 0;
        }
    }

    private static void ThrowCleanupFailures(IReadOnlyList<Exception> failures)
    {
        if (failures.Count == 1)
        {
            throw failures[0];
        }

        if (failures.Count > 1)
        {
            throw new AggregateException("One or more ConPTY cleanup operations failed.", failures);
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
    }

    private static Exception LastWaitException()
    {
        int error = Marshal.GetLastWin32Error();
        return new System.ComponentModel.Win32Exception(error, $"WaitForSingleObject failed with Win32 error {error}.");
    }
}
