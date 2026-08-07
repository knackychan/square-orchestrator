using System.Text;
using Square.Application.Primitives;
using Square.Domain.Primitives;
using Square.Domain.Terminals;
using Square.TestKit;

return TestRunner.Run(
    ("strong IDs normalize and remain typed", StrongIdsNormalize),
    ("invalid IDs fail closed", InvalidIdsFail),
    ("UTC format is canonical", UtcIsCanonical),
    ("content hashes are canonical", HashesAreCanonical),
    ("schema versions sort", VersionsSort),
    ("ID generator emits canonical values", GeneratorEmitsCanonicalValues),
    ("quiet is not a stall", QuietIsNotAStall),
    ("duplicate events are idempotent", DuplicateEventsAreIdempotent),
    ("final states are immutable", FinalStatesAreImmutable),
    ("illegal transitions are typed", IllegalTransitionsAreTyped));

static void StrongIdsNormalize()
{
    ProjectId project = ProjectId.Parse("PRJ_01jz9h6y8n4t2c3v5b7m9q1wxe");
    AssertEx.Equal("prj_01JZ9H6Y8N4T2C3V5B7M9Q1WXE", project.ToString());
    RequestId request = RequestId.Parse("req_01JZ9H6Y8N4T2C3V5B7M9Q1WXE");
    AssertEx.False(project.ToString() == request.ToString(), "Different identity types must retain distinct prefixes.");
}
static void InvalidIdsFail()
{
    AssertEx.False(TaskId.TryParse("tsk_contains-I", out _), "Non-Crockford payload must fail.");
    AssertEx.Throws<FormatException>(() => TaskId.Parse("tsk_short"));
}
static void UtcIsCanonical()
{
    UtcInstant instant = new(new DateTimeOffset(2026, 8, 7, 8, 30, 0, TimeSpan.FromHours(8)));
    AssertEx.Equal("2026-08-07T00:30:00.0000000Z", instant.ToString());
    AssertEx.Equal(instant, UtcInstant.Parse(instant.ToString()));
}
static void HashesAreCanonical()
{
    ContentHash hash = ContentHash.Compute(Encoding.UTF8.GetBytes("square"));
    AssertEx.Equal("sha256:4ba3e8e3765f2970eb37fae535353dd623d40a0507848c3c1dd240a5a7eb995e", hash.ToString());
    AssertEx.Equal(hash, ContentHash.Parse(hash.ToString()));
    AssertEx.Throws<FormatException>(() => ContentHash.Parse(hash.ToString().ToUpperInvariant()));
}
static void VersionsSort()
{
    AssertEx.True(new SchemaVersion(2, 0).CompareTo(new SchemaVersion(1, 99)) > 0, "Major version must dominate ordering.");
    AssertEx.Equal(new SchemaVersion(1, 0), SchemaVersion.Parse("1.0"));
}
static void GeneratorEmitsCanonicalValues()
{
    FrozenClock clock = new(new UtcInstant(new DateTimeOffset(2026, 8, 7, 0, 0, 0, TimeSpan.Zero)));
    CryptographicIdGenerator generator = new(clock);
    TaskId first = generator.New<TaskId>(); TaskId second = generator.New<TaskId>();
    AssertEx.True(TaskId.TryParse(first.ToString(), out _), "Generated task ID must parse.");
    AssertEx.True(first.ToString().StartsWith("tsk_", StringComparison.Ordinal), "Generated ID must carry its type prefix.");
    AssertEx.False(first == second, "Random components should make sequential IDs distinct.");
}
static void QuietIsNotAStall()
{
    TerminalSnapshot current = CreateRunningTerminal();
    Result<TerminalSnapshot> quiet = TerminalLifecycleReducer.Apply(current, new TerminalQuietObserved(Event(4), AddSeconds(current.LastTransitionAt, 1)));
    AssertEx.True(quiet.IsSuccess, "Quiet transition should succeed.");
    AssertEx.Equal(TerminalLifecycleState.QuietActive, quiet.Value.State);
    AssertEx.False(quiet.Value.State == TerminalLifecycleState.SuspectedStall, "Quiet activity must not imply a stall.");
}
static void DuplicateEventsAreIdempotent()
{
    TerminalSnapshot current = CreateRunningTerminal();
    TerminalQuietObserved lifecycleEvent = new(Event(4), AddSeconds(current.LastTransitionAt, 1));
    TerminalSnapshot once = TerminalLifecycleReducer.Apply(current, lifecycleEvent).Value;
    TerminalSnapshot twice = TerminalLifecycleReducer.Apply(once, lifecycleEvent).Value;
    AssertEx.True(ReferenceEquals(once, twice), "A duplicate event should return the original immutable snapshot.");
}
static void FinalStatesAreImmutable()
{
    TerminalSnapshot current = CreateRunningTerminal();
    TerminalSnapshot completing = TerminalLifecycleReducer.Apply(current, new TerminalCompletionObserved(Event(4), AddSeconds(current.LastTransitionAt, 1))).Value;
    TerminalSnapshot succeeded = TerminalLifecycleReducer.Apply(completing, new TerminalSucceeded(Event(5), AddSeconds(completing.LastTransitionAt, 1))).Value;
    Result<TerminalSnapshot> after = TerminalLifecycleReducer.Apply(succeeded, new TerminalActivityObserved(Event(6), AddSeconds(succeeded.LastTransitionAt, 1)));
    AssertEx.True(after.IsFailure, "A final state must reject later transitions.");
    AssertEx.Equal("terminal.final_state", after.Problem!.Code);
}
static void IllegalTransitionsAreTyped()
{
    UtcInstant now = new(new DateTimeOffset(2026, 8, 7, 0, 0, 0, TimeSpan.Zero));
    TerminalSnapshot created = TerminalSnapshot.Create(TerminalId.Parse("trm_01JZ9H6Y8N4T2C3V5B7M9Q1WXE"), now);
    Result<TerminalSnapshot> result = TerminalLifecycleReducer.Apply(created, new TerminalStartupConfirmed(Event(1), now));
    AssertEx.True(result.IsFailure, "Created -> Running without launch must fail.");
    AssertEx.Equal("terminal.invalid_transition", result.Problem!.Code);
}
static TerminalSnapshot CreateRunningTerminal()
{
    UtcInstant now = new(new DateTimeOffset(2026, 8, 7, 0, 0, 0, TimeSpan.Zero));
    TerminalSnapshot current = TerminalSnapshot.Create(TerminalId.Parse("trm_01JZ9H6Y8N4T2C3V5B7M9Q1WXE"), now);
    current = TerminalLifecycleReducer.Apply(current, new TerminalLaunchRequested(Event(1), now)).Value;
    current = TerminalLifecycleReducer.Apply(current, new TerminalProcessStarted(Event(2), AddSeconds(now, 1))).Value;
    return TerminalLifecycleReducer.Apply(current, new TerminalStartupConfirmed(Event(3), AddSeconds(now, 2))).Value;
}
static EventId Event(int value) => EventId.Parse($"evt_01JZ9H6Y8N4T2C3V5B7M9QX{value:000}");
static UtcInstant AddSeconds(UtcInstant value, int seconds) => new(value.Value.AddSeconds(seconds));
file sealed class FrozenClock(UtcInstant value) : IClock { public UtcInstant UtcNow { get; } = value; }
