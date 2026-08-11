# Delivery Roadmap — What to Build First

The full plan is intentionally exhaustive. It is not a recommendation to launch 97 agents at once. Work proceeds through gates and small dispatch packets.

## Product increments

| Increment | Included slices | User-visible result |
|---|---|---|
| Fork foundation | SA00 | Controlled AO fork, exact baseline, legal/identity boundaries |
| Safe Windows substrate | SA01 | Desktop/daemon/session/worktree lifecycle is safe enough to reuse |
| Frozen Square vocabulary | SA02 | Square Session, workflow, roles, routing/model contracts |
| Durable fake system | SA03–SA05 | Database/API/event history and deterministic fake workflows |
| Session UI foundation | SA06 | Approved rounded UI works against fixtures and then live read models |
| Square Core Alpha | SA07 | One real route completes a QUICK session in desktop and CLI |
| Operational reliability | SA08 | Questions, approvals, controller leases, cancellation and restart |
| Memory/context | SA09 | Project/global memory and bounded cited context |
| Planned orchestration | SA10 | Secretary/Planner/Orchestrator/workers run at bounded artifact boundaries |
| Square MVP | SA11 | Deterministic validation, independent review, finite repair, final receipt |
| Product expansion | SA12–SA14 | More routes/clients, resources/evaluation, security/release/upstream sync |
| Optional scale | SA15 | Parallelism/practice evolution only when measured value justifies it |

## Critical path

```text
SA00 Fork adoption → SA01 Windows lifecycle → SA02 Contracts → SA03 Durable state
→ SA04 Execution facade → SA05 Session API → SA07 QUICK → SA08 Interactions
→ SA09 Memory/context → SA10 PLANNED → SA11 MVP
```

Milestone references above are `SAxx — Short title` from `TASK_INDEX.md`. SA06 can begin from accepted SA02 fixture contracts while SA03–SA05 backend work continues, but its live integration waits for A5.

## First ten dispatches

```text
1.  SA00-T01  Create and pin the downstream fork
2.  SA00-T02  Capture the unchanged Windows build/test/package baseline
3.  SA00-T03  License, attribution, identity, telemetry, updater, and data isolation design
4.  SA00-T04  Import authority and activate architecture amendment
5.  SA00-T05  Adoption gate A0
6.  SA01-T01  Detach daemon/workflows from desktop window lifetime
7.  SA01-T03  Windows ConPTY, input/output/resize/cancel, and descendant cleanup
8.  SA01-T05  Worktree cleanup and dirty-state safety
9.  SA01-T02  Single daemon and Square data-directory ownership
10. SA01-T04  Restart reconciliation and controller generation
```

SA01-T01, SA01-T03, and SA01-T05 may use separate worktrees when their inspected write paths do not overlap. Gate **SA01-T06 — Windows foundation gate A1** follows all five.

## Complexity labels

Use these labels in future task packets rather than treating all tasks as equal:

- `S` — one narrow package/component and targeted tests;
- `M` — one atomic cross-layer resource or lifecycle behavior;
- `L` — integration task with several packages but one public outcome;
- `GATE` — evidence review and decision, no feature implementation.

A task that grows beyond one reviewable commit should be split before coding unless its transaction/migration/compatibility contract cannot be separated safely.

## What is deliberately postponed

The Alpha does not require:

- project-global fleet dashboard;
- VS Code client;
- many provider families;
- cost/exposure balancing;
- thermal/SSD scheduling;
- full docking customization;
- parallel writers;
- automatic practice evolution;
- final installer/updater.

The UI reference may display future concepts only when clearly fixture-labelled. Product code cannot imply they are implemented.
