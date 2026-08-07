using System.Security.Cryptography;

namespace Square.TerminalProof.Harness;

internal static class Hashing
{
    internal static string Sha256(byte[] content) => Convert.ToHexStringLower(SHA256.HashData(content));

    internal static async Task<string> Sha256FileAsync(string path, CancellationToken cancellationToken)
    {
        await using FileStream stream = File.OpenRead(path);
        byte[] hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexStringLower(hash);
    }
}
