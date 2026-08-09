# Square Orchestrator on Agent Orchestrator — Combined Implementation Plan

- **Date:** 2026-08-08
- **Status:** implementation-ready draft; owner acceptance required before dispatch
- **Product name:** Square Orchestrator
- **Primary command:** `square`
- **Upstream foundation:** Untrivial-ai/agent-orchestrator `v0.12.1`
- **Pinned upstream commit:** `1df40e9`
- **Target:** Windows-first, per-user, local agent-orchestration application
- **Previous implementation line:** .NET/WPF research line is archived, not continued as the production base

---

## 1. Executive decision

Square Orchestrator will be implemented as a maintained downstream fork of **Agent Orchestrator (AO)** rather than as a new .NET application.

The fork retains AO’s proven platform layer:

- Go daemon and Cobra CLI;
- SQLite, goose migrations, sqlc-generated queries, and trigger-driven CDC;
- Electron + React desktop application;
- REST, SSE, and terminal WebSocket transport;
- Windows ConPTY runtime and terminal mux;
- Git worktree isolation;
- session lifecycle, reaper, SCM observation, PR/CI/review feedback;
- agent and reviewer adapter catalogue;
- xterm.js terminal surface;
- OpenAPI-to-TypeScript contract generation; and
- Windows desktop packaging/update infrastructure.

Square will add the product layer that is not supplied by AO:

- natural-language request intake through a Secretary role;
- deterministic `QUICK`, `BOUNDED`, `PLANNED`, and `SYSTEMIC` workflow profiles;
- a durable software Task Manager rather than a model that remains alive to monitor workers;
- Task Briefs, Context Packs, Plan Sets, Task Contracts, Acceptance Contracts, Review Packets, Findings, Receipts, and Evidence;
- project and global learning memory with owner-controlled promotion;
- trust, authority, approval, idempotency, and writer-lease policies;
- route certification, specialist profiles, model-family exposure, and cost indicators;
- fast, normal, and slow resource profiles;
- a Square operations workspace layered into the AO desktop application;
- a stable `square` CLI and later a VS Code client.

This is an **architecture amendment**. The previous .NET 10, WPF/WebView2, Windows named-pipe, and custom SQLite implementation choices are no longer production requirements. Their behavioral requirements and test discoveries remain useful authority.

---

## 2. Authority hierarchy

When sources disagree, implementation agents must follow this order:

1. This combined plan after owner acceptance.
2. Accepted Square architecture amendments and ADRs created under this plan.
3. Preserved Square product contracts and non-negotiable rules from the earlier specifications.
4. The pinned AO `v0.12.1` source and documentation.
5. Later reviewed upstream AO releases adopted through the upstream-sync procedure.
6. Task-specific dispatch packets.
7. Local implementation discretion that does not alter public contracts, security, schema, or cross-task assumptions.

The earlier Square documents remain product authorities for:

- daemon-owned workflows and terminals;
- safe UI closure;
- typed interactions and approvals;
- bounded model roles;
- QUICK and PLANNED behavior;
- task, plan, review, evidence, memory, exposure, cost, and resource concepts;
- the terminal-first workspace experience;
- accessibility and authority rules.

They no longer lock the implementation to .NET, WPF, WebView2, named pipes, or the previous repository module tree.

---

## 3. Delivery outcome

Build Square as a Windows-installed local control plane in which a user can open a project, submit a natural-language task, review a dispatch preview, and let a deterministic daemon coordinate bounded planning, implementation, verification, review, repair, and memory updates through existing AO sessions and worktrees.

The first complete Square MVP must prove:

1. `square` and the desktop application connect to one durable daemon and display the same authoritative state.
2. Closing the desktop window does not stop daemon-owned workflows or agent processes.
3. A `QUICK` request launches one bounded worker with narrow scope and targeted validation without broad orchestration overhead.
4. A `PLANNED` request produces a Task Brief, bounded Context Pack, accepted Plan/Acceptance Contract, implementation attempts, deterministic checks, independent review where eligible, and a finite repair loop.
5. Model sessions end at artifact boundaries; no model remains open merely to monitor another model.
6. Every accepted mutation is idempotent and produces durable semantic events before external work begins.
7. Project/global memory is explicit, cited, reviewable, and never promoted automatically into global policy.
8. AO sessions, terminals, worktrees, Chat controllers, reviewers, PR observation, and adapters remain reusable platform capabilities rather than being reimplemented by Square.
9. Windows process, terminal, worktree, restart, and path behavior is tested against the fork before Square workflow code depends on it.
10. The fork can absorb reviewed AO releases without rewriting Square’s domain or scattering Square-specific logic across upstream packages.

---

## 4. Canonical task format

The new plan uses `SA` for **Square on AO**:

- `SA00` identifies a cohesive sub-plan.
- `SA00-T01` identifies one independently dispatchable task.
- `SA00-T01-ST01` is allowed only for sequential checkpoints that cannot safely be separated.
- IDs are immutable. A changed contract produces an amendment or `FIX` task.
- One task should normally fit one fresh worker session and one reviewable commit.

Every dispatch packet must contain:

- task ID and title;
- starting commit and upstream-base identity;
- authority-document hashes;
- prerequisites and gate state;
- allowed read/write paths;
- relevant invariants and acceptance IDs;
- exact expected resources, API routes, migrations, files, types, and behavior;
- errors and edge cases;
- validation commands and evidence shape;
- context/output/retry/resource budgets;
- discretion envelope;
- mandatory STOP conditions; and
- completion-receipt destination.

---

## 5. Pinned AO foundation

### 5.1 Baseline

The initial fork is based on:

```text
Repository: https://github.com/Untrivial-ai/agent-orchestrator
Tag:        v0.12.1
Commit:     1df40e9
Release:    stable, 2026-08-05
```

Nightly builds and moving `main` are not production baselines. They may be inspected or cherry-picked only through a reviewed upstream intake task.

### 5.2 AO capabilities accepted as foundation candidates

The pinned source is expected to supply:

- a long-running Go daemon;
- a thin Cobra CLI over daemon HTTP routes;
- Electron + React desktop UI;
- SQLite through `database/sql`;
- goose migrations;
- sqlc query generation;
- `change_log` trigger-driven CDC;
- SSE replay through `Last-Event-ID`;
- terminal WebSocket mux;
- ConPTY runtime on Windows;
- tmux runtime on Linux/macOS;
- Git CLI worktrees;
- lifecycle manager and runtime reaper;
- project/session/PR/review services;
- agent adapter registry;
- reviewer adapters;
- OpenAPI generation and frontend TypeScript generation;
- Playwright, Vitest, Go unit, race, lint, and build gates.

These are **foundation candidates**, not blindly trusted behavior. SA00–SA01 must prove them on the target Windows machine.

### 5.3 Known upstream risks that enter the adoption gate

The fork must explicitly test or repair these classes of behavior before Square relies on them:

- desktop closure stopping the daemon or live sessions;
- daemon restart producing zombie or `no_signal` sessions;
- Windows process termination leaving a worktree locked;
- stale process identity or broad process-group termination;
- ConPTY cancellation/resume limitations;
- Windows path, HOME, and temporary-directory assumptions in tests;
- migration or concurrent-daemon conflicts;
- session restore and Chat-controller recovery;
- provider-specific trust/approval setup;
- updater, telemetry, and app-data collisions with official AO installations.

---

## 6. New locked implementation decisions

These decisions replace the implementation-specific decisions in the earlier .NET plan.

| Area | Combined-plan decision |
|---|---|
| Production foundation | Maintained fork of AO `v0.12.1`; later upstream releases enter through controlled merge gates |
| Backend runtime | Go, following AO’s current Go version and dependency policy |
| Durable owner | One per-user Square daemon is the sole workflow, scheduler, lifecycle, memory, policy, and mutation owner |
| Execution substrate | AO sessions, worktrees, TUI runtimes, Chat controllers, reviewer processes, and adapters |
| Persistence | AO SQLite database extended through new forward-only Square migrations and sqlc queries |
| Semantic events | New append-only `square_events`; AO `change_log` remains CDC/invalidation, not the semantic event ledger |
| Large artifacts | Content-addressed files under the Square data directory, with hashes and references in SQLite |
| Local API | AO loopback REST/SSE/WebSocket retained for the internal MVP; no named-pipe rewrite |
| Desktop | AO Electron + React desktop application, rebranded and extended |
| Terminal UI | AO xterm.js terminal surface and mux retained |
| CLI | New `square` binary/command surface; legacy `ao` entry point retained temporarily for upstream compatibility and migration |
| Workflow ownership | Deterministic Square Task Manager; model sessions are bounded workers and finish at artifacts |
| Workspaces | AO Git worktrees for all writer attempts by default; no two writers in one worktree |
| UI authority | Desktop, CLI, and later VS Code are clients only; no direct SQLite mutation |
| Memory | Explicit project/global memory records with candidate/adopted/deprecated states and owner promotion |
| Routing | Eligibility first; exposure/cost/resource heuristics only rank already-safe routes |
| Windows target | Windows x64 first; other AO platforms remain upstream-compatible but are not initial Square release gates |
| Installer | AO Electron packaging pipeline adapted to Square identity and release channel |
| Telemetry | Disabled by default in Square builds; any future telemetry is opt-in |
| Updater | Official AO updater disabled/replaced before distributing Square builds |
| Mobile | Parked; do not expand Square scope to AO mobile during MVP |
| Docker | Development/test aid and optional later sandbox; not required for the normal Windows desktop workflow |

