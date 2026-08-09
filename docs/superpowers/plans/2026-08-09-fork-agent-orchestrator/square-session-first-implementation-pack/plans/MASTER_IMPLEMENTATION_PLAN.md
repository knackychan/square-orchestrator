# Square on Agent Orchestrator — Session-First Sliced Implementation Plan

- Date: 2026-08-09
- Status: implementation-ready draft; owner acceptance required before dispatch
- Supersedes: `docs/PREVIOUS_COMBINED_PLAN_SUPERSEDED.md` for undispatched work
- Product name: Square Orchestrator
- Primary command: `square`
- Upstream base: `Untrivial-ai/agent-orchestrator` stable `v0.12.1`, release commit prefix `1df40e9`
- Target: Windows x64 first; per-user local application
- Approved UI direction: `ui/square-session-workspace-rounded-reference.html`

## 1. Delivery outcome

Build Square as a session-first layer on Agent Orchestrator. A user creates a durable session around one topic or outcome, writes the task in a chat-like composer, and sees the software Task Manager create only the required Planner, Orchestrator, Worker, Scout, and Reviewer role runs inside that session. Every role run uses an AO execution session, terminal/Chat controller, and optional worktree. The user can switch Square Session tabs while every daemon-owned process continues.

The first useful Alpha must complete a real QUICK session. The MVP must complete QUICK and PLANNED sessions with memory, bounded context, configurable per-role model routes, typed human decisions, deterministic validation, independent review where eligible, finite repair, restart recovery, and retained terminal history.

## 2. Authority hierarchy

1. This plan after owner acceptance.
2. `docs/ARCHITECTURE_AMENDMENT.md`.
3. `docs/SESSION_DOMAIN_MODEL.md`.
4. `docs/ROLE_ROUTING_MODEL_SELECTION.md`.
5. `docs/PERSISTENCE_AND_EVENTS.md`.
6. `docs/API_AND_EXECUTION_FACADE.md`.
7. `docs/SESSION_FIRST_UI_SPEC.md` and the approved HTML reference.
8. Accepted ADRs/amendments created by tasks in this plan.
9. Pinned AO `v0.12.1` source and tag-specific documentation.
10. Task-specific dispatch packets.
11. Worker discretion that does not change public behavior, schema, security, dependency direction, or another task's assumptions.

The earlier Square UI principles remain binding: terminals/docks are views of daemon-owned work; hiding or closing them cannot stop the process; role/model/task/state remain visible; urgent interactions are explicit; terminal output is untrusted; QUICK hides irrelevant workflow surfaces. The prior .NET implementation decisions are not production authority.

## 3. Canonical terminology

| Term | Meaning |
|---|---|
| Square Session | Durable user-facing topic, conversation, workflow, role docks, decisions, history, and result |
| Workflow Run | One execution of QUICK/PLANNED/etc. inside a Square Session |
| Role Run | One bounded Secretary/Scout/Planner/Orchestrator/Worker/Reviewer/Triage invocation |
| AO Session | Concrete AO execution session, terminal/Chat controller, runtime, and optional worktree |
| Task Manager | Deterministic Go service advancing workflow from durable facts; never an LLM monitor |
| Route | Certified harness + provider/account boundary + model/mode + interface + permissions + executable/adapter identity |
| Assignment | Frozen role-route/authority/budget/artifact decision for one role run |
| Binding | Generation-fenced relationship between Square role/task attempt and AO session/worktree/controller |

## 4. Locked decisions

- Maintain a downstream fork, not a loose sidecar and not a source ZIP without history.
- Keep AO's Go daemon, SQLite/goose/sqlc/CDC, REST/SSE/WebSocket, Electron/React, ConPTY, terminal mux, worktrees, adapters, reviewers, and PR/CI observation.
- Add Square product resources under namespaced packages, tables, routes, frontend features, and data paths.
- Make Square Session the top-level product/UI object.
- Keep Task Manager deterministic; model roles are bounded and end at artifacts.
- Support role-specific `AUTO`, `PREFERRED`, and `PINNED` routes with task/session/project/global precedence.
- Persist requested, resolved, and actual model/route identity separately.
- Do not silently fall back from a pinned route or downgrade reviewer independence.
- Retain AO loopback HTTP/SSE/WebSocket for the MVP; threat-model and harden it later rather than replacing it immediately.
- Deliver Windows x64 first.
- Disable Square telemetry by default and prevent the official AO updater from replacing Square.
- Deliver QUICK before PLANNED; do not require full global dashboards, VS Code, specialists, resource telemetry, or parallel writers for the Alpha.

## 5. Global invariants

1. UI/session-tab closure, reload, focus change, layout change, or app exit does not stop or duplicate daemon-owned work.
2. Only the daemon mutates Square state, AO bindings, writer leases, route decisions, interactions, or workflow stage.
3. Every mutation is idempotent and appends a semantic event atomically with current projections before external work.
4. One writer attempt owns one worktree lease/fencing generation.
5. Silence is not a stall; known lifecycle classification is deterministic.
6. Questions, permissions, approvals, authentication, blockers, route failures, and completion are typed durable records.
7. No model stays alive merely to monitor another model.
8. Workers cannot alter requirements, architecture, public contracts, schema, security, acceptance, or cross-task behavior outside their packet.
9. QUICK retains scope, lock, receipt, cancellation, evidence, and recovery guarantees.
10. Configured model identity is not copied into `actual_model` without evidence.
11. A live route change creates a new attempt and handover; it never mutates a running process identity.
12. Reviewer independence is a policy requirement, not a routing preference.
13. Completed role terminals and artifacts remain attached to the Square Session under retention policy.
14. AO `change_log` is CDC/invalidation; `square_events` is semantic history.
15. Applied migrations are immutable.
16. Official AO and Square installations do not share data, updater, telemetry, run files, process identity, or worktree root.
17. Missing token/cost/resource telemetry is unknown/degraded, never zero/healthy.
18. Cleanup/update/uninstall cannot delete repositories, dirty worktrees, credentials, or uncommitted work.

