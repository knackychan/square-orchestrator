# M1 Dry-Run Foundation State

State records evidence; it does not grant authority.

## Cold start

Read root `AGENTS.md`, `CLAUDE.md`, `SPEC.md`, `STATUS.md`, and `HANDOVER.md`. Read
`CLIENT-EXECUTION.md` before any route or client work. Then read this directory's `PACKET.md`,
`BUILD.md`, `BUILD-TASKS.md`, and `STATE.md`.

## Planning position

| Field | Value |
|---|---|
| Current activity | T-M1-01 technically complete; implementation closed |
| Planning authorization | Owner instruction on 2026-08-05 |
| Starting HEAD | `0c6351e` — `docs: record client playbook context` |
| Planning commit | `44428ce` — `docs: plan m1 dry-run foundation` |
| Verified file count | 38 tracked files after T-M1-01 |
| Implementation authority | none; T-M1-01 authority consumed |
| Delegated agent authority | none |
| External calls / spend | `0 / $0` |
| Dependencies | `0` |
| Proposed implementation tasks | T-M1-01 complete; T-M1-02 through T-M1-07 inactive |
| Route checks | OpenCode allowance deferred by explicit owner exception; no worker launch |
| Open `STOP:` items | none; allowance question resolved by owner deferral for T-M1-01 |
| Owner gate | Review T-M1-01 and separately activate T-M1-02 if desired |

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
