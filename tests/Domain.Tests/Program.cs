using System.Text;
using System.Text.Json;
using Square.Application.Authority;
using Square.Application.Primitives;
using Square.Domain.Authority;
using Square.Domain.Practices;
using Square.Domain.Primitives;
using Square.Domain.Projects;
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
    ("illegal transitions are typed", IllegalTransitionsAreTyped),
    ("missing block returns authority missing", MissingBlockReturnsAuthorityMissing),
    ("duplicate task id returns validation failed", DuplicateTaskIdReturnsValidationFailed),
    ("wrong head returns authority drift", WrongHeadReturnsAuthorityDrift),
    ("overlapping paths return validation failed", OverlappingPathsReturnValidationFailed),
    ("alias route returns route invalid", AliasRouteReturnsRouteInvalid),
    ("fallback enabled returns route invalid", FallbackEnabledReturnsRouteInvalid),
    ("compile manifest is deterministic", CompileManifestIsDeterministic),
    ("manifest is canonical json", ManifestIsCanonicalJson),
    ("rejects inactive requested task", RejectsInactiveRequestedTask),
    ("rejects unknown field and missing ceiling", RejectsUnknownFieldAndMissingCeiling),
    ("rejects windows and empty segment paths", RejectsWindowsAndEmptySegmentPaths),
    ("rejects incorrect worktree disclosure and missing context pair", RejectsIncorrectWorktreeDisclosureAndMissingContextPair),
    ("route validation rejects empty client", RouteValidationRejectsEmptyClient),
    ("route validation rejects empty model", RouteValidationRejectsEmptyModel),
    ("path validation rejects absolute", PathValidationRejectsAbsolute),
    ("path validation rejects parent segment", PathValidationRejectsParentSegment),
    ("path validation rejects empty", PathValidationRejectsEmpty),
    ("canonical practice record accepted", CanonicalPracticeRecordAccepted),
    ("missing schema rejected", MissingSchemaRejected),
    ("substituted field rejected", SubstitutedFieldRejected),
    ("missing provenance rejected", MissingProvenanceRejected),
    ("confidence above one rejected", ConfidenceAboveOneRejected),
    ("adopted without authority rejected", AdoptedWithoutAuthorityRejected),
    ("rejected without authority rejected", RejectedWithoutAuthorityRejected),
    ("deprecated without authority rejected", DeprecatedWithoutAuthorityRejected),
    ("dependency order is acyclic", DependencyOrderIsAcyclic),
    ("cycle returns invalid input", CycleReturnsInvalidInput),
    ("duplicate owner returns invalid input", DuplicateOwnerReturnsInvalidInput),
    ("preview names required authority files", PreviewNamesRequiredAuthorityFiles),
    ("unknown blueprint field rejected", UnknownBlueprintFieldRejected),
    ("missing blueprint field rejected", MissingBlueprintFieldRejected));

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

// --- M1 authority manifest tests (ported from tests/test_authority.py) ---

static void MissingBlockReturnsAuthorityMissing()
{
    string tmp = NewTempDir();
    AuthorityFixture.InitGitRepo(tmp);
    string docs = PacketDocsDir(tmp);
    Directory.CreateDirectory(docs);
    AuthorityFixture.WriteStatus(tmp, "docs/superpowers/plans/2026-08-05-m1-dry-run-foundation", "T-TEST-01");
    AuthorityFixture.WritePacket(docs);
    File.WriteAllText(Path.Combine(docs, "BUILD-TASKS.md"), "# No blocks here\n");

    var error = AssertEx.Throws<AuthorityValidationException>(() => ManifestCompiler.Compile(tmp, "T-TEST-01"));
    AssertEx.Equal("AUTHORITY_MISSING", error.Code);
}

static void DuplicateTaskIdReturnsValidationFailed()
{
    string tmp = NewTempDir();
    string head = AuthorityFixture.InitGitRepo(tmp);
    string docs = PacketDocsDir(tmp);
    Directory.CreateDirectory(docs);
    AuthorityFixture.WriteStatus(tmp, "docs/superpowers/plans/2026-08-05-m1-dry-run-foundation", "T-TEST-01");
    AuthorityFixture.WritePacket(docs);
    string block = AuthorityFixture.TomlTaskBlock(head);
    AuthorityFixture.WriteBuildTasks(docs, block, block);

    var error = AssertEx.Throws<AuthorityValidationException>(() => ManifestCompiler.Compile(tmp, "T-TEST-01"));
    AssertEx.Equal("VALIDATION_FAILED", error.Code);
}

