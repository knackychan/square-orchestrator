# SA01-T05 — Preflight source map and worktree-safety plan

- Task: SA01-T05 — Worktree cleanup and dirty-state safety
- Task spec: `docs/superpowers/plans/2026-08-09-fork-agent-orchestrator/square-session-first-implementation-pack/plans/tasks/SA01-T05.md`
- Phase: SA01 — Windows lifecycle and AO platform hardening
- Gate contribution: A1
- Prerequisite: A0
- Document status: **PLANNING ONLY — READ ONLY**
- Product code changed: **none**
- Authority read: `docs/square/authority/02-session-domain-model.md`, `docs/square/authority/04-persistence-and-events.md`

## 0. Authorization status (blocking)

This document is a preflight scout report only. It is **not** an ADR, **not** task
evidence, and **not** a completion receipt.

Per `docs/square/gates/A0-adoption-review.json` at reviewed commit `6972e30e`:

- `decision`: **BLOCKED**
- `allowed_next_tasks`: **`[]`** (empty)
- `next_authorized_action`: *"Owner must create and explicitly authorize a new
  narrowly scoped SA00-FIXxx packet; after its committed receipt, SA00-T05 may be
  rerun as a superseding independent review."*

A0 blockers of record:

1. Canonical upstream remote is not preserved in current Git configuration.
2. Review workspace is dirty and includes unclassified `frontend/package-lock.json` drift.
3. SA00-T04 authority/ADR/evidence/receipt/supersession artifacts are absent from `reviewed_commit`.
4. Telemetry is not hard-disabled by default for packaged downstream builds.
5. Updater is not hard-disabled through SA14 and retains an official AO feed path.
6. Square and AO runtime/data/product identities are not implemented as isolated identities.
7. SA00-T02 and SA00-T03 evidence manifests do not verify against committed bytes.

**Consequence:** SA01-T05 is not an authorized task at this time. Nothing in
section 8 may be dispatched until A0 is re-reviewed and passes.

**Placement note:** this file deliberately does **not** live under
`docs/square/evidence/SA01-T05/`. A0 blocker 7 is precisely "evidence manifests do
not verify against committed bytes", so adding unstamped, unmanifested files to an
evidence tree would risk reproducing the failure mode that blocked the gate. When
SA01-T05 is authorized and executed, its evidence goes to
`docs/square/evidence/SA01-T05/<UTC-stamp>/` with the file set required by task
spec §12 and a `manifest.sha256` that verifies.

## 1. Exact sources

The entire worktree surface is one package:
`backend/internal/adapters/workspace/gitworktree/`.

| Concern | Symbol | Location |
|---|---|---|
| Managed root, canonicalization | `New`, `physicalAbs`, `validateManagedPath`, `pathWithin` | `workspace.go:94`, `:1140`, `:1295`, `:1321` |
| Path/branch naming | `managedPath`, `restorePath`, `resolvedSessionPrefix`, `defaultSessionBranchName` | `workspace.go:1236-1269` |
| ID containment guard | `validatePathComponent`, `cleanRelativePath` | `workspace.go:1226`, `:1280` |
| add/list/remove/prune argv | `worktreeAdd*Args`, `worktreeRemoveArgs`, `worktreeForceRemoveArgs`, `worktreePruneArgs`, `worktreeListPorcelainArgs` | `commands.go:26-72` |
| Cleanup entry points | `Destroy`, `ForceDestroy`, `DestroyWorkspaceProject`, `forceDestroyPath` | `workspace.go:261`, `:316`, `:238`, `:970` |
| Dirty detection | `isDirty` -> `git -C <wt> status --porcelain` | `workspace.go:1080`, `commands.go:66` |
| Dirty preservation | `StashUncommitted` -> `refs/ao/preserved/<id>`, `ApplyPreserved` | `workspace.go:353`, `:499` |
| Stale metadata | `registeredWorktreeDirMissing`, `staleRegistrationForPath`, `addNewBranchWorktree` | `workspace.go:740`, `:814`, `:852` |
| Branch collision | `findWorktreeByBranch`, `workspaceProjectBranch` (suffix loop) | `workspace.go:1339`, `:891` |
| Windows lock retry | `removeAllWithRetry` (18 tries / 7250ms cap, ctx-aware) | `remove.go:81` |
| Porcelain parse | `parseWorktreePorcelain`, `worktreeRecord{Locked,Prunable,...}` | `parse.go:18` |

