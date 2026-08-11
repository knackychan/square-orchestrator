# Workstream E3 / SA01-T03 — Windows ConPTY and Descendant Cleanup Plan

Status: **planning only**. No product code, test, or configuration file has been
modified to produce this plan. No implementation is dispatched.

A0 status: **BLOCKED**, confirmed twice — `docs/square/receipts/SA00-T05.json`
and `docs/square/receipts/SA00-T05-superseding-20260811T054849Z.json`
(`"sa01_dispatch": "PROHIBITED"`).

Plan authored against: `3e387757ac4cce2b7e63c59fe0c478701560f382` (`square/main`).

Source task packet: `docs/superpowers/plans/2026-08-09-fork-agent-orchestrator/square-session-first-implementation-pack/plans/tasks/SA01-T03.md`.

Prior reconnaissance already performed and preserved at
`docs/square/evidence/SA01-T03/20260811T043626Z/SA01-T03-scout-report.md`; this
plan restates and organizes that reconnaissance into the dependency/invariant/test
structure Workstream E requires and does not repeat its full exact-source map.

## 1. Objective (unchanged from task packet)

Prove and, where necessary, repair AO's existing Windows process/ConPTY runtime
so Square can rely on it instead of rebuilding terminal infrastructure. Cover
interactive input, Unicode/ANSI output, resize, quiet work, cancellation, nested
descendants, redirected parent handles, cleanup, and bounded resource stability.
A narrow reusable platform boundary, not another certification framework.

## 2. Dependencies

### Upstream

- **A0 PASS** — sole hard prerequisite (task packet header lists only A0).
- No dependency on T01 or T02: the task packet header says "may run parallel
  with: SA01-T05 on a separate branch/worktree," and its scope
  (`backend/internal/terminal/**`, runtime adapter leaves) does not intersect the
  Electron lifecycle or daemon-identity surfaces T01/T02 own.

### Cross-task coordination (not blocking dependencies)

- The scout report already records: "SA01-T01 owns Electron/window/daemon
  lifetime and desktop reopen E2E; it should *consume* this task's long-running
  fixture" (the `long_running_for_viewer_close` fixture mode). This is a
  one-directional supply relationship — T01 does not block T03, but T01's E2E
  suite is strongest if it reuses T03's fixture rather than inventing its own.
- "SA01-T05 owns worktree safety and cleanup classification; it should consume
  this task's definitive process-death facts." T05's ownership record (E4 plan
  §6) explicitly lists "inspect active process handles where feasible through
  existing runtime ownership" as a cleanup-algorithm step — that ownership
  signal is T03's process-identity/PID-plus-creation-time fencing output, not
  something T05 should reimplement.
- T02's process-identity fields (E2 plan §2) should stay representationally
  consistent with whatever "process identity" T03 defines (PID + creation time)
  so a future cross-cutting query does not need two incompatible identity
  schemas for the same OS concept. Flagged for E5 resolution, not a hard gate.

### Downstream consumers

- SA01-T04 (restart reconciliation) lists T02 and T03 as prerequisites.
- SA01-T05 consumes T03's process-death facts (see above).
- SA01-T06 (gate A1) consumes T03's evidence and acceptance criteria.

## 3. Invariants (task packet §3, §6, §9)

- Interactive character-mode CLI behavior is preserved.
- Terminal output is captured once by the daemon runtime and forwarded without
  leaking to parent stdout/stderr.
- Input and resize use explicit terminal APIs, never ambient/global state.
- A quiet process remains classified as active when process/child evidence says
  it is alive — no health-check heuristic that mistakes silence for death.
- Graceful cancellation (checkpoint/interrupt, e.g. Ctrl-C) is attempted before
  any authorized hard stop. The scout report flagged that today
  `SessionManager.Kill` calls `Runtime.Destroy` directly, skipping the graceful
  interrupt path — this is a named repair target, not an invariant violation to
  be preserved.