---

## 7. Preserved global invariants

1. Closing, hiding, reloading, or moving a UI must never stop or duplicate a daemon-owned workflow or agent process.
2. CLI, Electron renderer, VS Code, and mobile clients never open or mutate SQLite directly.
3. Every mutation is idempotent and records a durable semantic event before external work begins.
4. One writer lease exists per worktree. A Square task attempt references exactly one AO session/worktree controller generation.
5. Silence alone is not a stall and never authorizes termination.
6. Questions, permissions, approvals, authentication, blockers, and completion are typed durable records.
7. No model is used for continuous terminal monitoring.
8. A worker cannot decide requirements, architecture, public contracts, schema, security policy, or cross-task behavior outside its packet.
9. QUICK retains lock, evidence, cancellation, interaction, receipt, and scope safety.
10. Model context thresholds remain warning at 100K, handover at 120K, hard stop at 150K or the route’s lower limit.
11. Exposure, context pressure, actual cost, equivalent cost, and subscription allocation remain distinct metrics.
12. Resource protection may delay work but must not hold a model session open merely to wait.
13. Public records, API resources, artifacts, and receipts are versioned.
14. Destructive or authority-expanding actions require an explicit policy path and audit event.
15. An AO runtime or adapter probe failure is inconclusive, not proof of session death.
16. Dirty worktrees are never force-deleted.
17. Existing applied migrations are never edited; changes use new forward migrations.
18. AO adapters remain leaves and do not own Square product workflow decisions.
19. Square semantic events and current projections commit atomically.
20. Square-specific code must remain namespaced enough to keep upstream merges reviewable.

---

## 8. Reuse, adapt, add, and retire matrix

| Capability | Decision | Notes |
|---|---|---|
| AO project registry | Reuse/adapt | Add Square policy, memory, and workflow settings without duplicating project identity |
| AO sessions | Reuse | Execution units linked to Square attempts; not the top-level Square task model |
| AO orchestrator sessions | Compatibility only | Square workflows use a deterministic manager plus bounded planner sessions |
| AO Chat/TUI controllers | Reuse | Select by route capability; enforce one controller generation per attempt |
| AO worktrees | Reuse/harden | Default writer isolation; add writer lease and cleanup reconciliation |
| AO lifecycle/reaper | Reuse/harden | Supply execution facts to Square attempt reducer; never become Square workflow owner |
| AO SQLite/change_log | Reuse/extend | Add Square tables, triggers, semantic events, migrations, queries |
| AO SSE/WebSocket | Reuse | Extend OpenAPI/events; no separate transport stack |
| AO Electron shell | Reuse/rebrand | Add Square feature modules and navigation |
| AO terminal pane | Reuse/extend | Add Square interaction bar, contract/receipt links, controller authority |
| AO PR/CI/review observation | Reuse | Feed Square verification and review stages where applicable |
| AO reviewer agents | Reuse/certify | Add independence, capability, trust, and packet restrictions |
| AO browser preview | Reuse | Useful for UI verification; not workflow authority |
| AO adapters | Reuse/certify | Square registry adds exact version/capability/route qualification |
| AO mobile | Park | No MVP work |
| Previous .NET daemon/CLI/WPF | Retire from production | Archive as research and behavioral test source |
| Previous named-pipe stack | Retire | No production port unless loopback boundary later fails threat model |
| Previous C# ConPTY prototype | Archive/reference | Port relevant Windows fixtures and invariants into Go tests |
| Previous TypeScript dock prototype | Design reference | Do not force a full docking engine into the first Square UI increment |

---

## 9. Target architecture

### 9.1 Process topology

```text
square CLI ──────────────┐
Square Electron desktop ├── REST / SSE / WebSocket on loopback
Future VS Code extension┘
                         │
                  Square/AO Go daemon
                         │
        ┌────────────────┼───────────────────┐
        │                │                   │
   SQLite + CDC   Square artifact store   AO runtime/worktrees
        │                                    │
 Square workflow facts                  Agent CLIs / Chat drivers
 Square semantic events                 Reviewers / Git / SCM
 Memory / policy / usage
```

### 9.2 Responsibility split

#### AO platform layer

- project registration;
- session/worktree creation;
- terminal/Chat controller launch;
- agent process communication;
- runtime liveness facts;
- PR/CI/review observation;
- terminal mux;
- frontend daemon discovery;
- updater/packaging mechanics;
- adapter-specific executable/auth/launch behavior.

#### Square product layer

- natural-language request intake;
- workflow profile selection;
- Task Brief and Dispatch Preview;
- context job scheduling and Context Pack compilation;
- plan and acceptance compilation;
- task DAG and dispatch decisions;
- writer/route/resource reservations;
- deterministic validation policy;
- review packet and finding/fix policy;
- interactions and authority;
- receipts and evidence;
- project/global memory;
- exposure/cost/resource policy;
- outcome evaluation;
- Square UI/read models.

### 9.3 Required package boundaries

Proposed additions under the existing AO repository:

```text
backend/internal/domain/
  square_ids.go
  square_request.go
  square_task.go
  square_attempt.go
  square_interaction.go
  square_contracts.go
  square_memory.go
  square_route.go
  square_resource.go

backend/internal/service/
  squarerequest/
  squareworkflow/
  squarememory/
  squarecontract/
  squareinteraction/
  squarerouting/
  squareresource/
  squareevaluation/

backend/internal/square/
  taskmanager/
  scheduler/
  compiler/
  reducer/
  recovery/

backend/internal/ports/
  square_planner.go
  square_validator.go
  square_artifacts.go
  square_resource_probe.go

backend/internal/adapters/
  validation/command/
  artifacts/sha256fs/
  resource/windows/

backend/internal/storage/sqlite/
  migrations/<new_square_migrations>.sql
  queries/square_*.sql
  square_*_store.go

backend/internal/httpd/controllers/
  square_requests.go
  square_tasks.go
  square_memory.go
  square_interactions.go
  square_routes.go
  square_resources.go

frontend/src/renderer/features/square/
  request-intake/
  requests/
  tasks/
  plan/
  acceptance/
  interactions/
  memory/
  review/
  resources/
  exposure/

frontend/src/renderer/routes/
  square-*.tsx
```

The exact filenames must follow the checked-out AO conventions. The important rule is separation by product resource, not the literal path spelling above.

### 9.4 Dependency direction

- Domain contains stable vocabulary and pure reducers only.
- Services own controller-facing use cases and read models.
- `internal/square/*` owns deterministic workflow engines and compilers.
- Ports expose external capabilities.
- Adapters implement ports and remain leaves.
- HTTP and CLI remain protocol translation only.
- Frontend remains a client and does not implement workflow transitions.
- AO `session_manager` remains the execution/session command engine; Square calls it through a narrow execution facade rather than importing adapters directly.

---

## 10. Domain mapping between AO and Square

| AO concept | Square concept | Relationship |
|---|---|---|
| Project | Project | Same durable project identity; Square adds policy/memory/workflow config |
| Session | Attempt execution session | One Square attempt may launch one AO session; retries create new attempts/sessions |
| Runtime handle | Terminal/controller execution handle | Stored by AO; referenced through session/attempt link |
| Worktree | Writer workspace | One writer attempt owns one worktree lease |
| Activity state | Attempt health fact | Input to Square attempt reducer, not the whole task state |
| Display status | UI-derived AO status | May be shown, but Square task/request status derives from Square facts |
| Orchestrator session | Optional model planner/compatibility feature | Not the durable Square Task Manager |
| Conversation | Model-session narrative | May hold a bounded Secretary/planner/worker/reviewer conversation |
| PR | Integration artifact | Optional; local tasks can complete without a PR |
| Review run | Review execution | Linked to Square Review Packet and acceptance criteria |
| Notification | User attention delivery | Interaction/gate remains a durable Square resource |
| `change_log` | CDC/invalidation | Does not replace `square_events` semantic history |