### Command execution

Already argv-only: `runCommand` -> `aoprocess.CommandContext`
(`workspace.go:1378`). No shell-string Git invocation anywhere in the package.
Task spec STOP condition 3 is clear.

### Ownership metadata (persisted)

- `session_worktrees(session_id, repo_name, branch, base_sha, worktree_path,
  preserved_ref, state)` — `backend/internal/storage/sqlite/migrations/0009_workspace_projects.sql:16`.
  `state` enum already covers `active | removed | retry_remove | unavailable | stray_moved`.
- `sessions.workspace_repo_path` — `migrations/0034_add_session_workspace_repo_path.sql`,
  added explicitly as a teardown fact so cleanup survives project unregistration.
- `session_cleanup_facts` + `sessions.cleanup_generation` —
  `migrations/0030_session_cleanup_facts.sql`; domain types in
  `backend/internal/domain/cleanup.go` (`SessionCleanupRecord`, `WorkspaceDisposition`).

### Cleanup on success / failure / cancel / restart

- Kill: `session_manager.Manager.Kill` — `manager.go:906`. Dirty => `freed=false`,
  session still marked terminated.
- Sweep: `Manager.Cleanup` / `cleanupOne` — `manager.go:2242`, `:2278`.
- Spawn rollback: `rollbackPreparedSpawnWorkspace`, `preserveFailedSpawnWorkspace`
  — `manager.go:713`, `:734`.
- Capture-then-force: `saveAndTeardownOne` — `manager.go:1415`.

### API / UI cleanup status

`POST /api/v1/sessions/cleanup` -> `CleanupSessionsResponse{cleaned, skipped[{sessionId, reason}]}`
(`backend/internal/httpd/controllers/sessions.go:942`;
`backend/internal/service/session/service.go:68`).

### Existing Windows tests

`remove_test.go` covers the retry loop deterministically on every platform via the
`removeAllRetryEnabled` / `removeAll` seams. The `gitworktree` integration suite
(`workspace_integration_test.go`) already covers clean create/remove, dirty tracked,
locked worktree, stale registration, missing-dir-with-metadata, sibling
independence, remoteless repo, stash/apply round-trip and conflict,
branch-checked-out-elsewhere, and branch-not-fetched.

## 2. Findings that decide the shape of the work

### (a) `session_cleanup_facts` is fully unwired

`UpsertSessionCleanupFacts`, `GetSessionCleanupFacts`, and
`ListTerminalCleanupCandidates` have **zero non-test callers**. The table, CDC
triggers, retry/backoff columns, generation fencing, and the candidate-scan SQL all
exist and are tested at the store level — nothing writes them.

This is the largest lever available: the T05 state model can land on an existing,
migrated, CDC-wired substrate rather than a new migration, which keeps STOP
condition 2 (schema overlap with SA03) clear.

### (b) Cleanup outcome is not exposed as a durable field

`skipped[].reason` is a transient per-call string. `workspace_disposition` appears
in **no** httpd, OpenAPI, or frontend file — verified twice, including a sweep over
ignored/vendored files and `*.ts`/`*.tsx`/`*.yaml`; both sweeps returned the same
seven files, all under `backend/internal/{domain,storage}`.

So today a task can be `SUCCEEDED` with its cleanup warning visible only to whoever
watched that one cleanup response. This is exactly what SA01-AC-28 forbids.

### (c) `Destroy` can recursively delete an unproven path

`workspace.go:302`: when the path is not found in the post-prune worktree list,
`Destroy` calls `removeAllWithRetry(ctx, path)`. `validateManagedPath` proves only
*containment in `managedRoot`* — not AO ownership. A directory that exists at
`managedRoot/<projectID>/<sessionID>` but was never a registered worktree (fixture
"directory exists but ownership record missing") is recursively deleted.

The fix is narrow and local (prove ownership before removal), so this is a finding,
not a STOP.

