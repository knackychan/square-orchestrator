# Agent Instructions — Square Orchestrator

## Project state

This repository is in documentation and planning mode. Do not create application code, package
metadata, dependencies, installers, extensions, runtime databases, or launch delegated agents
unless `STATUS.md` explicitly names an active implementation milestone.

The project owner may activate an exact milestone at any time. Agents may not infer implementation
authority from a completed plan, an unblocked dependency, or the existence of a task list.

## Required reading order

1. This file.
2. The nearest `AGENTS.md` in the directory being changed.
3. `SPEC.md`.
4. `STATUS.md`.
5. `HANDOVER.md`.
6. `CLIENT-EXECUTION.md` when the task selects, launches, monitors, or reviews an agent client.
7. The active packet named by `STATUS.md`.

`CLAUDE.md` files carry equivalent context for Claude-based sessions. If a context pair conflicts,
stop and report the conflict rather than choosing one silently.

## Mandatory visible client workflow

At the start of every activated implementation, fix, or delegated review task, the primary session
must read `CLIENT-EXECUTION.md`, record one exact client/model route, complete its preflight, and
launch the assigned worker as the foreground process of a visible VS Code terminal or Windows
Terminal tab/window before worker edits begin. The primary session does not implement the worker
task inline unless `STATUS.md` records an explicit owner exception for that exact task.

| Client | Required command type |
|---|---|
| Command Code | `cmdc ... "<bounded-prompt>"` |
| OpenCode | `opencode run ... "<bounded-prompt>"` |
| Claude Code | `claude ... "<bounded-prompt>"` |
| Codex CLI | `codex exec ... "<bounded-prompt>"` |

The exact flags and launch profiles live in `CLIENT-EXECUTION.md`. Hidden, minimized, detached,
background, cloud, internal sub-agent, or silent-fallback execution is not an equivalent route. If
the approved visible surface or exact route is unavailable, record `STOP:` or `ROUTE_UNAVAILABLE`
and do not edit. Planning, packet authoring, ordinary read-only questions, and primary boundary
review do not require launching a worker unless their active packet explicitly assigns one.

When the primary opens a new Windows Terminal surface with native `wt.exe`, it must use the
argument-array wrapper in `CLIENT-EXECUTION.md`. A bounded prompt passed through `wt.exe` must
contain no semicolon characters: Windows Terminal interprets them as action separators even when
the prompt is one array element, which can open extra tabs or commands. Verify the guard and one
new worker surface before allowing edits.

## Global invariants

- A target repository remains the authority for its own scope, budgets, accepted results, and
  permitted routes. The orchestrator never grants project authority.
- Human-readable authority documents remain canonical. A machine manifest is a hash-bound
  execution projection and cannot widen them.
- Core scheduling and policy enforcement are deterministic. No model decides whether a task is
  authorized, whether paths overlap, or whether a budget may be exceeded.
- Cross-project learning is opt-in and provenance-bearing. Project content is not copied into a
  global catalogue merely because the orchestrator can read it.
- A learned practice is a candidate until reviewed. The application may propose a template,
  workflow, guardrail, refactor, skill, or tool; it may not adopt, install, or impose one silently.
- Architecture generation starts from responsibilities and dependency direction. It must avoid
  both monoliths and speculative one-function-per-file fragmentation.
- Shared components or libraries require genuine consumers and one-directional ownership; a
  generated project must not create speculative `common`, `utils`, or plugin layers.
- Every run selects one exact client/model route before launch. Aliases, silent substitution, and
  automatic fallback are prohibited.
- A route change is deliberate, justified, and recorded before the replacement edits.
- `STOP:` is a first-class halted state. It is resolved only from repository authority or by the
  owner; another agent must not invent the answer.
- No credential value may enter source control, prompts, manifests, logs, databases, or receipts.
- `.commandcode/**` is an ignored Command Code runtime namespace. Its contents are never task input
  or output and must not be read, staged, committed, or treated as an unexpected-path blocker.
- `__pycache__/**` is ignored Python runtime state. It is never task input or output and must not be
  read, staged, committed, or treated as an unexpected-path blocker.
- `docs/**` is owner-authorized planning output. A worker may create or update documentation there
  without an exact task-path claim, provided source and test changes still stay within the task's
  explicit allowed paths.
- The core launches installed clients; it does not call model-provider APIs directly.
- Same-repository write concurrency defaults to one. Read-only work against immutable commits may
  run in parallel. Worktree concurrency is a later opt-in feature, not a default assumption.
- A project that requires visible foreground terminals must fail closed if no approved visible
  surface is available.
- Plans and proposed interfaces must never be described as shipped behavior.
- Every project-owned directory contains both `AGENTS.md` and `CLAUDE.md`. Adding, moving, or
  removing a file requires updating the nearest context pair's file map.

## Repository map

| Path | Purpose |
|---|---|
| `README.md` | Short project introduction and current boundary |
| `SPEC.md` | Canonical planned product and safety contract |
| `STATUS.md` | Current authority and milestone state |
| `HANDOVER.md` | Cold-start operating workflow; never authority |
| `CLIENT-EXECUTION.md` | Dated client routes, launch profiles, and lifecycle reference |
| `archive/` | Documents, plans, source, and tooling from the frozen pre-fork line, sorted by type and retained for trace/history only; has its own context pair |
| `docs/` | Project documentation, with its own context pair |
| `docs/superpowers/` | Specs and execution plans, with its own context pair |
| `docs/superpowers/specs/` | Design specifications |
| `docs/superpowers/plans/` | Bounded execution packets |

The repository is now mid-pivot to a maintained downstream fork of Agent Orchestrator (see
`docs/superpowers/plans/2026-08-09-fork-agent-orchestrator/`). The former implementation and
toolchain trees are grouped under `archive/src/` and `archive/tooling/`; consult the active fork
branch and its own `docs/square/**` governance instead.

The repository carries pinned toolchain declarations (.NET SDK, Node, pnpm) and central package
management. No external package dependency, extension, installer, or runtime database is admitted
before an owner-approved task records its license/security/architectural review.

## Work protocol

No delegated work begins without:

1. a packet defining objective, authority, allowed and forbidden changes, budgets, validation,
   stops, and acceptance;
2. a build guide recording decisions, contracts, vocabularies, and forbidden patterns; and
3. an ordered task list naming exact artifacts, checks, and commit messages.

The primary orchestrating session writes those documents. Workers execute only their bounded task.
At task boundaries, review the commit and full diff rather than the worker summary. Stage exact
paths only; `git add -A`, `git add .`, `git add -u`, and `git commit -a` are prohibited.