---

## 11. Square records and state machines

### 11.1 Core records

- `SquareRequest`
- `TaskBrief`
- `DispatchPreview`
- `SquarePlan`
- `SquareTask`
- `TaskDependency`
- `AcceptanceCriterion`
- `ExecutionPacket`
- `ContextJob`
- `ContextReport`
- `ContextPack`
- `SquareAttempt`
- `SessionBinding`
- `InteractionRequest`
- `GateDecision`
- `ReviewPacket`
- `Finding`
- `FixTaskLink`
- `CompletionReceipt`
- `EvidenceReference`
- `MemoryEntry`
- `MemoryCandidate`
- `RouteCertification`
- `UsageEntry`
- `ExposureEntry`
- `ResourceProfile`
- `OutcomeEvaluation`

### 11.2 Request states

```text
DRAFT
CLARIFICATION_REQUIRED
PREVIEW_READY
APPROVAL_REQUIRED
QUEUED
RUNNING
PAUSED
BLOCKED
VERIFYING
REVIEWING
SUCCEEDED
FAILED
CANCELLED
```

### 11.3 Task states

```text
PENDING
READY
DISPATCHING
RUNNING
WAITING_INTERACTION
BLOCKED
VERIFYING
REVIEWING
FIX_REQUIRED
SUCCEEDED
FAILED
CANCELLED
SKIPPED
```

### 11.4 Attempt states

```text
CREATED
STARTING
RUNNING
QUIET_ACTIVE
WAITING_FOR_INPUT
WAITING_FOR_APPROVAL
AUTH_REQUIRED
BLOCKED
SUSPECTED_STALL
COMPLETING
CANCELLING
SUCCEEDED
FAILED
CANCELLED
HARD_STOPPED
LOST_PROCESS
```

### 11.5 Memory states

```text
CANDIDATE
ADOPTED_PROJECT
PROMOTION_PROPOSED
ADOPTED_GLOBAL
REJECTED
DEPRECATED
SUPERSEDED
```

All reducers receive explicit time/facts. No reducer reads the clock, filesystem, process table, database, UI state, or model directly.

---

## 12. Persistence design

### 12.1 Schema strategy

Square extends AO’s existing database with forward-only migrations. Tables use a `square_` prefix to reduce collision risk and upstream merge conflicts.

Initial logical tables:

```text
square_schema_registry
square_requests
square_task_briefs
square_dispatch_previews
square_plans
square_tasks
square_task_dependencies
square_acceptance_criteria
square_attempts
square_session_bindings
square_interactions
square_gate_decisions
square_context_jobs
square_context_reports
square_context_packs
square_execution_packets
square_review_packets
square_findings
square_receipts
square_evidence_refs
square_events
square_idempotency_keys
square_writer_leases
square_memory_entries
square_memory_candidates
square_route_certifications
square_usage
square_exposure
square_resource_profiles
square_outcome_evaluations
```

### 12.2 Semantic events versus CDC

- `square_events` is append-only and records domain semantics, causation, correlation, actor, version, and payload reference.
- Current projections update in the same transaction as the semantic event.
- AO database triggers add both projection and event changes to `change_log`.
- CDC/SSE invalidates client caches and supports replay; clients fetch authoritative read models.
- UI status is derived; do not store presentation-only status labels.

### 12.3 Artifact store

Proposed data layout:

```text
~/.square/
  data/square.db
  data/square.db-wal
  data/square.db-shm
  run/running.json
  worktrees/
  artifacts/sha256/ab/<full-hash>
  spool/
  terminal/
  cache/
  logs/
  electron/
```

Large immutable bodies include:

- Task Brief source snapshots;
- Context Reports and compiled Context Packs;
- Execution and Review Packets;
- validation logs;
- receipts;
- evidence manifests;
- exported diffs when Git identity alone is insufficient;
- screenshots and browser evidence;
- bounded terminal chunks when retention is enabled.

Writes use temporary spool files, hash/length validation, atomic rename, and database references. Referenced artifacts are never deleted.

### 12.4 Idempotency and leases

- Every mutating API command accepts or receives an idempotency key.
- Exact duplicate commands return the original result.
- Same key with conflicting command hash returns a typed conflict.
- One writer lease exists per worktree/repository target.
- Leases include holder, generation/fence, acquisition, expiry, and last renewal.
- A stale generation cannot commit task success or receipt application.

---

## 13. API and event contract

### 13.1 Route namespace

Square routes are additive and versioned:

```text
/api/v1/square/requests
/api/v1/square/requests/{id}
/api/v1/square/requests/{id}/preview
/api/v1/square/requests/{id}/approve
/api/v1/square/requests/{id}/pause
/api/v1/square/requests/{id}/cancel
/api/v1/square/tasks
/api/v1/square/tasks/{id}
/api/v1/square/tasks/{id}/retry
/api/v1/square/interactions
/api/v1/square/interactions/{id}/respond
/api/v1/square/memory
/api/v1/square/memory/{id}/promote
/api/v1/square/routes
/api/v1/square/resources
/api/v1/square/evaluations
```

### 13.2 Event behavior

- Existing `/api/v1/events` remains the shared SSE stream.
- Square tables enter CDC and produce Square resource invalidations.
- Each event includes stable resource type, resource ID, change sequence, correlation ID, and schema version.
- Client reconnect uses `Last-Event-ID`.
- Payload-heavy evidence is fetched separately.
- Unknown versions/types are rejected or shown as compatibility placeholders.

### 13.3 Terminal/controller behavior

Square does not create a second terminal transport. It uses AO’s terminal mux/Chat controllers and adds:

- attempt/session binding;
- one interactive-controller authority record;
- typed answer, approval, attach, cancel, checkpoint, and hard-stop commands;
- durable interaction/gate records;
- bounded output/evidence references;
- stage-aware completion/receipt reconciliation.

---

## 14. Workflow architecture

### 14.1 Deterministic Task Manager

The Square Task Manager is an in-daemon software service. It:

- reacts to durable Square events;
- evaluates reducers and policies;
- schedules stages and tasks;
- reserves route/worktree/resource capacity;
- creates bounded AO sessions;
- waits through durable state, not a live model monitor;
- terminates model sessions after required artifacts;
- launches fresh sessions for later stage boundaries;
- enforces finite retry and repair budgets;
- reconciles after daemon restart.

### 14.2 Model roles

- **Secretary:** optional bounded request-clarification/Task Brief compiler.
- **Scout:** read-only bounded repository question worker.
- **Planner/Orchestrator:** fresh bounded plan compiler; ends after Plan Set/dispatch packets.
- **Worker/Grunt:** implements one task contract in one worktree.
- **Reviewer:** fresh independent review against diff/evidence/acceptance.
- **Terminal Triage:** optional low-cost bounded classifier only for unknown terminal state.

No role remains alive simply to supervise another role.

### 14.3 QUICK

QUICK includes:

- request intake;
- deterministic risk/profile check;
- compact Dispatch Preview;
- one narrow task contract;
- one writer AO session/worktree;
- targeted deterministic validation;
- completion receipt;
- evidence and final state;
- optional owner review.

QUICK skips broad Scouts, a high-end planner, general review, clean rebuild, dependency reinstall, and large context scans unless a rule escalates it.

### 14.4 PLANNED

PLANNED includes:

- Task Brief;
- bounded Context Jobs/Reports;
- compiled Context Pack;
- fresh Planner/Orchestrator;
- Plan Set and Acceptance Contract;
- owner approval when policy requires it;
- dependency-aware task dispatch;
- deterministic task validations;
- fresh Review Packet and reviewer;
- bounded finding/fix loop;
- integration verification;
- final receipt and memory candidates.

### 14.5 Local task versus PR workflow

Square supports two completion modes:

- `LOCAL_RESULT`: commit/diff/evidence accepted locally; no remote PR required.
- `PR_RESULT`: AO SCM/PR/CI/review loop is used and linked to Square acceptance.

The profile and project policy select the mode. A non-GitHub local task must remain a first-class workflow.

---

## 15. Memory architecture

### 15.1 Scope

- **Project memory:** only for one project/repository identity.
- **Global memory:** reusable across projects.
- **Session history:** AO conversation/terminal history; not automatically memory.
- **Candidate practice:** proposed lesson with provenance and evidence.

