using Square.Persistence.Sqlite;
using Square.TestKit;

return TestRunner.Run(
    ("idempotent registration preserves values", IdempotentRegistrationPreservesValues),
    ("conflicting registration returns state conflict", ConflictingRegistrationReturnsStateConflict),
    ("holder acquires lock successfully", HolderAcquiresLockSuccessfully),
    ("second holder locked when first owns", SecondHolderLockedWhenFirstOwns),
    ("holder cannot release another holder lock", HolderCannotReleaseAnotherHolderLock),
    ("holder releases own lock successfully", HolderReleasesOwnLockSuccessfully));

static string TempDb()
{
    string dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    Directory.CreateDirectory(dir);
    return Path.Combine(dir, "state.db");
}

static void IdempotentRegistrationPreservesValues()
{
    string db = TempDb();
    string timestamp = "2025-01-01T00:00:00Z";
    ProjectRegistration first = StateStore.RegisterProject(db, "C:/projects/test", "Test Project", "C:/profiles/default", timestamp);
    ProjectRegistration second = StateStore.RegisterProject(db, "C:/projects/test", "Test Project", "C:/profiles/default", timestamp);
    AssertEx.Equal(first, second);
    AssertEx.Equal(first.AddedAtUtc, second.AddedAtUtc);
}

static void ConflictingRegistrationReturnsStateConflict()
{
    string db = TempDb();
    string timestamp = "2025-01-01T00:00:00Z";
    StateStore.RegisterProject(db, "C:/projects/test", "Test Project", "C:/profiles/default", timestamp);
    var error = AssertEx.Throws<StateConflictException>(() =>
        StateStore.RegisterProject(db, "C:/projects/test", "Different Name", "C:/profiles/default", timestamp));
    AssertEx.Equal(4, error.ExitCode);
}

static void HolderAcquiresLockSuccessfully()
{
    string db = TempDb();
    string timestamp = "2025-01-01T00:00:00Z";
    StateStore.RegisterProject(db, "C:/projects/test", "Test Project", "C:/profiles/default", timestamp);
    bool acquired = StateStore.AcquireLock(db, "C:/projects/test", "holder-a", "abc123", timestamp);
    AssertEx.True(acquired, "First holder must acquire the lock.");
    StateStore.ReleaseLock(db, "C:/projects/test", "holder-a");
}

static void SecondHolderLockedWhenFirstOwns()
{
    string db = TempDb();
    string timestamp = "2025-01-01T00:00:00Z";
    StateStore.RegisterProject(db, "C:/projects/test", "Test Project", "C:/profiles/default", timestamp);
    StateStore.AcquireLock(db, "C:/projects/test", "holder-a", "abc123", timestamp);
    try
    {
        var error = AssertEx.Throws<StateConflictException>(() =>
            StateStore.AcquireLock(db, "C:/projects/test", "holder-b", "def456", timestamp));
        AssertEx.Equal(4, error.ExitCode);
    }
    finally
    {
        StateStore.ReleaseLock(db, "C:/projects/test", "holder-a");
    }
}

static void HolderCannotReleaseAnotherHolderLock()
{
    string db = TempDb();
    string timestamp = "2025-01-01T00:00:00Z";
    StateStore.RegisterProject(db, "C:/projects/test", "Test Project", "C:/profiles/default", timestamp);
    StateStore.AcquireLock(db, "C:/projects/test", "holder-a", "abc123", timestamp);
    try
    {
        bool released = StateStore.ReleaseLock(db, "C:/projects/test", "holder-b");
        AssertEx.False(released, "A non-owner must not release the lock.");
    }
    finally
    {
        StateStore.ReleaseLock(db, "C:/projects/test", "holder-a");
    }
}

static void HolderReleasesOwnLockSuccessfully()
{
    string db = TempDb();
    string timestamp = "2025-01-01T00:00:00Z";
    StateStore.RegisterProject(db, "C:/projects/test", "Test Project", "C:/profiles/default", timestamp);
    StateStore.AcquireLock(db, "C:/projects/test", "holder-a", "abc123", timestamp);
    bool released = StateStore.ReleaseLock(db, "C:/projects/test", "holder-a");
    AssertEx.True(released, "The owning holder must release the lock.");
}
