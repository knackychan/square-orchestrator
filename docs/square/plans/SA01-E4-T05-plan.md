# Workstream E4 / SA01-T05 — Worktree Safety and Cleanup Classification Plan

Status: **planning only**. No product code, test, or configuration file has been
modified to produce this plan. No implementation is dispatched. No source
reconnaissance beyond the task packet has been performed for this plan; a
preflight source map (task packet §2) is still required before dispatch.

A0 status: **BLOCKED**, confirmed twice — `docs/square/receipts/SA00-T05.json`
and `docs/square/receipts/SA00-T05-superseding-20260811T054849Z.json`
(`"sa01_dispatch": "PROHIBITED"`).

Plan authored against: `3e387757ac4cce2b7e63c59fe0c478701560f382` (`square/main`).

Source task packet: `docs/superpowers/plans/2026-08-09-fork-agent-orchestrator/square-session-first-implementation-pack/plans/tasks/SA01-T05.md`.

## 1. Objective (unchanged from task packet)

Prove and harden Git worktree creation, ownership, cleanup, and restart behavior
on Windows. A successful agent process must not hide cleanup failure; cleanup,
update, and uninstall must never delete dirty, untracked, locked, ambiguous, or
user-owned work.

## 2. Dependencies

### Upstream

- **A0 PASS** — sole hard prerequisite (task packet header lists only A0).
- No dependency on T01 or T02: T05's header explicitly allows "may run parallel
  with: SA01-T03 in separate branch/worktree," and its scope
  (`backend/internal/ports`, `adapters/**/git*`, `service`, `lifecycle`,
  `domain`, narrow `storage/sqlite`, `httpd` status-only, `renderer` display-only)
  does not intersect Electron lifecycle or daemon identity.

### Cross-task coordination (not blocking, but load-bearing for correctness)

- Task packet §10 ("Windows/process coordination"): "Worktree cleanup must use
  the runtime/session facts from SA01-T03/T04 when available. Do not guess that
  a process is gone from terminal status alone. Where T04 is not yet accepted,
  record deferred integration and use deterministic fixture ownership." This
  means T05 can and should be dispatched and evidenced independently of T03/T04
  landing first, but its cleanup-algorithm step 5 ("inspect active process
  handles where feasible through existing runtime ownership," §8) is a stub
  against deterministic fixtures until T03's process-identity facts (E3 plan §3,
  §5) and T04's reconciliation are accepted. This plan records that as a
  **deferred integration, not a hard prerequisite** — matching the task packet's
  own wording exactly.
- T05 must not invent its own process-liveness heuristic in the interim; the
  task packet is explicit that guessing from terminal status alone is
  disallowed. Deterministic fixture ownership (synthetic, test-controlled
  ownership records) is the only substitute pending T03/T04.

### Downstream consumers

- SA01-T04 (restart reconciliation) benefits from T05's cleanup-state model when
  reconciling worktrees across daemon restarts, though T04's own prerequisites
  are T02/T03, not T05.
- SA01-T06 (gate A1) consumes T05's evidence and acceptance criteria.

## 3. Invariants (task packet §3, §6, §7, §10)

- One active writer binding owns one Square/AO-managed worktree.
- Repository root and worktree paths are canonicalized before any comparison.
- No shell-string Git invocation (argv-based only — command-injection surface
  must not exist).
- Dirty/untracked work is always preserved, never silently discarded.
- Cleanup warning is a status distinct from task/process success — a task may be
  `SUCCEEDED` with `CLEANUP_WARNING`, and UI/API must not hide that.
- Access-denied/in-use paths are never force-deleted.
- Worktree ownership must be proven before removal — a path alone is not proof.
- The main repository and user-created worktrees are never treated as managed
  temp worktrees, under any code path.
- Stale Git metadata (`.git/worktrees` entries) is reconciled conservatively,
  never destructively by default.
- Restart does not blindly recreate or delete a worktree.
- Spaces, Unicode, long paths, and case-insensitive comparison are tested, not
  assumed safe.

## 4. Ownership boundaries

**T05-owned** (task packet §5):

```
backend/internal/ports/**                 # only workspace interface if required
backend/internal/adapters/**/git* or workspace*
backend/internal/service/**               # only worktree lifecycle application behavior
backend/internal/lifecycle/**             # reconciliation/cleanup facts
backend/internal/domain/**                # AO worktree status/error types only
backend/internal/storage/sqlite/**         # only a new AO ownership/status migration if unavoidable
backend/internal/httpd/**                  # expose truthful cleanup status only
frontend/src/renderer/**                   # display cleanup warning/status only
relevant tests/fixtures/e2e
docs/square/adr/ADR-SA01-005-worktree-safety.md
docs/square/evidence/SA01-T05/**
docs/square/receipts/SA01-T05.json
```

