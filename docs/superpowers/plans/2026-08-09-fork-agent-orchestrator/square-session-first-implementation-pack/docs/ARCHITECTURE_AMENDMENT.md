# Architecture Amendment — Square Session-First Product on Agent Orchestrator

- Date: 2026-08-09
- Status: proposed owner decision; becomes authoritative after acceptance
- Supersedes: the unaccepted .NET production path and the first global-dashboard-oriented Square-on-AO draft
- Preserves: product behavior, authority rules, bounded model roles, terminal safety, evidence, memory, routing, and accessibility contracts

## 1. Decision

Square Orchestrator will be a maintained downstream fork of Agent Orchestrator stable `v0.12.1`, pinned to release commit prefix `1df40e9` until an upstream-sync gate accepts a later release.

The production stack is:

```text
Go daemon and CLI
SQLite + goose + sqlc + change_log CDC
Loopback REST + SSE + terminal WebSocket
Electron + React/TypeScript desktop
AO ConPTY runtime on Windows
AO agent adapters, sessions, Chat controllers, worktrees, reviewers, PR/CI observation
Square session, workflow, memory, role-routing, acceptance, evidence, and policy layer
```

The .NET/WPF/Windows-named-pipe implementation line is archived as research. Its behavioral findings remain inputs, especially around terminal ownership, UI closure, process containment, typed interactions, deterministic health classification, and evidence.

## 2. Product mental model

The top-level user object is a **Square Session**.

A Square Session is created for one topic, request, or outcome. It contains:

- a conversation with the user;
- the current goal and constraints;
- one or more workflow runs;
- Task Manager decisions;
- bounded model role runs;
- AO execution-session and worktree bindings;
- terminal/Chat/diff/evidence docks;
- plan and acceptance artifacts;
- questions, approvals, blockers, and owner decisions;
- review findings and repair history;
- terminal/process history after completion;
- final receipt, evidence, and memory candidates.

The application shell is deliberately small:

```text
Project selector
Session list
Open session tabs
New Session
Global attention count
Daemon health
```

The selected Square Session owns the visible workflow, docks, decisions, plan, review, and history. Switching tabs changes presentation only.

## 3. Responsibility split

### Agent Orchestrator platform layer

Reuse without duplicating:

- daemon process and composition root;
- project registry;
- SQLite foundation, goose migrations, sqlc generation, and CDC;
- session manager and lifecycle facts;
- agent adapter catalogue;
- runtime adapters, including Windows ConPTY;
- Git worktree creation and cleanup;
- terminal WebSocket mux and xterm.js surface;
- Chat controllers where supported;
- reviewer execution;
- PR, CI, merge-conflict, and review observation;
- Electron shell, preload boundary, React renderer, packaging pipeline;
- OpenAPI and generated TypeScript types.

### Square product layer

Add as namespaced resources and services:

- Square Session and conversation;
- deterministic Task Manager;
- QUICK and PLANNED workflow profiles initially;
- Secretary, Scout, Planner, Orchestrator/Synthesizer, Worker, Reviewer, and optional Triage roles;
- role route selection and certification;
- Task Brief, Dispatch Preview, Context Pack, Plan, Task Contract, Acceptance Contract, Review Packet, Finding, Receipt, and Evidence;
- project/global memory with owner promotion;
- idempotency, writer leases, controller generations, interaction authority;
- requested-versus-actual model identity;
- exposure/cost/resource indicators later;
- `square` CLI and later a VS Code client;
- session-first desktop workspace.

## 4. Non-negotiable invariants

