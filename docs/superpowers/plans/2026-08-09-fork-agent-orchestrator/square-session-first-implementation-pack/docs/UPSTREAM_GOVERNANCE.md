# Downstream Fork and Upstream Governance

- Base repository: `Untrivial-ai/agent-orchestrator`
- Initial tag: `v0.12.1`
- Initial release commit prefix: `1df40e9`
- License: Apache License 2.0

## 1. Remote and branch model

```text
upstream/v0.12.1     frozen source baseline
upstream/main        observed only; never merged directly to production
square/main          stable Square integration branch
feature/SAxx-Tyy-*   one task/commit line
fix/SAxx-*           explicit corrective task
upstream-sync/<tag>  controlled merge/cherry-pick validation branch
release/*            signed Square release preparation
```

Recommended setup:

```powershell
git clone --branch v0.12.1 https://github.com/Untrivial-ai/agent-orchestrator.git square-orchestrator
cd square-orchestrator
git remote rename origin upstream
git switch -c square/main
git tag square-base-v0.12.1
# Add your own origin only after creating the destination repository.
```

Do not push automatically from bootstrap scripts.

## 2. Baseline rule

Before any Square edit:

- record full commit SHA and tag signature status;
- capture toolchain versions;
- run backend build/tests/race/lint;
- run frontend install/typecheck/unit/E2E/package where the machine supports them;
- record known failures with raw logs;
- start unmodified desktop and exercise one harmless AO session on Windows;
- snapshot process/data/worktree behavior;
- create a baseline receipt.

A pre-existing failure is not silently fixed in **SA00-T02 — Capture the unchanged Windows build/test/package baseline**. It is recorded and dispatched as a separate adoption/hardening task.

## 3. Namespacing

Prefer additive, easily searchable Square ownership:

```text
Database:  square_*
API:       /api/v1/square/*
Go:        service/squaresession, service/squareworkflow, square/*
Frontend:  renderer/features/square/*
Tests:     Square*/square_*
Docs:      docs/square/*
Env/data:  SQUARE_* / SquareOrchestrator paths
```

Do not rename broad AO internals merely for branding before functionality requires it. Keep upstream diffs reviewable.

## 4. Identity isolation

Before distributing or regular use beside official AO, isolate:

- app/product/executable names;
- Electron app ID and protocol handlers;
- Windows installer identity;
- user data and cache directories;
- daemon run file/PID/port discovery;
- database/artifact/worktree roots;
- telemetry project/key and default state;
- updater feed/channel;
- log/diagnostics paths;
- notification identity;
- URL schemes/deep links;
- Git worktree naming prefix where collisions are possible.

Square must not update itself from official AO releases or discover/kill an official AO daemon.

## 5. License and attribution

- retain Apache-2.0 license text;
- inspect and retain any NOTICE file/attributions required by upstream and dependencies;
- mark modified files or maintain a clear downstream changes/notice document;
- do not use upstream trademarks to imply official affiliation;
- generate SBOM and third-party notices for releases;
- review new Square dependencies before admission.

## 6. Upstream intake process

For each candidate upstream stable release:

1. create `upstream-sync/<tag>` from current `square/main`;
2. fetch/tag-verify upstream;
3. produce changelog and source-boundary diff;
4. identify migration/API/adapter/runtime/frontend/updater/security changes;
5. merge or selectively cherry-pick with upstream commits preserved;
6. resolve conflicts without rewriting applied migrations/generated sources manually;
7. regenerate sqlc/OpenAPI/TypeScript artifacts;
8. run adoption, lifecycle, migration, workflow, UI, CLI, security, packaging suites;
9. run at least one certified real route in disposable repositories;
10. record accepted/rejected/deferred changes in an upstream ledger;
11. merge only after the upstream gate passes.

## 7. Rules for implementation agents

- refer to plan tasks/milestones by code plus short name (`SA00-T01 — Create and pin the downstream fork`); the canonical registry is `plans/TASK_INDEX.md`;
- inspect actual pinned source before assuming a path/symbol;
- do not port old TypeScript/legacy AO behavior over the Go rewrite;
- follow backend ownership docs: domain vocabulary, services/use cases, session manager commands, lifecycle facts, adapters as leaves, HTTP/CLI thin;
- never hand-edit sqlc or generated OpenAPI TypeScript;
- never edit an applied migration;
- no drive-by reformat/rename;
- do not combine upstream sync and Square feature work in one task;
- preserve baseline and failed evidence;
- STOP when a task requires unapproved architecture, schema, security, or cross-task change.

## 8. Release line

Square versions are independent of AO versions. Record both:

```text
Square version: 0.x.y
AO base: v0.12.1 + accepted upstream commits/tags
Square schema version
Square API version
```

A Square release manifest lists the exact upstream base and downstream commit.