## 6. Milestones and gates

| Gate | Result |
|---|---|
| A0 | Pinned fork, unchanged baseline, attribution/identity isolation, architecture amendment |
| A1 | Windows daemon/UI/ConPTY/worktree/restart lifecycle safe enough for Square |
| A2 | Session-first domain, artifact, routing, API/event contracts frozen as `1.0-draft` |
| A3 | Durable Square migrations/events/artifacts/idempotency/leases/bindings/recovery |
| A4 | AO execution facade and route identity/certification boundary proven with fake route |
| A5 | Session API/read models/SSE/history and deterministic fake workflows |
| A6 | Rounded session-first UI works against fixtures and authoritative API contracts |
| A7 | Square Core Alpha: one real certified route completes QUICK through desktop and CLI |
| A8 | Interactions/controller/restart lifecycle complete |
| A9 | Project/global memory and bounded Context Pack complete |
| A10 | PLANNED orchestration completes through implementation |
| A11 | Square MVP: validation, independent review, finite fix, final receipt/evidence |
| A12 | Role-routing UX, route plurality where available, CLI/optional VS Code parity |
| A13 | Sustained-operation resource/index/evaluation behavior |
| A14 | Security/installer/updater/upstream-sync Windows release qualification |
| A15 | Optional scale enabled only by evidence |

## 7. Dependency overview

```text
SA00 Fork adoption
  → SA01 Windows/platform hardening
  → SA02 Session/routing contracts
  → SA03 Persistence/events/artifacts
  → SA04 AO execution facade/route certification boundary
  → SA05 Session API + fake workflow
  ├─→ SA06 Session-first UI (fixtures can start after A2, integration after A5)
  └─→ SA07 QUICK vertical slice
        → SA08 Interactions/recovery
        → SA09 Memory/context
        → SA10 PLANNED workflow
        → SA11 Review/fix/MVP
        → SA12 Clients/route plurality
        → SA13 Resources/evaluation
        → SA14 Security/release/upstream sync
        → SA15 Optional scale
```

## 8. Dispatch packet rule

Every task below is independently dispatchable only after its prerequisites. Before coding, its packet must freeze:

- starting commit, branch, dirty state, AO base, authority hashes;
- exact allowed read/write paths;
- expected files/symbols/migrations/routes/components;
- public behavior, errors, edge cases, security/authority;
- validation commands and evidence destination;
- retry/context/output/resource budgets;
- discretion and mandatory STOP conditions;
- completion-receipt destination.

No agent may treat this master plan alone as permission to touch arbitrary paths.

# SA00 — Fork adoption and reproducible baseline

## Outcome

Create a legally and technically controlled downstream fork before product changes.

## SA00-T01 — Create and pin the downstream fork

**Prerequisites:** none.

**Required outcome:** Clone stable v0.12.1, verify commit/tag, rename remote to upstream, create square/main and baseline tag, add optional owner origin without pushing, add upstream ledger and authority directories.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA00-T02 — Capture the unchanged Windows build/test/package baseline

**Prerequisites:** SA00-T01.

**Required outcome:** Record toolchain and run backend build/tests/race/lint, frontend install/typecheck/unit/E2E/package, daemon/CLI smoke, and one harmless Windows AO session; preserve all pre-existing failures.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA00-T03 — License, attribution, identity, telemetry, updater, and data isolation design

**Prerequisites:** SA00-T02.

**Required outcome:** Inventory Apache/NOTICE/dependencies; define Square product/app/installer/data/run-file/worktree/telemetry/updater identities; disable transmission/update replacement in local Square builds without broad rebrand churn.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA00-T04 — Import authority and activate architecture amendment

**Prerequisites:** SA00-T03.

**Required outcome:** Copy accepted plans/UI/behavioral research into docs/square/authority; hash them; accept the session-first amendment and coding-agent rules; mark previous combined draft superseded.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA00-T05 — Adoption gate A0

**Prerequisites:** SA00-T01..T04.

**Required outcome:** Review baseline evidence, legal/identity boundaries, upstream pin, authorities, and open blockers; no Square product code may exist before PASS.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

**Gate effect:** A0 must be reviewed before downstream tasks that depend on it. A code-complete task is not automatically an accepted gate.

# SA01 — Windows lifecycle and AO platform hardening

## Outcome

Prove or repair the reused platform layer on the target machine.

## SA01-T01 — Detach daemon/workflows from desktop window lifetime

**Prerequisites:** A0.

**Required outcome:** Ensure closing/reloading Electron releases UI resources only; daemon and live sessions continue; reopen reconnects to the same controller/session without duplication.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA01-T02 — Single daemon and Square data-directory ownership

**Prerequisites:** SA01-T01.

**Required outcome:** Add/verify Square-specific run-file/mutex/port/data identity; simultaneous app/CLI startup creates one compatible daemon; official AO cannot be mistaken for Square.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA01-T03 — Windows ConPTY, input/output/resize/cancel, and descendant cleanup

