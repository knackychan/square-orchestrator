# M1 Build Tasks — Dry-Run Foundation

These tasks are inactive unless root `STATUS.md` activates one exactly. Before each implementation
task, the primary session replaces `ACTIVATION_REQUIRED` with the reviewed 40-character starting
commit, records route evidence or an explicit owner exception, and obtains exact activation in
root `STATUS.md`. The replacement is an authority amendment, not a worker action.

## T-M1-01 — Establish the CLI shell and output contract

Create the root `sqorch/` and `tests/` context pairs, update root file maps, and implement only the
`doctor` command plus argument/result plumbing. `doctor` reports Python, Git, repository path, and
the computed state path without creating a database.

Proposed dated route: `opencode` / `opencode-go/deepseek-v4-flash` (`ordinary`). Live preflight and
adoption are required at activation.

<!-- sqorch:task v1 -->
```toml
schema = 1
id = "T-M1-01"
role = "IMPLEMENT"
mode = "write"
starting_commit = "574249cb5f0209a30d13a8190e416be02c5e4fc9"
allowed_paths = ["AGENTS.md", "CLAUDE.md", "README.md", "sqorch/", "tests/"]
forbidden_paths = [".git/", ".env", "HANDOVER.md", "CLIENT-EXECUTION.md"]
validation = ["python -m unittest tests.test_cli -v", "python -m sqorch --json doctor"]
expected_commit_message = "feat: add dry-run cli shell"
external_call_limit = 0
spend_limit_usd = 0
turn_limit = 100
token_rotation_limit = 150000
client = "opencode"
model = "opencode-go/deepseek-v4-flash"
automatic_fallback = false
evidence_destination = "docs/superpowers/plans/2026-08-05-m1-dry-run-foundation/STATE.md"
acceptance_authority = "primary session technical review"
```
<!-- /sqorch:task -->

Literal assertions, written before implementation:

```python
assert json_result == {
    "ok": True,
    "data": {
        "git": expected_git_version,
        "python": expected_python_version,
        "repository": expected_repository,
        "state_db": expected_state_path,
    },
}
assert not state_path.exists()
assert invalid_command.returncode == 2
```

The first focused run must fail because `sqorch` or `doctor` does not exist. Stage exact paths only.
Commit message: `feat: add dry-run cli shell`.

## T-M1-02 — Compile and validate authority manifests

Add canonical task-block extraction, path-bound validation, Git `HEAD` comparison, active-packet
checks, raw-byte SHA-256 hashing, canonical JSON compilation, and route/fallback validation.

Proposed dated route: `opencode` / `opencode-go/deepseek-v4-pro` (`silent-failure`) because an
apparently valid authority projection could silently widen scope.

<!-- sqorch:task v1 -->
```toml
schema = 1
id = "T-M1-02"
role = "IMPLEMENT"
mode = "write"
starting_commit = "ACTIVATION_REQUIRED"
allowed_paths = ["sqorch/authority.py", "sqorch/application.py", "sqorch/cli.py", "tests/support.py", "tests/test_authority.py", "tests/test_cli.py"]
forbidden_paths = [".git/", ".env", "STATUS.md", "HANDOVER.md", "CLIENT-EXECUTION.md"]
validation = ["python -m unittest tests.test_authority tests.test_cli -v"]
expected_commit_message = "feat: validate authority manifests"
external_call_limit = 0
spend_limit_usd = 0
turn_limit = 100
token_rotation_limit = 150000
client = "opencode"
model = "opencode-go/deepseek-v4-pro"
automatic_fallback = false
evidence_destination = "docs/superpowers/plans/2026-08-05-m1-dry-run-foundation/STATE.md"
acceptance_authority = "primary session technical review"
```
<!-- /sqorch:task -->

Required failing cases:

```python
assert compile_error(missing_block).code == "AUTHORITY_MISSING"
assert compile_error(duplicate_task_id).code == "VALIDATION_FAILED"
assert compile_error(wrong_head).code == "AUTHORITY_DRIFT"
assert compile_error(overlapping_paths).code == "VALIDATION_FAILED"
assert compile_error(alias_route).code == "ROUTE_INVALID"
assert compile_error(fallback_enabled).code == "ROUTE_INVALID"
assert compile_manifest(fixture) == compile_manifest(fixture)
```