### 15.2 Entry requirements

Each memory entry includes:

- stable ID and version;
- scope;
- concise statement;
- category;
- applicability conditions;
- source project/request/task/attempt;
- evidence/receipt hashes;
- confidence;
- owner/reviewer decision;
- created/adopted/deprecated timestamps;
- supersession relation;
- context-inclusion priority and token estimate.

### 15.3 Promotion

1. A completed workflow may produce candidate lessons.
2. The deterministic service deduplicates and links evidence.
3. The owner reviews candidates.
4. A candidate may become project memory.
5. Global promotion requires an explicit separate owner action.
6. Later outcome evidence may deprecate or supersede an entry.
7. Models never self-authorize policy changes.

### 15.4 Context compilation

Memory is selected by project, task domain, route, trust, recency, outcome, and token budget. The compiler includes concise adopted entries and citations, not entire old transcripts.

---

## 16. Routing, specialists, exposure, cost, and resources

### 16.1 Route registry

Square overlays AO adapters with durable certification facts:

- executable path/hash;
- exact version;
- provider/client/model identity capabilities;
- TUI/Chat/reviewer support;
- structured activity and usage support;
- permission/auth behavior;
- checkpoint/cancel/restore capability;
- certified roles;
- canary version and evidence;
- quarantine reason.

### 16.2 Specialist profiles

Initial profiles:

- General Software Development
- Software Architecture
- UI Engineering
- Test/Quality
- Security
- Performance/Resource
- Integration Review

Specialists are packet/policy profiles, not permanent model personas.

### 16.3 Exposure and cost

- Actual provider tokens when available.
- Labelled estimates otherwise.
- Normalized exposure by model family.
- Rolling and lifetime exposure shares.
- Actual/equivalent/subscription-allocated cost kept separate.
- Route ranking only among already-safe/capable routes.

### 16.4 Resource profiles

Initial profiles:

| Profile | Behavior |
|---|---|
| FAST | Higher allowed session concurrency, normal UI refresh, normal validation breadth |
| NORMAL | Conservative default concurrency and validation |
| SLOW | One heavy writer at a time, reduced polling/render frequency, delayed noncritical indexing/checkpoints |

Temperature and SSD-health telemetry are later extensions. Missing telemetry remains unavailable/degraded, not healthy zero.

---

## 17. UI integration strategy

### 17.1 First principle

Do not replace AO’s usable desktop shell with the full final Square dock before workflow value is proven.

### 17.2 Reused UI

- project/sidebar navigation;
- sessions board;
- selected session Chat/terminal;
- xterm.js terminal;
- inspector rail;
- PR/review/browser panels;
- notifications;
- generated typed API client;
- TanStack Query cache invalidation from SSE;
- Electron daemon discovery and packaging.

### 17.3 Added Square UI

#### Phase 1

- global Square request composer;
- Requests and Tasks navigation;
- Dispatch Preview dialog;
- QUICK workflow status;
- selected task contract/attempt/session relation;
- typed interaction bar;
- receipt/evidence summary;
- project/global memory page.

#### Phase 2

- Plan and Acceptance panes;
- task dependency graph;
- review findings and fix tasks;
- approvals queue;
- exposure/cost/resource indicators;
- workflow event timeline.

#### Phase 3

- layout presets inspired by Operations, Focus, Plan, Review, and Resources;
- multi-terminal observation;
- richer inspector;
- optional dock engine only after license/accessibility/restore evaluation.

### 17.4 Non-negotiable UI behavior

- UI never owns workflow transitions.
- Closing a panel/window only releases subscriptions/controller authority.
- State is not encoded through color alone.
- Terminal output cannot invoke host commands.
- Interactions show exact requested authority and scope.
- Missing telemetry is explicit.
- QUICK hides irrelevant complexity.
- Keyboard and screen-reader paths cover submission, inspection, answer, approval, attach, checkpoint, and cancellation.

---

## 18. CLI strategy

### 18.1 Command identity

The fork introduces `square` as the primary user/agent entry point.

During migration:

- `ao` remains available for upstream compatibility and low-level session diagnostics.
- Square product behavior is exposed only through `square` and `/api/v1/square/*`.
- `square session ...` may delegate to compatible AO session operations.

### 18.2 Initial command surface

```text
square daemon status|doctor|stop
square project add|list|show
square request submit|show|approve|pause|resume|cancel
square task list|show|retry
square terminal list|attach|answer|approve-once|deny|checkpoint|cancel
square memory list|show|promote|deprecate
square route list|probe|certify|quarantine|exposure
square resource status|profile
square events watch|since
square ui
```

Rules:

- `--json` is versioned machine output on stdout.
- diagnostics go to stderr.
- non-interactive mode never waits on a human prompt.
- mutating commands accept an idempotency key.
- exit codes are stable and documented.
- CLI contains no direct storage, runtime, adapter, or workflow logic.

---

## 19. Security and privacy decisions

### 19.1 Initial local transport

For the internal MVP:

- primary listener stays on `127.0.0.1`;
- no LAN listener is enabled by Square by default;
- no named-pipe rewrite is attempted;
- renderer/CLI use generated, allow-listed API contracts;
- terminal output remains untrusted;
- secrets remain in provider CLIs and OS facilities;
- browser/preview bridges remain target-isolated;
- destructive actions use typed commands and confirmations.

A later threat-model task decides whether a per-install local token is required. It must not be added casually because it changes all clients and upstream assumptions.

### 19.2 Data and telemetry

- Set `VITE_AO_POSTHOG_KEY` empty in Square builds.
- Remove/disable AO telemetry defaults in the fork.
- Do not transmit paths, prompts, diffs, terminal output, memory, or project identifiers.
- Diagnostics export is opt-in, previewed, allow-listed, and redacted.

### 19.3 Update channel

- Official AO updater must not replace Square with an AO binary.
- Until Square release infrastructure exists, updater is disabled.
- Later updates use a Square feed, signed artifacts, state backup, migration checks, and rollback guards.

---

## 20. Fork and upstream governance

### 20.1 Git model

```text
upstream/v0.12.1       immutable base tag
upstream/main          observed moving upstream
square/main            stable Square integration branch
feature/SAxx-Tyy-*     task branches
upstream-sync/<ver>    reviewed upstream merge branches
release/<version>      Square release stabilization
```

### 20.2 Initial setup

```powershell
git clone --branch v0.12.1 https://github.com/Untrivial-ai/agent-orchestrator.git square-orchestrator
cd square-orchestrator
git remote rename origin upstream
git remote add origin <YOUR_SQUARE_FORK_URL>
git switch -c square/main
git tag square-base-v0.12.1
git push -u origin square/main
git push origin square-base-v0.12.1
```

### 20.3 Merge discipline

- Prefer Square additions in new packages, routes, tables, and feature directories.
- Modify generic AO behavior only when a shared platform defect or required extension cannot be isolated.
- Never mix upstream sync and Square feature work in one commit.
- Record every cherry-pick/merge and conflict resolution in `docs/square/upstream-ledger.md`.
- Run the complete adoption, backend race, frontend typecheck/build, Windows lifecycle, migration, and Square workflow suites after every upstream sync.
- Do not rebase published Square history over moving upstream.

### 20.4 Licensing and identity

- Preserve Apache-2.0 license and required notices.
- Mark modified files or maintain an aggregate changes notice as required by the license process.
- Rebrand executable, app name, icon, installer, protocol identity, data directory, telemetry identity, and update feed.
- Do not present Square as an official Untrivial product.

---

## 21. Migration from the .NET research line

### 21.1 Archive

Rename/preserve the previous repository as:

```text
square-orchestrator-dotnet-research
```

Do not delete it. Do not merge its generated binaries or C# source into the AO fork.

### 21.2 Transfer

Copy into `docs/square/legacy/` or a linked design repository:

- sliced implementation plan;
- technical architecture;
- UI design and handover;
- accepted product rules;
- ConPTY/Job Object findings;
- named-pipe and shared-UI proof records;
- failure reports and compatibility findings;
- relevant acceptance fixtures.

### 21.3 Translate behavior, not code

- Port Windows terminal test scenarios into Go/AO integration tests.
- Port task/plan/acceptance/memory/workflow contracts into Go domain and OpenAPI.
- Port UI concepts into React features.
- Do not port WPF, C# persistence, .NET IPC, or prototype project structure.

### 21.4 Historical status

The old G0 gate no longer blocks the new architecture. Its results become risk evidence for SA00–SA01. A new AO adoption gate replaces it.

---

# 22. Release gates

