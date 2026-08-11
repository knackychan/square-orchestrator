# Workstream E5 — SA01-T01/T02/T03/T05 Dependency and Ownership Map

Status: **planning only**. No product code, test, or configuration file has been
modified to produce this map. No implementation is dispatched.

A0 status: **BLOCKED**, confirmed twice — `docs/square/receipts/SA00-T05.json`
(`6972e30e...`) and the superseding review
`docs/square/receipts/SA00-T05-superseding-20260811T054849Z.json`
(`de5e7754...`, `"sa01_dispatch": "PROHIBITED"`). Every task mapped below is
blocked on A0 regardless of its position in this graph.

Plan authored against: `3e387757ac4cce2b7e63c59fe0c478701560f382` (`square/main`).

Inputs: task packets for SA01-T01/T02/T03/T05, `SA01.md` slice reference, and the
per-task plans produced alongside this map:
[SA01-E1-T01-plan.md](SA01-E1-T01-plan.md),
[SA01-E2-T02-plan.md](SA01-E2-T02-plan.md),
[SA01-E3-T03-plan.md](SA01-E3-T03-plan.md),
[SA01-E4-T05-plan.md](SA01-E4-T05-plan.md).

## 1. Dependency graph

```
                A0 (BLOCKED — gates all four tasks unconditionally)
                 |
        +--------+--------+-----------------+
        |                 |                 |
     SA01-T01           SA01-T03          SA01-T05
   (A0 only)          (A0 only;          (A0 only;
        |            parallel-safe        parallel-safe
        |            with T05)            with T03)
        v                 |                 |
     SA01-T02              \_______supplies_facts______/
  (A0 + accepted                   (soft, non-blocking:
      T01)                          T03 -> T05 process-death facts;
        |                           T01 conditionally consumes
        v                           T03's fixture and, until T02
     SA01-T04                       lands, falls back to
  (T02 + T03)                       conservative "keep-daemon-alive")
```

SA01-T06 (gate A1) depends on T01, T02, T03, T05 all being accepted; it is out of
this workstream's scope but shown for completeness of the chain to A1.

## 2. Edge-by-edge rationale

| Edge | Kind | Source | Why |
|---|---|---|---|
| A0 → T01, T02, T03, T05 | Hard prerequisite | Each task packet header | T01/T03/T05 list only A0; T02 lists A0 *and* accepted T01. No task's header lists anything not A0-derived. |
| T01 → T02 | Hard prerequisite | T02 header: "Prerequisites: A0 and accepted SA01-T01" | T02 must not be dispatched merely because A0 passes; an accepted (not just dispatched) T01 is required. |
| T02, T03 → T04 | Hard prerequisite (out of E5 scope, shown for context) | SA01.md slice reference, T04 header | Not owned by this workstream; recorded so E1–E4 owners know what downstream work is waiting on their acceptance. |
| T03 ⇢ T05 | Soft, one-directional data supply | T03 scout report + T05 task packet §10 | T05's cleanup algorithm step 5 wants "process handles... through existing runtime ownership." T03 is the authoritative source of process-identity/death facts. T05 does not block on T03; it falls back to deterministic fixture ownership per its own task packet wording, and later re-integrates T03/T04 facts. |
| T03 ⇢ T01 | Soft, one-directional data supply | T03 scout report | T01's Windows E2E fixture should reuse T03's `long_running_for_viewer_close` fixture mode rather than inventing a parallel one. Not a blocking dependency — T01 can be dispatched and evidenced without T03 existing, but its evidence is stronger if sequenced after T03's fixture is available. |
| T02 ⇢ T01 | Soft, conditional data supply (note: opposite direction of the hard T01→T02 edge above) | T01 scout report §"SA01-T02 overlap risks"; T01 plan §2 | T01 *wants* T02's typed safe-status/readiness contract for its safe-idle-exit query, but T01 is dispatched and accepted before T02 is even authorized (T02 requires *accepted* T01). This is resolved by T01 using the conservative fallback explicitly permitted in its own task packet §8 ("keep the daemon alive unless explicit stop is requested and it reports no active AO work") rather than waiting on T02. T01 must not invent T02's identity/takeover logic to fill the gap. |
| T02 ↔ T03 | Coordination only, no dependency | E2 plan §2, E3 plan §2 | Both should converge on the same "process identity = PID + creation time" representation so a later cross-cutting query isn't forced to reconcile two schemas. Neither task blocks the other; this is a naming/shape convention to settle once, ideally in whichever ADR lands first (`ADR-SA01-002-single-daemon-ownership.md` or `ADR-SA01-003-windows-terminal-runtime.md`), with the second ADR referencing the first. |

## 3. Ownership boundary matrix (no path may be claimed by two tasks)

