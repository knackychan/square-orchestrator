# M1 Dry-Run Foundation State

State records evidence; it does not grant authority.

## Cold start

Read root `AGENTS.md`, `CLAUDE.md`, `SPEC.md`, `STATUS.md`, and `HANDOVER.md`. Read
`CLIENT-EXECUTION.md` before any route or client work. Then read this directory's `PACKET.md`,
`BUILD.md`, `BUILD-TASKS.md`, and `STATE.md`.

## Planning position

| Field | Value |
|---|---|
| Current activity | T-M1-05 corrective chain technically accepted; owner acceptance pending |
| Planning authorization | Owner instruction on 2026-08-05 |
| Starting HEAD | `0c6351e` — `docs: record client playbook context` |
| Planning commit | `44428ce` — `docs: plan m1 dry-run foundation` |
| Verified file count | 38 tracked files after T-M1-01 |
| Implementation authority | none |
| Delegated agent authority | none |
| External calls / spend | `0 / $0` |
| Dependencies | `0` |
| Proposed implementation tasks | T-M1-01 complete; T-M1-02-FIX-01, T-M1-03, and T-M1-03-FIX-01 technically accepted; T-M1-04 corrective chain accepted by owner; T-M1-05 corrective chain technically accepted through FIX-02; T-M1-06 and T-M1-07 inactive |
| Route checks | Completed T-M1-05-FIX-02 CMDc / `deepseek/deepseek-v4-pro` executable, authentication, exact model availability, flags, fallback, HEAD, and terminal checks passed |
| T-M1-03-FIX-01 starting HEAD | `b2a8a96dbccee938123979932e42edb36768705d` — preserved T-M1-03 implementation commit |
| Open `STOP:` items | none |
| Owner gate | Accept or reject the T-M1-05 corrective chain; do not activate T-M1-06 |

## T-M1-01 activation attempt

- Owner instruction: accept the completed M1 plan and activate T-M1-01 on 2026-08-05.
- Attempt timestamp: `2026-08-05T10:46:23Z`.
- Reviewed starting HEAD: `574249cb5f0209a30d13a8190e416be02c5e4fc9`.
- Worktree before activation amendment: clean.
- Risk class: `ordinary`.
- Proposed route: `opencode` / `opencode-go/deepseek-v4-flash`.
- Selection reason: T-M1-01 is bounded CLI plumbing with literal visible-failure assertions.
- Escalation condition: not applicable.
- Executable evidence: `Get-Command opencode -ErrorAction SilentlyContinue` resolved
  `C:\Users\DEROK137\AppData\Roaming\npm\opencode.ps1`.
- Authentication evidence: `opencode providers list` reported a configured OpenCode Go credential;
  no credential value was read or persisted.
- Catalogue evidence: `opencode models opencode-go` found the exact model ID in the local cache.
  No `--refresh` or provider/model request was used, so this is not live allowance evidence.
- Allowance evidence: unavailable. The repository pins no safe allowance command, and the active
  budget permits zero external/provider requests.
- Automatic fallback: disabled.
- Visible terminal: `wt.exe` resolved at
  `C:\Users\DEROK137\AppData\Local\Microsoft\WindowsApps\wt.exe`; no terminal or worker launched.
- Token rotation used: `0 / 150000`; no worker session launched.
- Dependencies, external/provider calls, spend, destructive actions, and delegated launches used:
  `0 / 0 / $0 / 0 / 0`.
- ~~Activation result: blocked. The `ACTIVATION_REQUIRED` task placeholder was not changed and no
  implementation authority opened.~~ Resolved by the owner exception below.
- ~~`STOP:` What approved safe command or evidence source verifies current OpenCode Go allowance
  for `opencode-go/deepseek-v4-flash` while preserving T-M1-01's zero external/provider request
  budget?~~ Resolved by deferring allowance verification for this task.

### Owner resolution and activation

- Resolution: on 2026-08-05 the owner stated that OpenCode credits are currently unavailable,
  explicitly deferred that verification, and directed implementation to proceed now.
- Execution: the current primary Codex session implemented T-M1-01 directly; no OpenCode or other
  delegated client was launched.
- Authority: T-M1-01 only, at starting commit
  `574249cb5f0209a30d13a8190e416be02c5e4fc9`.
- Allowed implementation paths: `AGENTS.md`, `CLAUDE.md`, `README.md`, `sqorch/`, and `tests/`.
- Retained budgets: zero dependencies, zero implementation-time external/provider calls, zero
  spend, zero destructive actions, and one repository writer.
- Route carry-forward: verify OpenCode Go credit and allowance before any later delegated launch.

## T-M1-01 technical result

- Last completed task: T-M1-01.
- Starting commit: `574249cb5f0209a30d13a8190e416be02c5e4fc9`.
- Resulting implementation commit: `2ff5e93f024f6564af0a25e63c8e34dcb4e90b37` —
  `feat: add dry-run cli shell`.
- Red evidence: `python -m unittest tests.test_cli -v` failed two tests because Python reported
  `No module named sqorch` before source existed.
- Focused validation: `python -m unittest tests.test_cli -v` passed 2 tests.
- Full validation: `python -m unittest discover -s tests -v` passed 2 tests.
- CLI validation: `python -m sqorch --json doctor` returned one successful canonical JSON object
  with Python, Git, canonical repository, and computed state database path.
- State-write assertion: passed; `doctor` did not create the computed database or its parent.
- Boundary review: exactly one implementation commit changed 12 paths, all within the task's
  allowed paths; both new project-owned directories contain matching context pairs and file maps.
- `git diff --check 574249cb5f0209a30d13a8190e416be02c5e4fc9..2ff5e93f024f6564af0a25e63c8e34dcb4e90b37`:
  passed.
- Forbidden-pattern scan of `sqorch/`: passed; no dependency, network, client, terminal, fallback,
  shell interpolation, or package metadata pattern found.
- Dependencies, external/provider calls, spend, destructive actions, and delegated launches used:
  `0 / 0 / $0 / 0 / 0`.
- Reported worker input tokens: not applicable; no delegated worker session was launched.
- Open `STOP:` items: none.
- Acceptance: primary-session technical review passed. This does not activate T-M1-02 or record M1
  owner acceptance.

## T-M1-02 activation

- Owner instruction: activate T-M1-02 using Command Code with DeepSeek Pro on 2026-08-05.
- Activation timestamp: `2026-08-05T11:03:55Z`.
- Reviewed starting HEAD: `00758dbccfbb5f509063f4fd2ebd168eb77fb577`.
- Worktree before activation amendment: clean; `.git/index.lock` was absent.
- Risk class: `silent-failure`.
- Client: `cmdc`.
- Exact model ID: `deepseek/deepseek-v4-pro`.
- Selection reason: T-M1-02 compiles the authority projection; a plausible defect in path overlap,
  document hashing, active-packet binding, route identity, fallback, or starting-commit validation
  could silently widen execution authority.
- Escalation condition: not applicable; DeepSeek V4 Pro is the packet's silent-failure class.
- Executable evidence: `Get-Command cmdc -ErrorAction SilentlyContinue` resolved
  `C:\Users\DEROK137\AppData\Roaming\npm\cmdc.ps1`.
- Client evidence: `cmdc --no-auto-update --help` reported Command Code 1.12.0 and the pinned launch
  flags `--trust`, `--permission-mode`, `--model`, `--effort`, `--max-turns`, and
  `--no-auto-update`.
- Authentication evidence: `cmdc --no-auto-update status` reported authenticated status and
  verified authentication; no credential value was read or persisted.
- Catalogue and allowance evidence: `cmdc --no-auto-update --list-models` reported 50 models
  available for use and included exact ID `deepseek/deepseek-v4-pro`. This section records only the
  safe result, not a credential or credit balance; no model task was submitted and no spend was
  incurred during preflight.
