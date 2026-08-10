# Square Orchestrator

Square Orchestrator is a maintained downstream fork of Agent Orchestrator `v0.12.1`. The active
fork plan, governance decisions, contracts, and execution packets live under
`docs/superpowers/plans/2026-08-09-fork-agent-orchestrator/`.

## Repository shape

- **Governance:** `AGENTS.md`, `CLAUDE.md`, `SPEC.md`, `STATUS.md`, `HANDOVER.md`, and
  `CLIENT-EXECUTION.md` define the repository boundary and operating workflow.
- **Active fork plan:** `docs/superpowers/plans/2026-08-09-fork-agent-orchestrator/` contains the
  session-first implementation pack and its hash-bound planning records.
- **Frozen pre-fork line:** `archive/docs/`, `archive/plans/`, `archive/src/`, and
  `archive/tooling/` retain earlier research, plans, implementation trees, and toolchain files
  for trace/history only. Nothing under `archive/` is live product behavior or authority.

## Current boundary

The repository remains planning/adoption-only. Application implementation, dependency installation,
provider calls, and `git push` require explicit activation in `STATUS.md`. The active fork is built
from the pinned upstream baseline; the archived pre-fork source is reference material, not a porting
source.
