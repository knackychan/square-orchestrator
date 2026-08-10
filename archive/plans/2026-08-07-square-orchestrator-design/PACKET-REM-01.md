# Remediation Packet — REM-01: Revert Dependency-Free State Store

## Authority state

Owner-activated 2026-08-07 under the Option A decision recorded in
`dependency-securityfork-resolution.md` §5 and root `STATUS.md`. This packet is an active
implementation packet for exactly one task. No other task may edit source or tests until this task
is reviewed and closed.

## Objective

Revert the dependency-free file-store persistence introduced in commits `ed39978` and `66ea781`,
re-scope the `.NET` M1 port as a non-persistent dry-run slice, and correct
`Directory.Packages.props` so it no longer frames the file store as an accepted architecture
decision. SQLite remains the locked persistence technology; its admission belongs to `SP02-T01`.

## Measurable outcome

- `src/Square.Persistence.Sqlite/StateStore.cs` is deleted; the module reverts to its pre-port
  marker-only state (`ModuleMarker.cs`).
- `tests/Persistence.Tests/Program.cs` is deleted; the test project reverts to its pre-port
  placeholder state (`SuiteMarker.cs` only).
- `src/Square.Cli/Program.cs` no longer references `Square.Persistence.Sqlite`. The `project add`
  and `run` command paths are removed. The CLI surface becomes: `doctor`, `validate`,
  `project new --preview`, `project adopt --audit-only`, `practices validate`.
- `src/Square.Cli/Square.Cli.csproj` no longer references `Square.Persistence.Sqlite`. The
  `Square.Application` reference is retained.
- `build/SquareOrchestrator.Core.slnx`, `build/test.ps1`, `build/verify-repository.mjs`, and
  `tests/test-suites.json` no longer reference `Persistence.Tests` or `Square.Persistence.Sqlite`
  in CLI dependencies.
- `Directory.Packages.props` review note is replaced with a blocker/evidence note recording the
  security finding and the pending `SP02-T01` proof, not an accepted architecture change.
- The full .NET build and Domain.Tests + Contract.Tests + Architecture.Tests pass.
- No dependencies are added. No SQLite, migrations, file store, JSON store, or other persistence
  mechanism is introduced.

## Canonical inputs

1. Root `AGENTS.md`, `CLAUDE.md`, `SPEC.md`, `STATUS.md`, and `HANDOVER.md`.
2. Root `CLIENT-EXECUTION.md` for route and terminal invariants.
3. `dependency-securityfork-resolution.md` (this directory) — owner decision, proof guide, §5.
4. `square-orchestrator-sliced-implementation-plan.md` (this directory) — §3 locked decisions,
  SP01/SP02 sequencing.
5. This packet.

Lower inputs cannot widen higher authority.

## Entry conditions for implementation

- `STATUS.md` activates this exact task.
- The current worktree and starting commit are recorded.
- The task's exact client/model route is adopted and reverified through a safe live preflight.
- The visible terminal surface, token count, and zero-call/zero-spend budgets are recorded.
- No other writer owns this repository.

## Allowed implementation paths

- `src/Square.Persistence.Sqlite/StateStore.cs` (delete)
- `src/Square.Cli/Program.cs` (modify — remove persistence wiring)
- `src/Square.Cli/Square.Cli.csproj` (modify — remove Persistence.Sqlite reference)
- `tests/Persistence.Tests/Program.cs` (delete)
- `tests/Persistence.Tests/Persistence.Tests.csproj` (modify — restore to pre-port placeholder)
- `build/SquareOrchestrator.Core.slnx` (modify — remove Persistence.Tests entry)
- `build/test.ps1` (modify — remove Persistence.Tests run)
- `build/verify-repository.mjs` (modify — remove Square.Persistence.Sqlite from CLI allowed deps)
- `tests/test-suites.json` (modify — remove Persistence.Tests command)
- `Directory.Packages.props` (modify — replace review note with blocker/evidence note)
- `docs/superpowers/plans/2026-08-07-square-orchestrator-design/STATE-REM-01.md` (evidence ledger)

`docs/**` remains an owner-authorized planning-output exception.

## Forbidden changes and actions

- Any file not named above.
- The accepted `.NET` port work in `Square.Domain`, `Square.Application`, `Square.TestKit`, and
  `Domain.Tests` — these are accepted under the re-scoped non-persistent contract and must not be
  touched.