- Automatic fallback: disabled by the task block and launch profile; aliases and substitution are
  prohibited.
- Visible terminal: `wt.exe` resolved at
  `C:\Users\DEROK137\AppData\Local\Microsoft\WindowsApps\wt.exe`; no terminal or worker launched by
  this activation amendment.
- Starting token rotation use: `0 / 150000`.
- Allowed implementation paths: `sqorch/authority.py`, `sqorch/application.py`, `sqorch/cli.py`,
  `tests/support.py`, `tests/test_authority.py`, and `tests/test_cli.py`.
- Expected implementation commit: `feat: validate authority manifests`.
- Focused validation: `python -m unittest tests.test_authority tests.test_cli -v`.
- Retained budgets: zero third-party dependencies, zero task-side external/provider requests, zero
  spend, zero destructive actions, one repository writer, and 100 worker turns.
- Activation result: T-M1-02 is active. T-M1-03 through T-M1-07 remain inactive.
- Open `STOP:` items: none.

## T-M1-02 attempt 1 — `STOP:`

- Launch: one visible Windows Terminal worker using `cmdc` / `deepseek/deepseek-v4-pro`, high
  effort, 100-turn cap, no automatic update, and no automatic fallback.
- Launch timestamp: approximately `2026-08-05T11:07:29Z` from the worker process start time.
- Stop recorded: `2026-08-05T11:09:00Z`.
- Starting and resulting HEAD: `00758dbccfbb5f509063f4fd2ebd168eb77fb577`; no commit created.
- Unexpected artifact: Command Code created untracked `.commandcode/taste/taste.md` with zero-byte
  content at startup. The directory is not among T-M1-02's six allowed paths.
- Response: the primary session terminated only worker processes `node.exe` PID 11368 and its
  `powershell.exe` parent PID 36644 before any source edit appeared.
- Preservation: the generated `.commandcode/` directory was moved intact to
  `C:\Users\DEROK137\AppData\Local\Temp\square-orchestrator-commandcode-stop-20260805T1107Z` and
  can be restored. The repository now contains only the three intentional activation amendments.
- Changed implementation paths: none.
- Validation and commit: not run by the worker; no implementation commit exists.
- Reported input tokens: unavailable because the worker was terminated before handoff.
- Task-side dependencies, external calls, spend, and destructive actions: `0 / 0 / $0 / 0`.
  Command Code service usage for the interrupted startup was not reported.
- `STOP:` Should T-M1-02 be amended to allow Command Code's repository-local
  `.commandcode/taste/taste.md`, or may the primary add a separately verified Command Code
  no-write/taste-disable launch setting before relaunch?
- T-M1-03 through T-M1-07 remain inactive. No new attempt is authorized until the owner records the
  resolution.

### Owner resolution and retry authority

- Owner resolution: allow Command Code to create `.commandcode/taste/taste.md` during T-M1-02 and
  retry.
- Resolution boundary: the exception covers that exact untracked runtime file only. It does not
  make the file implementation output and does not permit staging, committing, task-input use, or
  any other `.commandcode/` artifact.
- Boundary cleanup: the primary session removes or archives the runtime file before final review.
- Route: unchanged `cmdc` / `deepseek/deepseek-v4-pro`, high effort, automatic update and fallback
  disabled.
- Starting HEAD, implementation paths, budgets, validation, and expected commit: unchanged.
- Retry authority: attempt 2 was authorized; T-M1-03 through T-M1-07 remain inactive.

## T-M1-02 attempt 2 — launcher failure

- Outcome: the worker did not reach task execution. Windows Terminal interpreted semicolons inside
  the bounded prompt as action separators and opened error tabs.
- Response: the primary session terminated only attempt 2 worker processes, closed the exact newly
  created terminal window while preserving the owner's existing terminal window, and changed no
  implementation file.
- Starting and resulting HEAD: `00758dbccfbb5f509063f4fd2ebd168eb77fb577`.
- Runtime preservation: `.commandcode/` was archived at
  `C:\Users\DEROK137\AppData\Local\Temp\square-orchestrator-commandcode-attempt2-20260805T1118Z`.
- Commit, validation, and reported tokens: none.
- Correction: attempt 3 uses the same exact route and authority with a semicolon-free bounded
  prompt so Windows Terminal receives one client command.
- Attempt 3 authority: active under the owner's retry instruction.

## T-M1-02 attempt 3 — `STOP:`

- Launch: corrected visible Windows Terminal invocation using one argument array and a bounded
  prompt containing no semicolons.
- Owner-confirmed launcher rule: this is the accepted Windows Terminal pattern. Semicolons must not
  occur inside a prompt argument because Windows Terminal interprets them as action separators.
  Record this in the client playbook through a later authorized documentation amendment.
- Route: `cmdc` / `deepseek/deepseek-v4-pro`, high effort, automatic update and fallback disabled.
- Starting and resulting HEAD: `00758dbccfbb5f509063f4fd2ebd168eb77fb577`; no commit created.
- Stop recorded: `2026-08-05T11:29:19Z`.
- Planned new files created: `tests/support.py` and `tests/test_authority.py`.
- Unexpected task paths modified: `tests/AGENTS.md` and `tests/CLAUDE.md`.
- Conflict: root `AGENTS.md` requires the nearest context pair's file map to be updated whenever a
  file is added, but T-M1-02's exact allowlist omits both test context files even though the task
  explicitly creates two new test files.
- Response: the primary session terminated only attempt 3 worker processes before source
  implementation began. All partial test and context-map changes are preserved, unstaged, and
  uncommitted for review. Nothing was reverted.
- Runtime preservation: `.commandcode/` was archived at
  `C:\Users\DEROK137\AppData\Local\Temp\square-orchestrator-commandcode-attempt3-20260805T1128Z`.
- Validation: `git diff --check` passed for the four worker-touched test paths. Red/focused/full
  tests were not observed before termination.
- Reported input tokens: unavailable because the worker was terminated before handoff.
- `STOP:` May T-M1-02 add `tests/AGENTS.md` and `tests/CLAUDE.md` to its allowed paths so the planned
  `tests/support.py` and `tests/test_authority.py` additions comply with the mandatory context-map
  invariant?
- No continuation or commit is authorized until the owner resolves this exact question.

### Owner resolution and attempt 4 authority

- Owner resolution: T-M1-02 may add `tests/AGENTS.md` and `tests/CLAUDE.md` to its allowed paths.
- Amended allowed paths: `sqorch/authority.py`, `sqorch/application.py`, `sqorch/cli.py`,
  `tests/AGENTS.md`, `tests/CLAUDE.md`, `tests/support.py`, `tests/test_authority.py`, and
  `tests/test_cli.py`.
- Preserved work: attempt 3's unstaged context-map and test-file changes remain in place. The
  continuation must review them and establish the required red evidence before implementation.
- Route, starting HEAD, runtime Taste exception, budgets, validation, and expected commit remain
  unchanged.
- Attempt 4 continuation is authorized. T-M1-03 through T-M1-07 remain inactive.

## T-M1-02 attempt 4 result and technical review

- Starting HEAD: `00758dbccfbb5f509063f4fd2ebd168eb77fb577`.
- Resulting commit: `6dc9906e34441ae352ed16fcec5e868921a704a3` —
  `feat: validate authority manifests`.
- Exact route: `cmdc` / `deepseek/deepseek-v4-pro`, high effort, automatic update and fallback
  disabled.
- Red evidence reported by the worker: focused tests failed initially with
  `ModuleNotFoundError: No module named 'sqorch.authority'`.
- Worker changed five amended-allowlist paths: `sqorch/authority.py`, `tests/AGENTS.md`,
  `tests/CLAUDE.md`, `tests/support.py`, and `tests/test_authority.py`.
- Commit boundary: exact message and allowed paths passed; authority amendments were not staged or
  committed.