| Gate | Included sub-plans | Required result |
|---|---|---|
| A0 — Fork adoption | SA00 | Reproducible pinned fork, clean legal/rebrand/data/update boundary |
| A1 — Windows foundation | SA01 | Desktop closure, daemon/session lifecycle, ConPTY, worktree, restart and path behavior proven or repaired |
| A2 — Square contracts | SA02 | Pure records/reducers/contracts stable and generated API types agree |
| A3 — Durable Square state | SA03 | Migrations, semantic events, artifacts, idempotency, leases and recovery pass |
| A4 — Square Core Alpha | SA04 | One real certified route completes QUICK through CLI and desktop |
| A5 — Memory/context | SA05 | Project/global memory and bounded Context Packs work |
| A6 — Square MVP | SA06–SA07 | PLANNED workflow, interactions, review/fix and receipts pass on one route |
| A7 — Operations workspace | SA08–SA09 | Desktop/CLI and optional VS Code show one authoritative state |
| A8 — Adaptive routes | SA10 | Specialists, route certification, independent review and route plurality work |
| A9 — Sustained operation | SA11 | Exposure/cost/resource profiles, caching and outcome evaluation are enforced |
| A10 — Windows release | SA12 | Signed/reviewed installer/update/diagnostics/security/upstream process qualifies |
| A11 — Optional scale | SA13 | Advanced parallelism/practice evolution enabled only with evidence |

---

# 23. Dependency overview

```text
SA00 Fork adoption
  └─ SA01 Windows foundation
      └─ SA02 Square contracts
          └─ SA03 Persistence/artifacts
              ├─ SA04 QUICK Core
              │   └─ SA05 Memory/context
              │       └─ SA06 PLANNED workflow
              │           └─ SA07 Interactions/review/receipts
              │               ├─ SA08 Desktop workspace
              │               └─ SA09 CLI/API/VS Code parity
              │                   └─ SA10 Routes/specialists
              │                       └─ SA11 Resources/evaluation
              │                           └─ SA12 Release hardening
              │                               └─ SA13 Optional scale
```

SA08 may begin against recorded fixtures once SA02 contracts stabilize. It must not invent backend behavior.

---

# SA00 — Fork adoption, legal boundary, and reproducible baseline

## Outcome

Create a clean, reproducible Square fork from AO `v0.12.1`, isolate its identity/data/update behavior, and record exactly what upstream provides before product changes begin.

## SA00-T01 — Create and pin the downstream fork

**Implementation**

- Clone tag `v0.12.1` and verify commit `1df40e9`.
- Configure `upstream` and Square `origin` remotes.
- Create `square/main` and `square-base-v0.12.1` tag.
- Add `docs/square/BASELINE.md` containing tag, commit, release asset hashes, build environment, and source-document hashes.
- Add `docs/square/upstream-ledger.md`.
- Import preserved Square specifications into a clearly marked design/reference directory without editing upstream docs.

**Validation**

- Git status clean.
- Tag/commit match.
- Remote roles verified.
- No generated build output committed.

**STOP if** source identity, license, or tag cannot be verified.

## SA00-T02 — Build and test the unmodified upstream baseline on Windows

**Implementation**

Run from a clean Windows checkout:

```powershell
npm ci
npm run lint
npm run frontend:typecheck

Push-Location backend
go build ./...
go test ./...
go test -race ./...
go vet ./...
Pop-Location

Push-Location frontend
npm run typecheck
npm run test
npm run build
npm run package
Pop-Location
```

- Record all pre-existing Windows failures without repairing them in the baseline commit.
- Package the desktop app locally.
- Start a supported agent CLI in a disposable repository.

**Evidence**

- tool versions;
- logs;
- failed-test inventory;
- packaged app identity;
- daemon/desktop startup logs.

**STOP if** the pinned release cannot build or run on Windows without an unreviewed architecture replacement.

## SA00-T03 — License, notices, telemetry, updater, and data-directory isolation

**Implementation**

- Preserve Apache-2.0 and notices.
- Add `NOTICE-SQUARE.md` and `docs/square/third-party-baseline.md`.
- Rebrand application display name and package identity minimally.
- Default data to `~/.square` using `SQUARE_DATA_DIR`.
- Ensure official AO and Square can coexist without sharing database, running file, Electron userData, worktrees, telemetry identity, or updater state.
- Disable PostHog transmission by default.
- Disable official AO auto-update until Square has its own feed.

**Tests**

- AO and Square installed side by side.
- Separate processes, databases, ports/run files, worktrees, and Electron profiles.
- No telemetry request under default build.
- No update request to AO release feed.

## SA00-T04 — Architecture amendment and coding-agent rules

**Implementation**

- Add `docs/square/architecture-amendment-0001-ao-foundation.md`.
- Add Square-specific instructions to `AGENTS.md` without weakening upstream hard rules.
- Define allowed package boundaries, migration naming, API namespace, generated-code rules, and upstream-merge rules.
- Record which previous Square decisions are preserved, superseded, or parked.

## SA00-T05 — Adoption gate A0

**Acceptance**

- exact upstream base is reproducible;
- Windows source build/package works or all blockers have accepted repairs;
- legal attribution is complete;
- Square and AO identities/data do not collide;
- telemetry/updater behavior is safe;
- new authority hierarchy is recorded.

---

# SA01 — Windows lifecycle and platform hardening

## Outcome

Prove that the AO foundation satisfies Square’s non-negotiable lifecycle behavior before Square workflow logic depends on it.

## SA01-T01 — Decouple daemon/workflows from desktop window lifetime

**Implementation**

- Audit Electron daemon supervision and quit behavior.
- Make window close hide/exit the client without terminating a daemon that owns live sessions, pending workflow wake-ups, or reconciliation work.
- Define explicit `Quit UI`, `Stop daemon`, and `Exit when idle` actions.
- On desktop reopen, discover the existing compatible daemon and reconnect without duplicate launch.
- Preserve update-safe shutdown as a separate controlled path.

**Tests**

- close desktop during active TUI session;
- close during Chat session;
- close while waiting for input;
- reopen and reconnect to same session/controller generation;
- no duplicate daemon/session/worktree;
- explicit stop is denied or gated while live work exists.

## SA01-T02 — Windows ConPTY and process-tree containment

**Implementation**

- Audit AO ConPTY runtime against preserved TerminalProof scenarios.
- Add Windows fixtures for Unicode, ANSI, burst, stdin, resize, quiet child, normal exit, crash, graceful cancel, forced stop, and nested descendants.
- Establish exact process identity and descendant containment.
- If AO lacks complete process-tree ownership, add a Windows Job Object or equivalent contained runtime behind the existing runtime port.
- Never infer death from one failed probe.

**Tests**

- repeated runs and 1/4/8 concurrency;
- output does not leak into parent console;
- no surviving descendants;
- no stale handle/process identity;
- controlled close/kill semantics.

## SA01-T03 — Restart reconciliation and session/controller recovery

**Implementation**

- Test daemon crash/restart with TUI and Chat sessions.
- Reconcile runtime, agent process, worktree, session facts, controller generation, and pending messages.
- Do not mark a bare shell or missing agent as healthy idle.
- Produce typed `RECOVERED`, `RESTART_REQUIRED`, `LOST_PROCESS`, or `ORPHAN_QUARANTINED` results.
- Never silently create a second agent for the same attempt.

## SA01-T04 — Windows worktree cleanup and dirty-state safety

**Implementation**

- Separate process termination success from worktree cleanup result.
- Wait/retry Windows handle release with bounded backoff.
- Preserve dirty worktrees and report cleanup warnings.
- Add later garbage collection for safe, registered, clean orphan worktrees.
- Never turn “session killed, cleanup pending” into an opaque internal error.

## SA01-T05 — Local daemon identity and transport safety

**Implementation**

- Ensure one daemon per Square data directory.
- Verify loopback-only binding.
- Validate Host/Origin/CORS behavior for Electron and CLI.
- Prevent a second daemon from concurrently migrating/writing the same database.
- Maintain request IDs and typed errors.

## SA01-T06 — Windows foundation gate A1

**Acceptance**

- UI closure is harmless;
- daemon reconnect is deterministic;
- all terminal fixtures pass;
- process trees are contained;
- restart cannot create zombie/duplicate sessions;
- worktree cleanup is safe and typed;
- one daemon owns the database.

---

# SA02 — Square contracts and deterministic domain kernel

## Outcome

Define Square’s stable records, reducers, policies, schemas, and API shapes without launching models or processes.

## SA02-T01 — IDs, versions, hashes, time, and result primitives

