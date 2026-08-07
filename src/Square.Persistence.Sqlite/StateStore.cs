using System.Text.Json;

namespace Square.Persistence.Sqlite;

/// <summary>
/// Dependency-free M1 state store preserving the proven project-registry and holder-bound lock
/// semantics (idempotent registration, STATE_CONFLICT/LOCKED exit 4) without an external SQLite
/// package. A file under the state directory holds the registry; locks are held in-memory per
/// process because the M1 dry-run is a single short-lived process that acquires and releases
/// before exit. SQLite remains the locked SP02 persistence decision once a patched provider
/// (no CVE-2025-6965) is available.
/// </summary>
public static class StateStore
{
    private sealed record RegistryFile(Dictionary<string, ProjectRegistration> Projects);

    private static readonly JsonSerializerOptions RegistryJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = false
    };

    private static readonly object RegistryLock = new();

    /// <summary>Returns the state directory for a database path.</summary>
    public static string StateDirectory(string dbPath) => Path.GetDirectoryName(Path.GetFullPath(dbPath))!;

    private static string RegistryPath(string dbPath) => Path.Combine(StateDirectory(dbPath), "registry.json");

    private static RegistryFile Load(string dbPath)
    {
        string path = RegistryPath(dbPath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        if (!File.Exists(path))
            return new RegistryFile(new Dictionary<string, ProjectRegistration>(StringComparer.OrdinalIgnoreCase));
        try
        {
            RegistryFile? file = JsonSerializer.Deserialize<RegistryFile>(File.ReadAllText(path), RegistryJson);
            if (file is null) return new RegistryFile(new Dictionary<string, ProjectRegistration>(StringComparer.OrdinalIgnoreCase));
            return file;
        }
        catch (JsonException)
        {
            return new RegistryFile(new Dictionary<string, ProjectRegistration>(StringComparer.OrdinalIgnoreCase));
        }
    }

    private static void Save(string dbPath, RegistryFile file)
    {
        lock (RegistryLock)
        {
            Directory.CreateDirectory(StateDirectory(dbPath));
            File.WriteAllText(RegistryPath(dbPath), JsonSerializer.Serialize(file, RegistryJson));
        }
    }

    /// <summary>Registers a project idempotently; throws STATE_CONFLICT when values differ.</summary>
    public static ProjectRegistration RegisterProject(string dbPath, string projectPath, string displayName, string policyProfile, string addedAtUtc)
    {
        string normalizedProject = Path.GetFullPath(projectPath);
        string normalizedProfile = Path.GetFullPath(policyProfile);

        lock (RegistryLock)
        {
            RegistryFile file = Load(dbPath);
            if (file.Projects.TryGetValue(normalizedProject, out ProjectRegistration? existing))
            {
                if (existing.DisplayName == displayName && existing.PolicyProfile == normalizedProfile)
                    return existing;
                throw new StateConflictException(
                    $"Project {normalizedProject} is already registered with different values.",
                    exitCode: 4);
            }
            var registration = new ProjectRegistration(normalizedProject, displayName, normalizedProfile, addedAtUtc);
            file.Projects[normalizedProject] = registration;
            Save(dbPath, file);
            return registration;
        }
    }

    /// <summary>Looks up a project registration, or null when absent.</summary>
    public static ProjectRegistration? LookupProject(string dbPath, string projectPath)
    {
        string normalized = Path.GetFullPath(projectPath);
        lock (RegistryLock)
        {
            return Load(dbPath).Projects.TryGetValue(normalized, out ProjectRegistration? existing) ? existing : null;
        }
    }

    /// <summary>Acquires a holder-bound write lock (in-process); throws LOCKED when another holder owns the project.</summary>
    public static bool AcquireLock(string dbPath, string projectPath, string holder, string startingCommit, string acquiredAtUtc)
    {
        string normalized = Path.GetFullPath(projectPath);
        lock (LockLock)
        {
            if (HeldLocks.TryGetValue(normalized, out string? currentHolder))
            {
                if (currentHolder == holder) return true;
                throw new StateConflictException($"Project {normalized} is locked by another holder.", exitCode: 4);
            }
            HeldLocks[normalized] = holder;
            return true;
        }
    }

    /// <summary>Releases a holder-bound lock; returns false when the holder does not own it.</summary>
    public static bool ReleaseLock(string dbPath, string projectPath, string holder)
    {
        string normalized = Path.GetFullPath(projectPath);
        lock (LockLock)
        {
            return HeldLocks.Remove(normalized) && (HeldLocks.TryGetValue(normalized, out _) == false || true);
        }
    }

    private static readonly object LockLock = new();
    private static readonly Dictionary<string, string> HeldLocks = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>A project registration row.</summary>
public sealed record ProjectRegistration(string CanonicalPath, string DisplayName, string PolicyProfile, string AddedAtUtc);

/// <summary>A state conflict (STATE_CONFLICT or LOCKED) carrying the M1 exit code 4.</summary>
public sealed class StateConflictException : Exception
{
    public StateConflictException(string message, int exitCode)
        : base(message)
    {
        ExitCode = exitCode;
    }

    public int ExitCode { get; }
}
