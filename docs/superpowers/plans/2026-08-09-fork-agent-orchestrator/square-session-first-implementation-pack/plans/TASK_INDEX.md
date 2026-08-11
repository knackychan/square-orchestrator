# Task Index — Milestone and Task Name Registry

Total tasks: **97** across milestones **SA00–SA15**.

Detailed packets are supplied for SA00 and SA01. Later packets must be compiled immediately before dispatch from the accepted start commit and inspected pinned-source symbols; this prevents stale paths from becoming authority.

## How to name milestones and tasks

Every milestone and task has a stable code plus a short human-readable name. Use the **readable name alongside the code** whenever you refer to one in prose, commits, completion receipts, evidence notes, or status reports:

| Kind | Format | Example |
|---|---|---|
| Milestone / subplan | `SAxx — Short title` | `SA00 — Fork adoption and reproducible baseline` |
| Task | `SAxx-Tyy — Short name` | `SA00-T01 — Create and pin the downstream fork` |
| Gate | `Ax — Gate name` | `A0 — Adoption gate` |

- The tables below are the **canonical name registry**. Task short names come from the `Title` column; milestone short titles come from the section heading.
- Reserve the bare code (`SA00-T01`) for machine `task_id` fields, file/directory names, packet links, and table keys — never as the only name in prose.
- Amendments and sub-tasks keep the parent code as a prefix, e.g. `SA00-T02-EA01 — Correct and persist baseline evidence`.
- If a task title changes, update it in this registry (and the `.json`/`.csv` indexes) rather than inventing a synonym elsewhere.

## Gates at a glance

| Gate | Name | Meaning |
|---|---|---|
| A0 | Adoption gate | Pinned fork, unchanged baseline, identity isolation, architecture amendment |
| A1 | Windows foundation gate | Daemon/UI/ConPTY/worktree/restart lifecycle safe for Square |
| A2 | Contract gate | Session-first domain, artifacts, routing, API/event contracts frozen as `1.0-draft` |
| A3 | Durable-state gate | Migrations/events/artifacts/idempotency/leases/bindings/recovery |
| A4 | Execution-facade gate | AO execution facade + route certification proven with a fake route |
| A5 | Session-API gate | Session API/read models/SSE/history + deterministic fake workflows |
| A6 | Session-UI gate | Rounded session-first UI works against fixtures and live contracts |
| A7 | Core Alpha gate | One real certified route completes QUICK through desktop and CLI |
| A8 | Interactions gate | Interactions/controller/restart lifecycle complete |
| A9 | Memory/context gate | Project/global memory + bounded Context Pack complete |
| A10 | PLANNED gate | PLANNED orchestration completes through implementation |
| A11 | MVP gate | Validation, independent review, finite fix, final receipt/evidence |
| A12 | Client/routing gate | Role-routing UX, route plurality, CLI/optional VS Code parity |
| A13 | Sustained-operation gate | Resource/index/evaluation behavior |
| A14 | Windows release gate | Security/installer/updater/upstream-sync release qualification |
| A15 | Scale gate | Optional scale enabled only by evidence |

## SA00 — Fork adoption and reproducible baseline

| Task | Short name | Prerequisites | Detailed packet |
|---|---|---|---|
| `SA00-T01` | Create and pin the downstream fork | none | [SA00-T01.md](tasks/SA00-T01.md) |
| `SA00-T02` | Capture the unchanged Windows build/test/package baseline | SA00-T01 | [SA00-T02.md](tasks/SA00-T02.md) |
| `SA00-T03` | License, attribution, identity, telemetry, updater, and data isolation design | SA00-T02 | [SA00-T03.md](tasks/SA00-T03.md) |
| `SA00-T04` | Import authority and activate architecture amendment | SA00-T03 | [SA00-T04.md](tasks/SA00-T04.md) |
| `SA00-T05` | Adoption gate A0 | SA00-T01..T04 | [SA00-T05.md](tasks/SA00-T05.md) |

## SA01 — Windows lifecycle and AO platform hardening

