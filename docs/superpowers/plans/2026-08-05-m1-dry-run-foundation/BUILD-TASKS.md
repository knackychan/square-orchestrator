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

Activated route: `cmdc` / `deepseek/deepseek-v4-pro` (`silent-failure`) because an apparently valid
authority projection could silently widen scope. The owner selected Command Code for this task on
2026-08-05; automatic fallback remains disabled.

Owner-approved launcher exception: Command Code may create untracked runtime file
`.commandcode/taste/taste.md` during this task. It is not an implementation path and must not be
staged, committed, read as task input, or retained at the review boundary. Any other path under
`.commandcode/` remains unexpected and requires `STOP:`.

<!-- sqorch:task v1 -->
```toml
schema = 1
id = "T-M1-02"
role = "IMPLEMENT"
mode = "write"
starting_commit = "00758dbccfbb5f509063f4fd2ebd168eb77fb577"
allowed_paths = ["sqorch/authority.py", "sqorch/application.py", "sqorch/cli.py", "tests/AGENTS.md", "tests/CLAUDE.md", "tests/support.py", "tests/test_authority.py", "tests/test_cli.py"]
forbidden_paths = [".git/", ".env", "STATUS.md", "HANDOVER.md", "CLIENT-EXECUTION.md"]
validation = ["python -m unittest tests.test_authority tests.test_cli -v"]
expected_commit_message = "feat: validate authority manifests"
external_call_limit = 0
spend_limit_usd = 0
turn_limit = 100
token_rotation_limit = 150000
client = "cmdc"
model = "deepseek/deepseek-v4-pro"
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

## T-M1-02-FIX-01 — Fail closed while compiling authority manifests

Correct only the rejected T-M1-02 authority-validation defects: exact active-task authorization,
closed task schema and budget validation, strict relative-POSIX task paths, complete enforcement
projection, dirty-state and context-pair gates, canonical manifest bytes, and the `validate` CLI.
Update the source context maps to record the already-created `authority.py`. Do not implement
project preview, practices, registry, locks, or dry-run execution.

The current primary Codex session executes this bounded fix directly. No delegated client launch,
provider request, network call, dependency, or spend is authorized.

<!-- sqorch:task v1 -->
```toml
schema = 1
id = "T-M1-02-FIX-01"
role = "IMPLEMENT"
mode = "write"
starting_commit = "6dc9906e34441ae352ed16fcec5e868921a704a3"
allowed_paths = ["sqorch/AGENTS.md", "sqorch/CLAUDE.md", "sqorch/authority.py", "sqorch/application.py", "sqorch/cli.py", "tests/support.py", "tests/test_authority.py", "tests/test_cli.py"]
forbidden_paths = [".git/", ".env", "STATUS.md", "HANDOVER.md", "CLIENT-EXECUTION.md", "docs/"]
validation = ["python -m unittest tests.test_authority tests.test_cli -v", "python -m sqorch --json validate --project . --task T-M1-02-FIX-01"]
expected_commit_message = "fix: harden authority manifest validation"
external_call_limit = 0
spend_limit_usd = 0
turn_limit = 100
token_rotation_limit = 150000
client = "codex"
model = "gpt-5"
automatic_fallback = false
evidence_destination = "docs/superpowers/plans/2026-08-05-m1-dry-run-foundation/STATE.md"
acceptance_authority = "primary session technical review"
```
<!-- /sqorch:task -->

Literal assertions, written before implementation:

```python
assert compile_error(inactive_requested_task).code == "AUTHORITY_DRIFT"
assert compile_error(unknown_task_field).code == "VALIDATION_FAILED"
assert compile_error(missing_budget_ceiling).code == "VALIDATION_FAILED"
assert compile_error(windows_or_empty_posix_path).code == "VALIDATION_FAILED"
assert compile_error(dirty_or_missing_context_pair).code == "AUTHORITY_DRIFT"
assert manifest["task"]["validation"] == ["python -m unittest"]
assert manifest["task"]["expected_commit_message"] == "feat: test"
assert manifest["task"]["external_call_limit"] == 0
assert manifest_bytes == expected_canonical_utf8_bytes
assert validate_json["ok"] is True
assert validate_json["data"] == json.loads(expected_canonical_utf8_bytes)
```

The focused test run must fail for these assertions before the correction. Stage exact paths only.
Commit message: `fix: harden authority manifest validation`.

## T-M1-03 — Preview projects and audit repositories

Implement explicit responsibility-graph validation, acyclic dependency ordering, new-project
preview, and read-only existing-repository inventory. Do not create, rename, or edit target files.

Activated route: `cmdc` / `deepseek/deepseek-v4-flash` (`ordinary`). The owner selected Command
Code on 2026-08-05. DeepSeek V4 Pro is reserved for a separately activated silent-failure task;
this attempt has one exact Flash route and automatic fallback remains disabled.

The root workflow ignores `.commandcode/**` as Command Code runtime state. Do not read, stage,
commit, or use it as task input or output.

<!-- sqorch:task v1 -->
```toml
schema = 1
id = "T-M1-03"
role = "IMPLEMENT"
mode = "write"
starting_commit = "ed9810c87c2e52b9e68ebb2bac141ee3247df227"
allowed_paths = ["sqorch/AGENTS.md", "sqorch/CLAUDE.md", "sqorch/projects.py", "sqorch/application.py", "sqorch/cli.py", "tests/AGENTS.md", "tests/CLAUDE.md", "tests/support.py", "tests/test_projects.py", "tests/test_cli.py"]
forbidden_paths = [".git/", ".env", "STATUS.md", "HANDOVER.md", "CLIENT-EXECUTION.md"]
validation = ["python -m unittest tests.test_projects tests.test_cli -v"]
expected_commit_message = "feat: preview and audit projects"
external_call_limit = 0
spend_limit_usd = 0
turn_limit = 100
token_rotation_limit = 150000
client = "cmdc"
model = "deepseek/deepseek-v4-flash"
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

## T-M1-03-FIX-01 — Isolate validate CLI test authority

Repair only the focused-validation failure in `test_validate_json_returns_compiled_manifest`.
Construct the smallest temporary authority fixture so the test proves a successful `validate` CLI
path without depending on this repository's live active task or current `HEAD`. Do not change
authority validation, project preview or audit behavior, CLI production code, or the T-M1-03
implementation commit.

Activated route: `cmdc` / `deepseek/deepseek-v4-flash` (`ordinary`). The task is a bounded test
fixture correction with a literal focused assertion. Automatic fallback remains disabled.

<!-- sqorch:task v1 -->
```toml
schema = 1
id = "T-M1-03-FIX-01"
role = "IMPLEMENT"
mode = "write"
starting_commit = "b2a8a96dbccee938123979932e42edb36768705d"
allowed_paths = ["tests/support.py", "tests/test_cli.py", "docs/"]
forbidden_paths = [".git/", ".env", "STATUS.md", "HANDOVER.md", "CLIENT-EXECUTION.md", "sqorch/"]
validation = ["python -B -m unittest tests.test_cli -v"]
expected_commit_message = "fix: isolate validate cli test"
external_call_limit = 0
spend_limit_usd = 0
turn_limit = 100
token_rotation_limit = 150000
client = "cmdc"
model = "deepseek/deepseek-v4-flash"
automatic_fallback = false
evidence_destination = "docs/superpowers/plans/2026-08-05-m1-dry-run-foundation/STATE.md"
acceptance_authority = "primary session technical review"
```
<!-- /sqorch:task -->

Required assertions:

```python
assert completed.returncode == 0
assert result["ok"] is True
assert result["data"]["task"]["id"] == "T-TEST-01"
```

The fixture must retain a matching `HEAD`, active task, context pairs, and all four hashed authority
documents. `docs/**` is an owner-authorized planning-output path. The root workflow ignores
`__pycache__/**` as Python runtime state. Stage exact paths only. Commit message:
`fix: isolate validate cli test`.

## T-M1-04 — Validate practice records

Implement pure JSON validation for the exact practice fields, closed state vocabulary, confidence
range, and approval requirements. Do not create a catalogue or database table.

Activated route: `cmdc` / `deepseek/deepseek-v4-pro` (`silent-failure`) because provenance and
approval semantics can fail plausibly. The owner replaced exhausted OpenCode Go with Command Code
on 2026-08-06. Automatic update and fallback remain disabled. The context-pair paths below are
allowed only to map the new `practices.py` and `test_practices.py` files.

<!-- sqorch:task v1 -->
```toml
schema = 1
id = "T-M1-04"
role = "IMPLEMENT"
mode = "write"
starting_commit = "f9f77486e7151eca0cf45145af2e9ccdb40071ec"
allowed_paths = ["sqorch/AGENTS.md", "sqorch/CLAUDE.md", "sqorch/practices.py", "sqorch/application.py", "sqorch/cli.py", "tests/AGENTS.md", "tests/CLAUDE.md", "tests/test_practices.py", "tests/test_cli.py"]
forbidden_paths = [".git/", ".env", "STATUS.md", "HANDOVER.md", "CLIENT-EXECUTION.md"]
validation = ["python -m unittest tests.test_practices tests.test_cli -v"]
expected_commit_message = "feat: validate practice records"
external_call_limit = 0
spend_limit_usd = 0
turn_limit = 100
token_rotation_limit = 150000
client = "cmdc"
model = "deepseek/deepseek-v4-pro"
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

## T-M1-04-FIX-01 — Enforce the exact practice-record contract

Correct only the rejected T-M1-04 defects. Replace the substituted field vocabulary with the exact
M1 design schema, add the exact `practices validate PATH` application and CLI path, and require a
non-empty approving authority for `ADOPTED`, `REJECTED`, and `DEPRECATED`. Preserve pure validation,
the closed lifecycle vocabulary, confidence bounds, and repository non-mutation. Do not add
storage, scoring, promotion, catalogue behavior, dependencies, or unrelated cleanup.

Activated route: `cmdc` / `deepseek/deepseek-v4-pro` (`silent-failure`) because the fix repairs a
schema and immutable lifecycle contract that previously passed green tests with the wrong meaning.
Automatic update and fallback remain disabled.

<!-- sqorch:task v1 -->
```toml
schema = 1
id = "T-M1-04-FIX-01"
role = "IMPLEMENT"
mode = "write"
starting_commit = "1276832ef3423f626ba3eaef09d661a8c110016f"
allowed_paths = ["sqorch/practices.py", "sqorch/application.py", "sqorch/cli.py", "tests/test_practices.py", "tests/test_cli.py"]
forbidden_paths = [".git/", ".env", "STATUS.md", "HANDOVER.md", "CLIENT-EXECUTION.md", "docs/", "sqorch/authority.py", "sqorch/projects.py", "tests/test_authority.py", "tests/test_projects.py"]
validation = ["python -B -m unittest tests.test_practices tests.test_cli -v"]
expected_commit_message = "fix: enforce practice record contract"
external_call_limit = 0
spend_limit_usd = 0
turn_limit = 100
token_rotation_limit = 150000
client = "cmdc"
model = "deepseek/deepseek-v4-pro"
automatic_fallback = false
evidence_destination = "docs/superpowers/plans/2026-08-05-m1-dry-run-foundation/STATE.md"
acceptance_authority = "primary session technical review"
```
<!-- /sqorch:task -->

Literal assertions, written and observed failing for the rejected reasons before implementation:

```python
assert validate(canonical_record)["state"] == "CANDIDATE"
assert validate(missing_schema).error.code == "INVALID_INPUT"
assert validate(substituted_field_record).error.code == "INVALID_INPUT"
assert validate(missing_provenance_reference).error.code == "INVALID_INPUT"
assert validate(confidence_above_one).error.code == "INVALID_INPUT"
assert validate(adopted_without_authority).error.code == "INVALID_INPUT"
assert validate(rejected_without_authority).error.code == "INVALID_INPUT"
assert validate(deprecated_without_authority).error.code == "INVALID_INPUT"
assert json_cli.returncode == 0
assert json.loads(json_cli.stdout) == {"ok": True, "data": canonical_record}
assert human_cli.returncode == 0
assert json.loads(human_cli.stdout) == canonical_record
assert invalid_json_cli.returncode == 2
assert json.loads(invalid_json_cli.stdout)["error"]["code"] == "INVALID_INPUT"
assert tree_digest(repository) == unchanged_digest
```

The CLI fixture uses a UTF-8 JSON file inside a temporary directory. Stage exact paths only.
Commit message: `fix: enforce practice record contract`.

## T-M1-04-FIX-02 — Reject invalid practice encoding

Correct only the rejected invalid-UTF-8 trust-boundary defect. Map a practice input file that
cannot be decoded as UTF-8 to the standard `INVALID_INPUT` JSON envelope with exit code 2 and no
traceback. Preserve every accepted schema, lifecycle, CLI, and non-mutation behavior from
T-M1-04-FIX-01. Do not change domain rules or perform unrelated cleanup.

Activated route: `cmdc` / `deepseek/deepseek-v4-pro` (`silent-failure`) because this is a
fail-closed input-validation repair for the practice schema boundary. Automatic update and
fallback remain disabled.

<!-- sqorch:task v1 -->
```toml
schema = 1
id = "T-M1-04-FIX-02"
role = "IMPLEMENT"
mode = "write"
starting_commit = "07985c16585f1222558292bf8608b0bc7f53a186"
allowed_paths = ["sqorch/application.py", "tests/test_cli.py"]
forbidden_paths = [".git/", ".env", "STATUS.md", "HANDOVER.md", "CLIENT-EXECUTION.md", "docs/", "sqorch/practices.py", "sqorch/authority.py", "sqorch/projects.py", "sqorch/cli.py", "tests/test_practices.py", "tests/test_authority.py", "tests/test_projects.py"]
validation = ["python -B -m unittest tests.test_cli -v"]
expected_commit_message = "fix: reject invalid practice encoding"
external_call_limit = 0
spend_limit_usd = 0
turn_limit = 100
token_rotation_limit = 150000
client = "cmdc"
model = "deepseek/deepseek-v4-pro"
automatic_fallback = false
evidence_destination = "docs/superpowers/plans/2026-08-05-m1-dry-run-foundation/STATE.md"
acceptance_authority = "primary session technical review"
```
<!-- /sqorch:task -->

Literal assertions, written and observed failing before implementation:

```python
assert invalid_utf8_cli.returncode == 2
assert json.loads(invalid_utf8_cli.stdout)["error"]["code"] == "INVALID_INPUT"
assert invalid_utf8_cli.stderr == ""
assert valid_practice_cli.returncode == 0
```

The test writes only one invalid byte file inside a temporary directory. Stage exact paths only.
Commit message: `fix: reject invalid practice encoding`.

## T-M1-05 — Register projects and enforce one writer

Implement the exact two-table SQLite schema, idempotent registration, conflicting-registration
failure, holder-bound acquisition/release, and `run --dry-run` integration. Runtime writes in tests
use temporary paths only.

Activated route: `cmdc` / `deepseek/deepseek-v4-pro` (`silent-failure`) because lock or identity
defects could silently permit concurrent writers. The owner-directed CMDc replacement is required
because OpenCode Go has exhausted its monthly credit. Automatic update and fallback are disabled.
The context-pair paths below are allowed only to map the new `state.py` and `test_state.py` files.

<!-- sqorch:task v1 -->
```toml
schema = 1
id = "T-M1-05"
role = "IMPLEMENT"
mode = "write"
starting_commit = "ecbb576a7fae7efaaf82bb7b8208eadd8e9c892a"
allowed_paths = ["sqorch/AGENTS.md", "sqorch/CLAUDE.md", "sqorch/state.py", "sqorch/application.py", "sqorch/cli.py", "tests/AGENTS.md", "tests/CLAUDE.md", "tests/test_state.py", "tests/test_cli.py"]
forbidden_paths = [".git/", ".env", "STATUS.md", "HANDOVER.md", "CLIENT-EXECUTION.md"]
validation = ["python -m unittest tests.test_state tests.test_cli -v"]
expected_commit_message = "feat: add project registry and locks"
external_call_limit = 0
spend_limit_usd = 0
turn_limit = 100
token_rotation_limit = 150000
client = "cmdc"
model = "deepseek/deepseek-v4-pro"
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

## T-M1-05-FIX-01 — Enforce the registry and dry-run contract

Correct only the rejected T-M1-05 defects. Use the exact two-table schema and UTC text timestamps,
implement the exact `project add PATH --name NAME --profile PATH` CLI, make identical registration
idempotent without rewriting its stored timestamp, return `STATE_CONFLICT` for changed identity or
profile values, isolate dry-run CLI tests from this repository's live task and HEAD, and map
authority failures to the existing JSON gate without a traceback. Preserve holder-bound locking,
temporary-only test state, no-launch dry-run behavior, and the zero-dependency boundary.

Activated route: `cmdc` / `deepseek/deepseek-v4-pro` (`silent-failure`). The owner authorized the
bounded fix on 2026-08-06. Automatic update and fallback remain disabled.

<!-- sqorch:task v1 -->
```toml
schema = 1
id = "T-M1-05-FIX-01"
role = "IMPLEMENT"
mode = "write"
starting_commit = "df6438ccf8a1031008381d4c567377de536ecd92"
allowed_paths = ["sqorch/state.py", "sqorch/application.py", "sqorch/cli.py", "tests/test_state.py", "tests/test_cli.py"]
forbidden_paths = [".git/", ".env", "STATUS.md", "HANDOVER.md", "CLIENT-EXECUTION.md", "docs/", "sqorch/authority.py", "sqorch/projects.py", "sqorch/practices.py", "tests/test_authority.py", "tests/test_projects.py", "tests/test_practices.py"]
validation = ["python -B -m unittest tests.test_state tests.test_cli -v"]
expected_commit_message = "fix: enforce registry and dry-run contract"
external_call_limit = 0
spend_limit_usd = 0
turn_limit = 100
token_rotation_limit = 150000
client = "cmdc"
model = "deepseek/deepseek-v4-pro"
automatic_fallback = false
evidence_destination = "docs/superpowers/plans/2026-08-05-m1-dry-run-foundation/STATE.md"
acceptance_authority = "primary session technical review"
```
<!-- /sqorch:task -->

Literal assertions, written and observed failing for the rejected reasons before correction:

```python
assert project_columns == [
    ("canonical_path", "TEXT"),
    ("display_name", "TEXT"),
    ("policy_profile", "TEXT"),
    ("added_at_utc", "TEXT"),
]
assert lock_columns[-1] == ("acquired_at_utc", "TEXT")
assert exact_project_add.returncode == 0
assert first_registration == second_registration
assert first_registration["added_at_utc"] == second_registration["added_at_utc"]
assert changed_registration.error.code == "STATE_CONFLICT"
assert dry_run_fixture.returncode == 0
assert dry_run_fixture.data["launch_performed"] is False
assert dry_run_fixture.data["automatic_fallback"] is False
assert authority_drift.returncode == 3
assert authority_drift.error.code == "AUTHORITY_DRIFT"
assert authority_drift.stderr == ""
```

The dry-run success fixture must have its own matching authority documents and HEAD. The correction
adds, moves, and removes no file, so no context-pair path is needed. Stage exact paths only. Commit
message: `fix: enforce registry and dry-run contract`.

## T-M1-05-FIX-02 — Locate registered projects during dry-run

Correct only the remaining integrated-flow defect. When `run --dry-run` receives a project already
registered through `project add`, locate and use that immutable registration rather than attempting
to replace its name or policy profile. Preserve fresh-project dry-run behavior, exact schema and
CLI contracts, idempotence, conflict detection for explicit registration, holder-bound locking,
authority gates, and no-launch behavior. Do not add migrations, recovery, dependencies, or
unrelated cleanup.

Activated route: `cmdc` / `deepseek/deepseek-v4-pro` (`silent-failure`). The owner authorized the
bounded fix on 2026-08-06. Automatic update and fallback remain disabled.

<!-- sqorch:task v1 -->
```toml
schema = 1
id = "T-M1-05-FIX-02"
role = "IMPLEMENT"
mode = "write"
starting_commit = "2e99362c11eba4486ed270909762f29f7980f355"
allowed_paths = ["sqorch/state.py", "sqorch/application.py", "tests/test_state.py", "tests/test_cli.py"]
forbidden_paths = [".git/", ".env", "STATUS.md", "HANDOVER.md", "CLIENT-EXECUTION.md", "docs/", "sqorch/authority.py", "sqorch/cli.py", "sqorch/projects.py", "sqorch/practices.py", "tests/test_authority.py", "tests/test_projects.py", "tests/test_practices.py"]
validation = ["python -B -m unittest tests.test_state tests.test_cli -v"]
expected_commit_message = "fix: locate registered dry-run projects"
external_call_limit = 0
spend_limit_usd = 0
turn_limit = 100
token_rotation_limit = 150000
client = "cmdc"
model = "deepseek/deepseek-v4-pro"
automatic_fallback = false
evidence_destination = "docs/superpowers/plans/2026-08-05-m1-dry-run-foundation/STATE.md"
acceptance_authority = "primary session technical review"
```
<!-- /sqorch:task -->

Literal assertions, written and observed failing before correction:

```python
assert exact_project_add.returncode == 0
assert dry_run_after_add.returncode == 0
assert dry_run_after_add.data["launch_performed"] is False
assert dry_run_after_add.data["automatic_fallback"] is False
assert stored_registration_after == stored_registration_before
assert remaining_lock_count == 0
assert fresh_project_dry_run.returncode == 0
assert authority_drift.returncode == 3
assert authority_drift.stderr == ""
```

The integration assertion uses one temporary authority fixture, profile, and SQLite database. The
correction adds, moves, and removes no file, so no context-pair path is needed. Stage exact paths
only. Commit message: `fix: locate registered dry-run projects`.

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