- Primary focused validation: `python -m unittest tests.test_authority tests.test_cli -v` passed 17
  tests.
- Primary full validation: `python -m unittest discover -s tests -v` passed 17 tests.
- Primary CLI validation: `python -m sqorch --json doctor` passed.
- `git diff --check 00758dbccfbb5f509063f4fd2ebd168eb77fb577..6dc9906e34441ae352ed16fcec5e868921a704a3`:
  passed.
- Runtime cleanup: `.commandcode/` was archived outside the repository at
  `C:\Users\DEROK137\AppData\Local\Temp\square-orchestrator-commandcode-attempt4-20260805T1204Z`.
  Only the owner's existing Windows Terminal window remains open.
- Worker-reported input tokens: unavailable. Task-side dependencies, external calls, spend, and
  destructive actions remained `0 / 0 / $0 / 0`; Command Code client usage was not reported.

### Rejected review findings

1. Active authority fails open. A temporary repository whose `STATUS.md` activates `T-OTHER`
   successfully compiles requested `T-TEST-01`, contrary to the required implementation-gate and
   task-ID checks.
2. Task schema and budgets fail open. Unknown fields and task blocks missing all four ceilings are
   accepted. The compiled task projection drops validation commands, expected commit message,
   every ceiling, evidence destination, and acceptance authority, so it cannot enforce the human
   authority it hashes.
3. Path validation fails open. `a//b`, `C:/absolute`, and `a\\b` are all accepted despite the
   relative-POSIX-path contract and prohibition on empty segments.
4. The exact CLI contract is absent. `python -m sqorch --json validate --project . --task T-M1-02`
   returns `INVALID_INPUT` because only `doctor` exists, and `compile_manifest` returns a Python
   dictionary rather than canonical JSON output.
5. Required tests are ineffective. The fourth document hash is checked only for key presence, and
   the canonical-byte test serializes a dictionary itself and checks round-trip equality rather
   than asserting exact canonical bytes. Context-pair and dirty-state validation, malformed blocks,
   unknown fields, missing ceilings, and active-task mismatch are untested.

- Technical acceptance: rejected. Green tests do not override the failed design and executable
  probes.
- Next gate: a separately authorized bounded fix task must add literal failing assertions for these
  findings, make the smallest cohesive correction, produce a separate exact commit, and return to
  review. T-M1-03 remains inactive.

## T-M1-02-FIX-01 activation

- Owner instruction: `T-M1-02-FIX-01 accept, go` on 2026-08-05.
- Reviewed fix base: `6dc9906e34441ae352ed16fcec5e868921a704a3`.
- Execution: the current primary Codex session implements the bounded fix directly; no delegated
  worker is launched.
- Allowed paths: `sqorch/AGENTS.md`, `sqorch/CLAUDE.md`, `sqorch/authority.py`,
  `sqorch/application.py`, `sqorch/cli.py`, `tests/support.py`, `tests/test_authority.py`, and
  `tests/test_cli.py`.
- Expected commit: `fix: harden authority manifest validation`.
- Required checks: `python -m unittest tests.test_authority tests.test_cli -v` and
  `python -m sqorch --json validate --project . --task T-M1-02-FIX-01`.
- Budgets: zero dependencies, external/provider requests, spend, and destructive actions; one
  repository writer; 100 turns; rotation at 150000 reported input tokens.
- T-M1-03 through T-M1-07 remain inactive.

## T-M1-02-FIX-01 technical result

- Starting commit: `6dc9906e34441ae352ed16fcec5e868921a704a3`.
- Resulting commit: `9c3de04b089f73bd65de12b19cb846adcadb6d44` —
  `fix: harden authority manifest validation`.
- Red evidence: the focused suite failed before the correction on inactive-task, strict-path,
  canonical-manifest, and absent-CLI assertions.
- Focused validation: `python -m unittest tests.test_authority tests.test_cli -v` passed 22 tests.
- Full validation: `python -m unittest discover -s tests -v` passed 22 tests.
- Required CLI validation: `python -m sqorch --json validate --project . --task T-M1-02-FIX-01`
  returned the canonical manifest in the standard JSON envelope before the implementation commit.
- Boundary review: one exact commit changed only the eight allowed paths; the message, path list,
  and `git diff --check 6dc9906e34441ae352ed16fcec5e868921a704a3..9c3de04b089f73bd65de12b19cb846adcadb6d44`
  passed. The forbidden-pattern scan found exact client names only in test data and assertions.
- Budgets consumed: zero dependencies, external/provider requests, spend, destructive actions,
  and delegated launches. Reported input tokens: not applicable; the primary session executed the
  task directly.
- Technical acceptance: passed. Owner acceptance remains required before a later M1 task.

## T-M1-03 activation

- Owner instruction: activate T-M1-03 and use CMDc DeepSeek Flash/DeepSeek Pro for implementation
  on 2026-08-05.
- Reviewed starting HEAD: `9c3de04b089f73bd65de12b19cb846adcadb6d44`.
- Risk class: ordinary. T-M1-03 validates a supplied graph and proves preview/audit non-mutation
  through literal assertions and tree digests. CMDc Flash is the single route for this task.
- Client and exact model: `cmdc` / `deepseek/deepseek-v4-flash`.
- Pro boundary: `cmdc` / `deepseek/deepseek-v4-pro` is available but not selected for this attempt.
  It is reserved for a later separately activated silent-failure task; route switching or fallback
  is prohibited.
- Preflight at `2026-08-05T12:31:01Z`: `cmdc --no-auto-update --help` reported Command Code 1.12.0
  and the required launch flags. `cmdc --no-auto-update status` reported authenticated status.
  `cmdc --no-auto-update --list-models` reported 50 available models including both exact DeepSeek
  IDs. No credential value, model prompt, task-side external/provider request, or spend was used.
- Automatic fallback and updates: disabled. Visible terminal: `wt.exe` resolved at
  `C:\\Users\\DEROK137\\AppData\\Local\\Microsoft\\WindowsApps\\wt.exe`.
- Context-pair amendment: `sqorch/AGENTS.md`, `sqorch/CLAUDE.md`, `tests/AGENTS.md`, and
  `tests/CLAUDE.md` are exact allowed paths solely to map the task's new `projects.py` and
  `test_projects.py`, as required by the root invariant.
- Expected commit: `feat: preview and audit projects`.
- Focused validation: `python -m unittest tests.test_projects tests.test_cli -v`.
- Budgets: zero dependencies, external/provider requests, spend, and destructive actions; one
  repository writer; 100 turns; rotation at 150000 reported input tokens.
- Activation result: T-M1-03 is active. T-M1-04 through T-M1-07 remain inactive.
- Pre-launch authority rebase: root visible-client workflow commit
  `24160a351034018156c21f16285f4e8a46c2676b` is now the task starting HEAD. No T-M1-03 source or
  test edit occurred before the rebase; the cache `STOP:`, exact route, budgets, and paths remain
  unchanged.
- `STOP:` Preflight found untracked `sqorch/__pycache__/` and `tests/__pycache__/` directories
  outside the three activation amendments. The primary session attempted only explicit removal of
  those two exact cache directories, but the execution environment rejected the cleanup command.
  No worker launch, source edit, task-side external/provider request, spend, dependency, or
  destructive action occurred. The owner must authorize a resolution or clean the exact caches
  before a visible worker starts; do not treat either cache as task output or stage it.

### Owner resolution and current rebase

- Owner resolution at `2026-08-05T12:53:06Z`: remove exactly `sqorch/__pycache__/` and
  `tests/__pycache__/`. Primary verification found neither cache directory afterward.
- Current preflight at `2026-08-05T12:53:06Z`: `cmdc` and `wt.exe` resolved, `cmdc` reported
  authenticated status, and its available-model catalogue included
  `deepseek/deepseek-v4-flash`. No credential value was read or persisted.
