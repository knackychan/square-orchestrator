using System.Text.Json;
using Square.Application.UseCases;
using Square.Domain.Projects;

// Square Orchestrator M1 CLI: the proven single-process command surface.
// Exit codes follow the M1 contract: 0 success, 2 validation/input, 3 authority drift,
// 4 state conflict/locked. Canonical JSON envelopes match the Python M1 implementation.

return Run(args);

static int Run(string[] arguments)
{
    string[] positional = arguments.Where(value => !string.Equals(value, "--json", StringComparison.Ordinal)).ToArray();
    bool asJson = arguments.Contains("--json", StringComparer.Ordinal);
    string? stateDb = ReadOption(arguments, "--state-db");

    if (positional.Length == 0 || positional.Contains("--help") || positional.Contains("-h"))
    {
        WriteHelp(asJson);
        return 0;
    }

    try
    {
        switch (positional[0])
        {
            case "doctor":
                return Doctor(asJson, stateDb);
            case "validate":
                return ValidateCommand(positional.Skip(1).ToArray(), asJson);
            case "project":
                return ProjectCommand(positional.Skip(1).ToArray(), asJson, stateDb);
            case "practices":
                return PracticesCommand(positional.Skip(1).ToArray(), asJson);
            case "run":
                return RunCommand(positional.Skip(1).ToArray(), asJson, stateDb);
            default:
                throw new ApplicationError("INVALID_INPUT", $"Unknown command: {positional[0]}", exitCode: 2);
        }
    }
    catch (ApplicationError error)
    {
        WriteError(asJson, error.Code, error.Message);
        return error.ExitCode;
    }
}

static int ValidateCommand(string[] arguments, bool asJson)
{
    string project = RequireOption(arguments, "--project");
    string task = RequireOption(arguments, "--task");
    string manifest = M1Handlers.Validate(project, task);
    WriteJson(asJson, new { ok = true, data = ParseJson(manifest) });
    return 0;
}

static int ProjectCommand(string[] arguments, bool asJson, string? stateDb)
{
    if (arguments.Length == 0 || !(arguments[0] is "new" or "adopt" or "add"))
        throw new ApplicationError("INVALID_INPUT", "project requires new, adopt, or add", exitCode: 2);

    switch (arguments[0])
    {
        case "new":
        {
            if (!arguments.Contains("--preview"))
                throw new ApplicationError("INVALID_INPUT", "project new requires --preview.", exitCode: 2);
            string input = RequireOption(arguments, "--input");
            BlueprintPreview preview = M1Handlers.PreviewProject(input);
            WriteJson(asJson, new { ok = true, data = new
            {
                dependency_order = preview.DependencyOrder,
                authority_files = preview.AuthorityFiles,
                context_pairs = preview.ContextPairs
            } });
            return 0;
        }
        case "add":
        {
            string path = arguments[1];
            string name = RequireOption(arguments, "--name");
            string profile = RequireOption(arguments, "--profile");
            string db = stateDb ?? DefaultStatePath();
            Square.Persistence.Sqlite.ProjectRegistration registration;
            try
            {
                registration = Square.Persistence.Sqlite.StateStore.RegisterProject(db, path, name, profile, UtcNow());
            }
            catch (Square.Persistence.Sqlite.StateConflictException error)
            {
                throw new ApplicationError("STATE_CONFLICT", error.Message, exitCode: 4);
            }
            WriteJson(asJson, new { ok = true, data = new
            {
                canonical_path = registration.CanonicalPath,
                display_name = registration.DisplayName,
                policy_profile = registration.PolicyProfile,
                added_at_utc = registration.AddedAtUtc
            } });
            return 0;
        }
        default: // adopt
        {
            if (!arguments.Contains("--audit-only"))
                throw new ApplicationError("INVALID_INPUT", "project adopt requires --audit-only.", exitCode: 2);
            string path = arguments[1];
            WriteJson(asJson, new { ok = true, data = M1Handlers.AuditProject(path) });
            return 0;
        }
    }
}

static int PracticesCommand(string[] arguments, bool asJson)
{
    if (arguments.Length < 2 || arguments[0] != "validate")
        throw new ApplicationError("INVALID_INPUT", "practices requires validate PATH", exitCode: 2);
    var record = M1Handlers.ValidatePractices(arguments[1]);
    WriteJson(asJson, new { ok = true, data = record });
    return 0;
}