**Prerequisites:** A0.

**Required outcome:** Exercise existing runtime through Unicode, ANSI, burst, quiet, question, resize, graceful cancel, hard stop, nested children, redirected parent handles, and leak checks; repair only platform leaves.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA01-T04 — Restart reconciliation and controller generation

**Prerequisites:** SA01-T02,T03.

**Required outcome:** Restart desktop/daemon around active/terminal sessions; classify alive/lost/stale/ambiguous bindings; prevent duplicate controllers and unsafe silent relaunch.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA01-T05 — Worktree cleanup and dirty-state safety

**Prerequisites:** A0.

**Required outcome:** Test paths with spaces/Unicode, worktree create/remove, process locks, dirty/untracked work, successful process with cleanup warning, branch collisions, and restart reconciliation; never force-delete user work.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA01-T06 — Windows foundation gate A1

**Prerequisites:** SA01-T01..T05.

**Required outcome:** Accept measurable lifecycle behavior or block Square dependencies; preserve raw evidence and any upstream issue/patch references.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

**Gate effect:** A1 must be reviewed before downstream tasks that depend on it. A code-complete task is not automatically an accepted gate.

# SA02 — Session-first contracts and deterministic domain

## Outcome

Freeze the product vocabulary before database/API/UI coupling.

## SA02-T01 — Strong IDs, versions, hashes, time, and typed results

**Prerequisites:** A1.

**Required outcome:** Implement Square strong IDs/value objects, explicit clock/ID ports, canonical UTC/hash/version behavior, typed errors/results; no I/O or AO dependency.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA02-T02 — Square Session, message, workflow, role, and AO binding records

**Prerequisites:** SA02-T01.

**Required outcome:** Implement records from SESSION_DOMAIN_MODEL, including user conversation, WorkflowRun, RoleProfile/Assignment/Run, AOExecutionBinding, layout/history references.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA02-T03 — Pure lifecycle reducers

**Prerequisites:** SA02-T02.

**Required outcome:** Implement exhaustive legal/illegal transitions for session/workflow/role/binding/task/interaction/lease; distinguish quiet/stall and final/superseded/new-attempt behavior.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA02-T04 — Task Brief, Preview, Plan, Task, Acceptance, Review, Receipt, Evidence contracts

**Prerequisites:** SA02-T01,T02.

**Required outcome:** Define versioned DTO/domain artifacts with falsifiable acceptance, path scopes, budgets, stop conditions, immutable baselines/results, artifact hashes, and golden examples.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA02-T05 — Role routing profile and actual model identity contract

**Prerequisites:** SA02-T01,T02.

**Required outcome:** Implement AUTO/PREFERRED/PINNED, precedence, route/certification/decision records, requested/resolved/actual identity, reviewer independence, fallback and live-route-change rules.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA02-T06 — Square API/event contract and generated TypeScript parity

**Prerequisites:** SA02-T02..T05.

**Required outcome:** Define service/HTTP DTOs and event resource shapes, register draft OpenAPI operations, generate TypeScript, add golden cross-language fixtures and version behavior.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA02-T07 — Contract gate A2

**Prerequisites:** SA02-T01..T06.

**Required outcome:** Freeze `square.contracts 1.0-draft`; pure domain has no host/storage/adapter dependency; all schemas/examples/reducers/routing matrices pass.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

**Gate effect:** A2 must be reviewed before downstream tasks that depend on it. A code-complete task is not automatically an accepted gate.

# SA03 — Durable state, semantic events, artifacts, leases, and recovery

## Outcome

Extend AO SQLite safely with generation-fenced Square state.

## SA03-T01 — Square migration range and schema registry

**Prerequisites:** A2.

**Required outcome:** Inspect upstream migration sequence, define collision policy, add forward-only registry/identity migration and backup/newer/inconsistent checks without editing AO history.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA03-T02 — Session, conversation, workflow, role, task, and binding schema

**Prerequisites:** SA03-T01.

**Required outcome:** Add core square_* tables/constraints/indexes/triggers, sqlc queries and stores; keep generated rows behind store methods.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA03-T03 — Semantic events and transactional projections

**Prerequisites:** SA03-T02.

**Required outcome:** Add append-only square_events, aggregate/global sequence, event+projection transaction helper, CDC triggers, duplicate and failure injection tests.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA03-T04 — Idempotency, writer leases, controller generations, and AO bindings

**Prerequisites:** SA03-T02,T03.

**Required outcome:** Implement command result replay/conflict, worktree lease/fence, binding generation/reconciliation states, stale commit denial, concurrent tests.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA03-T05 — Content-addressed artifacts and retained terminal/history references

**Prerequisites:** SA03-T02.

**Required outcome:** Add spool/hash/atomic move/dedup/read validation/orphan reconciliation and metadata for packets, reports, receipts, evidence, handovers, bounded terminal/Chat chunks.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA03-T06 — Migration/corruption/crash/recovery matrix and gate A3

**Prerequisites:** SA03-T01..T05.

**Required outcome:** Test baseline AO DB upgrade, every Square migration path, crash boundaries, newer/corrupt/inconsistent state, event atomicity, artifact/lease/binding recovery; accept A3.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

**Gate effect:** A3 must be reviewed before downstream tasks that depend on it. A code-complete task is not automatically an accepted gate.

# SA04 — Role route registry and AO execution facade

## Outcome

Use AO sessions/worktrees/adapters without leaking them into Square workflow logic.

## SA04-T01 — Inventory AO adapter/model/permission capabilities