- Owner instruction: rebase T-M1-03 to current HEAD.
- Rebased starting HEAD: `ed9810c87c2e52b9e68ebb2bac141ee3247df227` —
  `docs: pin native windows terminal wrapper`.
- Result: the cache `STOP:` is resolved. The exact Flash route, fallback prohibition, budgets,
  allowed paths, expected commit, and acceptance authority remain unchanged.

### Global Command Code Taste-file resolution

- Owner instruction: `.commandcode/taste/taste.md` is always accepted as an untracked Command Code
  runtime artifact across this workflow.
- Boundary: it is never task input or output and must not be staged, committed, or retained after
  boundary review. Any other `.commandcode/` artifact remains a `STOP:`.
- Result: the runtime-file `STOP:` is resolved and one visible CMDc Flash worker may launch for
  T-M1-03.

## T-M1-03 implementation result — `STOP:`

- Visible worker route: `cmdc` / `deepseek/deepseek-v4-flash`, high effort, 100-turn cap, and
  automatic fallback disabled.
- Implementation commit: `b2a8a96` — `feat: preview and audit projects`.
- Commit boundary: the message is exact and all ten changed paths are in the task allowlist.
  `git diff --check ed9810c87c2e52b9e68ebb2bac141ee3247df227..b2a8a96` passed.
- Focused validation, executed with `python -B` and `PYTHONDONTWRITEBYTECODE=1`: 11 tests passed
  and `tests.test_cli.test_validate_json_returns_compiled_manifest` failed. It requests inactive
  `T-M1-02-FIX-01`, so the hardened validator correctly returned exit code 3 and
  `AUTHORITY_DRIFT`.
- Full validation, CLI validation, and technical acceptance were not run after the focused failure.
- Worker shutdown: the active CMDc process was stopped after its commit. The sole Command Code
  runtime Taste file remains untracked for boundary cleanup. No `__pycache__` directory remains.
- `STOP:` A separately owner-authorized bounded fix must isolate the validate CLI test from the
  live active task, then rerun focused and full validation before technical review resumes.

## T-M1-03-FIX-01 activation

- Owner instruction: authorize the bounded validate CLI test repair.
- Starting HEAD: `b2a8a96dbccee938123979932e42edb36768705d`.
- Route: `cmdc` / `deepseek/deepseek-v4-flash`, ordinary risk, high effort, 100 turns, automatic
  fallback disabled, and rotation at 150000 reported input tokens.
- Preflight at `2026-08-05T14:06:27Z`: `cmdc` and `wt.exe` resolved, Command Code reported
  authenticated status, and its available-model catalogue included the exact Flash model. No
  credential value was read or persisted.
- Scope: `tests/support.py`, `tests/test_cli.py`, and owner-authorized planning output under
  `docs/**`. The worker must use a temporary authority fixture rather than this repository's live
  task or `HEAD`.
- Validation: `python -B -m unittest tests.test_cli -v`, then full M1 validation with bytecode
  writing disabled.
- Expected commit: `fix: isolate validate cli test`.
- Budgets: zero dependencies, task-side external/provider requests, spend, and destructive actions;
  one repository writer.
- The global `.commandcode/**` and `__pycache__/**` runtime namespaces are ignored. Do not read,
  stage, commit, or use them as task input or output.

## T-M1-03-FIX-01 attempt 1 — superseded runtime boundary

- Visible worker route: `cmdc` / `deepseek/deepseek-v4-flash`, high effort, 100-turn cap, and
  automatic fallback disabled.
- Outcome: before implementation, Command Code created `.commandcode/settings.json` and
  `.commandcode/taste/taste/taste.md`.
- Response: the primary stopped the worker's Node process. No allowed path changed and no commit was
  created. The preserved HEAD remains `b2a8a96dbccee938123979932e42edb36768705d`.
- Owner resolution: ignore `.commandcode/**` as Command Code runtime state across the workflow. Do
  not read, stage, commit, or use it as task input or output. The task may retry with its same route,
  starting commit, budgets, and fallback prohibition.

### Python bytecode resolution

- Owner resolution: ignore `__pycache__/**` as Python runtime state across the workflow. Do not
  read, stage, commit, or use it as task input or output.
- The retry worker recreated `sqorch/__pycache__/` before implementation. It was stopped with no
  allowed-path edit or commit. The owner resolution removes that runtime-only blocker.

### Documentation-output resolution

- Owner resolution: `docs/**` is authorized planning output across the workflow. Documentation may
  be created or updated without an exact task-path claim, while source and test paths remain exact.
- The worker-created owner-input-notifications plan and its parent context-map updates are preserved
  under this authority. They do not count as a T-M1-03-FIX-01 source-boundary violation.

## T-M1-03 and T-M1-03-FIX-01 technical review

- Resulting commits: `b2a8a96dbccee938123979932e42edb36768705d` —
  `feat: preview and audit projects`; `f9f77486e7151eca0cf45145af2e9ccdb40071ec` —
  `fix: isolate validate cli test`.
- Focused validation: `python -B -m unittest tests.test_cli -v` passed 5 tests.
- Full validation: `python -B -m unittest discover -s tests -v` passed 31 tests.
- CLI validation: `python -B -m sqorch --json doctor` passed without creating state.
- Diff boundaries: both implementation commit messages and path allowlists passed. `git diff --check`
  passed for `ed9810c..b2a8a96` and `b2a8a96..f9f7748`.
- Budget evidence: zero dependencies, task-side external/provider requests, spend, and destructive
  actions. Reported worker tokens were unavailable at shutdown; no task crossed the 150000-token
  rotation boundary in the recorded evidence.
- Technical acceptance: passed. Owner acceptance remains required before T-M1-04 or any other
  later task. The uncommitted documentation plans remain proposals only.

### Owner acceptance

- Owner accepted T-M1-03 and T-M1-03-FIX-01 on 2026-08-06.
- This does not activate T-M1-04 through T-M1-07 or any later milestone.

## T-M1-04 activation and pre-launch `ROUTE_UNAVAILABLE`

- Owner instruction: activate T-M1-04 on 2026-08-06.
- Activation timestamp: `2026-08-06T01:38:53Z`.
- Reviewed starting HEAD: `f9f77486e7151eca0cf45145af2e9ccdb40071ec` —
  `fix: isolate validate cli test`.
- Preserved worktree: the existing uncommitted documentation amendments and proposals remain
  unstaged and untouched. `.commandcode/**` and `__pycache__/**` remain ignored runtime namespaces.
- Risk class: `silent-failure` because provenance and approval semantics can appear valid while
  weakening the practice lifecycle contract.
- Client and exact model: `opencode` / `opencode-go/deepseek-v4-pro`.
- Selection reason: the task-defined silent-failure route.
- Escalation condition: not applicable.
- Executable evidence: OpenCode 1.18.13 resolved at
  `C:\Users\DEROK137\AppData\Roaming\npm\opencode.ps1`.
- Authentication evidence: `opencode auth list` reported a configured OpenCode Go API credential.
  No credential value was read or persisted.
- Catalogue evidence: `opencode models opencode-go --verbose` reported exact model
  `opencode-go/deepseek-v4-pro` with status `active` and variant `high`.
- Allowance evidence: unavailable. OpenCode 1.18.13 exposes no current-allowance command, and the
  provider documentation directs the operator to the authenticated console to track current Go
  usage. Authentication and an active catalogue entry do not prove remaining allowance.
- Launch-profile evidence: `opencode run --help` reported `--model`, `--variant`, `--auto`, and
  `--title`. The exact model ID and task block prohibit aliasing, substitution, and fallback.
- Automatic fallback: disabled.
- Visible terminal: `wt.exe` resolved at
  `C:\Users\DEROK137\AppData\Local\Microsoft\WindowsApps\wt.exe`; no terminal or worker launched.