1. One per-user Square daemon is the only authoritative scheduler and mutation owner.
2. Closing or reloading Electron, changing tabs, or hiding/rearranging a dock never stops, duplicates, or changes an AO execution session.
3. The renderer, CLI, and future VS Code extension never mutate SQLite directly.
4. Every mutation is idempotent and records a durable Square semantic event before external work begins.
5. One writer attempt owns one worktree lease and one AO session binding generation.
6. A lack of terminal output alone is not a stall and never authorizes termination.
7. Questions, permissions, approvals, authentication, blockers, and completion are durable typed records.
8. No model session remains active merely to monitor another model session.
9. The deterministic Task Manager advances workflows from durable facts and timers.
10. Workers cannot change architecture, public contracts, schema, security policy, acceptance, or another task's assumptions outside an accepted packet.
11. QUICK keeps safety, receipt, cancellation, scope, evidence, and restart contracts even when planning is skipped.
12. A role's configured route/model is not treated as the actual route/model until the adapter proves or reports it.
13. `PINNED` routing never silently falls back.
14. Reviewer-independence policy cannot silently select the implementation model family.
15. Completed role terminals and artifacts remain attached to the Square Session under retention policy.
16. UI status is derived from authoritative facts; it is not optimistic authority.
17. AO `change_log` is transport/CDC, not Square's semantic audit ledger.
18. Applied AO or Square migrations are never edited in place.
19. Square and official AO installations must not share state, updater identity, telemetry identity, process discovery files, or worktree roots.
20. Existing repositories, dirty worktrees, credentials, and uncommitted work are never deleted by update, cleanup, or uninstall.

## 5. Bounded roles

The Task Manager is software, not a model.

| Role | Implementation | Typical authority |
|---|---|---|
| Task Manager | deterministic Go service | workflow transition, scheduling, persistence; no repository writes |
| Secretary | bounded AO role run | request clarification and Task Brief only |
| Scout | bounded read-only AO role run | cited context report only |
| Planner | bounded AO role run | Plan and Acceptance Contract only |
| Orchestrator/Synthesizer | bounded AO role run | dispatch synthesis, amendment proposal, integration synthesis |
| Worker | AO session + worktree | one accepted task contract |
| Reviewer | fresh AO reviewer/session | read-only diff/evidence review |
| Triage | optional bounded classifier | unknown terminal-state classification only |

A role run ends after its artifact or typed blocker. Later work starts a new role run and usually a new AO session.

## 6. Local API decision

The fork retains AO's loopback REST/SSE/WebSocket design for the MVP. Replacing it with Windows named pipes would destroy much of the benefit of adopting AO.

Before release, the threat-model slice must decide whether to add a per-install bearer/capability token to the loopback API. Regardless of transport:

- bind to loopback only for local mode;
- Electron preload exposes allow-listed operations, not arbitrary fetch/shell access;
- terminal output is untrusted;
- controller actions are separately authorized;
- correlation, idempotency, and schema versions are mandatory.

## 7. Persistence decision

Square extends AO's existing database through forward-only `square_*` migrations and sqlc queries.

- `square_events` is append-only semantic history.
- current projections update atomically with the corresponding event.
- payload-heavy immutable artifacts are content-addressed files.
- AO sessions and worktrees are referenced through generation-fenced bindings.
- session messages are durable product conversation records, not merely provider transcripts.
- provider transcripts may be linked as evidence/history but are never automatically promoted to memory.

## 8. Frontend decision

The approved desktop direction is the rounded, session-focused reference included in this package.

The main shell contains sessions rather than a global fleet dashboard. Inside one session, Square dynamically reveals only the roles and docks that exist. A QUICK session may show Task Manager and one Worker; a PLANNED session may show Secretary, Scouts, Planner, Orchestrator, Workers, and Reviewer.

Detailed data remains available under Plan & Review, History, route settings, and role docks, but urgent problems and proposed decisions are shown directly in the affected session.

## 9. Fork governance

- Preserve `upstream` remote and baseline tag.
- Keep upstream merge commits distinct from Square feature commits.
- Namespace Square tables, API routes, services, frontend features, and tests.
- Do not scatter product logic into adapters, HTTP controllers, generated code, or the Electron renderer.
- Every upstream intake uses a dedicated branch and full adoption/lifecycle/migration/workflow/UI regression.
- Apache-2.0 license and applicable notices remain included; Square is clearly rebranded and does not imply official affiliation.

## 10. Gate effect

This amendment authorizes only the planning and adoption sequence until A0 passes. It does not authorize Square product code on top of an unmeasured or failing baseline.
