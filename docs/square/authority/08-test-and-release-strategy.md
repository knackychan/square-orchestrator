# Test, Evidence, Gate, and Release Strategy

## 1. Test layers

| Suite | Scope | Required cadence |
|---|---|---|
| AO Baseline | unchanged upstream build/test/package/lifecycle | adoption and every upstream sync |
| Square Domain | pure reducers, IDs, policy, routing, memory, fairness, breakers | every relevant change |
| Square Contracts | Go/OpenAPI/TypeScript/schema/golden parity | every contract change |
| Square Persistence | migrations, sqlc stores, events/projections, artifacts, idempotency, leases | every persistence change |
| Windows Runtime | ConPTY/process tree/input/output/resize/cancel/attach/desktop close | Windows CI and lifecycle changes |
| Workspace Recovery | worktree creation/cleanup, dirty safety, branch collision, restart | Windows CI/nightly |
| Route Certification | exact installed adapter/CLI/model/permissions/identity | authorized machines only |
| Square Workflow | fake QUICK/PLANNED/interactions/review/fix/restart | every workflow change |
| Square Memory | candidates, promotion, context selection, provenance | every memory change |
| Frontend Unit/A11y | components, query reducers, session/dock/decision fixtures | every frontend change |
| Frontend E2E | session create/switch/start/decision/terminal/history/reopen | frontend and release |
| CLI Contract | JSON/human output, errors, exit codes, redirection/idempotency | every CLI change |
| VS Code | later activation/API/webview/restoration | client changes |
| Performance/Resource | terminal volume, DB writes, session switching, cache/concurrency | nightly/release |
| Security | loopback API, Electron bridge, path scope, terminal output, updater, redaction | every boundary change |
| Upstream Sync | full downstream regression | every upstream intake |

Provider-backed tests never block deterministic CI unless run on an explicitly authorized runner. Store only redacted evidence.

## 2. Evidence package per task

Each task produces:

```text
docs/square/evidence/<task-id>/<timestamp>/
  environment.json
  dispatch.json
  changed-files.json
  commands.ndjson
  tests.json
  findings.json
  summary.json
  manifest.sha256
```

Task receipt records:

- task/attempt ID;
- starting/ending commits;
- authority hashes;
- upstream base;
- dirty-state declaration;
- changed paths;
- validation commands/results;
- evidence hashes;
- discretion decisions;
- STOP/escalation events;
- remaining risk;
- outcome and next eligible tasks.

Do not use the complete terminal transcript as the primary review artifact.

## 3. Gates

### A0 — Adoptable fork

- exact upstream pin and clean baseline;
- legal/notice/telemetry/updater/data isolation plan;
- source builds/tests/package baseline recorded;
- architecture amendment accepted;
- no Square feature code yet.

### A1 — Windows platform safety

- desktop closure does not stop live work;
- one daemon owns one Square data directory;
- Windows terminal/cancel/process/worktree cleanup behavior accepted;
- restart reconciles without duplicate sessions;
- paths with spaces/Unicode pass.

### A2 — Session contracts frozen (`1.0-draft`)

- Square Session/Workflow/Role/Binding/reducer contracts;
- artifacts and role-routing profile;
- API/event/golden schemas;
- no host/adapter dependency in pure domain.

### A3 — Durable state

- migrations and sqlc generated code;
- event/projection atomicity;
- idempotency/leases/bindings;
- artifact store;
- recovery/corruption matrix.

### A4 — Execution/routing substrate

- one certified or deterministic fake route can launch a bounded role through AO facade;
- requested/resolved/actual identity recorded;
- role artifact/termination/reconciliation works;
- no workflow logic in adapters/controllers.

### A5 — Session API and fake workflow

- session/message/workflow/role APIs;
- SSE/read models/history;
- fixture workflows and restart;
- frontend can implement against generated contracts.

### A6 — Session UI foundation

- approved rounded session-first shell;
- session switch/tabs/composer;
- dynamic role docks and attention;
- Plan/Review/History;
- role route setup;
- UI closure/presentation-only tests with fixtures.

### A7 — Square Core Alpha

- one real certified route completes QUICK;
- preview, one worker/worktree, targeted validation, receipt, cancellation;
- desktop and CLI show same state;
- restart-safe.

### A8 — Interactions, controller, and restart lifecycle

- durable question/approval/permission/auth/blocker interactions;
- one controller generation and safe multi-view behavior;
- checkpoint/cancel/hard-stop authority;
- session/AO binding reconciliation after UI/daemon restart.

### A9 — Memory/context

- project/global memory and owner promotion;
- bounded Scout reports/Context Pack;
- no transcript auto-memory.

### A10 — PLANNED workflow

- Secretary/Scout/Planner/Orchestrator/Workers;
- accepted plan/task DAG/acceptance;
- bounded sessions ending at artifacts;
- restart-safe dependency scheduling.

### A11 — Square MVP

- deterministic validation;
- independent review where eligible;
- finite findings/fix loop;
- final receipt/evidence;
- QUICK and PLANNED usable through one certified route.

### A12 — Client/route plurality

- full CLI, optional VS Code foundation;
- role profiles/Auto/Preferred/Pinned UI;
- additional certified routes where available;
- requested/actual identity and independence policies.

### A13 — Sustained operation

- resource profiles, context/index cache, buffered persistence, evaluation;
- measured performance and honest unavailable telemetry.

### A14 — Windows release

- threat-model closure;
- diagnostics/redaction;
- installer/updater/rollback/uninstall;
- SBOM/notices;
- upstream-sync gate;
- signed/reproducible release artifacts when signing is available.

### A15 — Optional scale

- parallel read-only and writer behavior remains disabled until measured;
- bounded auto-fix and practice evolution remain owner controlled;
- end-to-end benefit exceeds merge/resource/review/recovery cost.

## 4. Cross-cutting acceptance criteria

- UI/session-tab closure never stops or duplicates work.
- Task Manager is deterministic software, not an always-on model.
- known terminal states use no model monitoring.
- only one controller/writer generation can act.
- QUICK does not create irrelevant Planner/Reviewer roles.
- PLANNED packets omit raw transcripts.
- role configured model and actual model are distinct fields.
- pinned route cannot silently fall back.
- reviewer independence cannot silently downgrade.
- vague acceptance cannot dispatch.
- scope/commit/hash changes invalidate prior approval/review.
- semantic event and projection commit atomically.
- daemon restart cannot duplicate event/receipt/session binding/writer.
- completed terminal history remains attached to session.
- project/global memory promotion requires explicit owner transition.
- missing cost/token/resource telemetry is unknown/degraded, not zero.
- official AO and Square installations remain isolated.
- update/cleanup/uninstall never delete user repository work.

## 5. Review discipline

- one task, one reviewable commit normally;
- generated files committed with source;
- failure ends with evidence; fix uses a new task/attempt;
- reviewer reads contract, diff, tests, evidence, and relevant source—not every transcript;
- no task can self-approve a public contract/security/schema change outside its packet;
- integration review checks cross-task boundaries and migrations.

## 6. Release qualification

A release candidate must include:

- exact upstream/downstream source identity;
- clean install/upgrade/rollback/uninstall evidence;
- migration backup/recovery evidence;
- Windows lifecycle matrix;
- QUICK/PLANNED E2E on at least one certified route;
- session UI close/reopen/restart evidence;
- security suite;
- SBOM/license/NOTICE;
- known limitations and unsupported routes/models;
- checksums/signatures where available.
