# Workstream E2 / SA01-T02 — Daemon Identity and Lifecycle Contract Plan

Status: **planning only**. No product code, test, or configuration file has been
modified to produce this plan. No implementation is dispatched. No source
reconnaissance beyond reading the task packet, prior SA00 identity findings, and
the T01 scout report has been performed for this plan; a preflight source map
(task packet §2) is still required before dispatch and is out of scope for a
planning document.

A0 status: **BLOCKED**, confirmed twice — `docs/square/receipts/SA00-T05.json`
and `docs/square/receipts/SA00-T05-superseding-20260811T054849Z.json`
(`"sa01_dispatch": "PROHIBITED"`). SA01-T02 additionally requires **accepted
SA01-T01** per its own header (`Prerequisites: A0 and accepted SA01-T01`), so it
is blocked transitively even in the counterfactual where A0 alone passed.

Plan authored against: `3e387757ac4cce2b7e63c59fe0c478701560f382` (`square/main`).

Source task packet: `docs/superpowers/plans/2026-08-09-fork-agent-orchestrator/square-session-first-implementation-pack/plans/tasks/SA01-T02.md`.

## 1. Objective (unchanged from task packet)

One compatible per-user Square daemon owns one Square data directory/database and
local endpoint. Simultaneous desktop/CLI launches converge on that daemon. Stale
run files, unrelated listeners, official AO, incompatible versions, or ambiguous
process identity never trigger unsafe reuse or blind duplicate spawn.

## 2. Dependencies

### Upstream

- **A0 PASS** — hard prerequisite.
- **Accepted SA01-T01** — hard prerequisite per the task packet header. T02 owns
  identity/ownership; T01 owns window/daemon lifetime separation. T02's stale/
  takeover classification and readiness contract is exactly what T01 conditionally
  needs (§2 of the E1/T01 plan), so sequencing T02 after an *accepted* T01 (not
  merely dispatched) avoids two tasks independently inventing the safe-status
  contract. If schedule pressure requires overlap, the two task owners must agree
  on the exact shape of the safe-status/readiness query before either merges —
  this plan does not authorize T02 to bypass that coordination.
- **SA00-T03 identity register** — task packet §2 requires reviewing it before
  preflight; it establishes what AO-vs-Square identity fields already exist.

### Downstream consumers