- Starting-HEAD evidence: branch `main` at the reviewed starting commit with no `.git/index.lock`.
- Context-pair amendment: the allowlist adds `sqorch/AGENTS.md`, `sqorch/CLAUDE.md`,
  `tests/AGENTS.md`, and `tests/CLAUDE.md` solely to map new `practices.py` and
  `test_practices.py` files.
- Authority check: `python -B -m sqorch --json validate --project . --task T-M1-04` passed and
  compiled the exact active task, route, budgets, starting commit, validation, and amended paths.
- Baseline validation: `python -B -m unittest discover -s tests -v` passed 31 tests, and
  `git diff --check` passed. These are pre-launch checks, not T-M1-04 technical acceptance.
- Allowed implementation paths: `sqorch/AGENTS.md`, `sqorch/CLAUDE.md`, `sqorch/practices.py`,
  `sqorch/application.py`, `sqorch/cli.py`, `tests/AGENTS.md`, `tests/CLAUDE.md`,
  `tests/test_practices.py`, and `tests/test_cli.py`.
- Expected implementation commit: `feat: validate practice records`.
- Focused validation: `python -m unittest tests.test_practices tests.test_cli -v`.
- Budgets: zero third-party dependencies, zero task-side external/provider requests, zero spend,
  zero destructive actions, one repository writer, 100 worker turns, and rotation at 150000
  reported input tokens.
- Preflight result: `ROUTE_UNAVAILABLE`. T-M1-04 is the sole active task, but no source/test edit or
  worker launch is authorized until current allowance is safely verified. T-M1-05 through T-M1-07
  remain inactive.
- Exact gate: What safe evidence verifies current remaining OpenCode Go allowance for
  `opencode-go/deepseek-v4-pro` without reading or persisting a credential value?

### Owner resolution and launch authority

- Owner response: `go` at `2026-08-06T01:47:51Z`, after receiving the exact allowance gate.
- Resolution: authorize one attributable live attempt on the already-selected exact route. The
  first client response confirms usable allowance; rejection halts as `ROUTE_UNAVAILABLE`.
- Launch authority: one visible foreground OpenCode worker using
  `opencode run --model opencode-go/deepseek-v4-pro --variant high --auto` with the documented
  title and bounded prompt.
- Reverified immediately before launch: OpenCode and `wt.exe` resolve, HEAD remains
  `f9f77486e7151eca0cf45145af2e9ccdb40071ec`, and `.git/index.lock` is absent.
- Automatic fallback remains disabled. Starting commit, amended allowlist, budgets, validation,
  expected commit, and T-M1-05-through-T-M1-07 inactivity remain unchanged.

### OpenCode stop and deliberate CMDc replacement

- Owner clarification: OpenCode Go had reached its monthly credit limit. Use CMDc instead.
- OpenCode result: the process exited with no allowed-path edit and no commit. Starting and
  resulting HEAD remained `f9f77486e7151eca0cf45145af2e9ccdb40071ec`.
- Replacement selected before edits: `cmdc` / `deepseek/deepseek-v4-pro`.
- Replacement preflight timestamp: `2026-08-06T01:53:01Z`.
- Executable and profile: Command Code 1.12.0 resolved at
  `C:\Users\DEROK137\AppData\Roaming\npm\cmdc.ps1`; the pinned profile uses `--trust`,
  `--permission-mode auto-accept`, `--no-auto-update`, exact `--model`, `--effort high`, and
  `--max-turns 100`.
- Authentication and allowance evidence: `cmdc --no-auto-update status` reported authenticated and
  verified status. `cmdc --no-auto-update --list-models` reported 50 models available and included
  exact ID `deepseek/deepseek-v4-pro`.
- Automatic update and fallback: disabled. Aliases and route substitution remain prohibited.
- Starting HEAD and terminal: HEAD remains the reviewed starting commit, `.git/index.lock` is
  absent, and `wt.exe` resolves to the approved visible Windows Terminal surface.
- All paths, budgets, validation, expected commit, and later-task inactivity remain unchanged.

## T-M1-04 implementation result and technical rejection

- Review timestamp: `2026-08-06T02:01:41Z`.
- Starting commit: `f9f77486e7151eca0cf45145af2e9ccdb40071ec`.
- Resulting commit: `1276832ef3423f626ba3eaef09d661a8c110016f` —
  `feat: validate practice records`.
- Exact worker route: `cmdc` / `deepseek/deepseek-v4-pro`, high effort, automatic update and
  fallback disabled.
- Worker boundary: exactly one commit changed six allowed paths: both `sqorch/` context files,
  `sqorch/practices.py`, both `tests/` context files, and `tests/test_practices.py`.
- Focused validation: `python -B -m unittest tests.test_practices tests.test_cli -v` passed 10
  tests.
- Full validation: `python -B -m unittest discover -s tests -v` passed 36 tests.
- CLI and diff validation: `python -B -m sqorch --json doctor` passed, and
  `git diff --check f9f7748..1276832` passed.
- Budget evidence: zero dependencies, task-side external/provider requests, spend, and destructive
  actions. Client-route usage and reported input tokens were unavailable at shutdown.
- Preserved state: all pre-existing uncommitted documentation and ignored runtime artifacts remain
  unstaged and uncommitted.

### Rejected review findings

1. Exact schema mismatch. The M1 design requires `schema`, `proposed_scope`,
   `provenance_reference`, `observed_context`, and `affected_profiles`. The implementation instead
   requires invented alternatives including `scope`, `provenance`, `context`, `outcome`,
   `affected_versions`, and `opted_in_projects`. A canonical design record returns `INVALID_INPUT`.
2. The exact CLI is absent. `python -B -m sqorch --json practices validate missing.json` returns
   parser-level `INVALID_INPUT` because `practices` is not a command. No JSON path is read through
   the required application and CLI layers.
3. Approval enforcement is incomplete. The design requires non-empty approving authority for
   `ADOPTED`, `REJECTED`, and `DEPRECATED`; direct probes show `REJECTED` and `DEPRECATED` are
   accepted without authority.
4. The added tests encode the implementation's substituted vocabulary and omit the exact CLI and
   two lifecycle approval cases, so green focused and full suites do not exercise the canonical
   contract.

- Technical acceptance: rejected. No source or test fix is authorized in this review step.
- Exact `STOP:` May a separately bounded T-M1-04-FIX-01 correct the exact practice field schema,
  implement `practices validate PATH` through application and CLI layers, enforce approving
  authority for all three terminal decision states, and add literal failing assertions for those
  defects while retaining the same zero-dependency and no-storage boundary?
- T-M1-05 through T-M1-07 remain inactive.

## T-M1-04-FIX-01 activation

- Owner resolution: authorize the exact bounded fix on 2026-08-06.
- Activation timestamp: `2026-08-06T02:05:07Z`.
- Reviewed starting HEAD: `1276832ef3423f626ba3eaef09d661a8c110016f` —
  `feat: validate practice records`.
- Objective: correct only the rejected schema vocabulary, exact `practices validate PATH`
  application/CLI path, and approving-authority enforcement for `ADOPTED`, `REJECTED`, and
  `DEPRECATED`, with literal failing assertions for each defect.
- Allowed paths: `sqorch/practices.py`, `sqorch/application.py`, `sqorch/cli.py`,
  `tests/test_practices.py`, and `tests/test_cli.py`.
- No context-pair path is required because the fix adds, moves, and removes no file.
- Expected commit: `fix: enforce practice record contract`.
- Focused validation: `python -B -m unittest tests.test_practices tests.test_cli -v`.
- Risk class: `silent-failure` because the rejected implementation passed tests while changing the
  meaning of an exact schema and immutable lifecycle contract.