**Prerequisites:** A2,A3.

**Required outcome:** Inspect exact pinned adapters and launch commands; record model forwarding, actual identity, modes, permissions, Chat/terminal/reviewer, resume/cancel/activity, Windows behavior.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA04-T02 — Route discovery and certification service

**Prerequisites:** SA04-T01.

**Required outcome:** Implement discovered/probing/certified/degraded/quarantined/unavailable/expired records, executable/version/hash probes, role eligibility and redacted evidence.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA04-T03 — Requested/resolved/actual identity verification

**Prerequisites:** SA04-T02.

**Required outcome:** Create adapter conformance seam and fixtures; pinned route fails if required model cannot be verified; preferred fallback and AUTO decision artifact are durable.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA04-T04 — Square AO execution facade

**Prerequisites:** A3,SA04-T02.

**Required outcome:** Wrap AO service/session_manager for reserve/start/send/checkpoint/cancel/hard-stop/observe/release; no direct adapter/runtime/worktree access from workflow services.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA04-T05 — Role packet/prompt compiler and bounded artifact boundary

**Prerequisites:** SA02-T04,T05,SA04-T04.

**Required outcome:** Compile role-specific minimal input, authority and output schema; prevent nested agents/unbounded scope; finish role run after artifact/blocker and preserve handover.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA04-T06 — Fake route/adapter execution and gate A4

**Prerequisites:** SA04-T03..T05.

**Required outcome:** Use deterministic fake role scenarios for success/question/approval/auth/quiet/burst/crash/missing/conflicting artifact/cancel/restart; verify AO binding and no always-on model.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

**Gate effect:** A4 must be reviewed before downstream tasks that depend on it. A code-complete task is not automatically an accepted gate.

# SA05 — Session API, read models, events, and fake workflows

## Outcome

Expose the durable session product boundary before the real workflow.

## SA05-T01 — Session and durable conversation service/API

**Prerequisites:** A3,A4.

**Required outcome:** Create/list/get/archive sessions; add versioned messages/corrections; assemble session navigator/detail read models; idempotency and optimistic-version checks.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA05-T02 — Workflow-run, role-run, and direct action API

**Prerequisites:** SA05-T01.

**Required outcome:** Start/pause/resume/cancel workflow; query role hierarchy/docks; checkpoint/cancel route run; return authorized actions from services.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA05-T03 — Typed interactions and owner decisions API

**Prerequisites:** SA05-T01.

**Required outcome:** Create/list/respond to question/approval/auth/blocker/route/plan/force-stop interactions with authority, expiry, safe default and one-time semantics.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA05-T04 — SSE invalidation, history, artifacts, and terminal binding read models

**Prerequisites:** SA05-T01..T03.

**Required outcome:** Add Square CDC resources, replay/unknown-version behavior, session history and role terminal/Chat/diff/evidence references.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA05-T05 — Deterministic fixture workflows and gate A5

**Prerequisites:** SA05-T01..T04.

**Required outcome:** Run fixture sessions matching blank/QUICK/PLANNED/blocked/completed/restart JSON; verify API/OpenAPI/TS parity and read-model recovery.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

**Gate effect:** A5 must be reviewed before downstream tasks that depend on it. A code-complete task is not automatically an accepted gate.

# SA06 — Rounded session-first desktop UI foundation

## Outcome

Implement the approved experience against fixtures, then live API.

## SA06-T01 — Square identity, tokens, and reusable components

**Prerequisites:** A0,A2.

**Required outcome:** Rebrand isolated surfaces; implement rounded light/dark/high-contrast tokens, statuses, buttons, tabs, composer, attention, dock headers, accessible components without changing AO generic behavior unnecessarily.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA06-T02 — Session navigator, tabs, filters, and New Session composer

**Prerequisites:** SA06-T01,A5 fixtures.

**Required outcome:** Implement project/session list, Needs You/Active/Done/Archived, open tabs, blank composer, simple workflow/quality controls and expandable role setup.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA06-T03 — Session conversation and workflow progression

**Prerequisites:** SA06-T02.

**Required outcome:** Render durable messages/artifact summaries, follow-up composer, session header, relevant stage strip, authoritative pending/acknowledged states.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA06-T04 — Dynamic Task Manager and role docks

**Prerequisites:** SA06-T02,T03.

**Required outcome:** Render only existing roles; Task Manager structured timeline; Terminal/Chat/Diff/Evidence tabs; hierarchy, focus/collapse/splits, completed history, actual route/model.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA06-T05 — Direct attention strip and authorized decisions

**Prerequisites:** SA06-T03.

**Required outcome:** Show exact problem/why/suggested solution/actions in affected session; approval/input/auth/blocker/route/plan/cancel flows without inspector-click maze.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA06-T06 — Plan & Review, History, and role-routing setup

**Prerequisites:** SA06-T03,T04.

**Required outcome:** Task Brief/Plan/Acceptance/Diff/Findings/Evidence; workflow/role/terminal/decision history; Auto/Preferred/Pinned role controls and requested/actual mismatch.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA06-T07 — Layout persistence, performance, accessibility, and gate A6

**Prerequisites:** SA06-T01..T06.

**Required outcome:** Per-session versioned layouts, hidden-dock throttle, terminal sequence safety, virtualization, keyboard/a11y/200%/high-contrast, close/reopen fixture E2E.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

**Gate effect:** A6 must be reviewed before downstream tasks that depend on it. A code-complete task is not automatically an accepted gate.

# SA07 — QUICK vertical slice and Core Alpha

