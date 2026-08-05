# M1 Packet — Dry-Run Foundation

## Authority state

This packet is being authored under the owner's 2026-08-05 planning authorization. It is not an
active implementation packet. No task below may edit source or tests until root `STATUS.md`
activates that exact task and records its starting commit and route preflight.

## Objective

Implement the smallest deterministic `python -m sqorch` slice that proves project preview and
audit, practice validation, project registration, authority hashing, manifest compilation, exact
route preview, single-writer locking, and stable human/JSON output without launching a client.

## Measurable outcome

- Python 3.12+ standard-library implementation with no package or dependency metadata.
- The responsibility graph and exact interfaces in the M1 design are honored.
- New-project preview and existing-project audit make no target-repository changes.
- A canonical task block compiles to hash-bound stable JSON or fails closed.
- Practice records validate without being stored or promoted.
- SQLite registration and one-writer exclusion pass two-connection tests.
- `run --dry-run` reports an exact route and `launch_performed: false`.
- Human and `--json` modes cover every M1 command.
- No real terminal, client, provider, network, or task validation command is launched.

## Canonical inputs

1. Root `AGENTS.md`, `CLAUDE.md`, `SPEC.md`, `STATUS.md`, and `HANDOVER.md`.
2. Root `CLIENT-EXECUTION.md` for route and terminal invariants.
3. `../../specs/2026-08-05-square-orchestrator-design.md`.
4. `../../specs/2026-08-05-m1-dry-run-foundation-design.md`.
5. This packet, `BUILD.md`, and `BUILD-TASKS.md`.

Lower inputs cannot widen higher authority. `STATE.md` records evidence only.

## Entry conditions for implementation

- The owner accepts the completed M1 plan.
- `STATUS.md` activates one exact `T-M1-*` task.
- The current worktree and starting commit are recorded.
- The task's exact client/model route is adopted and reverified through a safe live preflight.
- The visible terminal surface, token count, and zero-call/zero-spend budgets are recorded.
- No other writer owns this repository.

## Allowed implementation paths

- Root `AGENTS.md`, `CLAUDE.md`, `README.md`, `SPEC.md`, and `STATUS.md` only where a task names them.
- `sqorch/` and its required context pair.
- `tests/` and its required context pair.
- This packet's `STATE.md` as the evidence ledger.

The packet, build guide, task list, design specs, root handover, and client playbook are read-only
during implementation unless a separate amendment task is activated.

## Forbidden changes and actions

- `pyproject.toml`, dependency files, installers, executables, virtual environments, or caches.
- Any source directory beyond root `sqorch/` and `tests/`.
- Real target-project creation, adoption mutation, task execution, Git commit automation, client
  launch, terminal launch, provider call, network call, or background process.
- Direct model-provider APIs, credentials, credential reads, raw prompts, or secret persistence.
- Automatic fallback, aliases, route substitution, stale-lock deletion, migrations, plugin layers,
  factories, interfaces with one implementation, or speculative shared utilities.
- Changes under Sticker Generator or any repository used as a read-only fixture source.
- Weakening an assertion to make a task pass.

## Budgets

| Budget | Ceiling |
|---|---|
| Third-party dependencies | `0` |
| External/provider requests | `0` |
| Spend | `$0` |
| Destructive actions | `0` |
| Concurrent repository writers | `1` |
| Worker turn limit when activated | `100` per attempt |
| Worker rotation | `150000` reported input tokens, checked between tasks |

Tests may write only inside `tempfile`-owned directories. An explicitly supplied temporary SQLite
path is the only runtime state used by packet validation.

## Route policy

`BUILD-TASKS.md` records dated proposed routes from `CLIENT-EXECUTION.md`. They are not current
availability evidence and do not authorize launch. At activation, each route must be adopted in
`STATUS.md` or `STATE.md`, checked in its own live authenticated catalogue, checked for allowance,
and launched only in an approved visible foreground terminal with fallback disabled.

If safe catalogue or allowance verification is unavailable, record `ROUTE_UNAVAILABLE` or `STOP:`;
do not substitute another route.

## Ordered work

Execute `BUILD-TASKS.md` sequentially. One write task and one exact commit are allowed at a time.
T-M1-07 is owner-gated and cannot run merely because technical validation passes.

## Validation

Run from the repository root:

```powershell
$env:PYTHONPATH = (Resolve-Path '.')
python -m unittest discover -s tests -v
python -m sqorch --json doctor
git diff --check <starting-head>..HEAD
git status --short
```

Focused commands and literal assertions are in `BUILD-TASKS.md`. Validation must also prove:

- no third-party import;
- no network, client, terminal, or background-process call;
- stable JSON across two identical invocations after normalizing documented timestamps;
- no write during preview, audit, practice validation, authority validation, or `doctor`;
- lock exclusion and holder-matched release; and
- changed paths remain within the active task.

## Stop conditions

Record `STOP:` and make no invented decision when:

- root authority conflicts with this packet or its design;
- a task block is missing, duplicated, ambiguous, or would require free-form model interpretation;
- a path boundary cannot be proved safe;
- a command would need a dependency, network call, client launch, hidden terminal, or credential;
- the selected route cannot be verified exactly;
- a supposedly failing assertion passes before implementation;
- unexpected files or concurrent edits appear; or
- acceptance would require describing planned behavior as shipped.

## Evidence destination

`STATE.md` records starting/resulting commits, routes, reported input tokens, checks, budgets,
changed paths, findings, fixes, and gates. It never grants authority.

## Acceptance authority

The primary session records technical completion after full boundary review. The project owner
separately accepts M1 through T-M1-07. Acceptance does not activate M2A or a real client launcher.