| Task | Short name | Prerequisites | Detailed packet |
|---|---|---|---|
| `SA01-T01` | Detach daemon/workflows from desktop window lifetime | A0 | [SA01-T01.md](tasks/SA01-T01.md) |
| `SA01-T02` | Single daemon and Square data-directory ownership | SA01-T01 | [SA01-T02.md](tasks/SA01-T02.md) |
| `SA01-T03` | Windows ConPTY, input/output/resize/cancel, and descendant cleanup | A0 | [SA01-T03.md](tasks/SA01-T03.md) |
| `SA01-T04` | Restart reconciliation and controller generation | SA01-T02,T03 | [SA01-T04.md](tasks/SA01-T04.md) |
| `SA01-T05` | Worktree cleanup and dirty-state safety | A0 | [SA01-T05.md](tasks/SA01-T05.md) |
| `SA01-T06` | Windows foundation gate A1 | SA01-T01..T05 | [SA01-T06.md](tasks/SA01-T06.md) |

## SA02 — Session-first contracts and deterministic domain

| Task | Short name | Prerequisites | Detailed packet |
|---|---|---|---|
| `SA02-T01` | Strong IDs, versions, hashes, time, and typed results | A1 | Compile at dispatch |
| `SA02-T02` | Square Session, message, workflow, role, and AO binding records | SA02-T01 | Compile at dispatch |
| `SA02-T03` | Pure lifecycle reducers | SA02-T02 | Compile at dispatch |
| `SA02-T04` | Task Brief, Preview, Plan, Task, Acceptance, Review, Receipt, Evidence contracts | SA02-T01,T02 | Compile at dispatch |
| `SA02-T05` | Role routing profile and actual model identity contract | SA02-T01,T02 | Compile at dispatch |
| `SA02-T06` | Square API/event contract and generated TypeScript parity | SA02-T02..T05 | Compile at dispatch |
| `SA02-T07` | Contract gate A2 | SA02-T01..T06 | Compile at dispatch |

## SA03 — Durable state, semantic events, artifacts, leases, and recovery

| Task | Short name | Prerequisites | Detailed packet |
|---|---|---|---|
| `SA03-T01` | Square migration range and schema registry | A2 | Compile at dispatch |
| `SA03-T02` | Session, conversation, workflow, role, task, and binding schema | SA03-T01 | Compile at dispatch |
| `SA03-T03` | Semantic events and transactional projections | SA03-T02 | Compile at dispatch |
| `SA03-T04` | Idempotency, writer leases, controller generations, and AO bindings | SA03-T02,T03 | Compile at dispatch |
| `SA03-T05` | Content-addressed artifacts and retained terminal/history references | SA03-T02 | Compile at dispatch |
| `SA03-T06` | Migration/corruption/crash/recovery matrix and gate A3 | SA03-T01..T05 | Compile at dispatch |

## SA04 — Role route registry and AO execution facade

| Task | Short name | Prerequisites | Detailed packet |
|---|---|---|---|
| `SA04-T01` | Inventory AO adapter/model/permission capabilities | A2,A3 | Compile at dispatch |
| `SA04-T02` | Route discovery and certification service | SA04-T01 | Compile at dispatch |
| `SA04-T03` | Requested/resolved/actual identity verification | SA04-T02 | Compile at dispatch |
| `SA04-T04` | Square AO execution facade | A3,SA04-T02 | Compile at dispatch |
| `SA04-T05` | Role packet/prompt compiler and bounded artifact boundary | SA02-T04,T05,SA04-T04 | Compile at dispatch |
| `SA04-T06` | Fake route/adapter execution and gate A4 | SA04-T03..T05 | Compile at dispatch |

## SA05 — Session API, read models, events, and fake workflows

| Task | Short name | Prerequisites | Detailed packet |
|---|---|---|---|
| `SA05-T01` | Session and durable conversation service/API | A3,A4 | Compile at dispatch |
| `SA05-T02` | Workflow-run, role-run, and direct action API | SA05-T01 | Compile at dispatch |
| `SA05-T03` | Typed interactions and owner decisions API | SA05-T01 | Compile at dispatch |
| `SA05-T04` | SSE invalidation, history, artifacts, and terminal binding read models | SA05-T01..T03 | Compile at dispatch |
| `SA05-T05` | Deterministic fixture workflows and gate A5 | SA05-T01..T04 | Compile at dispatch |

## SA06 — Rounded session-first desktop UI foundation