## Outcome

Deliver the first useful end-to-end workflow before broader orchestration.

## SA07-T01 — Request intake and compact Task Brief

**Prerequisites:** A5.

**Required outcome:** Compile explicit goal/outcome/scope/constraints/ambiguity/risk from session messages; material ambiguity creates interaction; bounded Secretary optional.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA07-T02 — Deterministic triage and Dispatch Preview

**Prerequisites:** SA07-T01,SA02-T05.

**Required outcome:** Select QUICK or escalate using explicit dimensions; propose worker route/worktree/write scope/validation/approval/resource/cost confidence; owner edit/approve/deny.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA07-T03 — QUICK task/role packet compiler

**Prerequisites:** SA07-T02.

**Required outcome:** Create one narrow task contract and Worker assignment with exact paths, baseline, validation, receipt, stop/escalation conditions; omit irrelevant roles/transcripts.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA07-T04 — Task Manager QUICK state machine and AO worker launch

**Prerequisites:** SA04,A3,SA07-T03.

**Required outcome:** Persist workflow/task/role/binding, acquire lease, launch one AO worker, react to facts/interactions, end process at artifact boundary, no model monitor.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA07-T05 — Targeted validation, receipt, and local/PR result

**Prerequisites:** SA07-T04.

**Required outcome:** Run task-scoped checks, reconcile process/worktree/artifact/commit, apply receipt once, support LOCAL_RESULT first and optional PR_RESULT link.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA07-T06 — QUICK desktop and CLI flow

**Prerequisites:** SA06,A5,SA07-T05.

**Required outcome:** New session → preview → start → worker dock → decision/cancel → validation/result/history; CLI parity and stable JSON/errors.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA07-T07 — First real certified route and Alpha gate A7

**Prerequisites:** SA04-T02,T03,SA07-T06.

**Required outcome:** Choose one installed route by evidence, run reversible disposable fixtures repeatedly, close/reopen desktop, restart daemon, verify no duplicate writer/session and complete evidence.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

**Gate effect:** A7 must be reviewed before downstream tasks that depend on it. A code-complete task is not automatically an accepted gate.

# SA08 — Interactions, controller authority, cancellation, and restart

## Outcome

Complete human-in-the-loop and lifecycle safety around active sessions.

## SA08-T01 — Durable question, approval, permission, auth, blocker, and route interactions

**Prerequisites:** A7.

**Required outcome:** Map AO/adapter/lifecycle signals to typed Square interactions with bounded redacted evidence and designated authority.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA08-T02 — Response, approval-once, denial, amendment, and auth takeover

**Prerequisites:** SA08-T01.

**Required outcome:** Validate state/expiry/actor/generation; secrets remain with CLI; send exact PTY/controller action only after durable decision.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA08-T03 — Interactive controller lease and multi-view behavior

**Prerequisites:** SA08-T01.

**Required outcome:** One controller across desktop/CLI/future VS Code; observers may coexist; transfer/expiry/reconnect/close semantics and visible VIEW/CONTROL states.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA08-T04 — Checkpoint, graceful cancellation, hard-stop gate, and safe cleanup

**Prerequisites:** SA08-T02,T03.

**Required outcome:** Separate commands/authority; preserve worktree/receipt evidence; descendant cleanup; no priority preemption of active writer.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA08-T05 — Startup reconciliation of Square sessions and AO bindings

**Prerequisites:** SA03,SA04,SA08-T04.

**Required outcome:** Recover active workflows, interactions, leases, role/AO bindings, terminal histories, artifacts and receipts; never silently rerun ambiguous work.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA08-T06 — UI closure/restart matrix and gate A8

**Prerequisites:** SA08-T01..T05.

**Required outcome:** E2E across active/quiet/input/approval/auth/cancel/completing states; close/reopen Electron and daemon restart; no duplicate process/input/receipt.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

**Gate effect:** A8 must be reviewed before downstream tasks that depend on it. A code-complete task is not automatically an accepted gate.

# SA09 — Project/global memory and bounded context

## Outcome

Add learning without allowing transcripts or models to self-authorize policy.

## SA09-T01 — Memory records, schema, service, API, and lifecycle

**Prerequisites:** A8.

**Required outcome:** Project/global/candidate/promotion/deprecated/superseded entries with provenance, evidence, applicability, confidence, owner state.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA09-T02 — Candidate extraction and deduplication

**Prerequisites:** SA09-T01.

**Required outcome:** Derive bounded candidates from accepted results/fixes/human corrections; no automatic adoption; detect duplicate/conflict/stale evidence.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA09-T03 — Owner promotion, rejection, deprecation, and rollback

**Prerequisites:** SA09-T01,T02.

**Required outcome:** Explicit authority and audit; global promotion from project candidate; preserve prior versions and supersession.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA09-T04 — Context Scout jobs and cited Context Reports

**Prerequisites:** A8,SA09-T01.

**Required outcome:** Read-only bounded question/path/command/snapshot jobs; path/symbol/range/hash citations, uncertainty/conflict, no raw transcript forwarding.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA09-T05 — Context Pack compiler and cache compatibility

**Prerequisites:** SA09-T04.

**Required outcome:** Deduplicate reports/memory, enforce size/token budget and authority/snapshot compatibility, produce CONTEXT_READY/BLOCKED artifact.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA09-T06 — Memory/context UI, CLI, and gate A9

**Prerequisites:** SA06,SA09-T01..T05.

**Required outcome:** Session/project memory views, candidates and promotion, context sources/citations; verify no transcript auto-memory and correct scope.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