### (d) Case-sensitivity split on Windows

`pathWithin` is case-*insensitive*: Go's `filepath.Rel` uses `sameWord` /
`strings.EqualFold` on Windows. But `findWorktree`, `findWorktreeByBranch`, and the
`addWorktree` conflict check use exact `==` on `filepath.Clean`, which is
case-*sensitive*. A case-differing path from `git worktree list --porcelain`
therefore reads as "not registered" and falls straight into finding (c)'s
`removeAllWithRetry`.

Also to verify: `validateManagedPath`'s `clean == w.managedRoot` guard is
exact-match, so a case-differing root may pass the "not the root itself" check.
`physicalAbs` / `EvalSymlinks` canonicalizes case for *existing* paths and falls
back preserving case for missing ones — which is why this is reachable precisely in
the stale/missing-directory cases.

**Must be measured on Windows before severity is asserted.**

### (e) `Destroy` runs an unconditional repo-wide `git worktree prune`

`workspace.go:274`. This is the exact operation that `workspace.go:718-731`
documents as unsafe, because it drops *sibling* sessions' registrations whose
directories git cannot currently see. The recovery path was hardened against this;
the teardown path was not.

## 3. Proposed ownership and cleanup state model

### Ownership record

Composed from existing storage; `session_worktrees` extended additively rather than
a new table.

| Task spec §6 requirement | Source |
|---|---|
| project / repository identity | `sessions.project_id` + `sessions.workspace_repo_path` (0034) |
| AO session / task identity | `session_worktrees.session_id` |
| canonical path | `session_worktrees.worktree_path` — store canonicalized, add a case-fold comparison key |
| branch / ref | `session_worktrees.branch` |
| creation baseline commit | `session_worktrees.base_sha` (exists; single-repo path does not populate it — gap) |
| creation generation / owner | `sessions.cleanup_generation` (exists; nothing bumps it) |
| created-by-AO marker | **missing** — propose a git-native per-worktree config key set at add time and read back to prove managed ownership |
| dirty / untracked status at cleanup | `isDirty` result recorded at attempt time |
| cleanup state / reason / last error | `session_cleanup_facts.workspace_disposition` / `failure_code` |

### Ownership proof rule

Removal requires **all three**:

1. canonical path within `managedRoot`;
2. a matching `session_worktrees` row;
3. a matching `git worktree list --porcelain` record.

Any two-of-three => `AMBIGUOUS_OWNERSHIP`, never a delete. A path alone is never
proof of ownership.

### State mapping

Task spec §7 states mapped onto existing enums. No new migration if both CHECK
constraints can be extended additively; if they cannot, that is a STOP-2 decision
point for the owner.

```text
ACTIVE                -> session_worktrees.state='active'
RELEASE_REQUESTED     -> facts.disposition='pending', attempt_count=0
CLEAN_REMOVABLE       -> transient (isDirty=false + ownership proven)
DIRTY_RETAINED        -> 'preserved_dirty' + failure_code=WORKSPACE_DIRTY
UNTRACKED_RETAINED    -> 'preserved_dirty' + failure_code=WORKSPACE_UNTRACKED
IN_USE_RETRYABLE      -> 'pending'         + failure_code=WORKSPACE_IN_USE (next_attempt_at set)
AMBIGUOUS_OWNERSHIP   -> 'failed'          + failure_code=WORKSPACE_OWNERSHIP_UNPROVEN
GIT_METADATA_STALE    -> 'pending'         + failure_code=WORKSPACE_METADATA_STALE
REMOVED               -> 'removed' / session_worktrees.state='removed'
CLEANUP_WARNING       -> derived: terminal session AND disposition not in (removed, not_applicable)
CLEANUP_FAILED        -> 'failed'
OWNER_ACTION_REQUIRED -> 'preserved_dirty' | 'failed' (both stop auto-retry; user retry required)
```