static void WrongHeadReturnsAuthorityDrift()
{
    string tmp = NewTempDir();
    AuthorityFixture.InitGitRepo(tmp);
    string docs = PacketDocsDir(tmp);
    Directory.CreateDirectory(docs);
    AuthorityFixture.WriteStatus(tmp, "docs/superpowers/plans/2026-08-05-m1-dry-run-foundation", "T-TEST-01");
    AuthorityFixture.WritePacket(docs);
    string block = AuthorityFixture.TomlTaskBlock(new string('0', 40));
    AuthorityFixture.WriteBuildTasks(docs, block);

    var error = AssertEx.Throws<AuthorityValidationException>(() => ManifestCompiler.Compile(tmp, "T-TEST-01"));
    AssertEx.Equal("AUTHORITY_DRIFT", error.Code);
}

static void OverlappingPathsReturnValidationFailed()
{
    AssertEx.Throws<AuthorityValidationException>(() =>
        PathValidation.ValidatePathClaims(new[] { "sqorch/" }, new[] { "sqorch/application.py" }));
}

static void AliasRouteReturnsRouteInvalid()
{
    AssertEx.Throws<AuthorityValidationException>(() => RouteValidation.ValidateRoute("cmdc", "latest", false));
}

static void FallbackEnabledReturnsRouteInvalid()
{
    AssertEx.Throws<AuthorityValidationException>(() => RouteValidation.ValidateRoute("cmdc", "deepseek/deepseek-v4-pro", true));
}

static void CompileManifestIsDeterministic()
{
    string tmp = NewTempDir();
    AuthorityFixture.MakeAuthorityFixture(tmp);
    string first = ManifestCompiler.Compile(tmp, "T-TEST-01");
    string second = ManifestCompiler.Compile(tmp, "T-TEST-01");
    AssertEx.Equal(first, second);
}

static void ManifestIsCanonicalJson()
{
    string tmp = NewTempDir();
    AuthorityFixture.MakeAuthorityFixture(tmp);
    string manifest = ManifestCompiler.Compile(tmp, "T-TEST-01");
    using JsonDocument document = JsonDocument.Parse(manifest);
    JsonElement root = document.RootElement;
    AssertEx.Equal(1, root.GetProperty("schema").GetInt32());
    AssertEx.Equal("T-TEST-01", root.GetProperty("task").GetProperty("id").GetString()!);
    AssertEx.True(root.GetProperty("hashes").TryGetProperty("STATUS.md", out _), "hashes must include STATUS.md");
    AssertEx.False(manifest.Contains('\n'), "canonical JSON must be single-line");
    AssertEx.False(manifest.Contains("  "), "canonical JSON must use compact separators");
}

static void RejectsInactiveRequestedTask()
{
    string tmp = NewTempDir();
    AuthorityFixture.MakeAuthorityFixture(tmp, "T-OTHER");

    var error = AssertEx.Throws<AuthorityValidationException>(() => ManifestCompiler.Compile(tmp, "T-TEST-01"));
    AssertEx.Equal("AUTHORITY_DRIFT", error.Code);
}

static void RejectsUnknownFieldAndMissingCeiling()
{
    string tmp = NewTempDir();
    string head = AuthorityFixture.InitGitRepo(tmp);
    string docs = PacketDocsDir(tmp);
    Directory.CreateDirectory(docs);
    AuthorityFixture.WriteStatus(tmp, "docs/superpowers/plans/2026-08-05-m1-dry-run-foundation", "T-TEST-01");
    AuthorityFixture.WritePacket(docs);
    AuthorityFixture.WriteBuildTasks(docs, AuthorityFixture.TomlTaskBlock(head) + "\nunexpected = \"no\"");
    var error = AssertEx.Throws<AuthorityValidationException>(() => ManifestCompiler.Compile(tmp, "T-TEST-01"));
    AssertEx.Equal("VALIDATION_FAILED", error.Code);
}

static void RejectsWindowsAndEmptySegmentPaths()
{
    foreach (string path in new[] { "a//b", "C:/absolute", "a\\b" })
    {
        var error = AssertEx.Throws<AuthorityValidationException>(() => PathValidation.ValidateRelativePosixPath(path));
        AssertEx.Equal("VALIDATION_FAILED", error.Code);
    }
}

