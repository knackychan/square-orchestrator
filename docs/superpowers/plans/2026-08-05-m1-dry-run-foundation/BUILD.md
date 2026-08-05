# M1 Build Guide — Dry-Run Foundation

This guide closes the decisions needed by `BUILD-TASKS.md`. It cannot activate a task or change a
budget.

## Decision register

**M1-001 — Standard-library Python first.**  Target Python 3.12+ and use `argparse`, `dataclasses`
only where they reduce ambiguity, `hashlib`, `json`, `pathlib`, `sqlite3`, `subprocess`, `tempfile`,
`tomllib`, and `unittest`. Third-party packages solve no demonstrated M1 need.

**M1-002 — Module execution before packaging.**  M1 runs as `python -m sqorch`. A global executable,
installer, build backend, and `pyproject.toml` are deferred until the accepted CLI contracts justify
distribution work.

**M1-003 — Flat cohesive package.**  `cli` owns arguments/rendering, `application` coordinates use
cases, and `authority`, `projects`, `practices`, and `state` own their rules. No `utils`, `common`,
adapter, factory, interface, service-container, or plugin layer is created.

**M1-004 — One result envelope.**  Application functions return plain result data or one typed
application error. `cli` alone converts it to human text or the stable JSON envelope. Domain
modules never print or exit.

**M1-005 — JSON is the projection format.**  CLI inputs for blueprints and practices and all
machine output use UTF-8 JSON. Manifest output is canonical JSON with sorted keys and compact
separators. M1 does not persist manifests.

**M1-006 — TOML task data stays inside human authority.**  A unique delimited TOML block inside
`BUILD-TASKS.md` is the canonical structured task record. `tomllib` parses it. Free-form prose is
never interpreted for authorization.

**M1-007 — Raw-byte authority hashes.**  SHA-256 hashes the exact bytes of `STATUS.md`, `PACKET.md`,
`BUILD.md`, and `BUILD-TASKS.md`. Newline normalization is forbidden because it would hide drift.

**M1-008 — Paths fail closed.**  Existing repository paths use `Path.resolve(strict=True)`. Task
paths must be relative POSIX paths with no empty, `.` or `..` segment. A directory claim contains
its descendants. Allowed and forbidden claims may not overlap in either direction.

**M1-009 — Owner-supplied responsibility graph.**  Project preview validates explicit nodes and
edges and projects the smallest file plan. It does not guess a product architecture. DAG checking
uses a small in-degree traversal; cycles are validation errors.

**M1-010 — Adoption is inventory only.**  M1 reports authority files, context-pair gaps, top-level
project shape, `HEAD`, and dirty state. Import analysis, duplicate-responsibility detection,
rearrangement proposals, and writes remain M2A.

**M1-011 — Practice validation is pure.**  Practice JSON is validated against the closed lifecycle
vocabulary and approval rules, then returned. No catalogue, scoring, promotion, or storage exists.

**M1-012 — SQLite schema version one has two tables.**  `projects` and `locks` use the exact design
schema with `PRAGMA user_version = 1` and foreign keys. There is no repository abstraction or
migration framework.

**M1-013 — Lock conflict is evidence, not recovery authority.**  Acquisition uses
`BEGIN IMMEDIATE`; a duplicate project key returns `LOCKED`. Release requires the same holder.
There is no TTL, force unlock, or automatic stale-lock deletion in M1.

**M1-014 — Dry-run means no launch.**  `run --dry-run` validates, compiles, locks, previews, and
releases. It never executes task checks or starts a terminal/client. The response literally records
`launch_performed: false`.

**M1-015 — Subprocess use is read-only and array-based.**  Only exact Git/environment inspection
commands are permitted, passed as argument arrays with a fixed working directory. Shell strings,
`shell=True`, PowerShell interpolation, and client commands are forbidden.

**M1-016 — Tests own their fixtures.**  Tests build minimal repositories and documents under
`TemporaryDirectory`, initialize local Git where needed, and use a temporary SQLite path. They do
not depend on Sticker Generator, a user database, network access, or installed agent clients.

**M1-017 — Dated routes remain gated.**  Proposed task routes reuse the dated OpenCode Go
DeepSeek V4 profile from `CLIENT-EXECUTION.md`: Flash for visible-failure work and Pro for authority,
provenance, state, and integration work with silent-failure risk. Live route adoption and preflight
are required at every later activation.

## Exact error vocabulary

| Code | Exit | Meaning |
|---|---:|---|
| `INVALID_INPUT` | 2 | Invalid JSON, fields, graph, path, or command combination |
| `NOT_A_REPOSITORY` | 2 | Existing-project operation did not receive a Git repository |
| `AUTHORITY_MISSING` | 3 | Required authority document or task block is absent |
| `AUTHORITY_DRIFT` | 3 | Commit, hash, active packet, or document state differs |
| `ROUTE_INVALID` | 3 | Route is absent, aliased, ambiguous, or permits fallback |
| `VALIDATION_FAILED` | 3 | A deterministic contract check failed |
| `STATE_CONFLICT` | 4 | Registration conflicts with stored values |
| `LOCKED` | 4 | Another holder owns the project write lock |

New public codes require an amendment. Error messages may add specifics but must not contain file
contents, credentials, environment values, or raw prompts.

## State database contract

Initialization occurs only for `project add` or `run --dry-run` after input validation. Parent
directories are created only for the explicit/default state path. SQL parameters are bound, never
formatted. Registration timestamps and lock timestamps are UTC seconds.

Registration rules:

1. canonicalize the project and profile paths;
2. insert a new project;
3. if the path exists and all values match, return the existing record;
4. if any value differs, return `STATE_CONFLICT`; and
5. never rewrite the existing row implicitly.

Lock rules:

1. begin an immediate transaction;
2. insert `(project_path, holder, starting_commit, acquired_at_utc)`;
3. commit and retain the logical lock row while the dry-run operation proceeds;
4. on duplicate key, roll back and return `LOCKED`; and
5. delete only with both project path and holder in `finally`.

The deliberate M1 ceiling is crash-orphaned lock rows. Add inspected recovery only when M2B needs
real launches; do not smuggle a force-unlock policy into the dry-run slice.

## Test contracts

- Every nontrivial rule has one focused failing assertion before implementation.
- Tests compare decoded JSON structures except where canonical byte equality is the contract.
- Two identical manifest compilations must be byte-identical.
- Preview/audit tests snapshot the target tree before and after and compare exact paths and hashes.
- Lock tests use two independent SQLite connections and two holders.
- CLI tests run `python -m sqorch` in a subprocess with `PYTHONPATH` pointing to the repository.
- The full suite uses only `python -m unittest discover -s tests -v`.

## Greppable forbidden patterns

Implementation review rejects unexplained occurrences of:

```text
requests
httpx
urllib.request
shell=True
Start-Process
Start-Job
cmdc
opencode run
claude --
codex exec
fallback-model
pyproject.toml
```

Exact route strings may appear in tests and planned task blocks; launch command fragments may not
appear in application source.

## Parked until evidence requires them

- packaging and a global executable;
- default-state portability beyond the Windows-first path and explicit override;
- schema migrations, stale-lock recovery, and run/attempt/event tables;
- real client and terminal adapters;
- target-project creation or adoption mutation;
- Git diff/review automation;
- blueprint inheritance, practice storage/scoring, and research;
- parallel repositories, worktrees, MCP, ACP, VS Code, daemon, or TUI integration.
