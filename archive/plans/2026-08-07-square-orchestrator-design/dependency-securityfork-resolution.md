# Dependency Security Fork — Owner Resolution and SP02-T01 Proof Guide

- Date: 2026-08-07
- Status: owner decision recorded; implementation-ready addendum
- Parent fork trace: `dependency-securityfork.md` (this directory)
- Authority: this addendum is planning output under `docs/**`. It records a decision and a proof
  procedure. It does not activate a task, admit a dependency, or describe shipped behavior. Only an
  owner-activated `SP02-T01` task may admit the candidate packages, and only after the proof below
  passes.

## 1. Purpose

`dependency-securityfork.md` records a fork that occurred during the M1 `.NET` port: a worker
identified a real dependency/security conflict, then resolved it by replacing the locked SQLite
persistence technology with a dependency-free file store. A second review (GPT 5.6 Sol) pushed back
and ruled the replacement forbidden. This document records the owner decision, explains who is right
and why, assesses the impact on the chain, and gives an exhaustive, drift-proof guide for the
`SP02-T01` dependency-admission proof so this class of design drift cannot recur.

## 2. The fork, restated

During the M1 `.NET` port, a worker needed persistent state for the project registry and locks. The
M1 Python implementation used the standard-library `sqlite3` module. The `.NET` port has no
built-in SQLite, so the worker reached for `Microsoft.Data.Sqlite`, an external NuGet package.

The worker then discovered:

- `Directory.Packages.props` was empty and stated that each external dependency must be reviewed
  (license, security, architectural role) before admission.
- The transitive native binary `SQLitePCLRaw.lib.e_sqlite3` carried by the
  `Microsoft.Data.Sqlite` meta-package is affected by CVE-2025-6965 (SQLite < 3.50.2,
  GHSA-2m69-gcr7-jv3q), with no patched release on the `<= 2.1.11` line.

The worker declared a hard security stop and resolved it by **rewriting M1 persistence as a
dependency-free file store** under `%LOCALAPPDATA%`, recording the decision in
`Directory.Packages.props`.

## 3. Who is right

**The second reviewer (Sol) is right. The worker's security instinct was correct; its resolution
was forbidden.**

### 3.1 What the worker got right

- `Microsoft.Data.Sqlite` is not part of the .NET BCL and requires a recorded review before
  admission.
- The empty `Directory.Packages.props` is an **admission process**, not a permanent zero-dependency
  policy. The architecture already expects external packages (xterm.js, WebView2, a .NET SQLite
  provider).
- A known vulnerable transitive native binary must not be admitted silently. The fail-closed reflex
  was correct.

### 3.2 Where the worker went wrong

1. **A hard security stop means escalate, not invent a different architecture.** The correct worker
   output was `STOP: reviewed package candidate currently appears to resolve a vulnerable native
   dependency. No architecture change has been made. Owner decision or dependency-proof task
   required.` Instead, the worker continued from "hard stop" to "I will rewrite persistence
   dependency-free," which is exactly the cross-boundary decision the worker rules prohibit.

2. **A dependency-free file store is not behaviorally equivalent to the locked SQLite
   implementation.** The locked persistence decision (sliced plan §3) is "SQLite metadata/event
   state plus content-addressed artifact files." Replacing SQLite with a JSON/file store changes
   far more than storage syntax: transaction atomicity, append-only events plus current
   projections, ordered schema migrations, unsupported-newer-schema detection,
   backup-before-migration, crash recovery, concurrent readers, leases and idempotency
   enforcement, WAL and checkpoint behavior, corruption handling, and event/projection consistency.
   `SP02-T01` explicitly requires `Square.Persistence.Sqlite`, ordered migrations, the full initial
   table set, append-only events, transactional projections, backup-before-migration, and
   migration/corruption tests. A JSON store is a different persistence architecture, not a temporary
   implementation detail.

3. **The vulnerability conclusion was incomplete.** CVE-2025-6965 affects SQLite < 3.50.2, and the
   old `SQLitePCLRaw.lib.e_sqlite3` package through `2.1.11` is affected. But
   `SQLitePCLRaw.bundle_e_sqlite3 3.0.5` resolves to native SQLite >= 3.53.4, which is above the
   `3.50.2` fix. The meta-package's broad lower bound does not *guarantee* that bundle is selected
   and locked — which is a reason to pin the bundle explicitly, not to abandon SQLite.

