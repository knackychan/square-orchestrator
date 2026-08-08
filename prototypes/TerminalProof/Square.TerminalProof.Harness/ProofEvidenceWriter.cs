using System.Text;
using System.Text.Json;

namespace Square.TerminalProof.Harness;

internal sealed class ProofEvidenceWriter : IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly FileStream _runsStream;
    private readonly StreamWriter _runsWriter;

    internal ProofEvidenceWriter(string directory)
    {
        DirectoryPath = Path.GetFullPath(directory);
        Directory.CreateDirectory(DirectoryPath);
        string runsPath = Path.Combine(DirectoryPath, "runs.ndjson");
        _runsStream = new FileStream(runsPath, FileMode.Create, FileAccess.Write, FileShare.Read, 64 * 1024, useAsync: true);
        _runsWriter = new StreamWriter(_runsStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), 64 * 1024, leaveOpen: true);
    }

    internal string DirectoryPath { get; }

    internal async Task WriteEnvironmentAsync(ProofEnvironmentEvidence environment, CancellationToken cancellationToken) =>
        await WriteJsonFileAsync("environment.json", environment, cancellationToken).ConfigureAwait(false);

    internal async Task WriteManifestSnapshotAsync(ProofManifest manifest, CancellationToken cancellationToken) =>
        await WriteJsonFileAsync("manifest.snapshot.json", manifest, cancellationToken).ConfigureAwait(false);

    internal async Task AppendRunAsync(SessionRunEvidence evidence, CancellationToken cancellationToken)
    {
        string json = JsonSerializer.Serialize(evidence, ProofJson.Create());
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await _runsWriter.WriteLineAsync(json.AsMemory(), cancellationToken).ConfigureAwait(false);
            await _runsWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    internal async Task WriteSummaryAsync(ProofSummaryEvidence summary, CancellationToken cancellationToken) =>
        await WriteJsonFileAsync("summary.json", summary, cancellationToken).ConfigureAwait(false);

    internal async Task WriteEvidenceManifestAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await _runsWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
            await _runsWriter.DisposeAsync().ConfigureAwait(false);
            await _runsStream.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }

        List<string> lines = new();
        foreach (string path in Directory.EnumerateFiles(DirectoryPath, "*", SearchOption.TopDirectoryOnly)
                     .Where(path => !string.Equals(Path.GetFileName(path), "evidence-manifest.sha256", StringComparison.OrdinalIgnoreCase))
                     .Order(StringComparer.Ordinal))
        {
            string hash = await Hashing.Sha256FileAsync(path, cancellationToken).ConfigureAwait(false);
            lines.Add($"{hash}  {Path.GetFileName(path)}");
        }

        await File.WriteAllLinesAsync(
            Path.Combine(DirectoryPath, "evidence-manifest.sha256"),
            lines,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            try { await _runsWriter.FlushAsync().ConfigureAwait(false); } catch (ObjectDisposedException) { }
            try { await _runsWriter.DisposeAsync().ConfigureAwait(false); } catch (ObjectDisposedException) { }
            try { await _runsStream.DisposeAsync().ConfigureAwait(false); } catch (ObjectDisposedException) { }
        }
        finally
        {
            _gate.Release();
            _gate.Dispose();
        }
    }

    private async Task WriteJsonFileAsync<T>(string filename, T value, CancellationToken cancellationToken)
    {
        string path = Path.Combine(DirectoryPath, filename);
        await using FileStream stream = new(path, FileMode.Create, FileAccess.Write, FileShare.Read, 64 * 1024, useAsync: true);
        await JsonSerializer.SerializeAsync(stream, value, ProofJson.Create(indented: true), cancellationToken).ConfigureAwait(false);
        await stream.WriteAsync("\n"u8.ToArray(), cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
}