static int RunCommand(string[] arguments, bool asJson, string? stateDb)
{
    if (!arguments.Contains("--dry-run"))
        throw new ApplicationError("INVALID_INPUT", "run requires --dry-run in M1.", exitCode: 2);
    string project = RequireOption(arguments, "--project");
    string task = RequireOption(arguments, "--task");
    string db = stateDb ?? DefaultStatePath();

    string manifest = M1Handlers.Validate(project, task);
    using var document = JsonDocument.Parse(manifest);
    var taskData = document.RootElement.GetProperty("task");
    string holder = Guid.NewGuid().ToString();

    try
    {
        var stored = Square.Persistence.Sqlite.StateStore.LookupProject(db, project);
        if (stored is null)
        {
            Square.Persistence.Sqlite.StateStore.RegisterProject(db, project, taskData.GetProperty("id").GetString()!, "default", UtcNow());
        }
        Square.Persistence.Sqlite.StateStore.AcquireLock(db, project, holder, taskData.GetProperty("starting_commit").GetString()!, UtcNow());
        WriteJson(asJson, new { ok = true, data = new
        {
            launch_performed = false,
            automatic_fallback = false,
            route = new
            {
                client = taskData.GetProperty("client").GetString(),
                model = taskData.GetProperty("model").GetString()
            },
            task_id = task,
            project_path = Path.GetFullPath(project)
        } });
        return 0;
    }
    catch (Square.Persistence.Sqlite.StateConflictException error)
    {
        throw new ApplicationError("LOCKED", error.Message, exitCode: 4);
    }
    finally
    {
        Square.Persistence.Sqlite.StateStore.ReleaseLock(db, project, holder);
    }
}

static string UtcNow() => DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");

static int Doctor(bool asJson, string? stateDb)
{
    string repository = Path.GetFullPath(Directory.GetCurrentDirectory());
    DoctorReport report = DoctorHandler.Run(repository, stateDb ?? DefaultStatePath());
    if (asJson)
    {
        WriteJson(true, new { ok = true, data = new { git = report.Git, python = report.Python, repository = report.Repository, state_db = report.StateDb } });
    }
    else
    {
        Console.Out.WriteLine($"Python: {report.Python}");
        Console.Out.WriteLine($"Git: {report.Git}");
        Console.Out.WriteLine($"Repository: {report.Repository}");
        Console.Out.WriteLine($"State DB: {report.StateDb}");
    }
    return 0;
}

static string DefaultStatePath()
{
    string? localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
    if (string.IsNullOrEmpty(localAppData))
        throw new ApplicationError("INVALID_INPUT", "LOCALAPPDATA is required when --state-db is not supplied.", exitCode: 2);
    return Path.Combine(localAppData, "SquareOrchestrator", "state.db");
}

static string RequireOption(string[] args, string option)
{
    int index = Array.IndexOf(args, option);
    if (index < 0 || index + 1 >= args.Length)
        throw new ApplicationError("INVALID_INPUT", $"Missing required option {option}", exitCode: 2);
    return args[index + 1];
}

static string? ReadOption(string[] args, string option)
{
    int index = Array.IndexOf(args, option);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

static void WriteJson(bool asJson, object value)
{
    if (!asJson) return;
    Console.Out.WriteLine(JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = false }));
}

static void WriteError(bool asJson, string code, string message)
{
    if (asJson)
    {
        WriteJson(true, new { ok = false, error = new { code, message, details = new { } } });
    }
    else
    {
        Console.Error.WriteLine($"{code}: {message}");
    }
}

static object? ParseJson(string json) =>
    JsonSerializer.Deserialize<JsonElement>(json);

static void WriteHelp(bool asJson)
{
    if (asJson)
    {
        WriteJson(true, new { ok = true, data = new { commands = new[] { "doctor", "validate", "project new|adopt|add", "practices validate", "run --dry-run" } } });
    }
    else
    {
        Console.Out.WriteLine("Square Orchestrator");
        Console.Out.WriteLine("Usage: square <command> [options]");
        Console.Out.WriteLine();
        Console.Out.WriteLine("Commands:");
        Console.Out.WriteLine("  doctor");
        Console.Out.WriteLine("  validate --project PATH --task ID");
        Console.Out.WriteLine("  project new --input PATH --preview");
        Console.Out.WriteLine("  project adopt PATH --audit-only");
        Console.Out.WriteLine("  project add PATH --name NAME --profile PATH");
        Console.Out.WriteLine("  practices validate PATH");
        Console.Out.WriteLine("  run --project PATH --task ID --dry-run");
    }
}
