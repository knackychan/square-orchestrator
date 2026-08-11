# Workstream E1 / SA01-T01 — Desktop Lifetime Detachment and Reconnect Plan

Status: **planning only**. No product code, test, or configuration file has been
modified to produce this plan. No implementation is dispatched.

A0 status: **BLOCKED**, confirmed twice —
`docs/square/receipts/SA00-T05.json` (`6972e30e...`, BLOCKED) and the superseding
review `docs/square/receipts/SA00-T05-superseding-20260811T054849Z.json`
(`de5e7754...`, BLOCKED, `"sa01_dispatch": "PROHIBITED"`). SA01-T01 implementation
is therefore not authorized under any circumstance until a new SA00-T05 review
records `PASS`.

Plan authored against: `3e387757ac4cce2b7e63c59fe0c478701560f382` (`square/main`).

Source task packet: `docs/superpowers/plans/2026-08-09-fork-agent-orchestrator/square-session-first-implementation-pack/plans/tasks/SA01-T01.md`.

Prior reconnaissance already performed and preserved at
`docs/square/evidence/SA01-T01/20260811T042847Z/SA01-T01-scout-report.md`; this
plan restates and organizes that reconnaissance into the dependency/invariant/test
structure Workstream E requires and does not repeat its full exact-source map.

## 1. Objective (unchanged from task packet)

Make the Electron window a client of the local daemon, not its owner. Closing or
reloading the window releases UI resources only. A daemon holding live sessions,
terminals, pending interactions, or recovery work continues running. Reopening the
app reconnects to the same daemon/session without duplication.

## 2. Dependencies

### Upstream (must exist before T01 implementation starts)

- **A0 PASS** — hard prerequisite per task packet header. Currently BLOCKED.
- A clean, committed review target and accepted authority/ADR/evidence set (A0
  blocker list items 1–5 in the superseding gate receipt).

### Cross-task (T02 identity/readiness contract)

T01 requires a typed daemon status query (§8 of the task packet: instance
identity/version, active session/terminal/controller counts, pending
interaction/recovery count, safe-idle-exit permission). The scout report
(`SA01-T01-scout-report.md` §"Proposed non-overlapping file scope" and
§"SA01-T02 overlap risks") already identified that:

- `frontend/src/shared/daemon-attach.ts`, `daemon-discovery.ts`, and
  `daemon-takeover.ts`, and `frontend/src/main/daemon-owner.ts` encode identity
  and takeover heuristics that belong to T02, not T01.
- The safe-stop contract (an authoritative "is it safe to exit" query) does not
  exist yet in the codebase and cannot be safely invented inside frontend-only
  scope.

**Ordering decision for this plan:** T01 may implement everything that does not
require authoritative safe-idle knowledge (window/renderer lifecycle separation,
reconnect-without-recreate, stdio drain) using the *existing* run-file/readiness
signals it already has. It must treat the safe-idle/typed-status query as a
narrow, explicitly-scoped consumer interface supplied by T02. If T02 has not
landed a compatible contract by the time T01 is dispatched, T01 must either (a)
stop and request a combined amendment per the task packet §2, or (b) implement
only the conservative fallback already permitted by the task packet §8: "keep the
daemon alive unless explicit stop is requested and it reports no active AO work."
Option (b) does not require inventing new identity semantics and is the
recommended default so T01 is not blocked waiting on T02.

### Downstream consumers

- SA01-T04 (restart reconciliation, prerequisite: T02, T03) consumes T01's
  lifecycle separation and controller-generation concept.
- SA01-T06 (gate A1) consumes T01's evidence and acceptance criteria.

## 3. Invariants (from task packet §3, §6, §7)

1. Closing the last window is not permission to terminate the daemon.
2. Renderer reload/navigation/crash is not permission to terminate the daemon or
   agent sessions.
3. Explicit "Stop daemon" is a distinct, authorized operation from "Close
   window"/"Quit UI" — never a process-group kill of an ambiguous target.
4. The daemon must not exit while it owns live work, a pending interaction, a
   writer/worktree, an active terminal, or in-progress recovery/reconciliation.
5. Reopening the desktop connects to the existing compatible daemon before any
   spawn is considered.
6. The UI must never re-create an AO session merely because its previous
   view/controller was closed.
7. If Electron spawned the daemon, detachment must not leave unconsumed stdio
   pipes (no blocked pipe, no orphaned inherited handle).
8. No `taskkill`/process-name kill, no arbitrary keep-alive timer without an
   ownership model, no hidden duplicate daemon, no optimistic renderer state that
   claims sessions are alive without daemon evidence.

## 4. Ownership boundaries