**Gate effect:** A9 must be reviewed before downstream tasks that depend on it. A code-complete task is not automatically an accepted gate.

# SA10 — PLANNED workflow and bounded orchestration

## Outcome

Add planning depth while Task Manager remains the durable coordinator.

## SA10-T01 — Secretary Task Brief compiler

**Prerequisites:** A9.

**Required outcome:** Launch bounded optional Secretary only when deterministic intake needs it; validate artifact/ambiguity; end session immediately.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA10-T02 — Context job scheduler and Context Pack readiness

**Prerequisites:** SA09-T05.

**Required outcome:** Dispatch bounded Scouts/read-only AO sessions, wait via durable events, handle missing/conflicting/stale reports and capacity without live coordinator model.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA10-T03 — Planner role and Plan/Acceptance compiler

**Prerequisites:** SA10-T01,T02.

**Required outcome:** Fresh Planner receives Task Brief/Context Pack/authority only; validate DAG, scopes, criteria, budgets, stop/integration; end at artifact.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA10-T04 — Orchestrator/Synthesizer role

**Prerequisites:** SA10-T03.

**Required outcome:** Fresh bounded synthesis for dispatch packets, conflict resolution proposals, amendments, integration strategy; cannot remain alive to supervise workers.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA10-T05 — Plan approval, amendment, and immutable binding

**Prerequisites:** SA10-T03,T04.

**Required outcome:** Owner/policy approval tied to exact plan/criteria/baseline/route hashes; changes invalidate approval and create amendment interaction.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA10-T06 — Dependency scheduler, worker assignments, and bounded handovers

**Prerequisites:** SA10-T05,SA04.

**Required outcome:** Ready tasks, writer/resource reservations, worktree/AO session launch, finite attempts, route change/handover, context thresholds, no duplicate wake.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA10-T07 — PLANNED implementation/restart gate A10

**Prerequisites:** SA10-T01..T06.

**Required outcome:** Fake and first real multi-task fixture reaches deterministic verification with all role sessions ending at artifacts; restart preserves DAG/bindings.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

**Gate effect:** A10 must be reviewed before downstream tasks that depend on it. A code-complete task is not automatically an accepted gate.

# SA11 — Verification, independent review, findings, fix loop, and Square MVP

## Outcome

Finish quality and immutable acceptance without transcript-based review.

## SA11-T01 — Task/integration deterministic validation service

**Prerequisites:** A10.

**Required outcome:** Run declared commands under scope/resource policy, capture structured output/evidence, fail before reviewer spend, distinguish task vs integration checks.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA11-T02 — Review Packet compiler

**Prerequisites:** SA11-T01.

**Required outcome:** Bounded contract/plan/criteria/diff/evidence/authority/risk packet; exclude implementation transcript and irrelevant repository content.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA11-T03 — Independent reviewer route selection and run

**Prerequisites:** SA04,SA11-T02.

**Required outcome:** Enforce configured independence, launch fresh read-only reviewer, validate findings against criteria/commit, gate if unavailable.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA11-T04 — Finding model and finite FIX task loop

**Prerequisites:** SA11-T03.

**Required outcome:** Severity/evidence/criterion/root cause/proposed bounded fix; separate FIX task/attempt; retry budget and new requirement/architecture gate.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA11-T05 — Final receipt, integration acceptance, outcome and memory candidates

**Prerequisites:** SA11-T01..T04.

**Required outcome:** Bind immutable commit/artifact/evidence/route/criteria; reconcile all worktrees/roles; create outcome evaluation seed and non-adopted memory candidates.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA11-T06 — Square MVP gate A11

**Prerequisites:** SA07,A8,A9,A10,SA11-T01..T05.

**Required outcome:** Real QUICK and PLANNED through one certified route, typed interaction, memory/context, independent review or explicit owner gate, repair, restart, retained history.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

**Gate effect:** A11 must be reviewed before downstream tasks that depend on it. A code-complete task is not automatically an accepted gate.

# SA12 — Role-routing UX, route plurality, CLI, and optional VS Code

## Outcome

Make model selection explicit and expose one authoritative state across clients.

## SA12-T01 — Global/project/session/task role-profile resolution

**Prerequisites:** A11.

**Required outcome:** Persist/edit profiles and presets; deterministic precedence, policy overrides, route decision history, settings UI and CLI.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA12-T02 — AUTO/PREFERRED/PINNED Dispatch Preview and role-dock UX

**Prerequisites:** SA12-T01,SA06.

**Required outcome:** Show requested/resolved/actual, availability/certification, fallback, permissions, independence; pinned fail closed; active change creates new attempt.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA12-T03 — Expanded adapter conformance and additional certified routes

**Prerequisites:** SA04,A11.

**Required outcome:** Certify installed AO adapters incrementally for model forwarding, identity, permissions, lifecycle, Windows cleanup; unavailable routes do not block others.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA12-T04 — Complete `square` CLI and stable JSON/exit codes

**Prerequisites:** A11.

**Required outcome:** Session/workflow/role/interaction/route/memory/events/doctor commands over daemon API; no direct storage/runtime; redirection and noninteractive behavior.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA12-T05 — Minimal VS Code client and cross-client consistency

**Prerequisites:** SA12-T04.

**Required outcome:** Extension host uses API/SSE, session tree/tabs/status/commands, validated webview if used; closing VS Code harmless; no direct terminal/DB authority.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA12-T06 — Client/route gate A12

**Prerequisites:** SA12-T01..T05.