- SA01-T01 conditionally consumes the safe-status/readiness contract.
- SA01-T04 (restart reconciliation) requires T02's instance-generation concept
  (task packet §8 "Restart" subsection: "restart publishes a new instance
  generation; client reconnect notices generation change").
- SA01-T03 does not depend on T02 directly (both list only A0 as a prerequisite
  and are documented as parallelizable in T05's header), but T02's process
  identity fields (PID + start time) should stay consistent with whatever
  process-identity fencing T03 defines for ConPTY children, so the two do not
  define incompatible notions of "process identity" for the same OS. This plan
  flags it as a coordination point for E5, not a hard dependency edge.

## 3. Invariants (task packet §3, §6, §7)

The locked reuse contract — an endpoint is reusable only when **all** of these
match:

```
product identity = Square
protocol/API compatibility = accepted range
per-user ownership = current user context
expected data-root identity = exact canonical root
instance readiness = ready
instance generation/ID = valid
```

Additional invariants:

- PID alone is never sufficient proof of ownership.
- A port responding with unrelated HTTP is not a Square daemon.
- A stale file is not authority by itself.
- Official AO and Square may coexist but must never share database/data root,
  run/PID/port file, endpoint identity, worktree root, or updater/telemetry
  identity.
- Ambiguous process identity is never killed — only surfaced for recovery
  instructions or owner-gated cleanup.
- Only one daemon may open the authoritative DB for writes; clients never open it
  directly; concurrent starts never run migrations twice; a daemon pointed at a
  different data root is a separate explicit instance, not silently reused; the
  official AO DB is never opened via fallback.
- Endpoint is loopback-only; port collision with an unrelated listener produces a
  typed error or a safe alternate endpoint under the same ownership record;
  clients never scan arbitrary ports and trust the first response; the run
  record is atomically written and as narrowly permissioned as the platform
  allows.

## 4. Ownership boundaries

**T02-owned** (per task packet §5, "Allowed writes"):

```
backend/internal/config/**
backend/internal/daemon/**
backend/internal/httpd/**               # readiness/identity/status only
backend/internal/cli/**                 # daemon connect/start only
backend/internal/storage/sqlite/**      # no schema change; only configured path/open ownership if needed
frontend/src/main/**                    # discovery/spawn/connect only
frontend/src/shared/**                  # typed daemon identity config
backend/internal/**/*_test.go
frontend/src/**/__tests__/**
frontend/e2e/**
docs/square/adr/ADR-SA01-002-single-daemon-ownership.md
docs/square/evidence/SA01-T02/**
docs/square/receipts/SA01-T02.json
```

This is exactly the set of frontend files the T01 scout report identified as
"excluded from SA01-T01" (`daemon-attach.ts`, `daemon-discovery.ts`,
`daemon-takeover.ts`, `daemon-owner.ts`) — T02 is their explicit owner. No
dependency or schema change is permitted without owner approval.

**Not T02**: window/renderer lifecycle (T01), ConPTY/terminal runtime (T03),
worktree cleanup (T05), any Square domain/workflow schema, session recovery
semantics beyond publishing an instance generation (T04 owns using it).

## 5. Stale-state classification (task packet §7, verbatim, load-bearing)

```
NO_RECORD
STARTING_OWNER
READY_COMPATIBLE
READY_INCOMPATIBLE
STALE_RECORD_NO_PROCESS
STALE_RECORD_PROCESS_ID_REUSED_OR_AMBIGUOUS
UNRELATED_LISTENER
DATA_ROOT_MISMATCH
OWNERSHIP_CONFLICT
```

Every classification decision in implementation and every test must map onto one
of these states; no ad hoc state is to be introduced without a documented reason
in the ADR.

## 6. Test strategy (task packet §8)

| Tier | Cases |
|---|---|
| Deterministic/unit | path canonicalization (spaces, Unicode, case-insensitivity); data-root fingerprint/identity; run-record encode/decode/version/atomic replacement; handshake compatibility matrix; stale classification (all nine states above); unrelated HTTP listener; product/data mismatch; corrupt/partial record; no secret leakage in diagnostic output |
| Windows concurrency | 2, 8, and 20 simultaneous CLI/UI start attempts converge on exactly one daemon/DB owner; all successful clients resolve the same instance ID; no duplicate migration/open; losing contenders exit or connect cleanly; a stale record after crash reconciles; PID reuse/ambiguous identity never kills a process; official AO and Square run simultaneously with zero shared files/endpoint/DB; two explicit Square test data roots stay isolated |
| Restart | normal stop removes/marks the run record safely; crash leaves recoverable stale state; restart publishes a new instance generation; client reconnect notices the generation change; active-session recovery itself is explicitly deferred to T04, not implemented here |

Evidence set (task packet §9): `environment.json`, `identity-config.json`,
`simultaneous-starts.ndjson`, `stale-state-cases.ndjson`, `coexistence.json`,
`database-ownership.json`, `tests.json`, `summary.json`, `manifest.sha256`, with
PIDs recorded alongside process start time/instance ID and usernames redacted
from shared evidence per policy.

## 7. Acceptance criteria (task packet §10)

SA01-AC-07 through SA01-AC-12: single compatible daemon/DB writer under
concurrency; all clients verify identical instance/product/protocol/data
identity; stale/ambiguous/unrelated states fail safely; AO/Square coexistence
without collision; only the daemon opens the authoritative DB; no new
schema/product feature introduced.

## 8. STOP conditions (task packet §11, unchanged)

Stop if: one-owner enforcement needs an unapproved system service/elevation;
exact user/data-root ownership cannot be determined safely; preventing duplicate
writers would require a schema/migration change in this task; stale cleanup
would require killing an ambiguous process; AO coexistence would require broad
final-rebrand work; a new external dependency is required; API compatibility
cannot be verified before reuse; tests would expose real user database/data;
concurrent starts still double-migrate/open the DB; source scope overlaps
T03/T04 materially.

## 9. Explicit blocked status

**Implementation blocked until A0 passes and SA01-T01 is accepted.** This is a
double gate, not a single one: even a hypothetical A0 PASS does not authorize
T02 dispatch without an accepted T01. No preflight source-map work, ADR draft, or
code change is authorized by this document.
