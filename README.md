# Square Orchestrator

Square Orchestrator is a planned, globally installed control plane for bounded work performed by
terminal coding agents. It is intended to let a primary Claude, Codex, OpenCode, or Command Code
session validate project authority, select an exact client/model route, launch visible worker
terminals, track `STOP:` events, and review immutable diffs.

It is also planned as a project foundry and practice lab: it can help create or adopt repositories
with coherent planning documents, dependency graphs, guardrails, and clean-code conventions, then
turn opted-in project outcomes and sourced engineering research into reviewable practice proposals.

## Current state

T-M1-01 is the first implemented M1 slice. The repository exposes a dependency-free
`python -m sqorch [--json] [--state-db PATH] doctor` shell that reports local Python, Git,
repository, and computed state-path information without creating runtime state. No package,
dependency, background service, model call, or delegated worker launch is part of this slice. The
canonical project contract is `SPEC.md`; `STATUS.md` is the only authority for active work.

## Intended shape

- one globally installed CLI, provisionally named `sqorch`;
- repository-local, versioned policy and authority;
- deterministic scheduling, routing validation, locks, and audit records;
- installed-client adapters for Command Code, OpenCode, Claude Code, and Codex CLI;
- visible foreground worker terminals;
- an optional VS Code bridge and MCP interface only after the CLI core is proven.
- a versioned project-blueprint and practice catalogue whose changes require review.

Sticker Generator is the first planned reference integration. Its project decisions remain local
to that repository and do not grant authority here.

A new session starts with `AGENTS.md`, `SPEC.md`, `STATUS.md`, and then `HANDOVER.md`. The handover
contains the complete manual workflow used until the planned CLI begins to ship.
