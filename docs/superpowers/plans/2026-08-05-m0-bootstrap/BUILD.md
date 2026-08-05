# M0 Build Guide — Decision Register

This guide constrains planning bootstrap only. It cannot authorize implementation.

## Decision register

**D-001 — Square Orchestrator is a separate repository.**  A global tool must not inherit one
product's file map, release history, or task authority.

**D-002 — Target repositories retain authority.**  The tool validates repository decisions; its
global state never activates work.

**D-003 — The core is deterministic.**  Models may plan, implement, and review within packets, but
authorization, hashes, locks, route equality, budgets, and state transitions are code decisions.

**D-004 — CLI before TUI, extension, MCP, or daemon.**  A universal command with human and JSON
output proves the workflow with the fewest integration surfaces.

**D-005 — Human documents remain canonical.**  A machine manifest is compiled from them, records
their hashes, and fails closed when they drift.

**D-006 — Routing is a versioned project profile.**  DEC-047 becomes a Sticker Generator reference
profile rather than a global hardcoded default.

**D-007 — One exact route per attempt.**  No alias or automatic fallback. A replacement is a new,
recorded attempt selected before it edits.

**D-008 — One writer per repository by default.**  Cross-repository writes and same-commit
read-only reviews may parallelize. Worktrees remain parked.

**D-009 — Visible terminal policy is enforced per project.**  The first candidate is Windows
Terminal; a VS Code bridge follows only when the core is proven.

**D-010 — Installed clients, not direct provider APIs.**  The core owns no model credentials and
does not become another inference gateway.

**D-011 — Standard library first.**  Python's CLI, paths, hashing, subprocess, TOML reader, and
SQLite capabilities are the proposed default. Dependencies require demonstrated need in an active
packet.

**D-012 — Runtime state and repository authority stay separate.**  A user-level SQLite database may
track runs and locks; accepted repository artifacts remain the durable record.

**D-013 — Project shape is generated from a responsibility graph.**  Templates supply the authority
workflow and safe defaults, but source folders follow accepted nodes, dependency edges, and the
first active vertical slice rather than a universal directory tree.

**D-014 — Clean code means cohesive ownership before reuse.**  Modules may contain related
functions; split by reason to change. Shared components and libraries require named consumers and
must not become miscellaneous dependency sinks.

**D-015 — Cross-project learning is opt-in and abstracted.**  Store practice claims, outcome
signals, and provenance—not source files, raw prompts, client data, or secrets.

**D-016 — Taste is versioned policy, not opaque memory.**  Practices move through observed,
candidate, trial, adopted, rejected, and deprecated states with owner-visible rationale.

**D-017 — Research and self-improvement remain proposal-only.**  Bounded research may suggest a
blueprint, guardrail, skill, check, or supporting tool. Adoption, installation, code changes, and
rollout require separate packets and acceptance.

**D-018 — Cold-start continuity lives in one root handover.**  `HANDOVER.md` records the manual
operating loop, role boundaries, delegation prompt, `STOP:` procedure, diff review, and milestone
handoff. It is required reading but never authority, so workflow guidance cannot activate work.

## Closed vocabulary for future task roles

`RESEARCH`, `PLAN_CONTRIBUTION`, `DOCUMENT`, `IMPLEMENT`, `REVIEW`, `FIX`, `AMENDMENT`.

## Closed vocabulary for practice state

`OBSERVED`, `CANDIDATE`, `TRIAL`, `ADOPTED`, `REJECTED`, `DEPRECATED`.

## Greppable forbidden claims

Until implementation is accepted, project documents must not say that Square Orchestrator
"launches", "enforces", "stores", or "supports" a feature without qualifying it as planned.

## M0 file contract

Every directory created in M0 has both context files and a complete file map. Root `SPEC.md` owns
durable product intent; root `STATUS.md` owns authority; packet `STATE.md` owns progress only.
