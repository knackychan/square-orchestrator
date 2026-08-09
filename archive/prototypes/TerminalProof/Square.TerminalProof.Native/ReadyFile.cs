using System.Diagnostics;
using System.Text;

namespace Square.TerminalProof.Native;

/// <summary>
/// Deterministic ready-file publication and consumption. The writer never exposes a partial
/// file under the final name: it writes to a unique temporary path in the same directory,
/// forces the content to disk, closes it, and atomically renames it into place. The reader
/// only ever opens the final name with an explicit FileShare.Read, closes each handle
/// immediately, and treats sharing violations as an incomplete-publication retry while any
/// parse/identity failure on stable content is a hard protocol failure.
/// </summary>
public static class ReadyFile
{
    public const FileShare FinalShareMode = FileShare.Read;

    public static async Task WriteAtomicallyAsync(
        string path,
        byte[] content,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(content);

        string directory = Path.GetDirectoryName(Path.GetFullPath(path))
            ?? throw new InvalidOperationException("The ready file must have a parent directory.");
        Directory.CreateDirectory(directory);
        string temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";

        OwnedResourceCounters.Increment(OwnedResourceKind.ReadyFileWriter);
        try
        {
            await using (FileStream stream = new(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 16 * 1024,
                FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await stream.WriteAsync(content, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                stream.Flush(flushToDisk: true);
            }

            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            OwnedResourceCounters.Decrement(OwnedResourceKind.ReadyFileWriter);
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    /// <summary>
    /// Waits for the final ready file and validates it. Recognized incomplete-publication
    /// states (absent file, transient sharing violation on the final path) are retried until
    /// <paramref name="timeout"/>; stable malformed content fails with InvalidDataException.
    /// </summary>
    public static async Task<string> ReadValidatedAsync(
        string path,
        TimeSpan timeout,
        Action<string>? validate,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        Stopwatch stopwatch = Stopwatch.StartNew();
        string text = string.Empty;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!File.Exists(path))
            {
                if (stopwatch.Elapsed >= timeout)
                {
                    throw new TimeoutException($"Ready file '{path}' was not published within {timeout}.");
                }

                await Task.Delay(TimeSpan.FromMilliseconds(20), cancellationToken).ConfigureAwait(false);
                continue;
            }

            OwnedResourceCounters.Increment(OwnedResourceKind.ReadyFileReader);
            bool sharingViolation = false;
            try
            {
                using (FileStream stream = new(path, FileMode.Open, FileAccess.Read, FinalShareMode, bufferSize: 4096, useAsync: false))
                using (StreamReader reader = new(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 4096, leaveOpen: false))
                {
                    text = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            catch (IOException) when (stopwatch.Elapsed < timeout)
            {
                // Sharing violation: the final path was published but another reader held it,
                // or the rename was in flight. This is a recognized incomplete-publication state.
                sharingViolation = true;
            }
            finally
            {
                OwnedResourceCounters.Decrement(OwnedResourceKind.ReadyFileReader);
            }

            if (sharingViolation)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(20), cancellationToken).ConfigureAwait(false);
                continue;
            }

            break;
        }

        if (validate is not null)
        {
            try
            {
                validate(text);
            }
            catch (InvalidDataException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new InvalidDataException(
                    $"Ready file '{path}' contained stable content that failed validation: {exception.Message}",
                    exception);
            }
        }

        return text;
    }
}