- Reuse AO’s existing ID generator where safe; add stable Square resource prefixes.
- Add typed Go IDs for request, task, attempt, interaction, gate, criterion, artifact, receipt, memory, route, and event.
- Add injected clock and ID interfaces.
- Add canonical UTC and SHA-256 representations.
- Add versioned problem/error catalogue.

## SA02-T02 — Pure lifecycle reducers

- Implement Request, Task, Attempt, Interaction, Gate, Memory, Route, Resource, and Circuit Breaker reducers.
- Exhaustively test legal/illegal transitions, finality, duplicate events, and explicit deadlines.
- AO activity/runtime/SCM facts enter as typed observations.

## SA02-T03 — Task Brief, plan, packet, acceptance, review, receipt, and evidence contracts

- Define versioned Go DTOs and OpenAPI schemas.
- Acceptance criteria require ID, falsifiable result, verifier, evidence type, pass authority, severity, and immutable baseline/result reference.
- Task contracts require dependencies, read/write scope, validations, budgets, discretion, prohibited decisions, and STOP conditions.
- Golden examples live under `docs/square/contracts/`.

## SA02-T04 — Trust, authority, and workflow profile policy

- Allowed project roots and path scopes.
- Writer eligibility and dirty-state gates.
- network/command/tool capabilities.
- approval authorities.
- prohibited nested coding agents unless explicitly scheduled.
- QUICK/BOUNDED/PLANNED/SYSTEMIC profile definitions.
- `PolicyDecision` with rule ID, explanation, and remediation.

## SA02-T05 — Square API and generated frontend contract

- Add OpenAPI DTOs for Square resources.
- Run `npm run api` and drift checks.
- Reject unknown/unsupported schema versions where required.
- Keep controller DTOs out of domain.

## SA02-T06 — Contract gate A2

**Acceptance**

- pure packages contain no HTTP/SQLite/adapter/UI dependencies;
- every state transition and policy matrix is tested;
- all examples validate;
- Go/OpenAPI/TypeScript shapes agree;
- API/version compatibility is recorded.

---

# SA03 — Durable Square state, artifacts, idempotency, and recovery

## Outcome

Extend AO persistence without bypassing its migration, sqlc, CDC, or service boundaries.

## SA03-T01 — Square migrations and schema registry

- Add ordered forward migrations with `square_` tables.
- Never edit upstream/applied migrations.
- Add migration inventory/checksum evidence.
- Support empty creation, upgrade, unsupported-newer refusal, and backup-before-destructive migration where needed.

## SA03-T02 — Semantic events and current projections

- Append `square_events` and mutate projections in one transaction.
- Enforce event immutability.
- Add causation/correlation/actor/schema fields.
- Ensure Square triggers feed AO `change_log` exactly once.

## SA03-T03 — Content-addressed artifact store

- Implement SHA-256 spool/atomic move/read validation/deduplication.
- Store metadata/references in SQLite.
- Reconcile orphan spool and unreferenced files safely.
- Keep high-volume bodies out of SQLite.

## SA03-T04 — Idempotency, leases, and session bindings

- Idempotent command records.
- Writer leases with fencing.
- attempt-to-AO-session/worktree/controller-generation binding.
- exactly-once receipt application.
- stale session/controller cannot complete a newer attempt.

## SA03-T05 — Recovery and migration test matrix

- crash injection around event/projection commit;
- artifact spool/move/metadata boundaries;
- daemon restart with pending launch/receipt;
- interrupted migration;
- concurrent-daemon refusal;
- corruption/backup fixtures.

## SA03-T06 — Durable state gate A3

---

# SA04 — Square Core QUICK workflow

## Outcome

Deliver the smallest useful Square experience through one certified agent route.

## SA04-T01 — Request intake and compact Task Brief

- Accept project, natural-language task, constraints, priority, optional profile, and local/PR completion mode.
- Use deterministic parsing for explicit fields.
- Invoke a bounded Secretary only when ambiguity/risk requires it.
- Material ambiguity creates `CLARIFICATION_REQUIRED`.

## SA04-T02 — Deterministic triage and Dispatch Preview

- Score scope, risk, design novelty, security, context need, validation cost, resource class, and approval need.
- Enable QUICK only initially.
- Display route, paths, validation, worktree mode, permissions, estimated context/resource class, and reasons.
- Owner can approve/edit/deny according to policy.

## SA04-T03 — QUICK packet compiler

- One exact task contract.
- Narrow canonical paths.
- explicit non-change rules.
- targeted validations.
- receipt/evidence requirements.
- escalation conditions.
- no broad transcript/context dump.

## SA04-T04 — Square Task Manager and AO execution facade

- Add a narrow facade over AO session creation/send/kill/status.
- Create AO worktree/session from an accepted Square attempt.
- Persist intent/event before spawn.
- Bind session/worktree/controller generation immediately.
- Scheduler returns control after launch and wakes from events.

## SA04-T05 — Validation and completion receipt

- Run task-scoped deterministic commands outside the model session where practical.
- Ingest/validate receipt or construct a daemon evidence result from exact commit/diff/validation/session facts.
- Reconcile process/session exit, worktree, changed paths, validation, and required artifacts.

## SA04-T06 — Minimal CLI and desktop flow

- `square request submit/show/cancel`.
- request composer and Dispatch Preview.
- selected task/attempt/session terminal.
- status and evidence summary.
- interaction notification.

## SA04-T07 — Real QUICK pilot and Alpha gate A4

Use a reversible small task in a disposable repository. Prove no broad scan, no planner, one writer, targeted validation, safe UI closure, cancellation, and final evidence.

---

# SA05 — Project/global memory and bounded context

## Outcome

Give Square durable, owner-controlled learning and efficient context handoffs.

## SA05-T01 — Memory schema/service/API

- project/global scopes;
- candidate/adopted/deprecated/superseded states;
- provenance and evidence;
- deduplication and versioning;
- no direct transcript promotion.

## SA05-T02 — Candidate extraction and owner promotion

- completed workflows may propose concise candidates;
- owner accepts/rejects/edits;
- global promotion is separate from project adoption;
- every change is audited.

## SA05-T03 — Context Scout jobs

- bounded read-only question, snapshot, path, command, output, and citation contract;
- use AO sessions in read-only worktrees or existing project snapshot capability;
- end Scout after report artifact.

## SA05-T04 — Context Pack compiler

- wait through durable events;
- deduplicate findings;
- verify hashes/citations;
- include adopted memory under token budget;
- omit raw transcripts;
- create `CONTEXT_BLOCKED` on missing required evidence.

## SA05-T05 — Memory/context UI and CLI

- memory list/show/promote/deprecate;
- context jobs/reports/pack inspection;
- provenance and token budget display.

## SA05-T06 — Memory/context gate A5

---

# SA06 — PLANNED workflow and bounded orchestration

## Outcome

Implement planning depth without a persistent model monitor.

## SA06-T01 — Secretary Task Brief compiler

- bounded model input/output schema;
- goal, visible outcome, scope, constraints, ambiguities, risks, candidate profile, owner questions;
- session ends after artifact.

## SA06-T02 — Planner/Orchestrator and Plan Set compiler

- launch fresh bounded planner with Task Brief, Context Pack, critical references, authority hashes, and budget;
- validate DAG, stable IDs, scopes, criteria, discretion, STOP conditions, validations, integration strategy;
- reject cycles, overlap, vague acceptance, missing owner decisions;
- session ends after accepted plan or blocker.

## SA06-T03 — Plan acceptance and amendment

- owner approve/edit/deny according to policy;
- immutable plan versions/hashes;
- amendments show exact contract changes;
- existing work cannot silently inherit changed requirements.

## SA06-T04 — Dependency scheduler and bounded handovers

- task readiness, priority, aging, writer/route/resource reservations;
- model context thresholds and handover artifacts;
- no live model during queue/resource wait;
- no two writers in one worktree.

## SA06-T05 — Deterministic verification and integration

- task-scoped checks before review;
- combined integration checks for multi-task plans;
- immutable evidence tied to result commit.

## SA06-T06 — Restart-safe PLANNED workflow

- reconcile stage/session/worktree/receipt after daemon restart;
- never silently rerun a completed or uncertain writer;
- owner-gate ambiguous recovery.

---

# SA07 — Interactions, review, findings, fix loop, and receipts

## Outcome

Complete the Square MVP safety and quality loop.

## SA07-T01 — Typed durable interactions

- question;
- permission;
- approval;
- authentication/manual takeover;
- blocker;
- unknown terminal state;
- force-stop gate.

