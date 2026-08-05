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
5. Only the active packet named there.

## Root file map

| Path | Purpose |
|---|---|
| `README.md` | Introduction and planning boundary |
| `AGENTS.md`, `CLAUDE.md` | Root agent context pair |
| `SPEC.md` | Canonical contract |
| `STATUS.md` | Current authority |
| `HANDOVER.md` | Cold-start operating workflow |
| `docs/` | Specifications and plans |

No source or package tree exists by design.