4. **The license attribution was wrong.** `Microsoft.Data.Sqlite` is MIT-licensed.
   `SQLitePCLRaw.bundle_e_sqlite3` is Apache-2.0-licensed. The worker attributed Apache-2.0 to
   `Microsoft.Data.Sqlite`.

5. **Sequencing violation.** Under the sliced plan, `SP01` is a pure deterministic slice that
   performs no filesystem or external I/O; SQLite is introduced in `SP02-T01`, which depends on
   `SP01-T06`. Persistence should not have been implemented during the M1 `.NET` port at all.

### 3.3 Why the locked decision wins

A worker cannot decide requirements, architecture, public contracts, schema, security policy, or
cross-task behavior outside its discretion envelope. Technology may be replaced only after a
recorded proof task demonstrates that a leaf choice cannot satisfy its contract, or after an
owner-approved plan amendment. The fail-closed security posture is honored by **pinning a patched
provider and proving it**, not by silently swapping the persistence architecture.

## 4. Impact on the chain

The worker's decision was committed:

- `ed39978` — `feat: port M1 domain to .NET and replace python cli`
- `66ea781` — `fix: enforce manifest newline handling and holder-bound release`

`Directory.Packages.props` records the dependency-free M1 state store as a security-driven decision
and states "SQLite remains the locked SP02 persistence decision, pending a patched provider." Under
Sol's ruling, the dependency-free M1 state store is a deviation from the locked SQLite decision and
must not stand as accepted production behavior.

Net impact: the current repository contains a persistence deviation framed as an accepted
security-driven decision. The locked architecture is preserved in intent but contradicted in
committed code. This is exactly the design drift this addendum exists to close.

### 4.1 Chain-integrity gap (separate from the persistence fork)

As of this addendum, `STATUS.md` is last updated 2026-08-06 and records the active subplan as
`2026-08-05-m1-dry-run-foundation/` with "Application implementation authorized: **no**". Commits
`ed39978` and `66ea781` (dated 2026-08-07) are not recorded anywhere in the authority chain: not in
`STATUS.md`, not in any task block, packet, or `STATE.md`. The `.NET` port's activating task, route,
packet, and owner authorization are absent.

This is a separate chain-integrity gap from the persistence fork. A remediation that fixes the
persistence deviation but leaves the port's authority unrecorded does not restore chain integrity.
Resolving the fork requires resolving this gap too — either by recording the authorization that
permitted the `.NET` port, or by flagging `ed39978` / `66ea781` as unauthorized work to be reverted.
The owner, not a worker, decides which.

## 5. Remediation of the committed deviation

> **Owner decision (2026-08-07): Option A — Revert and defer persistence to SP02-T01.**
> Option B is rejected. The dependency-free file store will be reverted from `ed39978` /
> `66ea781`, M1 is re-scoped as a non-persistent dry-run slice, and `Directory.Packages.props`
> will be corrected to a blocker/evidence note. The `.NET` port's authority is recorded
> retroactively through this amendment: the port is accepted as work that occurred, but its
> persistence layer must be reverted and re-scoped. This resolves the §4.1 chain-integrity gap
> by legitimizing the port (minus persistence) rather than flagging it unauthorized.

The owner must choose one of the following before `SP02-T01` is dispatched. The proof in §6 is the
same under either choice.

### Option A — Revert and defer persistence to SP02-T01 (preferred, OWNER-CHOSEN)

1. Revert the dependency-free M1 state store introduced in `ed39978` / `66ea781`.
2. Re-scope M1 as a non-persistent dry-run slice: the `.NET` M1 port performs no durable state
   writes. Project registry, locks, and `run --dry-run` state are deferred entirely to `SP02-T01`.
3. Correct the `Directory.Packages.props` review note so it no longer frames the file store as the
   locked decision. Replace it with a blocker/evidence note: the candidate package combination in
   §6.1 is pending the `SP02-T01` proof.
4. Record the re-scoping as an owner-approved plan amendment. M1 acceptance is re-based on the
   non-persistent contract.

### Option B — Keep M1 non-persistent without a revert (REJECTED by owner 2026-08-07)

1. Accept that the `.NET` M1 port is non-persistent, but explicitly re-scope it by plan amendment
   (do not let the file store stand as the locked persistence decision).
2. Remove or rewrite the `Directory.Packages.props` review note so it records only the security
   finding and the pending `SP02-T01` proof, not an accepted architecture change.
3. `SP02-T01` then introduces SQLite persistence for the first time, against the full schema in the
   sliced plan.

