# Execution Waves — Square Session-First Fork

This is the operational sequencing view of the master plan. It does not replace task prerequisites or gate receipts.

Naming: milestones read as `SAxx — Short title`, tasks as `SAxx-Tyy — Short name`, gates as `Ax — Gate name`. See `TASK_INDEX.md` for the canonical registry.

## Wave 0 — Fork control (`SA00 — Fork adoption and reproducible baseline`)

```text
SA00-T01 Create/pin fork → SA00-T02 Capture baseline → SA00-T03 Isolate identity/updater/telemetry
        → SA00-T04 Import authority → SA00-T05 Adoption gate A0
```

No product code. Outcome: pinned, attributable, isolated, evidence-backed fork.

## Wave 1 — Windows foundation (`SA01 — Windows lifecycle and AO platform hardening`)

```text
A0 — Adoption gate
├─ SA01-T01 Detect daemon/workflows from desktop window lifetime
├─ SA01-T03 Windows ConPTY, input/output/resize/cancel, descendant cleanup
└─ SA01-T05 Worktree cleanup and dirty-state safety

SA01-T01 → SA01-T02 Single daemon/data ownership
SA01-T03 + SA01-T02 → SA01-T04 Restart reconciliation/controller generation
all → SA01-T06 Windows foundation gate A1
```

Parallel work is allowed only in separate worktrees with nonoverlapping files after packets freeze paths.

## Wave 2 — Contracts (`SA02 — Session-first contracts and deterministic domain`)

```text
A1 → SA02-T01 Strong IDs/versions/hashes/time → SA02-T02 Session/message/workflow/role records
SA02-T01 → SA02-T03 Pure lifecycle reducers
SA02-T02 + T03 → SA02-T04 Task/artifact contracts → SA02-T05 Role routing & identity contract
SA02-T02..T05 → SA02-T06 API/event contract → SA02-T07 Contract gate A2
```

Freeze `square.contracts 1.0-draft`. UI fixture work may start only after A2.

## Wave 3 — Durable state and execution boundary

```text
A2 → SA03 Durable state/events/artifacts → A3
A2 + A3 → SA04 Route registry + AO execution facade → A4
A3 + A4 → SA05 Session API/fake workflows → A5
```

No real provider workflow is required. Use deterministic fake routes and session fixtures.

## Wave 4 — Session-first UI and QUICK Alpha

```text
A2 → SA06 fixture UI
A5 → SA06 live integration → A6
A4 + A5 + A6 → SA07 QUICK vertical slice → A7 Core Alpha
```

A7 is the first milestone intended for regular internal use.

## Wave 5 — Reliability and memory

```text
A7 → SA08 Interactions/controller/restart → A8
A8 → SA09 Project/global memory + bounded context → A9
```

## Wave 6 — PLANNED and MVP

```text
A9 → SA10 PLANNED workflow implementation → A10
A10 → SA11 Verification/independent review/fix/final receipt → A11 MVP
```

## Wave 7 — Product breadth

```text
A11 → SA12 Route/client breadth → A12
A12 → SA13 Resources/evaluation → A13
A13 → SA14 Security/release/upstream sync → A14
```

## Wave 8 — Optional scale

`SA15 — Optional advanced scale and controlled practice evolution` remains disabled until measured workloads show a material bottleneck and owner accepts the risk/cost.

## First five commits

Recommended sequence:

```text
SA00-T01: establish pinned Square downstream fork            (Create and pin the downstream fork)
SA00-T02: record unchanged AO Windows baseline               (Capture the unchanged Windows baseline)
SA00-T03: decide Square identity and isolation boundaries    (Identity/telemetry/updater/data isolation design)
SA00-T04: install accepted Square session-first authorities  (Import authority and activate architecture amendment)
SA00-T05: record adoption gate A0                            (Adoption gate)
```

Never combine these into one commit. The unchanged baseline must remain distinguishable from later decisions.