- Client and exact model: `cmdc` / `deepseek/deepseek-v4-pro`.
- Selection reason: retain the accepted T-M1-04 silent-failure route for its bounded repair.
- Escalation condition: not applicable.
- Executable and profile: Command Code 1.12.0 resolved at
  `C:\Users\DEROK137\AppData\Roaming\npm\cmdc.ps1`; the pinned profile uses `--trust`,
  `--permission-mode auto-accept`, `--no-auto-update`, exact `--model`, `--effort high`, and
  `--max-turns 100`.
- Authentication and allowance evidence: `cmdc --no-auto-update status` reported authenticated and
  verified status. `cmdc --no-auto-update --list-models` reported 50 models available and included
  exact ID `deepseek/deepseek-v4-pro`.
- Automatic update and fallback: disabled. Aliases and route substitution are prohibited.
- Starting HEAD and terminal: HEAD matched the reviewed fix base, `.git/index.lock` was absent,
  and `wt.exe` resolved to the approved visible Windows Terminal surface.
- Starting token rotation use: `0 / 150000` for the new worker session.
- Budgets: zero dependencies, task-side external/provider requests, spend, and destructive actions;
  one repository writer and 100 worker turns.
- Preserved state: all existing uncommitted documentation and ignored runtime artifacts remain
  unstaged and are not task input or output.
- Activation result: T-M1-04-FIX-01 is active. T-M1-05 through T-M1-07 remain inactive.

## T-M1-04-FIX-01 implementation result and technical rejection

- Review timestamp: `2026-08-06T02:14:09Z`.
- Starting commit: `1276832ef3423f626ba3eaef09d661a8c110016f`.
- Resulting commit: `07985c16585f1222558292bf8608b0bc7f53a186` —
  `fix: enforce practice record contract`.
- Exact worker route: `cmdc` / `deepseek/deepseek-v4-pro`, high effort, automatic update and
  fallback disabled.
- Worker boundary: exactly one commit changed the five allowed paths and no context map because no
  file was added, moved, or removed.
- Focused validation: `python -B -m unittest tests.test_practices tests.test_cli -v` passed 17
  tests.
- Full validation: `python -B -m unittest discover -s tests -v` passed 43 tests.
- CLI and diff validation: `python -B -m sqorch --json doctor` passed, the missing-file CLI probe
  returned `INVALID_INPUT` with exit 2, and `git diff --check 1276832..07985c1` passed.
- Contract probes: the canonical schema returned `CANDIDATE`, substituted fields returned
  `INVALID_INPUT`, and `ADOPTED`, `REJECTED`, and `DEPRECATED` without authority each returned
  `INVALID_INPUT`. A non-object JSON root returned the correct JSON error envelope with exit 2.
- Budget evidence: zero dependencies, task-side external/provider requests, spend, and destructive
  actions. Client-route usage and reported input tokens were unavailable at shutdown.
- Preserved state: all pre-existing uncommitted documentation and ignored runtime artifacts remain
  unstaged and uncommitted.

### Rejected review finding

1. Invalid UTF-8 fails open at the CLI trust boundary. A temporary practice file containing byte
   `0xff` raises an uncaught `UnicodeDecodeError`, exits 1, emits no JSON envelope, and writes a
   traceback to stderr. The UTF-8 JSON input contract and `INVALID_INPUT` vocabulary require a
   deterministic exit-2 error envelope instead.

- Technical acceptance: rejected. No source or test repair is authorized in this review step.
- Exact `STOP:` May a separately bounded T-M1-04-FIX-02 change only `sqorch/application.py` and
  `tests/test_cli.py` to map invalid UTF-8 practice input to `INVALID_INPUT` exit 2, add one literal
  failing CLI assertion, and produce exactly `fix: reject invalid practice encoding` under the
  same CMDc Pro route and zero-dependency/no-storage budgets?
- T-M1-05 through T-M1-07 remain inactive.

## T-M1-04-FIX-02 activation

- Owner resolution: authorize the exact two-file encoding repair on 2026-08-06.
- Activation timestamp: `2026-08-06T02:16:00Z`.
- Reviewed starting HEAD: `07985c16585f1222558292bf8608b0bc7f53a186` —
  `fix: enforce practice record contract`.
- Objective: map invalid UTF-8 practice input to `INVALID_INPUT` exit 2 with the standard JSON
  envelope and no traceback, while preserving every accepted FIX-01 behavior.
- Allowed paths: `sqorch/application.py` and `tests/test_cli.py`.
- Expected commit: `fix: reject invalid practice encoding`.
- Focused validation: `python -B -m unittest tests.test_cli -v`.
- Risk class: `silent-failure` because this is fail-closed validation at the practice-file trust
  boundary.
- Client and exact model: `cmdc` / `deepseek/deepseek-v4-pro`.
- Selection reason: retain the accepted practice-contract repair route.
- Escalation condition: not applicable.
- Executable and profile: Command Code 1.12.0 resolved at
  `C:\Users\DEROK137\AppData\Roaming\npm\cmdc.ps1`; the pinned profile uses `--trust`,
  `--permission-mode auto-accept`, `--no-auto-update`, exact `--model`, `--effort high`, and
  `--max-turns 100`.
- Authentication and allowance evidence: `cmdc --no-auto-update status` reported authenticated and
  verified status. `cmdc --no-auto-update --list-models` reported 50 models available and included
  exact ID `deepseek/deepseek-v4-pro`.
- Automatic update and fallback: disabled. Aliases and route substitution are prohibited.
- Starting HEAD and terminal: HEAD matched the reviewed fix base, `.git/index.lock` was absent, no
  other matching worker existed, and `wt.exe` resolved to the approved visible terminal surface.
- Starting token rotation use: `0 / 150000` for the new worker session.
- Budgets: zero dependencies, task-side external/provider requests, spend, and destructive actions;
  one repository writer and 100 worker turns.
- Preserved state: all existing uncommitted documentation and ignored runtime artifacts remain
  unstaged and are not task input or output.
- Activation result: T-M1-04-FIX-02 is active. T-M1-05 through T-M1-07 remain inactive.

## T-M1-04-FIX-02 implementation result

- Starting commit: `07985c16585f1222558292bf8608b0bc7f53a186`.
- Result commit: `ecbb576a7fae7efaaf82bb7b8208eadd8e9c892a` —
  `fix: reject invalid practice encoding`.
- Route used: one visible foreground CMDc worker with exact model
  `deepseek/deepseek-v4-pro`; automatic update and fallback remained disabled.
- Changed paths: `sqorch/application.py` and `tests/test_cli.py` only.
- Root correction: the existing practice-file read boundary now maps invalid UTF-8 decoding to the
  standard `INVALID_INPUT` application error without changing practice-domain rules.
- Regression assertion: invalid byte `0xff` returns exit 2, emits JSON error code `INVALID_INPUT`,
  and leaves stderr empty.
- Focused validation: `python -B -m unittest tests.test_cli -v` passed 9 tests.
- Full validation: `python -B -m unittest discover -s tests -v` passed 44 tests.
- Runtime validation: `python -B -m sqorch --json doctor` passed.
- Boundary validation: exact message, one commit, two allowed paths, clean claimed paths, full diff,
  and `git diff --check` passed.
- Budgets: zero dependencies, task-side external/provider requests, spend, storage changes, and
  destructive actions; one visible repository writer.
- Technical acceptance: passed for T-M1-04, T-M1-04-FIX-01, and T-M1-04-FIX-02 as one corrective
  chain. The owner acceptance below resolves this gate.

## T-M1-04 owner acceptance and T-M1-05 activation

- Owner instruction: `T-M1-05 accept` on 2026-08-06 is recorded as acceptance of the completed
  T-M1-04 corrective chain and activation of the next unimplemented task, T-M1-05.
- Accepted T-M1-04 chain result: `ecbb576a7fae7efaaf82bb7b8208eadd8e9c892a`.
- Activation timestamp: `2026-08-06T02:28:26Z`.
- Reviewed starting HEAD: `ecbb576a7fae7efaaf82bb7b8208eadd8e9c892a` —
  `fix: reject invalid practice encoding`.
