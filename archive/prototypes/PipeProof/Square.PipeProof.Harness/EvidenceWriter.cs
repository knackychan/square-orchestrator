using System.Security.Cryptography;
using System.Text;
using Square.PipeProof.Protocol;

namespace Square.PipeProof.Harness;

internal sealed class EvidenceWriter : IAsyncDisposable
{
    private readonly string _outputDirectory;
    private readonly FileStream _scenarioStream;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private int _disposed;

    internal EvidenceWriter(string outputDirectory)
    {
        _outputDirectory = Path.GetFullPath(outputDirectory);
        Directory.CreateDirectory(_outputDirectory);
        string scenarioPath = Path.Combine(_outputDirectory, "scenarios.ndjson");
        _scenarioStream = new FileStream(
            scenarioPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.Read,
            16_384,
            FileOptions.Asynchronous | FileOptions.WriteThrough);
    }

    internal Task WriteEnvironmentAsync(HarnessEnvironmentEvidence environment) =>
        WriteJsonAsync(Path.Combine(_outputDirectory, "environment.json"), environment);

    internal async Task AppendScenarioAsync(
        ScenarioEvidence scenario,
        CancellationToken cancellationToken = default)
    {
        byte[] json = ProtocolJson.Serialize(scenario);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await _scenarioStream.WriteAsync(json, cancellationToken).ConfigureAwait(false);
            await _scenarioStream.WriteAsync("\n"u8.ToArray(), cancellationToken).ConfigureAwait(false);
            await _scenarioStream.FlushAsync(cancellationToken).ConfigureAwait(false);
            _scenarioStream.Flush(flushToDisk: true);
        }
        finally
        {
            _gate.Release();
        }
    }

    internal Task WriteSummaryAsync(HarnessSummary summary) =>
        WriteJsonAsync(Path.Combine(_outputDirectory, "summary.json"), summary);

    internal async Task<string> WriteEvidenceManifestAsync(CancellationToken cancellationToken = default)
    {
        string manifestPath = Path.Combine(_outputDirectory, "evidence-manifest.sha256");
        string[] files = Directory
            .EnumerateFiles(_outputDirectory, "*", SearchOption.AllDirectories)
            .Where(path => !string.Equals(path, manifestPath, StringComparison.OrdinalIgnoreCase))
            .OrderBy(static path => path, StringComparer.Ordinal)
            .ToArray();
        StringBuilder content = new();
        foreach (string file in files)
        {
            string hash = await ComputeSha256Async(file, cancellationToken).ConfigureAwait(false);
            string relative = Path.GetRelativePath(_outputDirectory, file).Replace('\\', '/');
            content.Append(hash).Append("  ").Append(relative).Append('\n');
        }
        await File.WriteAllTextAsync(manifestPath, content.ToString(), new UTF8Encoding(false), cancellationToken)
            .ConfigureAwait(false);
        return Path.GetFileName(manifestPath);
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            await _scenarioStream.FlushAsync().ConfigureAwait(false);
            _scenarioStream.Flush(flushToDisk: true);
            await _scenarioStream.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
            _gate.Dispose();
        }
    }

    internal static async Task WriteJsonAsync<T>(
        string path,
        T value,
        CancellationToken cancellationToken = default)
    {
        string fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        string temporary = $"{fullPath}.{Guid.NewGuid():N}.tmp";
        await File.WriteAllBytesAsync(temporary, ProtocolJson.Serialize(value), cancellationToken).ConfigureAwait(false);
        File.Move(temporary, fullPath, overwrite: true);
    }

    internal static async Task<string> ComputeSha256Async(
        string path,
        CancellationToken cancellationToken = default)
    {
        await using FileStream stream = File.OpenRead(path);
        byte[] digest = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexStringLower(digest);
    }
}