static void RejectsIncorrectWorktreeDisclosureAndMissingContextPair()
{
    string tmp = NewTempDir();
    AuthorityFixture.MakeAuthorityFixture(tmp);
    AuthorityFixture.WriteStatus(tmp, "docs/superpowers/plans/2026-08-05-m1-dry-run-foundation", "T-TEST-01", worktreeState: "clean");
    var error = AssertEx.Throws<AuthorityValidationException>(() => ManifestCompiler.Compile(tmp, "T-TEST-01"));
    AssertEx.Equal("AUTHORITY_DRIFT", error.Code);

    string tmp2 = NewTempDir();
    AuthorityFixture.MakeAuthorityFixture(tmp2);
    File.Delete(Path.Combine(tmp2, "CLAUDE.md"));
    var error2 = AssertEx.Throws<AuthorityValidationException>(() => ManifestCompiler.Compile(tmp2, "T-TEST-01"));
    AssertEx.Equal("AUTHORITY_DRIFT", error2.Code);
}

static void RouteValidationRejectsEmptyClient()
{
    AssertEx.Throws<AuthorityValidationException>(() => RouteValidation.ValidateRoute("", "deepseek/deepseek-v4-pro", false));
}

static void RouteValidationRejectsEmptyModel()
{
    AssertEx.Throws<AuthorityValidationException>(() => RouteValidation.ValidateRoute("cmdc", "", false));
}

static void PathValidationRejectsAbsolute()
{
    AssertEx.Throws<AuthorityValidationException>(() => PathValidation.ValidateRelativePosixPath("/absolute/path"));
}

static void PathValidationRejectsParentSegment()
{
    AssertEx.Throws<AuthorityValidationException>(() => PathValidation.ValidateRelativePosixPath("../escape"));
}

static void PathValidationRejectsEmpty()
{
    AssertEx.Throws<AuthorityValidationException>(() => PathValidation.ValidateRelativePosixPath(""));
}

// --- M1 practice-record tests (ported from tests/test_practices.py) ---

static Dictionary<string, object?> PracticeCandidate(Dictionary<string, object?>? overrides = null)
{
    var record = new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        ["schema"] = "practice/v1",
        ["id"] = "P-001",
        ["category"] = "testing",
        ["statement"] = "Always write tests first",
        ["proposed_scope"] = "project",
        ["source_type"] = "observation",
        ["provenance_reference"] = "T-M1-04 review",
        ["observed_context"] = "Test project M1 dry run",
        ["trade_offs"] = new List<object?> { "slower initial velocity" },
        ["counterexamples"] = new List<object?>(),
        ["confidence"] = 0.9,
        ["review_date"] = "2026-08-06",
        ["state"] = "CANDIDATE",
        ["approving_authority"] = null,
        ["affected_profiles"] = new List<object?>()
    };
    if (overrides is not null)
        foreach (KeyValuePair<string, object?> pair in overrides)
            record[pair.Key] = pair.Value;
    return record;
}

static void CanonicalPracticeRecordAccepted()
{
    PracticeRecord result = PracticeRecordValidator.Validate(PracticeCandidate());
    AssertEx.Equal("CANDIDATE", result.State);
}

static void MissingSchemaRejected()
{
    var record = PracticeCandidate();
    record.Remove("schema");
    var error = AssertEx.Throws<PracticeValidationException>(() => PracticeRecordValidator.Validate(record));
    AssertEx.Equal("INVALID_INPUT", error.Code);
}

static void SubstitutedFieldRejected()
{
    var record = PracticeCandidate(new Dictionary<string, object?> { ["scope"] = "project" });
    var error = AssertEx.Throws<PracticeValidationException>(() => PracticeRecordValidator.Validate(record));
    AssertEx.Equal("INVALID_INPUT", error.Code);
}

static void MissingProvenanceRejected()
{
    var record = PracticeCandidate(new Dictionary<string, object?> { ["provenance_reference"] = null! });
    var error = AssertEx.Throws<PracticeValidationException>(() => PracticeRecordValidator.Validate(record));
    AssertEx.Equal("INVALID_INPUT", error.Code);
}

static void ConfidenceAboveOneRejected()
{
    var record = PracticeCandidate(new Dictionary<string, object?> { ["confidence"] = 1.5 });
    var error = AssertEx.Throws<PracticeValidationException>(() => PracticeRecordValidator.Validate(record));
    AssertEx.Equal("INVALID_INPUT", error.Code);
}

static void AdoptedWithoutAuthorityRejected()
{
    var record = PracticeCandidate(new Dictionary<string, object?> { ["state"] = "ADOPTED", ["approving_authority"] = null });
    var error = AssertEx.Throws<PracticeValidationException>(() => PracticeRecordValidator.Validate(record));
    AssertEx.Equal("INVALID_INPUT", error.Code);
}

static void RejectedWithoutAuthorityRejected()
{
    var record = PracticeCandidate(new Dictionary<string, object?> { ["state"] = "REJECTED", ["approving_authority"] = null });
    var error = AssertEx.Throws<PracticeValidationException>(() => PracticeRecordValidator.Validate(record));
    AssertEx.Equal("INVALID_INPUT", error.Code);
}