Also assert exact hashes for all four authority documents and exact canonical JSON bytes. Stage
exact paths only. Commit message: `feat: validate authority manifests`.

## T-M1-03 — Preview projects and audit repositories

Implement explicit responsibility-graph validation, acyclic dependency ordering, new-project
preview, and read-only existing-repository inventory. Do not create, rename, or edit target files.

Proposed dated route: `opencode` / `opencode-go/deepseek-v4-flash` (`ordinary`).

<!-- sqorch:task v1 -->
```toml
schema = 1
id = "T-M1-03"
role = "IMPLEMENT"
mode = "write"
starting_commit = "ACTIVATION_REQUIRED"
allowed_paths = ["sqorch/projects.py", "sqorch/application.py", "sqorch/cli.py", "tests/support.py", "tests/test_projects.py", "tests/test_cli.py"]
forbidden_paths = [".git/", ".env", "STATUS.md", "HANDOVER.md", "CLIENT-EXECUTION.md"]
validation = ["python -m unittest tests.test_projects tests.test_cli -v"]
expected_commit_message = "feat: preview and audit projects"
external_call_limit = 0
spend_limit_usd = 0
turn_limit = 100
token_rotation_limit = 150000
client = "opencode"
model = "opencode-go/deepseek-v4-flash"
automatic_fallback = false
evidence_destination = "docs/superpowers/plans/2026-08-05-m1-dry-run-foundation/STATE.md"
acceptance_authority = "primary session technical review"
```
<!-- /sqorch:task -->

Required assertions:

```python
assert preview(canonical_input)["dependency_order"] == expected_order
assert preview(cycle).error.code == "INVALID_INPUT"
assert preview(duplicate_owner).error.code == "INVALID_INPUT"
assert tree_digest(target_before) == tree_digest(target_after_preview)
assert tree_digest(target_before) == tree_digest(target_after_audit)
assert audit(non_repository).error.code == "NOT_A_REPOSITORY"
```

The preview must name every required root authority file and context pair. Stage exact paths only.
Commit message: `feat: preview and audit projects`.

## T-M1-04 — Validate practice records

Implement pure JSON validation for the exact practice fields, closed state vocabulary, confidence
range, and approval requirements. Do not create a catalogue or database table.

Proposed dated route: `opencode` / `opencode-go/deepseek-v4-pro` (`silent-failure`) because
provenance and approval semantics can fail plausibly.

<!-- sqorch:task v1 -->
```toml
schema = 1
id = "T-M1-04"
role = "IMPLEMENT"
mode = "write"
starting_commit = "ACTIVATION_REQUIRED"
allowed_paths = ["sqorch/practices.py", "sqorch/application.py", "sqorch/cli.py", "tests/test_practices.py", "tests/test_cli.py"]
forbidden_paths = [".git/", ".env", "STATUS.md", "HANDOVER.md", "CLIENT-EXECUTION.md"]
validation = ["python -m unittest tests.test_practices tests.test_cli -v"]
expected_commit_message = "feat: validate practice records"
external_call_limit = 0
spend_limit_usd = 0
turn_limit = 100
token_rotation_limit = 150000
client = "opencode"
model = "opencode-go/deepseek-v4-pro"
automatic_fallback = false
evidence_destination = "docs/superpowers/plans/2026-08-05-m1-dry-run-foundation/STATE.md"
acceptance_authority = "primary session technical review"
```
<!-- /sqorch:task -->

Required assertions:

```python
assert validate(candidate)["state"] == "CANDIDATE"
assert validate(missing_provenance).error.code == "INVALID_INPUT"
assert validate(confidence_above_one).error.code == "INVALID_INPUT"
assert validate(adopted_without_authority).error.code == "INVALID_INPUT"
assert tree_digest(repository) == unchanged_digest
```

Stage exact paths only. Commit message: `feat: validate practice records`.

## T-M1-05 — Register projects and enforce one writer

Implement the exact two-table SQLite schema, idempotent registration, conflicting-registration
failure, holder-bound acquisition/release, and `run --dry-run` integration. Runtime writes in tests
use temporary paths only.