- Objective: implement the exact SQLite schema version one with `projects` and `locks`, idempotent
  registration, conflicting-registration failure, holder-bound lock acquisition/release, and
  `run --dry-run` integration without launching a client.
- Allowed paths: `sqorch/AGENTS.md`, `sqorch/CLAUDE.md`, `sqorch/state.py`,
  `sqorch/application.py`, `sqorch/cli.py`, `tests/AGENTS.md`, `tests/CLAUDE.md`,
  `tests/test_state.py`, and `tests/test_cli.py`.
- Context-pair reason: `state.py` and `test_state.py` are new files and must be added to their
  nearest file maps. The context paths cannot widen source or test behavior.
- Expected commit: `feat: add project registry and locks`.
- Focused validation: `python -m unittest tests.test_state tests.test_cli -v`.
- Risk class: `silent-failure` because identity or lock defects could silently permit concurrent
  repository writers.
- Client and exact model: `cmdc` / `deepseek/deepseek-v4-pro`.
- Selection reason: the task requires the Pro silent-failure route. OpenCode Go is unavailable
  because the owner reported its monthly credit exhausted, so the prior owner-directed CMDc
  replacement remains deliberate and recorded before edits.
- Escalation condition: not applicable.
- Executable and profile: Command Code 1.12.0 resolved through documented shim
  `C:\Users\DEROK137\AppData\Roaming\npm\cmdc.ps1`; the pinned profile uses `--trust`,
  `--permission-mode auto-accept`, `--no-auto-update`, exact `--model`, `--effort high`, and
  `--max-turns 100`.
- Authentication and allowance evidence: `cmdc --no-auto-update status` reported authenticated and
  verified status. `cmdc --no-auto-update --list-models` included exact ID
  `deepseek/deepseek-v4-pro`.
- Automatic update and fallback: disabled. Aliases and route substitution are prohibited.
- Starting HEAD and terminal: HEAD matched the reviewed base, `.git/index.lock` was absent, no
  other T-M1-05 worker existed, and `wt.exe` resolved to the approved visible terminal surface.
- Starting token rotation use: `0 / 150000` for the new worker session.
- Budgets: zero dependencies, task-side external/provider requests, spend, and destructive actions;
  one repository writer and 100 worker turns.
- Preserved state: all existing uncommitted documentation and ignored runtime artifacts remain
  unstaged and are not task input or output.
- Activation result: T-M1-05 is active. T-M1-06 and T-M1-07 remain inactive.

## T-M1-05 implementation result and technical rejection

- Starting commit: `ecbb576a7fae7efaaf82bb7b8208eadd8e9c892a`.
- Resulting commit: `df6438ccf8a1031008381d4c567377de536ecd92` —
  `feat: add project registry and locks`.
- Exact route: one visible foreground CMDc worker using `deepseek/deepseek-v4-pro`, high effort,
  with automatic update and fallback disabled.
- Commit boundary: exactly one commit changed the nine allowed paths, including both required
  context-pair maps. The message, path allowlist, clean claimed paths, full diff, and
  `git diff --check` passed.
- Focused validation: `python -B -m unittest tests.test_state tests.test_cli -v` ran 18 tests with
  17 passing and one failing.
- Full validation: `python -B -m unittest discover -s tests -v` ran 53 tests with 52 passing and
  one failing.
- Runtime validation: `python -B -m sqorch --json doctor` passed.
- Budget evidence: zero dependencies, task-side external/provider requests, spend, destructive
  actions, and real launches from application code. Reported worker tokens were unavailable.

### Rejected review findings

1. Exact schema and CLI contract mismatch. The design requires project columns `canonical_path`,
   `display_name`, `policy_profile`, and UTC TEXT `added_at_utc`, plus UTC TEXT
   `acquired_at_utc` in locks. The implementation substitutes `project_path`, `profile_path`, and
   REAL timestamps and omits required `--name` and `--profile` arguments.
2. Registration is not idempotent through the implemented application/CLI path. Two identical
   `project add` invocations return exit 0 and then `STATE_CONFLICT` because each invocation passes
   a new timestamp into the stored-value comparison.
3. Dry-run validation is coupled to live repository authority. After the implementation commit,
   the focused and full suites fail because their T-M1-05 invocation no longer matches the task's
   starting HEAD. The test must own a temporary authority fixture as the existing validate CLI test
   does.
4. Authority errors fail open at the CLI boundary. Direct `run --dry-run` probing returns exit 1,
   empty stdout, and an uncaught `AuthorityError` traceback rather than the required exit-3
   `AUTHORITY_DRIFT` JSON envelope.

- Technical acceptance: rejected. No source or test repair is authorized in this review step.
- Exact `STOP:` May a separately bounded T-M1-05-FIX-01 change only `sqorch/state.py`,
  `sqorch/application.py`, `sqorch/cli.py`, `tests/test_state.py`, and `tests/test_cli.py` to enforce
  the exact schema and CLI, restore idempotent registration, isolate dry-run authority fixtures,
  map authority failures to the standard gate, and produce exactly
  `fix: enforce registry and dry-run contract` under the same CMDc Pro route and existing budgets?
- T-M1-06 and T-M1-07 remain inactive.

## T-M1-05-FIX-01 activation

- Owner resolution: authorize the exact bounded fix on 2026-08-06.
- Activation timestamp: `2026-08-06T03:01:12Z`.
- Reviewed starting HEAD: `df6438ccf8a1031008381d4c567377de536ecd92` —
  `feat: add project registry and locks`.
- Objective: enforce the exact registry schema and `project add` CLI, restore idempotent
  registration, isolate dry-run authority fixtures, and map authority errors to the existing JSON
  gate without changing the holder-bound lock or no-launch contracts.
- Allowed paths: `sqorch/state.py`, `sqorch/application.py`, `sqorch/cli.py`,
  `tests/test_state.py`, and `tests/test_cli.py`.
- Context-pair update: none required because the fix adds, moves, and removes no file.
- Expected commit: `fix: enforce registry and dry-run contract`.
- Focused validation: `python -B -m unittest tests.test_state tests.test_cli -v`.
- Risk class: `silent-failure` because the fix repairs identity, storage-schema, and authority-gate
  behavior that previously appeared plausible while violating the canonical contract.
- Client and exact model: `cmdc` / `deepseek/deepseek-v4-pro`.
- Selection reason: retain the task's accepted Pro silent-failure route for its bounded correction.
- Escalation condition: not applicable.
- Executable and profile: Command Code resolved at
  `C:\Users\DEROK137\AppData\Roaming\npm\cmdc.ps1`; the pinned profile uses `--trust`,
  `--permission-mode auto-accept`, `--no-auto-update`, exact `--model`, `--effort high`, and
  `--max-turns 100`.
- Authentication and allowance evidence: `cmdc --no-auto-update status` exited successfully with
  verified authentication. `cmdc --no-auto-update --list-models` exited successfully and included
  exact ID `deepseek/deepseek-v4-pro`.
- Automatic update and fallback: disabled. Aliases and route substitution are prohibited.
- Starting HEAD and terminal: HEAD matched the rejected implementation commit, `.git/index.lock`
  was absent, no other matching worker existed, all five claimed paths were clean, and `wt.exe`
  resolved to the approved visible terminal surface.
- Starting token rotation use: `0 / 150000` for the new worker session.
- Budgets: zero dependencies, task-side external/provider requests, spend, and destructive actions;
  one repository writer and 100 worker turns.
- Preserved state: all existing uncommitted documentation and ignored runtime artifacts remain
  unstaged and are not task input or output.
- Activation result: T-M1-05-FIX-01 is active. T-M1-06 and T-M1-07 remain inactive.

## T-M1-05-FIX-01 implementation result and technical rejection