- `Square.Application/Authority/ManifestCompiler.cs` — the 66ea781 newline/holder-bound fix is
  accepted; do not revert it.
- Adding dependencies, NuGet packages, SQLite, migrations, file stores, JSON stores, or any other
  persistence mechanism.
- Weakening an assertion to make a task pass.
- `pyproject.toml`, dependency files, installers, or persistent caches.
- Real target-project creation, Git commit automation, client launch, terminal launch, provider
  call, network call, or background process.
- Automatic fallback, aliases, route substitution, or speculative shared utilities.

## Re-scope detail

The `.NET` M1 port is re-scoped as a non-persistent dry-run slice. The following M1 features are
deferred entirely to `SP02-T01`:

- `project add PATH --name NAME --profile PATH` — project registration.
- `run --project PATH --task ID --dry-run` — integrated dry-run with registration and locks.
- Holder-bound write locks (`AcquireLock`, `ReleaseLock`).
- `STATE_CONFLICT` / `LOCKED` exit code 4 paths.

The CLI surface after this task: `doctor`, `validate --project PATH --task ID`,
`project new --input PATH --preview`, `project adopt PATH --audit-only`,
`practices validate PATH`.

`doctor` continues to report a `state_db` path string in its output. It does not create or write a
database. The `--state-db` option remains available for `doctor` only.

## Budgets

| Budget | Ceiling |
|---|---|
| Third-party dependencies | `0` |
| External/provider requests | `0` |
| Spend | `$0` |
| Destructive actions | `0` (file deletion inside allowed paths is revert, not destructive action) |
| Concurrent repository writers | `1` |
| Worker turn limit when activated | `100` per attempt |
| Worker rotation | `150000` reported input tokens, checked between tasks |

## Route policy

Activated route: `cmdc` / `deepseek/deepseek-v4-pro` (`silent-failure`). The remediation reverts a
security-driven architecture deviation — a plausible-looking defect could silently keep the file
store framed as accepted behavior. Automatic update and fallback remain disabled. The context-pair
paths below are allowed only to map the new `state.py` and `test_state.py` files.

## Validation

Run from the repository root:

```powershell
dotnet build build/SquareOrchestrator.Core.slnx --no-restore
dotnet run --project tests/Domain.Tests/Domain.Tests.csproj
dotnet run --project tests/Contract.Tests/Contract.Tests.csproj
dotnet run --project tests/Architecture.Tests/Architecture.Tests.csproj
git diff --check <starting-head>..HEAD
git status --short
```

Validation must also prove:

- `src/Square.Persistence.Sqlite/StateStore.cs` does not exist.
- `tests/Persistence.Tests/Program.cs` does not exist.
- No source file references `Square.Persistence.Sqlite.StateStore` or
  `Square.Persistence.Sqlite.StateConflictException` or `Square.Persistence.Sqlite.ProjectRegistration`.
- `Square.Cli` does not reference `Square.Persistence.Sqlite` in its `.csproj` or source.
- `build/verify-repository.mjs` does not list `Square.Persistence.Sqlite` in the CLI's allowed
  dependency set.
- `Directory.Packages.props` does not contain the phrase "is therefore implemented dependency-free"
  or "M1 state store."
- `dotnet build` succeeds with zero warnings for first-party code.
- Domain.Tests, Contract.Tests, and Architecture.Tests all pass.
- Changed paths remain within the allowed list.

## Stop conditions

Record `STOP:` and make no invented decision when:

- root authority conflicts with this packet;
- a path boundary cannot be proved safe;
- the build fails for a reason outside this task's scope;
- the selected route cannot be verified exactly;
- unexpected files or concurrent edits appear, excluding `.commandcode/**`, `__pycache__/**`, and
  owner-authorized documentation under `docs/**`; or
- acceptance would require describing planned behavior as shipped.

## Evidence destination

`STATE-REM-01.md` (this directory) records starting/resulting commits, routes, reported input
tokens, checks, budgets, changed paths, findings, and gates. It never grants authority.

## Acceptance authority

The primary session records technical completion after full boundary review. The owner separately
accepts the re-scoping. Acceptance does not activate `SP02-T01` or any later work.