**Required outcome:** Desktop/CLI/VS Code where implemented show same event/interaction/result; route profiles work across roles; actual identity honest.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

**Gate effect:** A12 must be reviewed before downstream tasks that depend on it. A code-complete task is not automatically an accepted gate.

# SA13 — Exposure, cost, resource profiles, context reuse, and evaluation

## Outcome

Optimize only after useful workflow data exists.

## SA13-T01 — Usage and model-family exposure ledger

**Prerequisites:** A11.

**Required outcome:** Provider tokens or labelled estimates, normalized exposure by role/model family/project/window/confidence; context pressure separate.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA13-T02 — Actual/equivalent/subscription cost schedules

**Prerequisites:** SA13-T01.

**Required outcome:** Dated source/currency/rates/billing mode/confidence; never show subscription as free or merge cost with exposure.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA13-T03 — FAST/NORMAL/SLOW resource and scheduler policy

**Prerequisites:** A11.

**Required outcome:** Role/concurrency/validation/retention/cache behavior with explicit owner policy; release model sessions before cooldown/wait.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA13-T04 — Snapshot-aware repository index and Context Pack cache

**Prerequisites:** SA09.

**Required outcome:** Content-addressed commit/path/symbol index, incremental invalidation, shared Scout snapshot, compatibility/provenance and bounded storage.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA13-T05 — Buffered persistence, retention, and resource telemetry

**Prerequisites:** SA03,A11.

**Required outcome:** Batch noncritical writes, debounce layouts, terminal chunks, WAL/checkpoint measurement, volume/process I/O and honest unsupported telemetry.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA13-T06 — Outcome evaluation and sustained-operation gate A13

**Prerequisites:** SA11,SA13-T01..T05.

**Required outcome:** Cohorted acceptance/findings/retries/human corrections/time/tokens/cost/exposure/I/O; sample/confidence; measured performance and storage safety.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

**Gate effect:** A13 must be reviewed before downstream tasks that depend on it. A code-complete task is not automatically an accepted gate.

# SA14 — Security, diagnostics, packaging, updater, and upstream sync

## Outcome

Qualify a supportable Windows downstream product.

## SA14-T01 — Threat model and automated security closure

**Prerequisites:** A12.

**Required outcome:** Loopback API, Electron preload, terminal escapes, prompt injection, path/reparse escape, executable replacement, receipt spoofing, secrets, updater, diagnostics, skills.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA14-T02 — Local API and Electron bridge hardening

**Prerequisites:** SA14-T01.

**Required outcome:** Decide/add install capability token, origin/CORS/loopback rules, allow-listed preload methods, schema validation, rate/bounds, terminal-output isolation.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA14-T03 — Privacy-safe doctor and support bundle

**Prerequisites:** SA14-T01.

**Required outcome:** Versions, daemon/data/migrations/routes/worktrees/bindings/terminal/updater checks; allow-listed redacted manifest shown before export.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA14-T04 — Square Windows identity, installer, data migration, and coexistence

**Prerequisites:** A12.

**Required outcome:** Product/executable/app ID/paths/protocols/notifications/icons/attribution; non-admin install, spaces/Unicode, coexistence with AO, retain/remove state choice.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA14-T05 — Square updater, live-work gate, backup, rollback

**Prerequisites:** SA14-T04.

**Required outcome:** Own release feed/signature/checksums; stop new dispatch, checkpoint/reconcile, backup/migrate, replace/restart/validate, rollback when schema compatible; never kill work merely to update.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA14-T06 — Upstream-sync automation, migration/API drift, SBOM and notices

**Prerequisites:** A12.

**Required outcome:** Controlled intake branch, changelog/diff, generated-code/migration conflict checks, full regression, exact upstream ledger, SBOM/license/NOTICE.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA14-T07 — Windows release gate A14

**Prerequisites:** SA14-T01..T06.

**Required outcome:** Clean install/upgrade/rollback/uninstall, QUICK/PLANNED E2E, security/lifecycle/recovery, signed/checksummed artifacts where available, known limitations.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

**Gate effect:** A14 must be reviewed before downstream tasks that depend on it. A code-complete task is not automatically an accepted gate.

# SA15 — Optional advanced scale and controlled practice evolution

## Outcome

Disabled by default until workload evidence justifies it.

## SA15-T01 — Parallel read-only Scouts and specialists

**Prerequisites:** A14.

**Required outcome:** One pinned snapshot, shared index/resource reservations, deterministic result collection, no shared mutable plan.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA15-T02 — Parallel isolated writer worktrees and merge queue

**Prerequisites:** SA15-T01.

**Required outcome:** Nonoverlap preflight, one lease/worktree per writer, ordered integration, combined validation, conflict gate, measured resource/cost burden.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA15-T03 — Automatic bounded fix dispatch

**Prerequisites:** SA11,A14.

**Required outcome:** Only accepted narrow findings, unchanged acceptance, eligible route, finite retry; no new architecture/security/requirement decision.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

## SA15-T04 — Controlled practice evolution and scale gate A15

**Prerequisites:** SA13,SA15-T01..T03.

**Required outcome:** Outcome evidence proposes trial/adoption/deprecation; owner controls policy; enable scale only if end-to-end benefit exceeds merge/resource/review/recovery cost.

**Task packet must specify:** exact source paths/symbols, tests/evidence, security/authority constraints, generated files, and STOP conditions before editing.

# 9. Cross-cutting acceptance criteria