| Task | Short name | Prerequisites | Detailed packet |
|---|---|---|---|
| `SA06-T01` | Square identity, tokens, and reusable components | A0,A2 | Compile at dispatch |
| `SA06-T02` | Session navigator, tabs, filters, and New Session composer | SA06-T01,A5 fixtures | Compile at dispatch |
| `SA06-T03` | Session conversation and workflow progression | SA06-T02 | Compile at dispatch |
| `SA06-T04` | Dynamic Task Manager and role docks | SA06-T02,T03 | Compile at dispatch |
| `SA06-T05` | Direct attention strip and authorized decisions | SA06-T03 | Compile at dispatch |
| `SA06-T06` | Plan & Review, History, and role-routing setup | SA06-T03,T04 | Compile at dispatch |
| `SA06-T07` | Layout persistence, performance, accessibility, and gate A6 | SA06-T01..T06 | Compile at dispatch |

## SA07 — QUICK vertical slice and Core Alpha

| Task | Short name | Prerequisites | Detailed packet |
|---|---|---|---|
| `SA07-T01` | Request intake and compact Task Brief | A5 | Compile at dispatch |
| `SA07-T02` | Deterministic triage and Dispatch Preview | SA07-T01,SA02-T05 | Compile at dispatch |
| `SA07-T03` | QUICK task/role packet compiler | SA07-T02 | Compile at dispatch |
| `SA07-T04` | Task Manager QUICK state machine and AO worker launch | SA04,A3,SA07-T03 | Compile at dispatch |
| `SA07-T05` | Targeted validation, receipt, and local/PR result | SA07-T04 | Compile at dispatch |
| `SA07-T06` | QUICK desktop and CLI flow | SA06,A5,SA07-T05 | Compile at dispatch |
| `SA07-T07` | First real certified route and Alpha gate A7 | SA04-T02,T03,SA07-T06 | Compile at dispatch |

## SA08 — Interactions, controller authority, cancellation, and restart

| Task | Short name | Prerequisites | Detailed packet |
|---|---|---|---|
| `SA08-T01` | Durable question, approval, permission, auth, blocker, and route interactions | A7 | Compile at dispatch |
| `SA08-T02` | Response, approval-once, denial, amendment, and auth takeover | SA08-T01 | Compile at dispatch |
| `SA08-T03` | Interactive controller lease and multi-view behavior | SA08-T01 | Compile at dispatch |
| `SA08-T04` | Checkpoint, graceful cancellation, hard-stop gate, and safe cleanup | SA08-T02,T03 | Compile at dispatch |
| `SA08-T05` | Startup reconciliation of Square sessions and AO bindings | SA03,SA04,SA08-T04 | Compile at dispatch |
| `SA08-T06` | UI closure/restart matrix and gate A8 | SA08-T01..T05 | Compile at dispatch |

## SA09 — Project/global memory and bounded context

| Task | Short name | Prerequisites | Detailed packet |
|---|---|---|---|
| `SA09-T01` | Memory records, schema, service, API, and lifecycle | A8 | Compile at dispatch |
| `SA09-T02` | Candidate extraction and deduplication | SA09-T01 | Compile at dispatch |
| `SA09-T03` | Owner promotion, rejection, deprecation, and rollback | SA09-T01,T02 | Compile at dispatch |
| `SA09-T04` | Context Scout jobs and cited Context Reports | A8,SA09-T01 | Compile at dispatch |
| `SA09-T05` | Context Pack compiler and cache compatibility | SA09-T04 | Compile at dispatch |
| `SA09-T06` | Memory/context UI, CLI, and gate A9 | SA06,SA09-T01..T05 | Compile at dispatch |

## SA10 — PLANNED workflow and bounded orchestration

| Task | Short name | Prerequisites | Detailed packet |
|---|---|---|---|
| `SA10-T01` | Secretary Task Brief compiler | A9 | Compile at dispatch |
| `SA10-T02` | Context job scheduler and Context Pack readiness | SA09-T05 | Compile at dispatch |
| `SA10-T03` | Planner role and Plan/Acceptance compiler | SA10-T01,T02 | Compile at dispatch |
| `SA10-T04` | Orchestrator/Synthesizer role | SA10-T03 | Compile at dispatch |
| `SA10-T05` | Plan approval, amendment, and immutable binding | SA10-T03,T04 | Compile at dispatch |
| `SA10-T06` | Dependency scheduler, worker assignments, and bounded handovers | SA10-T05,SA04 | Compile at dispatch |
| `SA10-T07` | PLANNED implementation/restart gate A10 | SA10-T01..T06 | Compile at dispatch |