Proposed dated route: `opencode` / `opencode-go/deepseek-v4-pro` (`silent-failure`) because lock or
identity defects could silently permit concurrent writers.

<!-- sqorch:task v1 -->
```toml
schema = 1
id = "T-M1-05"
role = "IMPLEMENT"
mode = "write"
starting_commit = "ACTIVATION_REQUIRED"
allowed_paths = ["sqorch/state.py", "sqorch/application.py", "sqorch/cli.py", "tests/test_state.py", "tests/test_cli.py"]
forbidden_paths = [".git/", ".env", "STATUS.md", "HANDOVER.md", "CLIENT-EXECUTION.md"]
validation = ["python -m unittest tests.test_state tests.test_cli -v"]
expected_commit_message = "feat: add project registry and locks"
external_call_limit = 0
spend_limit_usd = 0
turn_limit = 100
token_rotation_limit = 150000
client = "opencode"
model = "opencode-go/deepseek-v4-pro"
automatic_fallback = false
evidence_destination = "docs/superpowers/plans/2026-08-05-m1-dry-run-foundation/STATE.md"
acceptance_authority = "primary session technical review"
```
<!-- /sqorch:task -->

Required assertions:

```python
assert schema_user_version(db) == 1
assert register(project) == register(project)
assert register(conflicting_project).error.code == "STATE_CONFLICT"
assert acquire(holder_a).ok
assert acquire(holder_b).error.code == "LOCKED"
assert not release(holder_b)
assert release(holder_a)
assert dry_run_result["launch_performed"] is False
assert dry_run_result["automatic_fallback"] is False
```

Stage exact paths only. Commit message: `feat: add project registry and locks`.

## T-M1-06 — Verify the integrated dry-run slice

Run every focused and full check, inspect the full diff, verify forbidden patterns, and record
technical completion. This task does not fix source; any finding becomes a separately activated
bounded fix task. It may update only the human status and evidence ledger after checks pass.

Proposed dated route: `opencode` / `opencode-go/deepseek-v4-pro` (`silent-failure`, read-only review
plus documentation write). A separate immutable read-only reviewer may be authorized later but is
not implicit.

<!-- sqorch:task v1 -->
```toml
schema = 1
id = "T-M1-06"
role = "DOCUMENT"
mode = "write"
starting_commit = "ACTIVATION_REQUIRED"
allowed_paths = ["README.md", "STATUS.md", "docs/superpowers/plans/2026-08-05-m1-dry-run-foundation/STATE.md"]
forbidden_paths = [".git/", ".env", "sqorch/", "tests/", "HANDOVER.md", "CLIENT-EXECUTION.md"]
validation = ["python -m unittest discover -s tests -v", "python -m sqorch --json doctor", "git diff --check"]
expected_commit_message = "docs: record m1 technical completion"
external_call_limit = 0
spend_limit_usd = 0
turn_limit = 100
token_rotation_limit = 150000
client = "opencode"
model = "opencode-go/deepseek-v4-pro"
automatic_fallback = false
evidence_destination = "docs/superpowers/plans/2026-08-05-m1-dry-run-foundation/STATE.md"
acceptance_authority = "primary session technical review"
```
<!-- /sqorch:task -->

Validation additionally asserts the exact six implementation commits, allowed paths per commit,
zero dependency metadata, zero external calls/spend, no weakened assertion, and a clean worktree.
Commit message: `docs: record m1 technical completion`.

## T-M1-07 — Record owner acceptance

This is a primary-session documentation amendment, not a compiled worker task. It runs only after
the owner explicitly accepts the reviewed M1 technical result. Update `SPEC.md` for durable shipped
behavior, `README.md`, `STATUS.md`, and `STATE.md`; do not change source or tests and do not activate
M2A or M2B.

Literal assertions:

```powershell
assert (Select-String -Path STATUS.md -Pattern 'M1 status: \*\*accepted')
assert (Select-String -Path SPEC.md -Pattern 'M1 dry-run foundation')
assert -not (Select-String -Path STATUS.md -Pattern 'M2.*active')
```

Stage exact paths only. Commit message: `docs: record m1 owner acceptance`.
