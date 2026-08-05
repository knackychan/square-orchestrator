# M1 Design — Dry-Run Foundation

- Date: 2026-08-05
- Status: planned; implementation inactive
- Parent contract: `../../../SPEC.md`
- Implementation packet: `../plans/2026-08-05-m1-dry-run-foundation/PACKET.md`

## Outcome

M1 proves the authority and project-foundry workflow without launching an agent client. It produces
a standard-library Python CLI that can inspect a repository, validate explicit machine-readable
authority embedded in human documents, preview an exact route, exercise a single-writer lock, and
return stable human or JSON output.

M1 is deliberately not a globally installed product. It runs as `python -m sqorch` from the
repository. Packaging is deferred until the dry-run contracts are accepted.

## In scope

1. local environment diagnostics without mutation;
2. new-project blueprint and responsibility-graph preview from explicit owner input;
3. read-only existing-project inventory;
4. practice-record validation with provenance and lifecycle state;
5. local project registration in SQLite;
6. authority-document hashing and task-manifest compilation;
7. exact client/model route preview with fallback disabled;
8. one repository write lock; and
9. stable human output and one-object JSON output.

## Not in scope

- real client, terminal, provider, or network calls;
- background processes, services, daemons, VS Code, MCP, ACP, or TUI work;
- project file creation or adoption mutation;
- Git commits, reviews, merges, worktrees, or `STOP:` resolution by the application;
- automatic architecture inference, model selection, fallback, stale-lock recovery, or migrations;
- practice scoring, learning, research, promotion, or cross-project evidence ingestion; or
- installation, packaging, release automation, or a global `sqorch` executable.

## Responsibility graph

```text
__main__
   -> cli
       -> application
           -> authority
           -> projects
           -> practices
           -> state

authority -> pathlib, hashlib, json, tomllib, subprocess
projects  -> pathlib, json, subprocess
practices -> json
state     -> pathlib, sqlite3
```

Dependencies point downward. `cli` parses arguments and renders results; it contains no authority,
path, graph, practice, or lock policy. `application` selects one use case and coordinates domain
modules. Domain modules do not import `cli` or each other. There are no client or terminal adapters
because M1 never launches them.

## First source projection

```text
sqorch/
  AGENTS.md
  CLAUDE.md
  __init__.py
  __main__.py
  cli.py
  application.py
  authority.py
  projects.py
  practices.py
  state.py
tests/
  AGENTS.md
  CLAUDE.md
  support.py
  test_cli.py
  test_authority.py
  test_projects.py
  test_practices.py
  test_state.py
```

This flat package is intentional. M1 has six cohesive responsibilities and no genuine subpackage
boundary. `utils`, `common`, interfaces, factories, repositories, plugin layers, and dependency
injection are forbidden until real consumers require them.

## Runtime and dependency contract

- Python: 3.12 or newer; local planning observation was Python 3.14.5.
- Dependencies: Python standard library only.
- Tests: `unittest`, `tempfile`, and subprocess calls to the local module.
- Invocation: `$env:PYTHONPATH=(Resolve-Path '.'); python -m sqorch ...`.
- State override: global `--state-db <path>`.
- Default state on Windows: `%LOCALAPPDATA%\SquareOrchestrator\state.db`.
- If `LOCALAPPDATA` is absent and no override is supplied, state-writing commands fail closed.
- `doctor`, previews, inventories, and validation do not create the state database.

No `pyproject.toml`, installer, virtual environment, or dependency lockfile is needed for M1.

## CLI contract

The exact M1 command surface is:

```text
python -m sqorch [--json] [--state-db PATH] doctor
python -m sqorch [--json] project new --input PATH --preview
python -m sqorch [--json] project adopt PATH --audit-only
python -m sqorch [--json] [--state-db PATH] project add PATH --name NAME --profile PATH
python -m sqorch [--json] practices validate PATH
python -m sqorch [--json] validate --project PATH --task TASK_ID
python -m sqorch [--json] [--state-db PATH] run --project PATH --task TASK_ID --dry-run
```

`project new` without `--preview`, `project adopt` without `--audit-only`, and `run` without
`--dry-run` are rejected. Planned future commands are not accepted as aliases.

### Output envelope

With `--json`, stdout contains exactly one UTF-8 JSON object and no incidental text:

```json
{"ok":true,"data":{}}
```

Errors use:

```json
{"ok":false,"error":{"code":"AUTHORITY_DRIFT","message":"...","details":{}}}
```

Objects use sorted keys; timestamps use UTC `YYYY-MM-DDTHH:MM:SSZ`; paths are absolute canonical
strings. Human output is concise and deterministic. Exit codes are:

| Code | Meaning |
|---|---|
| `0` | Success |
| `2` | Invalid command or input |
| `3` | Authority, route, or validation gate |
| `4` | State or lock conflict |

Unexpected exceptions may include a traceback during development but must return a nonzero code;
they are defects, not a fifth public result class.

## Authority block and manifest

M1 does not interpret free-form prose. Each executable task must contain exactly one delimited TOML
block inside the active `BUILD-TASKS.md`:

````text
<!-- sqorch:task v1 -->
```toml
schema = 1
id = "T-EXAMPLE-01"
role = "IMPLEMENT"
mode = "write"
starting_commit = "<40-character-sha>"
allowed_paths = ["sqorch/", "tests/"]
forbidden_paths = [".git/", ".env"]
validation = ["python -m unittest discover -s tests -v"]
expected_commit_message = "feat: example"
external_call_limit = 0
spend_limit_usd = 0
turn_limit = 100
token_rotation_limit = 150000
client = "opencode"
model = "opencode-go/deepseek-v4-flash"
automatic_fallback = false
evidence_destination = "docs/.../STATE.md"
acceptance_authority = "project owner"
```
<!-- /sqorch:task -->
````

Exactly one delimited block must match the requested task ID; duplicate IDs or malformed blocks
fail closed. Unknown fields, duplicate keys, missing ceilings, absolute paths, `..` segments,
overlapping allowed/forbidden boundaries, route aliases, `latest`, `auto`, or
`automatic_fallback = true` also fail closed.

The validator reads exact status fields identifying the active packet and implementation gate. It
checks the task ID, starting `HEAD`, clean/dirty state disclosure, packet path, context pairs, and
task block. It hashes raw bytes of `STATUS.md`, `PACKET.md`, `BUILD.md`, and `BUILD-TASKS.md` with
SHA-256. The compiled manifest is canonical JSON returned to stdout; M1 does not persist it.

Human Markdown remains authority because the structured block is embedded in the reviewed task
list. The compiled JSON can narrow or reproduce those fields but never widen them.

## Project foundry contracts

### New-project preview

The input is UTF-8 JSON with these required fields:

- `product_boundary`, `owner`, `language`, `deployment_context`;
- `external_effects`, `data_sensitivity`, `expected_scale`, `acceptance_authority`;
- `responsibilities`: unique IDs, descriptions, and proposed owned paths; and
- `dependencies`: directed `from`/`to` responsibility edges.

The preview validates IDs, relative paths, unique ownership, known edge endpoints, and an acyclic
graph. It emits the proposed root authority files, directory context pairs, responsibility graph,
dependency order, and smallest first-slice file projection. It creates nothing.

The primary session or owner supplies responsibilities. M1 does not pretend deterministic code can
make product architecture judgements.

### Existing-project audit

The audit canonicalizes an existing Git repository and reports, without writing:

- current `HEAD` and worktree cleanliness;
- root authority-file presence;
- every discovered directory missing an `AGENTS.md`/`CLAUDE.md` pair;
- top-level source, test, script, and package metadata paths; and
- whether an active packet path named by `STATUS.md` exists.

It does not parse language imports, infer a replacement architecture, or propose moves in M1; that
deeper fixture-backed adoption work belongs to M2A.

## Practice record contract

M1 validates one JSON object with:

- `schema`, `id`, `category`, `statement`, `proposed_scope`;
- `source_type`, `provenance_reference`, `observed_context`;
- `trade_offs`, `counterexamples`, `confidence`, `review_date`;
- `state`, `approving_authority`, and `affected_profiles`.

`state` is one of `OBSERVED`, `CANDIDATE`, `TRIAL`, `ADOPTED`, `REJECTED`, or `DEPRECATED`.
`confidence` is a number from 0 through 1. `ADOPTED`, `REJECTED`, and `DEPRECATED` require a
non-empty approving authority. Validation never stores, promotes, or applies the record.

## Registry and lock contract

SQLite uses `PRAGMA user_version = 1`, foreign keys, and two tables only:

```sql
CREATE TABLE projects (
  canonical_path TEXT PRIMARY KEY,
  display_name TEXT NOT NULL,
  policy_profile TEXT NOT NULL,
  added_at_utc TEXT NOT NULL
);

CREATE TABLE locks (
  project_path TEXT PRIMARY KEY REFERENCES projects(canonical_path),
  holder TEXT NOT NULL,
  starting_commit TEXT NOT NULL,
  acquired_at_utc TEXT NOT NULL
);
```

Registration is idempotent only when all stored values match; conflicting re-registration fails.
Lock acquisition uses `BEGIN IMMEDIATE` and an insert. A second holder fails with `LOCKED`. Release
requires the same holder and occurs in `finally` for dry-run execution. M1 has no TTL or automatic
stale-lock deletion; recovery remains an owner-visible later decision.

## Dry-run execution

`run --dry-run` performs validation, compiles the manifest, registers or locates the project,
acquires its write lock for a write task, returns the exact route and would-run evidence, and then
releases the lock. It never checks a live catalogue, launches a terminal, starts a client, runs the
task validation commands, or edits the target repository.

The result states `launch_performed: false` and `automatic_fallback: false`. A missing route or
failed authority check returns a gate instead of a partial preview.

## Acceptance boundary

M1 is technically complete when all focused tests and the full `unittest` suite pass, representative
CLI commands return stable human and JSON output, a two-connection lock test proves exclusion, and
the full diff contains only authorized paths. Technical completion awaits owner acceptance.

Only after owner acceptance may a later milestone add a real visible terminal launcher.