Process outcome (`sessions.is_terminated`, Kill's `freed`) and cleanup outcome
(`workspace_disposition`) remain separate fields. The schema already enforces this
separation.

## 4. Safe path-containment tests

All pure or table-driven; no real repository required.

1. `validateManagedPath` rejects: empty, relative, unclean, outside root, **equal to
   root under differing case**, and a path whose parent symlinks out of root.
2. `pathWithin` vs `findWorktree` **agreement test** — the same pair must never be
   "inside" for one and "unregistered" for the other. This encodes finding (d) as an
   executable invariant.
3. `validatePathComponent` / `cleanRelativePath` fuzz: `..`, `.`, `/`, `\`, UNC
   `\\?\`, `C:`, trailing dot/space, reserved device names (`CON`, `NUL`), Unicode
   NFC/NFD pairs.
4. **No-delete-outside-root property test:** wrap the `removeAll` seam with a stub
   asserting every argument is strictly under a per-test `t.TempDir()`; fail on the
   first escape. Cover `Destroy`, `ForceDestroy`, `forceDestroyPath`, and
   `DestroyWorkspaceProject`.
5. Fixture-root containment self-check: every fixture asserts its own paths are
   under `t.TempDir()` before acting, making STOP condition 6 a test assertion.
6. argv snapshot tests extending `TestCommandArgs`: assert no argument is ever a
   shell-metacharacter-joined string, and that `--force` never appears in the
   non-force paths.

## 5. Disposable fixture matrix

All fixtures use `t.TempDir()` and `git init` only. No user repository is touched.

Already covered by the existing integration suite — do **not** rebuild: clean
create/remove, dirty tracked, locked worktree, stale registration, missing directory
with present metadata, sibling independence, remoteless repo, stash/apply round-trip
and conflict, branch-checked-out-elsewhere, branch-not-fetched.

New fixtures SA01-T05 must add:

| # | Fixture | Expected state |
|---|---|---|
| 1 | path with spaces | REMOVED |
| 2 | Unicode repo / worktree / file name | REMOVED |
| 3 | staged-only change | DIRTY_RETAINED |
| 4 | untracked file *and* untracked directory | UNTRACKED_RETAINED |
| 5 | merge conflict in worktree | DIRTY_RETAINED |
| 6 | open file handle held by test process | IN_USE_RETRYABLE (Windows) |
| 7 | child process cwd = worktree | IN_USE_RETRYABLE |
| 8 | long path within supported policy | REMOVED |
| 9 | branch-name collision on create | suffixed branch, no clobber |
| 10 | user-created worktree, no ownership row | AMBIGUOUS_OWNERSHIP, untouched |
| 11 | directory present, ownership row absent | AMBIGUOUS_OWNERSHIP, untouched (finding c) |
| 12 | case-differing registered path | must resolve as registered (finding d) |
| 13 | sibling worktree with missing dir during a Destroy | sibling registration survives (finding e) |
| 14 | process succeeds, cleanup fails | terminated + CLEANUP_WARNING |
| 15 | cancel / hard stop then cleanup | facts recorded, no force delete |
| 16 | daemon restart mid-cleanup | generation fencing rejects stale finalize |
| 17 | repeated cleanup request | idempotent; only attempt_count increments |
| 18 | main repository passed as target | refused, never treated as managed |

Fixtures 6, 7, and 16 depend on runtime/process facts — see section 6.

## 6. Non-overlapping implementation scope

### Owned by SA01-T05

- `backend/internal/adapters/workspace/gitworktree/**` — ownership proof before
  removal, case-safe record matching, targeted reconciliation instead of repo-wide
  prune, typed cleanup classification.
- `backend/internal/domain/cleanup.go` — additive states and failure codes only.
- `backend/internal/lifecycle/**` or `backend/internal/session_manager` — wire the
  existing `session_cleanup_facts` finalizer and retry sweep.
- `backend/internal/httpd/**` and `frontend/src/renderer/**` — surface durable
  disposition; truthful status only, no new UI concepts.
- Tests and fixtures in the same trees.
- `docs/square/adr/ADR-SA01-005-worktree-safety.md`,
  `docs/square/evidence/SA01-T05/**`, `docs/square/receipts/SA01-T05.json`.

### Explicitly not SA01-T05

- ConPTY and process-runtime internals — **SA01-T03**.
- Restart reconciliation and controller-generation semantics — **SA01-T04**.
- Any `square_*` table or Square session/workflow schema.

Where T05 needs "is a process still holding this path", it consumes T04's facts.
**T04 is not accepted**, so T05 records a deferred integration and uses
deterministic fixture ownership, which task spec §10 explicitly permits. Fixtures 6,
7, and 16 are written against a seam, not against T03/T04 internals.

Parallel-safety with T03: no shared files. T03 touches `process/`, `terminal/`, and
runtime adapters; T05 touches `adapters/workspace/`, `lifecycle/`, and cleanup
storage.

## 7. STOP conditions — status at preflight

| # | Condition | Status |
|---|---|---|
| 1 | Code can delete unproven work, no narrow fix | **NOT triggered** — finding (c) is real, but the fix (prove ownership before `removeAllWithRetry`) is narrow and local |
| 2 | Ownership needs an SA03-overlapping migration | **WATCH** — clear only if the `workspace_disposition` and `session_worktrees.state` CHECK constraints extend additively; a new table means STOP and escalate |
| 3 | Git requires shell-string execution | **Clear** — `aoprocess.CommandContext` is argv-only throughout |
| 4 | Windows locks bypassable only by force deletion | **OPEN** — must be measured on Windows; if fixtures 6/7 cannot reach IN_USE_RETRYABLE without force, STOP |
| 5 | Main repo or user worktree damaged in a test | **Clear** — must stay clear; fixture 18 plus the no-delete-outside-root stub enforce it |
| 6 | Fixture path escapes its temporary root | **Clear** — enforced as an assertion (§4.5) |
| 7 | Cleanup depends on killing ambiguous processes | **Clear by design** — no process-name killing proposed; unresolvable holds become IN_USE_RETRYABLE |
| 8 | Upstream structure needs broad redesign | **Clear** — the substrate exists and is unwired, not misdesigned |
| 9 | Unreviewed dependency license/security | **Clear** — no new dependency proposed |
| 10 | Evidence contains real repository data | **Clear** — evidence records temp paths and hashes only |

**Recommended additional STOP:** if finding (d) proves that `validateManagedPath`
accepts the managed root itself on Windows, that is a live "delete the whole managed
root" path and warrants immediate escalation rather than a quiet fix.

## 8. Post-A0 dispatch brief

**Not dispatchable now.** A0 is BLOCKED with `allowed_next_tasks: []`. The gate
requires an owner-authorized `SA00-FIXxx` packet and a superseding SA00-T05 review
before any SA01 task, including this one, becomes eligible.

Once A0 passes, dispatch in this order, each a separately reviewable step:

1. **Measure first (read-only, Windows).** Run the existing `gitworktree`
   integration suite on Windows. Record `git-version.json`, `environment.json`, and
   the case/locking behaviour behind findings (d) and (e). This resolves STOP-4 and
   STOP-2 before any code moves.
2. **Ownership proof and containment.** Add the three-part ownership check ahead of
   every `removeAllWithRetry`; make record matching case-correct; replace the
   repo-wide prune in `Destroy` with targeted reconciliation. Tests from §4;
   fixtures 10-13, 18.
3. **Classification and durable state.** Map cleanup outcomes to typed states and
   failure codes; wire the existing `session_cleanup_facts` finalizer and
   capped-backoff sweep with generation fencing. Fixtures 14-17.
4. **Truthful exposure.** Surface `workspace_disposition` through the sessions read
   model and render a cleanup warning distinct from task success. Fixture 14
   asserted end-to-end.
5. **Evidence, ADR, receipt** per task spec §12, then the A1 readiness report.

Acceptance coverage: step 2 -> AC-26 / AC-27 / AC-31; step 3 -> AC-28 / AC-29 /
AC-30; step 4 -> AC-28.

## 9. Open question for the owner

The pre-existing `session_cleanup_facts` machinery (migration 0030, store, domain
types, CDC triggers, candidate-scan SQL) was built and tested but never connected to
any caller. Confirm whether wiring it is in scope for SA01-T05, or whether it was
deliberately parked by an earlier task. If it was parked intentionally, step 3 of
the dispatch brief changes owner.
