# Square on Agent Orchestrator — Session-First Implementation Pack

- Date: 2026-08-09
- Status: implementation-ready planning package; owner acceptance required before dispatch
- Product: Square Orchestrator
- Base: `Untrivial-ai/agent-orchestrator` stable `v0.12.1`, expected release commit prefix `1df40e9` (full hash verified locally by SA00-T01)
- Primary experience: one durable Square Session per topic/outcome, containing conversation, workflow, role runs, docked terminals/Chat, decisions, plan, review, history, and receipt

This package supersedes the earlier combined implementation-plan draft for undispatched work. It keeps Agent Orchestrator as the platform foundation but makes the **Square Session**—not a global operations dashboard—the top-level product object.

## Start here

1. `plans/START_HERE.md`
2. `plans/OWNER_ACCEPTANCE_CHECKLIST.md`
3. `plans/MASTER_IMPLEMENTATION_PLAN.md`
4. `plans/TASK_INDEX.md` — all 97 slices, prerequisites, and packet status
5. `docs/ARCHITECTURE_AMENDMENT.md`
6. `docs/SESSION_DOMAIN_MODEL.md`
7. `docs/ROLE_ROUTING_MODEL_SELECTION.md`
8. `docs/PERSISTENCE_AND_EVENTS.md`
9. `docs/API_AND_EXECUTION_FACADE.md`
10. `docs/SESSION_FIRST_UI_SPEC.md`
11. `docs/UPSTREAM_GOVERNANCE.md`
12. `docs/TEST_AND_RELEASE_STRATEGY.md`
13. `source-map/AO_SOURCE_PLACEMENT_MAP.md`
14. `plans/tasks/SA00-T01.md`
15. `plans/KICKOFF_PROMPT_SA00-T01.md`

## Complete plan coverage

The master plan contains **97 independently identifiable tasks** across:

```text
SA00  fork adoption and baseline
SA01  Windows lifecycle and AO hardening
SA02  session-first contracts and role routing
SA03  persistence, events, artifacts, leases, recovery
SA04  AO execution facade and route certification
SA05  session API/read models/fake workflows
SA06  rounded session-first UI
SA07  QUICK / Core Alpha
SA08  interactions, controller leases, restart
SA09  memory and bounded context
SA10  PLANNED orchestration
SA11  validation, independent review, finite fix, MVP
SA12  role-routing UX, CLI, additional routes, optional VS Code
SA13  exposure, cost, resources, context reuse, evaluation
SA14  security, installer, updater, upstream sync, Windows release
SA15  optional evidence-gated scale
```

Machine-readable task indices are in `plans/TASK_INDEX.json` and `.csv`.

## Detailed ready-to-dispatch packets

Full packets are included for the controlled adoption and Windows foundation waves:

```text
SA00-T01 .. SA00-T05
SA01-T01 .. SA01-T06
```

Later packets are intentionally compiled just before dispatch from the accepted commit and inspected pinned-source symbols. The master plan, packet template, schemas, source map, and acceptance criteria provide their authority without freezing stale paths against a maintained fork.

## Included executable/reference material

- `scripts/bootstrap-square-fork.ps1` — clones and pins the downstream fork without committing or pushing.
- `scripts/verify-ao-baseline.ps1` — captures the unchanged AO Windows baseline.
- `scripts/capture-authority-hashes.ps1` — writes deterministic authority hashes.
- `scripts/generate-task-index.py` — regenerates the 97-task indices from the master plan.
- `scripts/validate-pack.py` — validates JSON, schemas/fixtures when `jsonschema` is available, task index, references, and manifest.
- `schemas/*.schema.json` — initial session, workflow-run, role-run, and role-routing contracts.
- `fixtures/*.json` — QUICK, PLANNED, blocked, and completed session fixtures.
- `templates/*` — dispatch, completion receipt, evidence, gate, ADR, and task-packet templates.
- `examples/*` — per-role routing and new-session examples.
- `starter-overlay/*` — minimal downstream documentation/config skeleton, not product code.
- `ui/square-session-workspace-rounded-reference.html` — approved visual/interaction direction.

## Important interpretation

A **Square Session** is not the same as an AO session.

- Square Session: user-facing topic, conversation, workflow, history, decisions, and durable outcome.
- AO session: one concrete bounded agent execution, Chat controller, terminal process, and optionally a worktree.

One Square Session may create many AO sessions over time. Completed AO terminal histories remain attached to the Square Session.

The Task Manager is deterministic software. Planner, Orchestrator, Workers, Reviewers, Scouts, and Secretary are configurable bounded role runs. Per-role model selection supports `AUTO`, `PREFERRED`, and `PINNED`, with task → session → project → global → default precedence and no silent pinned fallback.

## First controlled sequence

```text
SA00-T01  Create and pin the downstream fork
SA00-T02  Capture the unchanged Windows baseline
SA00-T03  Isolate identity, data, telemetry, updater, and attribution
SA00-T04  Import authorities and activate the architecture amendment
SA00-T05  Adoption gate A0
SA01       Windows lifecycle hardening
SA02       Session-first contracts and role-routing contracts
```

Do not implement workflow, persistence extensions, or the redesigned UI before their gates. Do not merge Square product changes until the unchanged AO baseline and pre-existing failures have been recorded.