**T01-owned** (post-A0, per scout report):
- `frontend/src/main.ts` — desktop lifecycle separation, renderer-only cleanup.
- `frontend/src/main/supervisor-link.ts` — only if the Electron liveness socket
  must stop representing desktop lifetime.
- Renderer reconnect paths, only where a test demonstrates a real gap:
  `frontend/src/renderer/lib/daemon-status.ts`,
  `frontend/src/renderer/lib/event-transport.ts`,
  `frontend/src/renderer/hooks/useDaemonStatus.ts`,
  `frontend/src/renderer/lib/terminal-mux.ts`,
  `frontend/src/renderer/hooks/useTerminalSession.ts`.
- Matching frontend unit tests and a new real-Electron lifecycle E2E harness.
- `backend/internal/daemon/**` and `backend/internal/httpd/**` only for the
  narrow safe-status/safe-stop contract if T02 has not already supplied it (task
  packet §5 permits this only conditionally).

**Explicitly not T01** (owned by T02 or later):
- `frontend/src/shared/daemon-attach.ts`, `daemon-discovery.ts`,
  `daemon-takeover.ts`, `frontend/src/main/daemon-owner.ts`.
- Backend run-file, instance identity, stale/foreign-owner classification,
  takeover rules — all T02.
- Migrations, storage schema, session-manager semantics, ConPTY/runtime behavior
  (T03), worktree cleanup (T05), updater/telemetry, UI redesign.

**Forbidden regardless of scope** (task packet §6): Square domain/API/migration/UI
redesign, route/model behavior changes, worktree cleanup changes, terminal
runtime changes beyond connect/disconnect ownership, new updater/telemetry
behavior.

## 5. Test strategy

Structure follows the task packet §9 plus the scout report's evidence matrix.

| Layer | Cases | Notes |
|---|---|---|
| Main-process unit/integration | multi-window partial close; last-window close; renderer reload; renderer crash; explicit UI quit; explicit safe daemon stop (calls daemon operation, not kill); reopen-connects-to-existing; incompatible endpoint surfaced | Use a fake/mock daemon endpoint for determinism; real endpoint only in the Windows E2E tier |
| Windows E2E (real Electron + real daemon + deterministic long-running fixture, ideally the fixture SA01-T03 defines) | start desktop → create session → record daemon/session/process identity → close window → prove daemon/child identity alive → reopen → prove same daemon/session/controller generation, no duplicate session/process/worktree → stop fixture via authorized API → safe daemon stop → prove cleanup | This is the closest existing harness is `test/e2e-pod/real-app.spec.ts`, which currently proves boot/readiness/run-file ownership only, not close/reopen continuity — new coverage is required, not an extension of a black-box assumption |
| Reload/crash-specific | separate real reload case and forced renderer-process termination case | Task packet §9 explicitly requires these as distinct cases from window close |
| Simultaneous reopen/spawn dedup | proof that duplicate daemons are not created on concurrent reopen | Full ownership proof is T02's; T01 only proves it does not itself spawn blindly when the T02 contract says an owner exists |

Required evidence artifact set (task packet §10, already named in the prior
scout report): `environment.json`, `before-process-topology.json`,
`lifecycle-cases.ndjson`, `identity-comparison.json`, `stdout-stderr-drain.json`,
`tests.json`, `summary.json`, `manifest.sha256`, each case recording daemon PID +
start time/instance ID, AO session ID, controller generation (where available),
agent process identity, worktree path, and duplicate/no-duplicate outcome.

## 6. Acceptance criteria (task packet §11, restated for traceability)

SA01-AC-01 through SA01-AC-06: no work-stopping on UI close/reload/crash; exact
reconnect not recreate; UI/daemon stop as separate authorized operations; no
blocked/inherited stdio; explicit incompatibility signaling; no product
feature/migration introduced.

## 7. STOP conditions (task packet §12, unchanged)

Stop if: safe detachment needs unreviewed OS service/elevation; live-ownership
identification would overlap T02 materially; reconnect requires recreating
sessions instead of reading durable state; Electron close semantics cannot change
without out-of-scope updater/publisher changes; the only workaround is
disabling window close; a child/process leak appears; exact identity cannot be
measured; tests require a valuable repository or credentials; source paths exceed
approved scope; upstream behavior differs enough to need an architecture
amendment.

## 8. Explicit blocked status

**Implementation blocked until A0 passes.** This plan, and the preserved scout
report it builds on, are reconnaissance and design artifacts only. No dispatch of
SA01-T01 implementation is authorized by this document. Re-dispatch requires: an
accepted A0 receipt, the post-A0 dispatch brief items already listed in the scout
report (clean baseline commit, accepted T02 identity/readiness contract or an
explicit decision to use the conservative fallback in §2 above, explicit
ownership of the safe-status/safe-stop API, approved path list, and real-Electron
test harness location).
