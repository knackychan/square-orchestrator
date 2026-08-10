using System.Text.Json;

namespace Square.SharedUiProof.WebView2;

internal sealed record ProofInputs(
    JsonElement Fixture,
    JsonElement Benchmark,
    string FixtureSha256,
    string BenchmarkSha256,
    string RunId)
{
    public static async Task<ProofInputs> LoadAsync(string baseDirectory, CancellationToken cancellationToken)
    {
        var fixtureBytes = await File.ReadAllBytesAsync(Path.Combine(baseDirectory, "fixtures", "canonical-state.json"), cancellationToken);
        var benchmarkBytes = await File.ReadAllBytesAsync(Path.Combine(baseDirectory, "fixtures", "benchmark-manifest.json"), cancellationToken);
        var fixtureSha256 = (await File.ReadAllTextAsync(Path.Combine(baseDirectory, "fixtures", "canonical-state.sha256"), cancellationToken)).Trim();
        using var fixtureDocument = JsonDocument.Parse(fixtureBytes);
        using var benchmarkDocument = JsonDocument.Parse(benchmarkBytes);
        var runId = benchmarkDocument.RootElement.GetProperty("runId").GetString()
            ?? throw new InvalidDataException("Benchmark runId is missing.");
        return new ProofInputs(
            fixtureDocument.RootElement.Clone(),
            benchmarkDocument.RootElement.Clone(),
            fixtureSha256,
            Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(benchmarkBytes)).ToLowerInvariant(),
            runId);
    }
}