Either way: **the dependency-free file store must not be described or carried forward as the locked
persistence architecture.** SQLite remains the required persistence technology.

### 5.1 Status of remediation as of this addendum

The planning-layer fix is complete: this addendum adopts Sol's ruling, the sliced plan
`SP02-T01` block embeds the locked candidate package combination, the meta-package prohibition, and
a pointer back here, and the owner has chosen **Option A** (revert and defer). The code-layer
remediation is **complete**:

- The owner accepted REM-01 on 2026-08-07. Commit `b7cd5de03d8b91ea111fcfe01655b2d637ff9201`
  removed the dependency-free file store, re-scoped the `.NET` M1 port as a non-persistent dry-run
  slice, and corrected `Directory.Packages.props` to a blocker/evidence note. `STATUS.md` records
  the acceptance.
- `Directory.Packages.props` no longer frames the dependency-free file store as an accepted
  security-driven decision. It records the CVE-2025-6965 finding and the pending `SP02-T01` proof.
- `src/Square.Persistence.Sqlite/StateStore.cs` and `tests/Persistence.Tests/Program.cs` are deleted;
  the CLI no longer references `Square.Persistence.Sqlite`.

`SP02-T01` remains inactive. The candidate package proof in §6 runs only when the owner activates
`SP02-T01`. The remediation revert and `SP02-T01` are separate tasks; the revert has landed.

## 6. SP02-T01 dependency-admission proof — exhaustive guide

This is the authoritative procedure for admitting SQLite to the repository. It is deliberately
over-specified to prevent drift. A worker executing `SP02-T01` follows it exactly and stops on any
failure.

### 6.1 Exact candidate package combination

| Package | Version | License | Role |
|---|---|---|---|
| `Microsoft.Data.Sqlite.Core` | `10.0.10` | MIT | Managed ADO.NET provider for SQLite |
| `SQLitePCLRaw.bundle_e_sqlite3` | `3.0.5` | Apache-2.0 | Native SQLite binary bundle (>= 3.53.4) |

Rules:

- Add **both** as direct, centrally versioned `PackageReference` entries owned **only** by
  `Square.Persistence.Sqlite`. No other project references these packages directly.
- **Do not reference the `Microsoft.Data.Sqlite` meta-package.** Its broad lower bound
  (`SQLitePCLRaw.bundle_e_sqlite3 >= 2.1.11`) does not make the selected native SQLite version
  explicit and can resolve to the vulnerable `<= 2.1.11` line.
- Pin both versions in `Directory.Packages.props` (central package management). Do not leave
  floating.
- The combination is a **candidate, not pre-approved.** It must pass every check in §6.3 before
  production admission.

### 6.2 Pre-admission setup

1. Confirm central package management is enabled and `Directory.Packages.props` is the single
   version source.
2. Add the two `PackageReference` entries to `src/Square.Persistence.Sqlite/...csproj` only.
3. Update `THIRD_PARTY.md` with, for each package: license, provenance, runtime role, redistribution
   terms, native-binary source, and a security-review record. Use the **correct** licenses:
   `Microsoft.Data.Sqlite.Core` is MIT; `SQLitePCLRaw.bundle_e_sqlite3` is Apache-2.0.
4. Generate and commit the applicable `packages.lock.json` files so resolution is reproducible.

### 6.3 Proof procedure

Run each command from the repository root. Record exact output in task evidence.

**6.3.1 Restore with full audit:**

```powershell
dotnet restore SquareOrchestrator.slnx `
  -p:NuGetAuditMode=all `
  -p:NuGetAuditLevel=low
```

**6.3.2 Transitive package inventory:**

```powershell
dotnet list `
  src/Square.Persistence.Sqlite/Square.Persistence.Sqlite.csproj `
  package --include-transitive
```

**6.3.3 Vulnerability scan:**

```powershell
dotnet list `
  src/Square.Persistence.Sqlite/Square.Persistence.Sqlite.csproj `
  package --include-transitive --vulnerable
```

**6.3.4 Build:**

```powershell
dotnet build SquareOrchestrator.slnx --no-restore
```

**6.3.5 Persistence tests:**

```powershell
dotnet test tests/Persistence.Tests `
  --no-build `
  --logger "console;verbosity=detailed"
```

**6.3.6 Runtime SQLite version verification** — open a database and execute:

```sql
SELECT sqlite_version();
```

Record the exact returned version string in test evidence. It must be `>= 3.50.2` (the CVE fix
line); the candidate bundle resolves to `>= 3.53.4`.

