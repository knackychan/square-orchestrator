using System.Text.Json;

namespace Square.SharedUiProof.WebView2;

internal static class EvidenceWriter
{
    public static async Task WriteAtomicAsync(string path, object evidence, CancellationToken cancellationToken)
    {
        var fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? throw new InvalidOperationException("Evidence path has no parent directory."));
        var temporary = $"{fullPath}.{Guid.NewGuid():N}.tmp";
        await using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 65_536, FileOptions.WriteThrough))
        {
            await JsonSerializer.SerializeAsync(stream, evidence, new JsonSerializerOptions { WriteIndented = true }, cancellationToken);
            await stream.FlushAsync(cancellationToken);
            stream.Flush(true);
        }
        File.Move(temporary, fullPath, true);
    }
}