- Hard stop cleans the *complete* descendant tree, not just the direct ConPTY
  child/pty-host PID (scout report: "No Windows Job Object or process-tree
  enumeration exists" today — this is the primary repair target).
- UI/terminal viewer closure does not own or terminate the process.
- No global inheritable handle, no process-name kill, no health-monitoring model.
- Exact runtime/process identity is recorded (PID + creation time, per §7 of the
  task packet, to fence against PID reuse — the scout report notes today's
  registry records PID but not creation identity).
- Terminal escape output cannot invoke host commands.

## 4. Ownership boundaries

**T03-owned** (task packet §5):

```
backend/internal/terminal/**
backend/internal/adapters/**/runtime*        # runtime leaf integration only
backend/internal/ports/**                    # only if existing port is insufficient and owner approves
backend/internal/lifecycle/**                # terminal/runtime callbacks only, not product workflow
tests/fixtures for Windows runtime
frontend/e2e/**                              # terminal attach/input/resize smoke only
docs/square/adr/ADR-SA01-003-windows-terminal-runtime.md
docs/square/evidence/SA01-T03/**
docs/square/receipts/SA01-T03.json
```

No Square domain/API/migration/UI redesign. Job Object containment is explicitly
permitted "in the Windows runtime adapter after recording an ADR" but workflow
logic must not move into it.

**Not T03**: Electron window lifecycle (T01), daemon identity/run-file/takeover
(T02), worktree cleanup decisions (T05, though T03 supplies the process-death
facts T05 consumes).

## 5. Repair scope already identified (from prior scout, load-bearing for dispatch sizing)

1. Add Windows Job Object containment: one unnamed Job Object per pty-host,
   assign before launching the ConPTY workload, use
   `JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE` so descendants inherit containment. No
   new dependency needed — `golang.org/x/sys/windows` v0.44.0 already exposes the
   required APIs.
2. Fence PID operations with Windows process creation time to close the PID-reuse
   gap in the recovery registry.
3. Attempt graceful Ctrl-C and bounded natural exit before hard stop; currently
   `SessionManager.Kill` bypasses this.

## 6. Deterministic fixture scenarios (task packet §6, verbatim)

`normal_exit`, `stdout_stderr_markers`, `unicode`, `ansi`, `large_burst`,
`quiet_active`, `stdin_question`, `resize`, `crash`, `graceful_cancel`,
`ignore_cancel`, `nested_children`, `long_running_for_viewer_close`. Markers
unique per run; no provider credentials required.

## 7. Test strategy (task packet §10–§12)

| Category | Requirement |
|---|---|
| Startup contract | child attached to intended ConPTY; stdout/stderr markers enter the ConPTY stream and never leak to separately redirected parent stdout/stderr; cwd/env/argv preserve spaces and Unicode; non-child-inheritable handles are non-inheritable; one documented owner/close point per handle; output reader starts before overrun/block; startup failure releases every acquired resource |
| Stream/terminal behavior | byte order preserved; UTF-8 and ANSI supported; no silent truncation of markers; bounded high-volume presentation without blocking drain; read-completion vs broken-pipe vs error distinguished; resize observed by fixture; input reaches fixture exactly once; terminal close ≠ viewer detach |
| Cancellation/containment | graceful path (checkpoint/interrupt → bounded grace → observe natural exit/descendants → close resources → report) and hard-stop path (only after explicit test authorization: terminate full process tree, keep draining/closing terminal, verify every recorded PID+creation-identity is gone, report hard-stop separately from natural exit) are tested as distinct, non-conflatable paths |
| Resource stability | 25 sequential cycles per core scenario (or justified smaller expensive subset); 10 rounds at concurrency 1, 4, 8 for normal/burst/quiet/cancel/nested; process/owned-runtime counters return to zero after each session; no round-to-round growth in handles/goroutines/threads after warmup; exact peak/stable values recorded — no threshold-raising to hide growth |
| Windows matrix | normal PowerShell parent; parent stdout redirected; parent stdout+stderr redirected; path with spaces; Unicode path/file; normal (non-admin) user; desktop terminal attach/detach; app close/reopen coordinated with T01 where available |

Evidence set (task packet §13): `environment.json`, `runtime-source-map.json`,
`dependency-license.json`, `scenarios.ndjson`, `concurrency.ndjson`,
`process-identities.json`, `resource-checkpoints.json`,
`terminal-output-hashes.json`, `tests.json`, `summary.json`, `manifest.sha256`.
Store bounded excerpts plus hashes/lengths, not unbounded raw output.

## 8. Acceptance criteria (task packet §14)

SA01-AC-13 through SA01-AC-19: all fixture I/O/resize/exit scenarios pass on
Windows; stdout/stderr enter ConPTY without leaking to parent streams;
viewer/UI closure does not stop the process; graceful and hard-stop paths are
distinct and complete; no descendant survives an authorized hard stop or tested
owner-loss scenario; no persistent per-cycle resource growth after
warmup/quiescence; no provider account required.

## 9. STOP conditions (task packet §15, unchanged)

Stop if: the target AO terminal architecture cannot support interactive Windows
CLIs; correct operation requires globally inheritable handles or unsafe
shell-string execution; complete descendant containment cannot be achieved in
the runtime leaf; a terminal-close thread/goroutine remains blocked; output
leaks after correct startup/handle isolation; linear resource growth persists;
a natural scenario requires cleanup hard stop; repair would require changing
Square workflow/domain/UI; a new native dependency has unclear license/security;
test evidence would require credentials or valuable repositories. The scout
report's already-active STOP conditions (dirty shared checkout — implementation
requires a clean isolated worktree; A0 not passed) remain in force.

## 10. Explicit blocked status

**Implementation blocked until A0 passes.** A0 is the sole hard prerequisite for
this task, and it is BLOCKED under both the original and superseding reviews. No
dispatch, clean-worktree creation, or code change is authorized by this document.
Per the prior scout report's dispatch decision: after A0 passes, dispatch
fixture-first evidence and runtime repair only within the task packet's
terminal/runtime scope, starting from a clean isolated branch/worktree off the
accepted A0 tip.
