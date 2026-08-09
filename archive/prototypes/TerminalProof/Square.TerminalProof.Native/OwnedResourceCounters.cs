using System.Collections.Frozen;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace Square.TerminalProof.Native;

/// <summary>
/// TerminalProof-owned resource buckets, tracked only while an object is acquired.
/// These counters are diagnostic evidence for the SP00-T02-FIX03 proof; they are never
/// referenced by production code and must not become product APIs.
/// </summary>
public enum OwnedResourceKind
{
    PseudoConsole,
    ConPtySidePipeHandle,
    HostSidePipeHandle,
    JobObject,
    ProcessHandle,
    PrimaryThreadHandle,
    OutputPumpThread,
    ClosePseudoConsoleThread,
    OwnerCrashProcess,
    OwnerCrashStdoutDrain,
    OwnerCrashStderrDrain,
    ReadyFileReader,
    ReadyFileWriter,
    OwnerCrashTimeoutScope
}

/// <summary>
/// Interlocked-backed ownership counters. Acquisition increments only after the underlying
/// resource is successfully created; release decrements exactly once. Negative counts are
/// rejected as a proof failure. A nonzero bucket after a session or probe disposal boundary
/// is evidence of retention, independent of Process.HandleCount.
/// </summary>
public static class OwnedResourceCounters
{
    private static readonly int[] Values = new int[Enum.GetValues<OwnedResourceKind>().Length];
    private static readonly string[] Names = Enum.GetNames<OwnedResourceKind>();

    public static int Count(OwnedResourceKind kind) => Volatile.Read(ref Values[(int)kind]);

    public static void Increment(OwnedResourceKind kind)
    {
        int index = (int)kind;
        int value = Interlocked.Increment(ref Values[index]);
        if (value < 0)
        {
            throw new InvalidOperationException(
                $"OwnedResourceCounters.{Names[index]} became negative ({value}).");
        }
    }

    public static void Decrement(OwnedResourceKind kind)
    {
        int index = (int)kind;
        int value = Interlocked.Decrement(ref Values[index]);
        if (value < 0)
        {
            throw new InvalidOperationException(
                $"OwnedResourceCounters.{Names[index]} became negative ({value}).");
        }
    }

    public static IReadOnlyList<OwnedResourceCount> Snapshot()
    {
        OwnedResourceCount[] snapshot = new OwnedResourceCount[Values.Length];
        for (int index = 0; index < Values.Length; index++)
        {
            snapshot[index] = new OwnedResourceCount(Names[index], Volatile.Read(ref Values[index]));
        }

        return snapshot.AsReadOnly();
    }

    /// <summary>
    /// Throws when any bucket is nonzero. Used as the disposal-boundary assertion.
    /// </summary>
    public static void AssertZero(string context)
    {
        List<OwnedResourceCount> nonzero = Snapshot().Where(count => count.Count != 0).ToList();
        if (nonzero.Count != 0)
        {
            throw new OwnedResourceRetentionException(context, nonzero);
        }
    }

    public static bool IsZero => Snapshot().All(count => count.Count == 0);

    internal static OwnedResourceCountsSnapshot PublicSnapshot =>
        new(DateTimeOffset.UtcNow, Snapshot());
}

/// <summary>Immutable snapshot of one resource bucket.</summary>
public sealed record OwnedResourceCount(string Kind, int Count);

/// <summary>Ownership-snapshot evidence record emitted at checkpoints.</summary>
public sealed record OwnedResourceCountsSnapshot(
    DateTimeOffset CapturedAtUtc,
    IReadOnlyList<OwnedResourceCount> Counts)
{
    public IEnumerable<OwnedResourceCount> NonzeroCounts =>
        Counts.Where(count => count.Count != 0);
}

/// <summary>Raised when an owned-resource bucket is nonzero at a disposal boundary.</summary>
public sealed class OwnedResourceRetentionException : Exception
{
    public OwnedResourceRetentionException(string context, IReadOnlyList<OwnedResourceCount> nonzero)
        : base(BuildMessage(context, nonzero))
    {
        NonzeroCounts = nonzero;
    }

    public IReadOnlyList<OwnedResourceCount> NonzeroCounts { get; }

    private static string BuildMessage(string context, IReadOnlyList<OwnedResourceCount> nonzero)
    {
        StringBuilder builder = new();
        builder.Append(context);
        builder.Append(" left owned TerminalProof resources outstanding: ");
        builder.Append(string.Join(", ", nonzero.Select(count => $"{count.Kind}={count.Count}")));
        builder.Append('.');
        return builder.ToString();
    }
}
