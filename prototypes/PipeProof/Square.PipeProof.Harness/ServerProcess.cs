using Square.PipeProof.Client;
using Square.PipeProof.Protocol;
using Square.PipeProof.Transport.Windows;

namespace Square.PipeProof.Harness;

internal sealed class ServerProcess : IAsyncDisposable
{
    private readonly HarnessOptions _harness;
    private readonly string _pipeName;
    private StartedProcess? _process;
    private int _generation;

    internal ServerProcess(HarnessOptions harness, string pipeName, string stateDirectory)
    {
        _harness = harness;
        _pipeName = pipeName;
        StateDirectory = Path.GetFullPath(stateDirectory);
        Directory.CreateDirectory(StateDirectory);
    }

    internal string PipeName => _pipeName;
    internal string StateDirectory { get; }
    internal ServerReadyEvidence? Ready { get; private set; }
    internal string? ReadyFile { get; private set; }
    internal string? MetricsFile { get; private set; }
    internal int Generation => _generation;
    internal bool IsRunning => _process is { HasExited: false };

    internal async Task<ServerReadyEvidence> StartAsync(CancellationToken cancellationToken = default)
    {
        if (IsRunning)
        {
            throw new InvalidOperationException("PipeProof server is already running.");
        }
        _generation++;
        string serverDirectory = Path.Combine(_harness.OutputDirectory, "servers", $"generation-{_generation:D2}");
        Directory.CreateDirectory(serverDirectory);
        ReadyFile = Path.Combine(serverDirectory, "ready.json");
        MetricsFile = Path.Combine(serverDirectory, "final.json");
        File.Delete(ReadyFile);
        File.Delete(MetricsFile);

        string[] arguments =
        [
            "--pipe-name", _pipeName,
            "--state-dir", StateDirectory,
            "--ready-file", ReadyFile,
            "--metrics-file", MetricsFile,
            "--control-queue-capacity", "8",
            "--event-queue-capacity", "8",
            "--subscription-queue-capacity", "8",
            "--journal-retention-capacity", "64",
            "--maximum-replay-events", "8",
            "--maximum-inflight-requests", "16",
            "--write-timeout-ms", "250",
            "--maximum-publish-count", "20000",
            "--maximum-published-payload-bytes", "65536",
            "--shutdown-drain-timeout-ms", "3000",
            "--maximum-connections", "32"
        ];
        _process = ProcessRunner.StartArtifact(_harness.ServerArtifact, arguments);
        try
        {
            Ready = await WaitForJsonFileAsync<ServerReadyEvidence>(
                ReadyFile,
                _process,
                TimeSpan.FromSeconds(15),
                cancellationToken).ConfigureAwait(false);
            return Ready;
        }
        catch
        {
            await KillAsync("startup failure", cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    internal async Task<ProcessResult> ShutdownAsync(CancellationToken cancellationToken = default)
    {
        if (_process is null)
        {
            throw new InvalidOperationException("PipeProof server has not been started.");
        }
        if (!_process.HasExited)
        {
            try
            {
                await using ProtocolClientConnection client = await ConnectAsync(cancellationToken).ConfigureAwait(false);
                _ = await client.RequestAsync(
                    "proof.shutdown",
                    new { reason = "harness graceful shutdown" },
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is IOException or RemoteProtocolException or TimeoutException)
            {
                _process.Kill();
            }
        }
        ProcessResult result = await _process.WaitAsync(TimeSpan.FromSeconds(15), cancellationToken)
            .ConfigureAwait(false);
        await SaveProcessLogsAsync(result).ConfigureAwait(false);
        await _process.DisposeAsync().ConfigureAwait(false);
        _process = null;
        return result;
    }

    internal async Task<ProcessResult?> KillAsync(
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (_process is null)
        {
            return null;
        }
        if (!_process.HasExited)
        {
            _process.Kill();
        }
        ProcessResult result = await _process.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken)
            .ConfigureAwait(false);
        await SaveProcessLogsAsync(result, reason).ConfigureAwait(false);
        await _process.DisposeAsync().ConfigureAwait(false);
        _process = null;
        return result;
    }

    internal async Task<ProtocolClientConnection> ConnectAsync(
        CancellationToken cancellationToken = default,
        string requestedVersion = ProtocolConstants.CurrentVersion)
    {
        Stream stream = await NamedPipeConnector.ConnectAsync(
            _pipeName,
            TimeSpan.FromSeconds(10),
            cancellationToken).ConfigureAwait(false);
        ProtocolClientConnection connection = new(stream, new ProtocolClientOptions
        {
            ClientKind = "harness",
            ClientVersion = "0.1.0",
            ClientInstanceId = $"harness-{Guid.NewGuid():N}",
            RequestedVersion = requestedVersion,
            LocalSubscriptionCapacity = 256
        });
        try
        {
            _ = await connection.HandshakeAsync(cancellationToken).ConfigureAwait(false);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_process is not null)
        {
            await KillAsync("harness disposal").ConfigureAwait(false);
        }
    }

    private async Task SaveProcessLogsAsync(ProcessResult result, string? reason = null)
    {
        string directory = Path.Combine(_harness.OutputDirectory, "servers", $"generation-{_generation:D2}");
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory, "stdout.log"), result.StandardOutput).ConfigureAwait(false);
        await File.WriteAllTextAsync(Path.Combine(directory, "stderr.log"), result.StandardError).ConfigureAwait(false);
        await EvidenceWriter.WriteJsonAsync(Path.Combine(directory, "process.json"), new
        {
            schema_version = "1.0",
            generation = _generation,
            exit_code = result.ExitCode,
            duration_milliseconds = checked((long)result.Duration.TotalMilliseconds),
            stop_reason = reason
        }).ConfigureAwait(false);
    }

    private static async Task<T> WaitForJsonFileAsync<T>(
        string path,
        StartedProcess process,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow + timeout;
        Exception? lastReadFailure = null;
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (process.HasExited)
            {
                ProcessResult exited = await process.WaitAsync(TimeSpan.FromSeconds(1), cancellationToken)
                    .ConfigureAwait(false);
                throw new InvalidOperationException(
                    $"Server exited before writing readiness (exit {exited.ExitCode}): {exited.StandardError}");
            }
            if (File.Exists(path))
            {
                try
                {
                    return ProtocolJson.DeserializeText<T>(await File.ReadAllTextAsync(path, cancellationToken)
                        .ConfigureAwait(false));
                }
                catch (Exception exception) when (exception is IOException or System.Text.Json.JsonException or InvalidDataException)
                {
                    lastReadFailure = exception;
                }
            }
            await Task.Delay(25, cancellationToken).ConfigureAwait(false);
        }
        throw new TimeoutException(
            $"Timed out waiting for JSON file '{path}'.",
            lastReadFailure);
    }
}
