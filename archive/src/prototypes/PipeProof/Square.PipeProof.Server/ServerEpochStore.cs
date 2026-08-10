using System.Globalization;

namespace Square.PipeProof.Server;

internal static class ServerEpochStore
{
    internal static async Task<long> NextAsync(
        string stateDirectory,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(stateDirectory);
        string path = Path.Combine(stateDirectory, "server.epoch");
        long current = 0;
        if (File.Exists(path))
        {
            string text = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
            if (!long.TryParse(text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out current)
                || current < 0)
            {
                throw new InvalidDataException($"Server epoch file '{path}' is invalid.");
            }
        }
        long next = checked(current + 1);
        string temporary = $"{path}.{Guid.NewGuid():N}.tmp";
        await File.WriteAllTextAsync(
            temporary,
            next.ToString(CultureInfo.InvariantCulture),
            cancellationToken).ConfigureAwait(false);
        File.Move(temporary, path, overwrite: true);
        return next;
    }
}