No Square workflow/session schema change.

**Not T05**: Electron lifecycle (T01), daemon identity (T02), ConPTY/terminal
runtime (T03) — T05 *consumes* T03's process-death facts but does not redefine
them; process-liveness determination for the purpose of "is this worktree still
in use" is T03/T04's fact, not a new T05 heuristic.

## 5. Worktree ownership record (task packet §6, load-bearing schema)

A managed worktree must carry:

```
project/repository identity
AO session/task identity
canonical path
branch/ref
creation baseline commit
creation generation/owner
created-by-AO marker/fact
dirty/untracked status at cleanup
cleanup state/reason
last error
```

A path alone is never sufficient ownership proof — this is the same principle
T02 applies to daemon endpoints (E2 plan §3: "PID alone is never sufficient
proof") applied to worktrees.

## 6. Cleanup state model (task packet §7, verbatim, load-bearing)

```
ACTIVE
RELEASE_REQUESTED
CLEAN_REMOVABLE
DIRTY_RETAINED
UNTRACKED_RETAINED
IN_USE_RETRYABLE
AMBIGUOUS_OWNERSHIP
GIT_METADATA_STALE
REMOVED
CLEANUP_WARNING
CLEANUP_FAILED
OWNER_ACTION_REQUIRED
```

Process outcome and cleanup outcome are tracked as separate fields at all times.

## 7. Safe cleanup algorithm (task packet §8, ten-step, must not be reordered/skipped)

1. prove managed ownership and canonical repository relation;
2. ensure no current writer/controller binding uses it;
3. inspect Git worktree metadata;
4. inspect tracked modifications, staged changes, untracked files, conflicts;
5. inspect active process handles where feasible through existing runtime
   ownership (deferred to T03/T04 facts per §2 above; deterministic fixture
   ownership stands in until they are accepted);
6. remove only clean, owned worktree through a Git-aware operation;
7. never use recursive filesystem deletion as the normal path;
8. on failure, retain path/branch/metadata and expose the actionable state;
9. prune only proven-stale metadata, never user work;
10. record every cleanup attempt idempotently.

## 8. Test strategy

### Fixture matrix (task packet §9, verbatim — 19 disposable-repository cases)

Clean worktree create/remove; path with spaces; Unicode repository/worktree/
file; uncommitted tracked change; staged change; untracked file/directory; merge
conflict; open file/process lock; current working directory held by a child
process; long path within supported policy; branch-name collision; existing
user-created worktree; stale `.git/worktrees` metadata; missing directory but
present metadata; directory exists but ownership record missing; process
succeeds but cleanup fails; cancellation/hard stop then cleanup; daemon restart
during cleanup; repeated cleanup request.

### Required tests (task packet §11)

Canonical path and repository-root containment; command argument
safety/injection; ownership record validation; dirty/untracked preservation;
in-use retry classification; user-worktree refusal; branch collision; stale
metadata reconciliation; idempotent remove/prune; process-success +
cleanup-warning UI/API surfacing; restart recovery; no recursive delete outside
the managed root.

Evidence set (task packet §12): `environment.json`, `git-version.json`,
`cases.ndjson`, `paths-and-ownership.json`, `dirty-preservation.json`,
`cleanup-statuses.json`, `tests.json`, `summary.json`, `manifest.sha256`.
Evidence may include temporary paths but never real user repository contents;
fixture files are hashed when needed.

## 9. Acceptance criteria (task packet §13)

SA01-AC-26 through SA01-AC-31: every owned clean fixture creates and removes
safely; dirty/untracked/conflicted/user-owned work is retained; cleanup failure
is separate from process/task result; spaces/Unicode/branch-collision/restart
cases are safe; repeated cleanup is idempotent; no path outside a proven managed
worktree is deleted.

## 10. STOP conditions (task packet §14, unchanged)

Stop if: current code can delete unproven/user work and a narrow safe fix is
unclear; safe ownership would require a schema migration overlapping SA03 rather
than an AO platform fact; Git commands would require shell-string execution;
Windows locks can only be bypassed with force deletion; a main repository or
user-created worktree is removed/damaged in a test; a test fixture path escapes
its temporary root; cleanup would depend on killing ambiguous processes;
upstream source structure requires broad redesign; a new dependency's
license/security is unreviewed; evidence would contain real repository data.

## 11. Explicit blocked status

**Implementation blocked until A0 passes.** A0 is the sole hard prerequisite for
this task, and it is BLOCKED under both the original and superseding reviews. No
preflight source-map work, ADR draft, or code change is authorized by this
document.
