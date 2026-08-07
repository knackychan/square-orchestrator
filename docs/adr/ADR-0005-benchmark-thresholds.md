# ADR-0005 — Benchmark thresholds: reject unmeasured performance budgets at G0

- Status: `REJECTED_FOR_PROMOTION`
- Date: 2026-08-07
- Task: `SP00-T05`
- Scope: terminal, IPC, and shared-UI proof thresholds
- Revisit condition: complete measured SP00-T02 through SP00-T04 evidence and owner-reviewed limits

## Context

SP00-T02 through SP00-T04 define reproducible run shapes and collectable metrics, but no qualifying
Windows measurement set exists. The available numbers are scenario sizes, queue capacities, fixture
sizes, and one provisional handle-growth tolerance. They are not sufficient to claim an acceptable
production CPU, memory, latency, or SSD-write budget.

## Decision

Reject any permanent performance threshold or production capacity claim at G0. Correctness admission
conditions are retained, but they do not open the gate without measured results.

### Retained correctness admission conditions

| Proof | Required correctness result before reconsideration |
|---|---|
| SP00-T02 | All 11 scenarios pass; at least 100 reliability repetitions; all 1/4/8-session groups; owner-crash containment; zero surviving exact process identities; no failed leak check; current provisional handle-growth limit is not silently raised |
| SP00-T03 | All 9 scenarios pass; framing and version errors are typed; DACL and negative access pass; restart/replay is monotonic; queue depths never exceed declared bounds; slow subscriber cannot exhaust unbounded memory |
| SP00-T04 | Both hosts pass; all 27 benchmark cells execute; fixture/source identities match; no sequence loss; hidden panes catch up; bridge/security/accessibility checks pass; cross-host semantic comparison passes |

### Not accepted as production thresholds

- the SP00-T02 handle-growth tolerance of 24;
- the SP00-T03 1 MiB frame limit and proof queue capacities;
- the SP00-T04 262,144 bytes per terminal and 8,192-byte frame size;
- any unmeasured CPU, working-set, JavaScript heap, latency, throughput, or write-volume target; and
- any claim that 1, 4, or 8 terminals are operationally acceptable merely because the source defines
  those cases.

These values remain proof inputs or fail-closed provisional limits. They may be adopted, lowered, or
rejected only after the raw distributions and host details are reviewed.

## Measurement required for a superseding decision

The review set must include, where the proof supports it:

- environment and exact toolchain identities;
- per-scenario success/failure counts;
- p50, p95, maximum, and outliers for output/render latency and total duration;
- CPU time/utilization and peak working set by 1/4/8 concurrency;
- handle/process counts and growth across repetitions;
- bytes read/written and evidence/log volume;
- bounded queue high-water marks and backpressure outcomes;
- host and available browser-heap memory for every UI matrix cell;
- accessibility, scaling, theme, CSP, and lifecycle outcomes; and
- explicit unsupported/unavailable metrics, never substituted with zero.

The owner must then record numeric acceptance limits and their rationale in a superseding ADR. A failed
measurement cannot be converted to `PASS` by increasing a limit in place without an amendment and a
fresh canonical run.

## Consequences

- G0 remains blocked even though host-neutral correctness checks pass.
- SP01 must not be finalized or dispatched as an accepted production contract.
- Later resource/SSD policy work must not reuse proof fixture sizes as product policy.
