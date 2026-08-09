# Execution Waves — Square Session-First Fork

This is the operational sequencing view of the master plan. It does not replace task prerequisites or gate receipts.

## Wave 0 — Fork control

```text
SA00-T01 → SA00-T02 → SA00-T03 → SA00-T04 → SA00-T05 / A0
```

No product code. Outcome: pinned, attributable, isolated, evidence-backed fork.

## Wave 1 — Windows foundation

```text
A0
├─ SA01-T01 desktop/daemon lifetime
├─ SA01-T03 ConPTY/process lifecycle
└─ SA01-T05 worktree safety

SA01-T01 → SA01-T02 single daemon/data ownership
SA01-T03 + SA01-T02 → SA01-T04 restart/controller generation
all → SA01-T06 / A1
```

Parallel work is allowed only in separate worktrees with nonoverlapping files after packets freeze paths.

## Wave 2 — Contracts

```text
A1 → SA02-T01 → SA02-T02
SA02-T01 → SA02-T03
SA02-T02 + T03 → SA02-T04/T05
SA02-T02..T05 → SA02-T06 → SA02-T07 / A2
```

Freeze `square.contracts 1.0-draft`. UI fixture work may start only after A2.

## Wave 3 — Durable state and execution boundary

```text
A2 → SA03 → A3
A2 + A3 → SA04 → A4
A3 + A4 → SA05 → A5
```

No real provider workflow is required. Use deterministic fake routes and session fixtures.

## Wave 4 — Session-first UI and QUICK Alpha

```text
A2 → SA06 fixture UI
A5 → SA06 live integration → A6
A4 + A5 + A6 → SA07 QUICK → A7 Core Alpha
```

A7 is the first milestone intended for regular internal use.

## Wave 5 — Reliability and memory

```text
A7 → SA08 interactions/recovery → A8
A8 → SA09 memory/context → A9
```

## Wave 6 — PLANNED and MVP

```text
A9 → SA10 PLANNED implementation → A10
A10 → SA11 verification/review/fix/final receipt → A11 MVP
```

## Wave 7 — Product breadth

```text
A11 → SA12 route/client breadth → A12
A12 → SA13 resources/evaluation → A13
A13 → SA14 security/release/upstream sync → A14
```

## Wave 8 — Optional scale

SA15 remains disabled until measured workloads show a material bottleneck and owner accepts the risk/cost.

## First five commits

Recommended sequence:

```text
SA00-T01: establish pinned Square downstream fork
SA00-T02: record unchanged AO Windows baseline
SA00-T03: decide Square identity and isolation boundaries
SA00-T04: install accepted Square session-first authorities
SA00-T05: record adoption gate A0
```

Never combine these into one commit. The unchanged baseline must remain distinguishable from later decisions.