static void DeprecatedWithoutAuthorityRejected()
{
    var record = PracticeCandidate(new Dictionary<string, object?> { ["state"] = "DEPRECATED", ["approving_authority"] = null });
    var error = AssertEx.Throws<PracticeValidationException>(() => PracticeRecordValidator.Validate(record));
    AssertEx.Equal("INVALID_INPUT", error.Code);
}

// --- M1 project-foundry tests (ported from tests/test_projects.py) ---

static Dictionary<string, object?> CanonicalBlueprint()
{
    return new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        ["product_boundary"] = "A CLI that coordinates bounded agent work.",
        ["owner"] = "Owner",
        ["language"] = "Python",
        ["deployment_context"] = "Local terminal",
        ["external_effects"] = "none",
        ["data_sensitivity"] = "low",
        ["expected_scale"] = "single repository",
        ["acceptance_authority"] = "primary session",
        ["responsibilities"] = new List<object?>
        {
            new Dictionary<string, object?> { ["id"] = "cli", ["description"] = "Argument parsing and rendering", ["owned_path"] = "sqorch/cli.py" },
            new Dictionary<string, object?> { ["id"] = "application", ["description"] = "Use-case coordination", ["owned_path"] = "sqorch/application.py" }
        },
        ["dependencies"] = new List<object?>
        {
            new Dictionary<string, object?> { ["from"] = "cli", ["to"] = "application" }
        }
    };
}

static void DependencyOrderIsAcyclic()
{
    BlueprintPreview result = BlueprintValidator.Preview(CanonicalBlueprint());
    AssertEx.True(result.DependencyOrder.SequenceEqual(new[] { "application", "cli" }), "dependency order must be acyclic and application-first");
}

static void CycleReturnsInvalidInput()
{
    var blueprint = CanonicalBlueprint();
    blueprint["dependencies"] = new List<object?>
    {
        new Dictionary<string, object?> { ["from"] = "cli", ["to"] = "application" },
        new Dictionary<string, object?> { ["from"] = "application", ["to"] = "cli" }
    };
    var error = AssertEx.Throws<ProjectValidationException>(() => BlueprintValidator.Preview(blueprint));
    AssertEx.Equal("INVALID_INPUT", error.Code);
}

static void DuplicateOwnerReturnsInvalidInput()
{
    var blueprint = CanonicalBlueprint();
    blueprint["responsibilities"] = new List<object?>
    {
        new Dictionary<string, object?> { ["id"] = "cli", ["description"] = "Argument parsing", ["owned_path"] = "sqorch/cli.py" },
        new Dictionary<string, object?> { ["id"] = "other", ["description"] = "Another responsibility", ["owned_path"] = "sqorch/cli.py" }
    };
    var error = AssertEx.Throws<ProjectValidationException>(() => BlueprintValidator.Preview(blueprint));
    AssertEx.Equal("INVALID_INPUT", error.Code);
}

static void PreviewNamesRequiredAuthorityFiles()
{
    BlueprintPreview result = BlueprintValidator.Preview(CanonicalBlueprint());
    AssertEx.True(
        result.AuthorityFiles.SequenceEqual(new[] { "AGENTS.md", "CLAUDE.md", "SPEC.md", "STATUS.md", "HANDOVER.md" }),
        "authority files must be the canonical root set");
    AssertEx.True(result.ContextPairs.Contains("docs/superpowers/AGENTS.md"), "context pair must include superpowers AGENTS.md");
    AssertEx.True(result.ContextPairs.Contains("docs/superpowers/CLAUDE.md"), "context pair must include superpowers CLAUDE.md");
}

static void UnknownBlueprintFieldRejected()
{
    var blueprint = CanonicalBlueprint();
    blueprint["surprise"] = "field";
    var error = AssertEx.Throws<ProjectValidationException>(() => BlueprintValidator.Preview(blueprint));
    AssertEx.Equal("INVALID_INPUT", error.Code);
}

static void MissingBlueprintFieldRejected()
{
    var blueprint = CanonicalBlueprint();
    blueprint.Remove("owner");
    var error = AssertEx.Throws<ProjectValidationException>(() => BlueprintValidator.Preview(blueprint));
    AssertEx.Equal("INVALID_INPUT", error.Code);
}

static string NewTempDir()
{
    string dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    Directory.CreateDirectory(dir);
    return dir;
}

static string PacketDocsDir(string root) =>
    Path.Combine(root, "docs", "superpowers", "plans", "2026-08-05-m1-dry-run-foundation");

file sealed class FrozenClock(UtcInstant value) : IClock { public UtcInstant UtcNow { get; } = value; }