## SA11 — Verification, independent review, findings, fix loop, and Square MVP

| Task | Short name | Prerequisites | Detailed packet |
|---|---|---|---|
| `SA11-T01` | Task/integration deterministic validation service | A10 | Compile at dispatch |
| `SA11-T02` | Review Packet compiler | SA11-T01 | Compile at dispatch |
| `SA11-T03` | Independent reviewer route selection and run | SA04,SA11-T02 | Compile at dispatch |
| `SA11-T04` | Finding model and finite FIX task loop | SA11-T03 | Compile at dispatch |
| `SA11-T05` | Final receipt, integration acceptance, outcome and memory candidates | SA11-T01..T04 | Compile at dispatch |
| `SA11-T06` | Square MVP gate A11 | SA07,A8,A9,A10,SA11-T01..T05 | Compile at dispatch |

## SA12 — Role-routing UX, route plurality, CLI, and optional VS Code

| Task | Short name | Prerequisites | Detailed packet |
|---|---|---|---|
| `SA12-T01` | Global/project/session/task role-profile resolution | A11 | Compile at dispatch |
| `SA12-T02` | AUTO/PREFERRED/PINNED Dispatch Preview and role-dock UX | SA12-T01,SA06 | Compile at dispatch |
| `SA12-T03` | Expanded adapter conformance and additional certified routes | SA04,A11 | Compile at dispatch |
| `SA12-T04` | Complete `square` CLI and stable JSON/exit codes | A11 | Compile at dispatch |
| `SA12-T05` | Minimal VS Code client and cross-client consistency | SA12-T04 | Compile at dispatch |
| `SA12-T06` | Client/route gate A12 | SA12-T01..T05 | Compile at dispatch |

## SA13 — Exposure, cost, resource profiles, context reuse, and evaluation

| Task | Short name | Prerequisites | Detailed packet |
|---|---|---|---|
| `SA13-T01` | Usage and model-family exposure ledger | A11 | Compile at dispatch |
| `SA13-T02` | Actual/equivalent/subscription cost schedules | SA13-T01 | Compile at dispatch |
| `SA13-T03` | FAST/NORMAL/SLOW resource and scheduler policy | A11 | Compile at dispatch |
| `SA13-T04` | Snapshot-aware repository index and Context Pack cache | SA09 | Compile at dispatch |
| `SA13-T05` | Buffered persistence, retention, and resource telemetry | SA03,A11 | Compile at dispatch |
| `SA13-T06` | Outcome evaluation and sustained-operation gate A13 | SA11,SA13-T01..T05 | Compile at dispatch |

## SA14 — Security, diagnostics, packaging, updater, and upstream sync

| Task | Short name | Prerequisites | Detailed packet |
|---|---|---|---|
| `SA14-T01` | Threat model and automated security closure | A12 | Compile at dispatch |
| `SA14-T02` | Local API and Electron bridge hardening | SA14-T01 | Compile at dispatch |
| `SA14-T03` | Privacy-safe doctor and support bundle | SA14-T01 | Compile at dispatch |
| `SA14-T04` | Square Windows identity, installer, data migration, and coexistence | A12 | Compile at dispatch |
| `SA14-T05` | Square updater, live-work gate, backup, rollback | SA14-T04 | Compile at dispatch |
| `SA14-T06` | Upstream-sync automation, migration/API drift, SBOM and notices | A12 | Compile at dispatch |
| `SA14-T07` | Windows release gate A14 | SA14-T01..T06 | Compile at dispatch |

## SA15 — Optional advanced scale and controlled practice evolution

| Task | Short name | Prerequisites | Detailed packet |
|---|---|---|---|
| `SA15-T01` | Parallel read-only Scouts and specialists | A14 | Compile at dispatch |
| `SA15-T02` | Parallel isolated writer worktrees and merge queue | SA15-T01 | Compile at dispatch |
| `SA15-T03` | Automatic bounded fix dispatch | SA11,A14 | Compile at dispatch |
| `SA15-T04` | Controlled practice evolution and scale gate A15 | SA13,SA15-T01..T03 | Compile at dispatch |