| ID | Falsifiable result |
|---|---|
| SF-AC-01 | Closing/reloading Electron or switching/closing a Square Session tab does not stop, pause, duplicate, or rebind daemon-owned role runs |
| SF-AC-02 | One compatible Square daemon owns one Square data directory/database and cannot collide with official AO |
| SF-AC-03 | Known terminal/runtime states are classified without an LLM monitor; silence alone never authorizes termination |
| SF-AC-04 | A QUICK workflow creates no irrelevant Secretary/Scout/Planner/Orchestrator/Reviewer role runs |
| SF-AC-05 | A PLANNED workflow's model role sessions end at their required artifact/blocker boundary |
| SF-AC-06 | Every role dock shows role, task, actual harness/model or UNVERIFIED, state, permissions, and worktree/controller status |
| SF-AC-07 | Switching sessions changes only presentation; active terminals retain exact process/controller identity |
| SF-AC-08 | Questions/approvals/auth/blockers/routes appear directly in the affected session and survive restart |
| SF-AC-09 | AUTO/PREFERRED/PINNED resolve with task→session→project→global→default precedence |
| SF-AC-10 | A PINNED unavailable/unverified route blocks and never silently falls back |
| SF-AC-11 | Requested, resolved, and actual model/route identities are separate durable fields and UI values |
| SF-AC-12 | Changing an active role route creates a new role attempt and handover, preserving both histories |
| SF-AC-13 | Reviewer independence cannot silently downgrade; no eligible reviewer creates an explicit gate/owner path |
| SF-AC-14 | Every accepted mutation appends a semantic event and current projection atomically |
| SF-AC-15 | Restart cannot duplicate a semantic event, receipt, writer lease, AO binding, role run, or agent process |
| SF-AC-16 | Completed role terminals remain available in the Square Session history under retention policy |
| SF-AC-17 | Session conversation is durable Square state and remains distinct from provider transcripts |
| SF-AC-18 | No provider transcript becomes project/global memory automatically |
| SF-AC-19 | Project/global memory promotion requires explicit owner transition with provenance/evidence |
| SF-AC-20 | Worker writes are constrained to accepted canonical scope and one writer lease/worktree generation |
| SF-AC-21 | Vague acceptance or unbounded write scope cannot dispatch |
| SF-AC-22 | Review/fix findings reference stable criteria, immutable baseline/result commits, and evidence |
| SF-AC-23 | UI/CLI/VS Code where implemented display the same final event/interaction/result |
| SF-AC-24 | Terminal output cannot invoke arbitrary Electron/preload/extension-host commands |
| SF-AC-25 | Missing token/cost/resource telemetry is unknown/degraded, not zero or healthy |
| SF-AC-26 | AO and Square applied migrations are never edited; upstream migration collision is detected before merge |
| SF-AC-27 | A local workflow can finish without GitHub/PR when policy selects LOCAL_RESULT |
| SF-AC-28 | Worktree cleanup cannot destroy dirty/untracked work or hide a cleanup warning behind process success |
| SF-AC-29 | Upstream sync cannot merge until adoption/lifecycle/migration/workflow/UI/security suites pass |
| SF-AC-30 | Installer/update/uninstall never deletes repositories, credentials, dirty worktrees, or uncommitted work |

# 10. Parallelization rules

Allowed only after prerequisites and with nonoverlapping paths/worktrees:

- SA01-T03 Windows runtime and SA01-T05 worktree safety may run in parallel after SA00.
- SA06 fixture UI may begin after A2 using committed fixtures while SA03–SA05 backend proceeds, but live integration waits for A5.
- Within PLANNED workflow, Scouts and independent worker tasks may run concurrently only when Task Manager owns separate AO sessions/worktrees and resource policy permits.
- Upstream sync, migration work, rebrand/updater work, and Square features never share one implementation task.

# 11. Initial controlled dispatch order

```text
1. Owner accepts this plan and Architecture Amendment.
2. Run SA00-T01 using its full task packet.
3. Run SA00-T02 and preserve the unchanged baseline.
4. Run SA00-T03, SA00-T04, then gate SA00-T05/A0.
5. Complete SA01 and A1 before relying on AO Windows lifecycle.
6. Complete SA02 in order and freeze `1.0-draft` contracts.
7. Complete SA03, SA04, and SA05.
8. Build SA06 from fixtures in parallel only after A2; integrate after A5.
9. Complete SA07 and use Square Core Alpha internally before implementing PLANNED.
10. Complete SA08 and SA09.
11. Complete SA10–SA11 and reach Square MVP.
12. Add route plurality/clients/resources/release only after MVP evidence.
13. Keep SA15 disabled.
```

# 12. Definition of ready

A task is ready only when:

- all prerequisite receipts/gates are accepted;
- starting commit and authority hashes are known;
- actual source paths/symbols have been inspected;
- write scope is nonoverlapping;
- public contracts and acceptance criteria are unambiguous;
- dependencies/security/licensing decisions are recorded;
- test environment and evidence path exist;
- STOP conditions are explicit.

# 13. Definition of done

A task is done only when:

- implementation and generated outputs are complete;
- targeted and required regression tests pass;
- no forbidden path changed;
- diff and evidence are reviewable;
- completion receipt is generated;
- remaining risks/deviations are explicit;
- task result is independently reviewed;
- downstream readiness is stated but not assumed.

# 14. Immediate next action

Use `plans/KICKOFF_PROMPT_SA00-T01.md` and `plans/tasks/SA00-T01.md`. Do not begin with rebranding, UI implementation, persistence migrations, or Square workflow code.