- Starting commit: `df6438ccf8a1031008381d4c567377de536ecd92`.
- Resulting commit: `2e99362c11eba4486ed270909762f29f7980f355` —
  `fix: enforce registry and dry-run contract`.
- Exact route: one visible foreground CMDc worker using `deepseek/deepseek-v4-pro`, high effort,
  with automatic update and fallback disabled.
- Commit boundary: exactly one commit changed the five allowed paths. The message, path allowlist,
  clean claimed paths, full diff, and `git diff --check` passed.
- Focused validation: `python -B -m unittest tests.test_state tests.test_cli -v` passed 23 tests.
- Full validation: `python -B -m unittest discover -s tests -v` passed 58 tests.
- Runtime validation: `python -B -m sqorch --json doctor` passed.
- Passing direct probes: exact schema names/types, UTC text timestamp format, required `project add`
  CLI, repeated-registration equality and timestamp preservation, changed-name conflict, isolated
  fresh-project dry-run, exit-3 JSON authority gate, empty stderr, and lock cleanup.
- Budget evidence: zero dependencies, task-side external/provider requests, spend, destructive
  actions, and real launches from application code. Reported worker tokens were unavailable.

### Rejected review finding

1. Registered-project dry-run conflicts. A temporary project successfully registered through the
   exact CLI then returns exit 4 `STATE_CONFLICT` from `run --dry-run`. The run path supplies the
   task ID and synthetic default profile to `register_project`, so it conflicts with the immutable
   display name and policy profile instead of locating the existing canonical path. The design
   requires dry-run to register or locate the project. No lock row remains after the failure.

- Technical acceptance: rejected. No source or test repair is authorized in this review step.
- Exact `STOP:` May a separately bounded T-M1-05-FIX-02 change only `sqorch/state.py`,
  `sqorch/application.py`, `tests/test_state.py`, and `tests/test_cli.py` to locate an existing
  canonical registration during dry-run, preserve fresh-project behavior and all accepted FIX-01
  contracts, and produce exactly `fix: locate registered dry-run projects` under the same CMDc Pro
  route and existing budgets?
- T-M1-06 and T-M1-07 remain inactive.

## T-M1-05-FIX-02 activation

- Owner resolution: authorize the exact bounded fix on 2026-08-06.
- Activation timestamp: `2026-08-06T03:20:54Z`.
- Reviewed starting HEAD: `2e99362c11eba4486ed270909762f29f7980f355` —
  `fix: enforce registry and dry-run contract`.
- Objective: locate an existing immutable project registration during dry-run instead of
  re-registering it with synthetic identity values, while preserving fresh-project dry-run and all
  accepted FIX-01 contracts.
- Allowed paths: `sqorch/state.py`, `sqorch/application.py`, `tests/test_state.py`, and
  `tests/test_cli.py`.
- Context-pair update: none required because the fix adds, moves, and removes no file.
- Expected commit: `fix: locate registered dry-run projects`.
- Focused validation: `python -B -m unittest tests.test_state tests.test_cli -v`.
- Risk class: `silent-failure` because incorrect identity lookup can block an apparently valid
  dry-run or silently rewrite project registration semantics.
- Client and exact model: `cmdc` / `deepseek/deepseek-v4-pro`.
- Selection reason: retain the accepted Pro silent-failure route for the final bounded integration
  correction.
- Escalation condition: not applicable.
- Executable and profile: Command Code resolved at
  `C:\Users\DEROK137\AppData\Roaming\npm\cmdc.ps1`; the pinned profile uses `--trust`,
  `--permission-mode auto-accept`, `--no-auto-update`, exact `--model`, `--effort high`, and
  `--max-turns 100`.
- Authentication and allowance evidence: `cmdc --no-auto-update status` exited successfully with
  verified authentication. `cmdc --no-auto-update --list-models` exited successfully and included
  exact ID `deepseek/deepseek-v4-pro`.
- Automatic update and fallback: disabled. Aliases and route substitution are prohibited.
- Starting HEAD and terminal: HEAD matched the FIX-01 commit, `.git/index.lock` was absent, no
  other matching worker existed, all four claimed paths were clean, and `wt.exe` resolved to the
  approved visible terminal surface.
- Starting token rotation use: `0 / 150000` for the new worker session.
- Budgets: zero dependencies, task-side external/provider requests, spend, and destructive actions;
  one repository writer and 100 worker turns.
- Preserved state: all existing uncommitted documentation and ignored runtime artifacts remain
  unstaged and are not task input or output.
- Activation result: T-M1-05-FIX-02 is active. T-M1-06 and T-M1-07 remain inactive.

## T-M1-05-FIX-02 implementation result and technical acceptance

- Starting commit: `2e99362c11eba4486ed270909762f29f7980f355` —
  `fix: enforce registry and dry-run contract`.
- Resulting commit: `2e982eba469e66d87ad491fe113a42a8e9da88fc` —
  `fix: locate registered dry-run projects`.
- Exact route: one visible foreground CMDc worker using `deepseek/deepseek-v4-pro`, high effort,
  with automatic update and fallback disabled.
- Commit boundary: exactly one commit with the reviewed FIX-01 parent changed only
  `sqorch/application.py`, `sqorch/state.py`, and `tests/test_cli.py`. All are inside the four-path
  allowlist; no file was added, moved, or removed, so no context-pair update was needed.
- Diff review: exact message, full diff, `git show --check`, clean claimed paths, and a scan for
  provider, network, client, terminal, subprocess, and fallback additions passed.
- Focused validation: `python -B -m unittest tests.test_state tests.test_cli -v` passed 24 tests.
- Full validation: `python -B -m unittest discover -s tests -v` passed 59 tests.
- Runtime validation: `python -B -m sqorch --json doctor` returned `ok: true`.
- Independent black-box validation: `project add` returned 0; registered-project `run --dry-run`
  returned 0; `launch_performed` and `automatic_fallback` were false; canonical path, display name,
  policy profile, and added timestamp were unchanged; zero locks remained; fresh-project dry-run
  returned 0; authority drift returned exit 3 with `AUTHORITY_DRIFT` JSON and empty stderr.
- Budget evidence: zero dependencies, task-side external/provider requests, spend, destructive
  actions, and real launches from application code. Reported worker tokens were unavailable.
- Boundary closure: the foreground worker was stopped after commit, no matching CMDc worker
  remains, HEAD is the exact result commit, the index is clean, and preserved documentation and
  ignored runtime artifacts remain unstaged.
- Technical acceptance: passed. T-M1-05, T-M1-05-FIX-01, and T-M1-05-FIX-02 are technically
  accepted as one corrective chain. Owner acceptance remains required; T-M1-06 and T-M1-07 remain
  inactive.

## Planning verification

- Design responsibility graph matches the source projection: passed.
- Packet fields for authority, paths, budgets, stops, validation, evidence, and acceptance: passed.
- Build decisions for runtime, CLI, manifest, paths, projects, practices, SQLite, locks, and tests:
  closed.
- Six implementation task blocks plus owner-gated T-M1-07: present and inactive.
- New plan directory context pair and parent spec/plan file maps: passed.
- `rg --files`: 29 documentation files.
- `git diff --check 0c6351e..44428ce`: passed.
- Implementation-artifact guard: passed; no `sqorch/`, `tests/`, or `pyproject.toml` exists.
- External calls, spend, dependencies, client launches, and delegated agents used: `0`.
- The extra blank line at the end of `CLIENT-EXECUTION.md`: removed.

## Carry-forward

- Replace each `ACTIVATION_REQUIRED` starting commit only after its owner-authorized activation
  preflight passes or an explicit owner exception is recorded.
- Reverify the exact proposed route and allowance immediately before every launch; never fall back.
- M2A adoption mutation, M2B real launch/review, packaging, migrations, and stale-lock recovery stay
  parked.