Map AO activity/controller facts into Square interactions without scraping ordinary model prose as authority.

## SA07-T02 — Interaction response and controller authority

- answer, approve once, deny, amend, attach, checkpoint, graceful cancel, hard stop;
- authority, expiry, attempt/session/controller-generation validation;
- secrets never enter events/artifacts.

## SA07-T03 — Review Packet and independent reviewer

- compile task contract, diff, evidence, relevant authority, risks, and acceptance map;
- omit implementation transcript;
- select independent route/model family when eligible;
- use AO reviewer process through a constrained packet/worktree.

## SA07-T04 — Finding and finite fix loop

- findings reference stable acceptance IDs and immutable commits;
- accepted findings create separate FIX tasks;
- finite retry/fix budget;
- new requirements/architecture/security decisions require amendment.

## SA07-T05 — Receipt and outcome reconciliation

- validate IDs, nonce, hashes, commit, changed paths, validations, outcome, usage;
- duplicate receipt idempotent;
- conflicting receipt becomes a durable finding;
- final acceptance binds plan/commit/evidence hashes.

## SA07-T06 — Square MVP gate A6

A real route completes QUICK and PLANNED repeatedly, including question, owner approval, injected finding, one fix, UI close/reopen, and restart recovery.

---

# SA08 — Square operations workspace

## Outcome

Add Square’s product experience without relocating authority into React/Electron.

## SA08-T01 — Square design tokens and product identity

- rebrand app and navigation;
- accessible state tokens;
- text/icon/shape states;
- retain AO frontend conventions and component stack.

## SA08-T02 — Request/task navigation and command bar

- project selector;
- global request field;
- Requests/Tasks/Approvals/Memory navigation;
- live-work summary and pause controls.

## SA08-T03 — Task graph, plan, and acceptance

- task DAG;
- stable IDs;
- current writer/blocked dependencies;
- Plan and Acceptance tables;
- amendment comparison.

## SA08-T04 — Agent Fleet and terminal interaction bar

- role, route/model, task, attempt, state, elapsed, writer/read-only, attention reason;
- terminal controller authority;
- typed question/approval/auth/blocker/stall bars;
- closing pane never stops process.

## SA08-T05 — Review, memory, events, exposure, and resources

- diff/findings/evidence;
- memory candidates/promotion;
- typed event timeline;
- separate exposure/cost/context/resource indicators.

## SA08-T06 — Layout presets and bounded rendering

- Operations, Focus, Plan, Review, Resources presets;
- reuse current shell first;
- full dock library only after separate decision;
- high-volume terminal and large-list virtualization.

## SA08-T07 — UI gate A7-part1

---

# SA09 — CLI/API parity and VS Code client

## Outcome

Make Square operable by humans, scripts, agents, desktop, and later VS Code through one daemon API.

## SA09-T01 — Complete `square` CLI

- stable commands, JSON output, errors, exit codes, correlation/idempotency IDs;
- daemon discovery/start/doctor;
- no direct storage/runtime access.

## SA09-T02 — Cross-client contract/replay tests

- CLI/Desktop same final result;
- SSE reconnect/replay;
- incompatible schema/version behavior;
- no optimistic authority-changing UI state.

## SA09-T03 — Minimal VS Code extension

- activation on Square use;
- loopback API client in extension host;
- Activity Bar tree/status;
- editor panel hosting shared Square views where feasible;
- webviews receive validated messages only;
- file/diff navigation through extension host.

## SA09-T04 — Cross-host gate A7-part2

Closing VS Code or Electron changes no workflow/session state. CLI/Desktop/VS Code show the same event/interaction outcome.

---

# SA10 — Route certification, specialists, skills, and route plurality

## Outcome

Turn AO’s broad adapter catalogue into a trusted Square route registry.

## SA10-T01 — Adapter conformance and route certification

- discovery, exact version, auth readiness, TUI/Chat/reviewer capabilities, prompt/activity, identity, usage, cancel/restore, receipt bridge;
- canary fixtures and quarantine.

## SA10-T02 — First certified route

- owner selects safest available route after evidence;
- capture redacted fixtures;
- fail closed outside certified range.

## SA10-T03 — Specialist and Skill Profiles

- profiles, capabilities, risks, source/version/hash, qualification, expiry, revocation;
- unverified skill cannot dispatch.

## SA10-T04 — Temporary specialist teams

- independent read-only packets against one snapshot;
- one primary planner owns synthesis;
- specialists do not continuously converse or edit one plan artifact.

## SA10-T05 — Independent review policy

- route/model-family independence where eligible;
- explicit gate when no independent reviewer exists.

## SA10-T06 — Additional certified routes and gate A8

---

# SA11 — Exposure, cost, resource profiles, context reuse, and evaluation

## Outcome

Make sustained use efficient and measurable without rebuilding a distributed platform.

## SA11-T01 — Usage and Model Exposure Ledger

- actual/estimated tokens;
- normalized exposure by model family/role/project;
- rolling/lifetime shares;
- consecutive assignments;
- confidence and source.

## SA11-T02 — Cost indicators

- dated rate schedules;
- actual/equivalent/subscription-allocated values;
- confidence;
- never substitute cost for exposure or capability.

## SA11-T03 — FAST/NORMAL/SLOW scheduler policy

- session, writer, heavy-validation, indexing, polling, and rendering limits;
- no model held during cooldown/wait;
- owner-visible reason for delay.

## SA11-T04 — Snapshot-aware repository index and Context Pack cache

- content-addressed metadata/symbol index;
- commit/path invalidation;
- compatible evidence reuse only;
- provenance.

## SA11-T05 — Buffered persistence and resource telemetry

- batch noncritical metrics;
- critical transitions commit before action;
- terminal/output retention policy;
- disk/free-space/I/O signals when available;
- unavailable remains degraded.

## SA11-T06 — Outcome Evaluation

- acceptance, findings/fix loops, human corrections, timings, tokens, exposure, cost, retries, resource delay, and unnecessary STOP;
- cohort/sample/confidence rather than one global leaderboard.

## SA11-T07 — Sustained-operation gate A9

---

# SA12 — Security, diagnostics, packaging, updater, and upstream release process

## Outcome

Ship a supportable Square Windows build without losing the ability to merge upstream.

## SA12-T01 — Threat model and security closure

Cover:

- loopback API clients;
- Electron preload/renderer bridge;
- terminal escape/output;
- repository prompt injection;
- path/reparse escape;
- agent executable replacement;
- receipt spoofing;
- browser bridge;
- secrets;
- updater;
- memory poisoning;
- skill/adapter qualification.

## SA12-T02 — Privacy-safe diagnostics

- daemon/DB/migration/session/worktree/adapter/route checks;
- redacted opt-in bundle;
- manifest preview before creation;
- no credentials or unrestricted transcript export.

## SA12-T03 — Windows installer and Square identity

- x64 signed installer first;
- `square` PATH command;
- separate data/update/app identity;
- per-user install;
- repair/uninstall with retain/remove state choice;
- never delete repositories or dirty worktrees.

## SA12-T04 — Safe update and rollback

- stop new dispatch;
- checkpoint/reconcile active work;
- back up state;
- migrate;
- replace binaries;
- restart/validate;
- rollback binary when schema allows;
- never kill active writer merely to update.

## SA12-T05 — Upstream sync automation and SBOM

- source/base ledger;
- reviewed dependency diff;
- license/notice update;
- generated-code drift;
- complete regression matrix;
- one publisher;
- signed artifacts/checksums/SBOM.

## SA12-T06 — Windows release gate A10

---

# SA13 — Optional advanced scale and controlled practice evolution

Disabled by default until measured need.

## SA13-T01 — Parallel read-only Scouts/specialists

Shared pinned snapshot/index/resource budget; deterministic collection.

## SA13-T02 — Parallel Square writers

AO already supplies isolated worktrees. Square enables concurrent writers only for non-overlapping accepted tasks, with overlap preflight, planned integration order, combined checks, and merge gates.

## SA13-T03 — Automatic bounded fix dispatch

Only for accepted narrow findings, finite retries, unchanged acceptance, and eligible routes.

## SA13-T04 — Controlled practice evolution

Outcome evidence proposes candidates; owner approves trials/adoption/deprecation; no self-authorizing policy changes.

## SA13-T05 — Scale gate A11

---

## 24. Cross-cutting acceptance criteria