| Path prefix | Owner | Notes |
|---|---|---|
| `frontend/src/main.ts` | T01 | Desktop lifecycle separation |
| `frontend/src/main/supervisor-link.ts` | T01 | Only if Electron socket must stop representing desktop lifetime |
| `frontend/src/main/daemon-owner.ts` | T02 | Identity/ownership, not window lifecycle |
| `frontend/src/shared/daemon-attach.ts`, `daemon-discovery.ts`, `daemon-takeover.ts` | T02 | Explicitly excluded from T01 by its own scout report |
| `frontend/src/renderer/lib/{daemon-status,event-transport}.ts`, `hooks/{useDaemonStatus,useTerminalSession}.ts`, `lib/terminal-mux.ts` | T01 | Only where a test demonstrates a real reconnect gap |
| `frontend/src/renderer/**` (worktree cleanup display only) | T05 | Display/status only, not reconnect logic |
| `backend/internal/daemon/**` | T02 primary; T01 only for the narrow safe-status query if T02 hasn't supplied one yet | Conflict risk — see §4 below |
| `backend/internal/httpd/**` | T02 primary (readiness/identity/status); T01 conditionally (explicit status/shutdown contract); T05 (truthful cleanup status exposure only) | Three tasks touch this tree — see §4 |
| `backend/internal/config/**` | T02 | Product/data identity roots |
| `backend/internal/cli/**` | T02 | Daemon connect/start only |
| `backend/internal/terminal/**`, `adapters/**/runtime*` | T03 | ConPTY/runtime leaf only |
| `backend/internal/ports/**` | T03 (runtime port) or T05 (workspace port) — narrow, owner-approved additions only, not overlapping | Both tasks may touch this directory for different, non-overlapping interfaces |
| `backend/internal/lifecycle/**` | T03 (terminal/runtime callbacks) and T05 (reconciliation/cleanup facts) — non-overlapping subsets | Both tasks may touch this directory; neither owns product workflow logic here |
| `backend/internal/adapters/**/git*`, `**/workspace*` | T05 | Worktree/Git operations |
| `backend/internal/service/**` | T01 (session live-ownership query, conditional) and T05 (worktree lifecycle application) — non-overlapping subsets | |
| `backend/internal/domain/**` | T05 only (AO worktree status/error types) | T01/T02/T03 do not touch domain types per their task packets |
| `backend/internal/storage/sqlite/**` | T02 (no schema change; path/open ownership only) and T05 (only a new AO ownership/status migration if unavoidable) | Both narrow; neither owns Square domain schema |
| `docs/square/adr/ADR-SA01-{001,002,003,005}-*.md` | 001 T01, 002 T02, 003 T03, 005 T05 | No numbering collision found; 004 is unassigned in the reviewed task packets (T04's ADR, if any, is out of this workstream's scope) |
| `docs/square/evidence/SA01-{T01,T02,T03,T05}/**`, `docs/square/receipts/SA01-{T01,T02,T03,T05}.json` | Matching task | No collision |

## 4. Conflicts identified and resolution

1. **`backend/internal/httpd/**` is claimed by three tasks** (T01 conditionally,
   T02 primarily, T05 for cleanup status only). Resolution: T02 is the primary
   owner of readiness/identity/status routes. T01's claim is conditional and
   only activates if T02's contract does not exist yet at T01 dispatch time —
   but T02 requires an *accepted* T01 to even start, so in practice T01 will
   always be the first to touch this tree, and T02 must build its contract
   compatibly with whatever narrow status route T01 added rather than
   replacing it unilaterally. T05's claim (cleanup status exposure) is
   additive and does not modify T01/T02's routes. **Action for dispatch
   packets:** T01's post-A0 dispatch brief (E1 plan §8) must record the exact
   shape of any interim status route it adds, so T02's implementer treats it as
   a starting contract to extend, not dead code to delete.
2. **`backend/internal/daemon/**` is claimed by both T01 (conditionally) and
   T02 (primary).** Same resolution as above — T01's conditional narrow
   addition, if used, becomes the seed T02 extends into the full identity
   contract.
3. **`backend/internal/lifecycle/**` and `backend/internal/ports/**` are
   claimed by two tasks each (T03/T05).** Both task packets scope their claim
   narrowly ("terminal/runtime callbacks, not product workflow" for T03;
   "reconciliation/cleanup facts" for T05, and "only a workspace interface if
   required" for T05's ports claim vs. T03's runtime port claim). No file-level
   overlap is expected because the two tasks add different files under the same
   directory. **Action for dispatch packets:** each task's first-response
   report (task packet §"First response required" in both T03 and T05) should
   list the exact new file names under these directories so a later reviewer
   can confirm no filename collision occurred.
4. **No conflict found** between T01 and T05, or between T02 and T03/T05 — their
   claimed path sets are disjoint.

## 5. Sequencing recommendation for post-A0 dispatch

Given the dependency graph and the soft-supply edges, the lowest-risk dispatch
order is:

1. **T03 and T05 in parallel** (explicitly sanctioned by both task packets,
   separate worktrees) — neither depends on T01/T02, and T05 benefits from
   having T03's process-identity ADR/shape available early even though it does
   not block on it.
2. **T01** — consumes T03's fixture if available by this point; otherwise
   proceeds with the conservative safe-idle fallback (E1 plan §2). Does not
   need to wait for T05.
3. **T02** — dispatched only after T01 is *accepted*, not merely merged. If T03
   has already landed its process-identity ADR, T02 should align its process
   ownership fields with it per §2 edge "T02 ↔ T03" above.
4. **T04** (out of this workstream) — after T02 and T03 are both accepted.
5. **T06 / gate A1** (out of this workstream) — after T01, T02, T03, T05 are all
   accepted.

This ordering satisfies every hard prerequisite edge in §1 and resolves the two
soft-supply edges (T03→T01, T03→T05) by having T03 land first among the four,
without introducing any hard dependency the task packets themselves do not
state.

## 6. Explicit blocked status

**Implementation for all four tasks (T01, T02, T03, T05) is blocked until A0
passes.** This map does not authorize dispatch of any task. Once A0 passes,
this map's ownership matrix (§3) and conflict resolutions (§4) should be
re-validated against the accepted A0 commit before any task packet is issued,
since file layout may have shifted as part of the A0-fix work
(`SA00-FIX04` and successors) that the current A0 blockers require.
