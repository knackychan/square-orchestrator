using System.Collections.Concurrent;

namespace Square.PipeProof.ServerCore;

public sealed class ProofProtocolServer : IAsyncDisposable
{
    private readonly IConnectionListener _listener;
    private readonly EventHub _events;
    private readonly ProofServerOptions _options;
    private readonly ServerMetrics _metrics;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly SemaphoreSlim _connectionSlots;
    private readonly ConcurrentDictionary<string, ConnectionSession> _sessions = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, Task> _sessionTasks = new(StringComparer.Ordinal);
    private readonly TaskCompletionSource<string> _shutdownRequested = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private Task? _acceptTask;
    private int _started;
    private int _stopped;

    public ProofProtocolServer(
        IConnectionListener listener,
        EventHub events,
        ProofServerOptions options,
        ServerMetrics metrics)
    {
        _listener = listener ?? throw new ArgumentNullException(nameof(listener));
        _events = events ?? throw new ArgumentNullException(nameof(events));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        _options.Validate();
        _connectionSlots = new SemaphoreSlim(options.MaximumConnections, options.MaximumConnections);
    }

    public string Endpoint => _listener.Endpoint;
    public string ServerInstanceId => _metrics.ServerInstanceId;
    public long ServerEpoch => _metrics.ServerEpoch;
    public EventJournalBounds EventBounds => _events.Bounds;
    public Task<string> ShutdownRequested => _shutdownRequested.Task;

    public void Start()
    {
        if (Interlocked.Exchange(ref _started, 1) != 0)
        {
            throw new InvalidOperationException("Proof server was already started.");
        }
        _acceptTask = Task.Run(AcceptLoopAsync);
    }

    public ServerMetricsSnapshot Snapshot() => _metrics.Snapshot(_events.Bounds);

    public void RequestShutdown(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            reason = "requested";
        }
        _shutdownRequested.TrySetResult(reason);
    }

    public async Task StopAsync(string reason, CancellationToken cancellationToken = default)
    {
        if (Interlocked.Exchange(ref _stopped, 1) != 0)
        {
            return;
        }

        ConnectionSession[] sessions = _sessions.Values.ToArray();
        foreach (ConnectionSession session in sessions)
        {
            await session.NotifyServerGoingAwayAsync(reason, cancellationToken).ConfigureAwait(false);
        }

        TimeSpan notificationWindow = TimeSpan.FromMilliseconds(
            Math.Min(250, Math.Max(25, _options.WriteTimeoutMilliseconds / 2)));
        await Task.Delay(notificationWindow, cancellationToken).ConfigureAwait(false);
        _lifetime.Cancel();
        await _listener.DisposeAsync().ConfigureAwait(false);

        if (_acceptTask is not null)
        {
            try
            {
                await _acceptTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (IOException) when (_lifetime.IsCancellationRequested)
            {
            }
        }

        Task[] active = _sessionTasks.Values.ToArray();
        if (active.Length > 0)
        {
            using CancellationTokenSource drain = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            drain.CancelAfter(_options.ShutdownDrainTimeout);
            try
            {
                await Task.WhenAll(active).WaitAsync(drain.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                foreach (ConnectionSession session in _sessions.Values)
                {
                    session.ForceStop();
                }
                await Task.WhenAll(_sessionTasks.Values.ToArray()).ConfigureAwait(false);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await StopAsync("disposed").ConfigureAwait(false);
        }
        finally
        {
            await _events.DisposeAsync().ConfigureAwait(false);
            _connectionSlots.Dispose();
            _lifetime.Dispose();
        }
    }

    private async Task AcceptLoopAsync()
    {
        try
        {
            while (!_lifetime.IsCancellationRequested)
            {
                await _connectionSlots.WaitAsync(_lifetime.Token).ConfigureAwait(false);
                Stream? stream = null;
                try
                {
                    stream = await _listener.AcceptAsync(_lifetime.Token).ConfigureAwait(false);
                    ConnectionMetrics connectionMetrics = _metrics.ConnectionAccepted(
                        _options.ControlQueueCapacity,
                        _options.EventQueueCapacity);
                    ConnectionSession session = new(
                        stream,
                        _events,
                        _options,
                        _metrics,
                        connectionMetrics,
                        RequestShutdown);
                    stream = null;
                    if (!_sessions.TryAdd(connectionMetrics.ConnectionId, session))
                    {
                        await session.DisposeAsync().ConfigureAwait(false);
                        throw new InvalidOperationException("Generated a duplicate active connection identifier.");
                    }
                    TaskCompletionSource<object?> startGate = new(
                        TaskCreationOptions.RunContinuationsAsynchronously);
                    Task task = RunSessionAsync(
                        connectionMetrics.ConnectionId,
                        session,
                        connectionMetrics,
                        startGate.Task);
                    if (!_sessionTasks.TryAdd(connectionMetrics.ConnectionId, task))
                    {
                        session.ForceStop();
                        startGate.TrySetCanceled();
                        throw new InvalidOperationException("Generated a duplicate session task identifier.");
                    }
                    startGate.TrySetResult(null);
                }
                catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
                {
                    if (stream is not null)
                    {
                        await stream.DisposeAsync().ConfigureAwait(false);
                    }
                    _connectionSlots.Release();
                    break;
                }
                catch (IOException) when (_lifetime.IsCancellationRequested)
                {
                    if (stream is not null)
                    {
                        await stream.DisposeAsync().ConfigureAwait(false);
                    }
                    _connectionSlots.Release();
                    break;
                }
                catch
                {
                    if (stream is not null)
                    {
                        await stream.DisposeAsync().ConfigureAwait(false);
                    }
                    _connectionSlots.Release();
                    throw;
                }
            }
        }
        catch (Exception exception) when (!_lifetime.IsCancellationRequested)
        {
            _shutdownRequested.TrySetException(exception);
            throw;
        }
    }

    private async Task RunSessionAsync(
        string connectionId,
        ConnectionSession session,
        ConnectionMetrics connectionMetrics,
        Task startGate)
    {
        try
        {
            await startGate.ConfigureAwait(false);
            await session.RunAsync(_lifetime.Token).ConfigureAwait(false);
        }
        catch (Exception exception) when (!_lifetime.IsCancellationRequested)
        {
            _shutdownRequested.TrySetException(exception);
        }
        finally
        {
            await session.DisposeAsync().ConfigureAwait(false);
            _sessions.TryRemove(connectionId, out _);
            _sessionTasks.TryRemove(connectionId, out _);
            _metrics.ConnectionClosed(connectionMetrics);
            _connectionSlots.Release();
        }
    }
}