| ID | Falsifiable result |
|---|---|
| SA-AC-01 | Closing Electron or VS Code does not stop or duplicate a daemon-owned session/workflow |
| SA-AC-02 | One compatible daemon owns one Square data directory/database |
| SA-AC-03 | All known terminal fixture states are classified without an LLM monitor |
| SA-AC-04 | Quiet-but-active work is not failed or force-stopped |
| SA-AC-05 | Every writer attempt owns one AO worktree lease and controller generation |
| SA-AC-06 | Daemon restart cannot duplicate a writer, session binding, semantic event, or receipt |
| SA-AC-07 | QUICK avoids Scouts, broad planning, broad scans, and full validation unless escalated |
| SA-AC-08 | PLANNED packets omit raw Scout/worker transcripts |
| SA-AC-09 | 100K/120K/150K context thresholds warn, hand over, and stop as defined |
| SA-AC-10 | A worker cannot write outside declared canonical path scope |
| SA-AC-11 | Vague or incomplete acceptance criteria cannot dispatch |
| SA-AC-12 | Review findings/fixes reference stable criteria and immutable commits |
| SA-AC-13 | Model exposure, context, actual cost, equivalent cost, and subscription allocation remain distinct |
| SA-AC-14 | Missing token/cost/resource telemetry is unknown/degraded, not zero |
| SA-AC-15 | A model session ends after Secretary, Scout, Plan, Worker, or Review artifact boundary |
| SA-AC-16 | Project/global memory promotion always requires explicit owner-controlled state transition |
| SA-AC-17 | No transcript becomes adopted memory automatically |
| SA-AC-18 | CLI/Desktop/VS Code display the same final event and interaction outcome |
| SA-AC-19 | Terminal output cannot invoke an arbitrary Electron/extension-host command |
| SA-AC-20 | Unverified/revoked skill or uncertified/quarantined route cannot dispatch |
| SA-AC-21 | Only-eligible route warns rather than selecting an unsafe alternative |
| SA-AC-22 | Two heavy stages obey the selected resource profile without holding an idle model session |
| SA-AC-23 | Square and official AO installations do not share state, updater, telemetry, process identity, or worktrees |
| SA-AC-24 | Failed worktree cleanup never destroys dirty work or hides successful process termination |
| SA-AC-25 | Semantic event and current projection commit atomically |
| SA-AC-26 | Existing applied AO or Square migrations are never edited |
| SA-AC-27 | A local task can complete without GitHub/PR when policy selects `LOCAL_RESULT` |
| SA-AC-28 | An upstream sync cannot merge until Square adoption, migration, workflow, UI, and Windows lifecycle suites pass |
| SA-AC-29 | Telemetry is off by default and update source cannot install official AO over Square |
| SA-AC-30 | Installer/update/uninstall never deletes repositories or uncommitted user work |

---

## 25. Required test suites

| Suite | Scope |
|---|---|
| `SquareDomain` | Pure reducers, policies, routing, memory, exposure, fairness, breakers |
| `SquareContracts` | OpenAPI/Go/TypeScript parity, versions, golden examples |
| `SquarePersistence` | Migrations, semantic events, atomicity, artifacts, idempotency, leases, crash points |
| `WindowsRuntime` | ConPTY, process tree, input/output/resize/cancel, UI close, restart, handle/process leaks |
| `WorkspaceRecovery` | Worktree creation/cleanup, dirty safety, branch collisions, restart reconciliation |
| `AOAdapterConformance` | Exact installed CLI versions, activity, auth, permissions, cancel/restore, usage |
| `SquareWorkflow` | QUICK, PLANNED, interactions, review/fix, restart, receipt, local/PR completion |
| `SquareMemory` | candidate extraction, dedupe, promotion, context selection, deprecation |
| `FrontendUnit` | reducers, query invalidation, components, accessibility, interactions |
| `FrontendE2E` | request submit, terminal, approval, close/reopen, plan/review/memory flows |
| `CliContract` | JSON/human output, exit codes, daemon errors, idempotency, redirected streams |
| `VscodeIntegration` | activation, API client, webview validation, restoration, close safety |
| `PerformanceResource` | terminal/output, DB writes, context cache, concurrency profiles |
| `Security` | local API, Electron bridge, path scope, prompt/receipt spoofing, redaction, updater |
| `UpstreamSync` | baseline build, generated code, migrations, API drift, Square regression |

Provider-backed tests are not part of deterministic required CI. Route certification runs only on authorized machines and stores redacted evidence.

---

## 26. Commit and review discipline

- One task normally produces one commit: `<task-id>: <imperative outcome>`.
- Keep upstream merge commits distinct from Square feature commits.
- Generated sqlc/OpenAPI/TypeScript output is committed with source changes.
- A completion receipt records start/end commits, changed paths, validations, evidence hashes, remaining risk, route/session outcome, and discretion.
- Reviewers inspect packet, diff, validations, and evidence—not the complete terminal transcript.
- Failed attempts end with evidence. Fixes use a new attempt/task.
- No drive-by cleanup, broad rename, formatting churn, or speculative abstraction.
- Never hand-edit sqlc-generated files.
- Never place Square workflow logic in CLI, Electron renderer, adapter, or HTTP controller.

---

## 27. Initial dispatch order

1. Owner accepts this architecture amendment and the new locked decisions.
2. Dispatch SA00-T01 through SA00-T04 sequentially.
3. Complete A0 before Square product code.
4. Dispatch SA01 Windows lifecycle tasks; terminal and worktree tasks may run in parallel only on separate branches/worktrees.
5. Complete A1.
6. Dispatch SA02 in dependency order and freeze `square.contracts 1.0-draft` at A2.
7. Complete SA03 persistence/artifacts.
8. Implement SA04 QUICK and use it as the first internal Alpha.
9. Add SA05 memory/context.
10. Implement SA06–SA07 and reach Square MVP A6.
11. Build SA08 UI progressively from recorded fixtures while SA04–SA07 backend work proceeds.
12. Complete CLI/API parity, then VS Code.
13. Add route plurality/specialists only after one route is certified and useful.
14. Add exposure/resource/evaluation after real workflow data exists.
15. Release hardening follows sustained internal use.
16. Keep SA13 disabled until measured evidence justifies it.

---

## 28. Decisions resolved by this plan

- Use AO as the production base.
- Use Go/Electron/React/SQLite/REST-SSE-WebSocket rather than .NET/WPF/named pipes.
- Pin initial base to stable `v0.12.1`/`1df40e9`.
- Windows x64 first.
- `square` remains product/command name.
- Keep AO session/runtime/worktree/adapter infrastructure.
- Build a deterministic Square Task Manager, not a persistent monitoring model.
- Support both local and PR completion.
- Disable telemetry by default.
- Disable/replace official AO updater before distribution.
- Keep mobile and advanced parallelism out of MVP.
- Use project/global memory with owner promotion.
- Deliver QUICK before PLANNED.
- Do not build a full docking system before core workflow value is proven.

---

## 29. Decisions deferred to named tasks

- Exact Square icon and visual brand.
- Whether local loopback API requires an installation token after threat-model review.
- Exact public ID encoding if AO lacks a suitable existing generator.
- First certified real route after adapter evidence.
- Default owner approval requirement for QUICK.
- Default project trust/network policy.
- Terminal/artifact retention.
- Exposure weights and windows.
- Resource-profile numeric limits.
- Optional docking library.
- VS Code terminal rendering versus native terminal attachment.
- Windows arm64 timing.
- Whether mobile is ever re-enabled.

Conservative prototype defaults may be used, but they cannot silently become release policy.

---

## 30. Definition of milestones

### Adoption baseline

A0–A1 pass. The fork is buildable, legally isolated, safe to run beside AO, and reliable enough on Windows to host Square work.

### Square Core Alpha

A4 passes. One real route completes a QUICK request with preview, worktree, terminal, validation, receipt, cancellation, and desktop/CLI visibility.

### Square MVP

A6 passes. QUICK and PLANNED work through one certified route; project/global memory, deterministic context/plan/task management, typed interactions, review/fix, receipts, restart recovery, and safe UI closure are operational.

### First release candidate

A7–A10 pass. Desktop/CLI/VS Code parity, route plurality, resource/evaluation indicators, security, updater, installer, diagnostics, attribution, and upstream sync qualify.

The product should not wait for specialists, four route families, thermal telemetry, mobile, or advanced parallel writers before the MVP is useful.

---

## 31. Immediate first task

The first implementation task is:

```text
SA00-T01 — Create and pin the downstream fork
```

It must perform no product feature work. Its output is a clean Square fork, baseline tag/commit verification, remote/branch structure, authority import, and upstream ledger. SA00-T02 then establishes the unmodified Windows build/test baseline before any rebrand or lifecycle repair.

