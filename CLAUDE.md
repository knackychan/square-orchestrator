# Claude Context — Square Orchestrator

Read `AGENTS.md` first; it is the full authority for all agents in this repository.

## Current authority

This project is planning-only. Do not create code, package metadata, dependencies, installers,
extensions, runtime state, or launch delegated workers unless `STATUS.md` names an exact active
implementation milestone.

The target repository always owns task authority. The orchestrator may validate and enforce that
authority, but it never creates it. Exact routes, no silent fallback, first-class `STOP:` handling,
no stored credentials, deterministic policy enforcement, and one writer per repository by default
are binding.

Project setup and cross-project practice learning are also planned. Learning is opt-in,
provenance-bearing, and proposal-only: never copy private project content, rewrite a template,
install a tool, or change global policy without recorded approval.

## Reading order

1. `AGENTS.md` and the nearest context pair.
2. `SPEC.md`.
3. `STATUS.md`.
4. `HANDOVER.md`.
5. `CLIENT-EXECUTION.md` for any agent-client selection, launch, monitoring, or review task.
6. Only the active packet named there.

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

Use the exact flags in `CLIENT-EXECUTION.md`. Hidden, minimized, detached, background, cloud,
internal sub-agent, or silent-fallback execution is prohibited. If the approved visible surface or
exact route is unavailable, record `STOP:` or `ROUTE_UNAVAILABLE` and do not edit. Planning,
packet authoring, ordinary read-only questions, and primary boundary review do not require a worker
unless the active packet explicitly assigns one.

When the primary opens a new Windows Terminal surface with native `wt.exe`, it must use the
argument-array wrapper in `CLIENT-EXECUTION.md`. A bounded prompt passed through `wt.exe` must
contain no semicolon characters: Windows Terminal interprets them as action separators even when
the prompt is one array element, which can open extra tabs or commands. Verify the guard and one
new worker surface before allowing edits.

## Root file map

| Path | Purpose |
|---|---|
| `README.md` | Introduction and planning boundary |
| `AGENTS.md`, `CLAUDE.md` | Root agent context pair |
| `SPEC.md` | Canonical contract |
| `STATUS.md` | Current authority |
| `HANDOVER.md` | Cold-start operating workflow |
| `CLIENT-EXECUTION.md` | Client routes, launch profiles, and lifecycle reference |
| `docs/` | Specifications and plans |
| `sqorch/` | Standard-library Python CLI source |
| `tests/` | Standard-library tests |

No package metadata, dependencies, extension, installer, or runtime database exists by design.