**6.3.7 Behavioral verification** — prove each of the following with a test:

- WAL file creation under a write workload.
- Transaction rollback on a forced failure.
- Atomic event/projection commit (an event and its projection commit together or not at all).
- Database reopen after clean shutdown preserves all committed state.
- `PRAGMA integrity_check;` returns `ok` after a workload.
- Process restart mid-workload recovers to a consistent state (reconcile with the receipt spool
  and WAL).

### 6.4 Fail criteria — the task fails if the resolved graph includes

- `SQLitePCLRaw.lib.e_sqlite3 <= 2.1.11`;
- a native SQLite version below the approved security baseline (`< 3.50.2`);
- any `NU1901` / `NU1902` / `NU1903` / `NU1904` advisory; or
- any unreviewed additional package not named in §6.1.

### 6.5 Stop conditions

- If the package combination fails to compile or load, **STOP**. Submit the complete restore graph,
  compiler/runtime error, and package lock. Do not introduce a different persistence technology.
- If a patched provider is unavailable and no equivalent reviewed combination exists, **STOP** and
  record the evidence. Do not substitute a file store, JSON store, or other persistence mechanism
  without an owner-approved plan amendment or a SQLite-provider ADR.
- If the proof passes, the candidate is admitted for `SP02-T01` only. It is not a blanket approval
  for other tasks or projects.

## 7. Anti-drift guardrails

These rules prevent this class of design drift from recurring. They restate existing authority in
the specific shape this fork exposed.

1. **A hard security `STOP:` is an escalation, not a license to redesign.** The worker records the
   finding and halts. The owner or a separately authorized dependency-proof task resolves it.
2. **Workers cannot change locked decisions.** The locked persistence decision is SQLite. A worker
   may not replace it with a file store, JSON store, or any other persistence mechanism.
3. **Technology replacement requires a recorded proof or plan amendment.** A failed proof can
   replace a leaf technology only through the amendment process, not through a worker's in-task
   judgment.
4. **The empty `Directory.Packages.props` is an admission process, not a zero-dependency policy.**
   "No dependencies yet" does not mean "dependencies are prohibited." Admission requires a recorded
   review; it does not require avoidance.
5. **Persistence belongs to its assigned task.** Do not implement persistence during `SP01` (a pure
   deterministic slice) or during a port whose contract is non-persistent. SQLite admission belongs
   to `SP02-T01`, which depends on `SP01-T06`.
6. **Pin the native binary explicitly.** When a meta-package's broad lower bound can resolve to a
   vulnerable transitive, reference the reviewed bundle directly and lock it centrally. Do not rely
   on the meta-package to select a safe version.
7. **Attribute licenses correctly.** `Microsoft.Data.Sqlite` / `Microsoft.Data.Sqlite.Core` is MIT.
   `SQLitePCLRaw.bundle_e_sqlite3` is Apache-2.0. Verify before recording.
8. **Do not describe planned behavior as shipped.** A candidate package combination is a candidate
   until the proof in §6.3 passes. A file store is not the locked persistence architecture.
9. **Unrecorded commits are a drift vector.** A source commit not reflected in `STATUS.md` and its
   task block escapes the authority chain. The `.NET` port (`ed39978` / `66ea781`) stood for a day
   with no recorded activating task, route, or owner authorization before this addendum flagged it.
   A worker that commits source must be recorded in `STATUS.md` by the activation that authorized it;
   a primary session that accepts a commit must amend `STATUS.md` at the same boundary. A stale
   `STATUS.md` plus unrecorded source commits is the same class of design drift as a forbidden
   architecture swap — both let a worker's decision stand as accepted behavior without owner trace.

## 8. Authority references

- Sliced plan §3 — locked implementation decisions (persistence: SQLite metadata/event state plus
  content-addressed artifact files).
- Sliced plan `SP02-T01` — SQLite schema and migration runner (depends on `SP01-T06`).
- Sliced plan `SP01` — contracts and deterministic domain kernel; performs no filesystem or
  external I/O.
- `SPEC.md` §14 — storage and audit (Python standard-library SQLite for the candidate first
  implementation).
- `Directory.Packages.props` — central package management and the dependency-admission process.
- `dependency-securityfork.md` — the fork trace and the Sol ruling this addendum adopts.
- Root `AGENTS.md` — global invariants (workers cannot decide architecture, schema, or security
  policy outside their discretion envelope; technology replacement requires a recorded proof or
  plan amendment).
