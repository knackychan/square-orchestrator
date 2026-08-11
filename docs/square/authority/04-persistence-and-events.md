# Persistence, Semantic Events, Artifacts, and Recovery

- Status: contract draft for SA03
- Base: AO SQLite connection, goose migrations, sqlc queries, `change_log` CDC

## 1. Storage responsibilities

AO storage continues to own AO project/session/runtime/worktree/PR/review facts. Square adds namespaced workflow/session resources without redefining AO tables.

Square persistence must support:

- durable session conversation;
- workflow/role/task state;
- requested/resolved/actual route identity;
- interactions and owner decisions;
- semantic audit events;
- idempotency and generation-fenced leases/bindings;
- plans, acceptance, findings, receipts, memory, usage, exposure, and evaluation;
- restart reconstruction;
- content-addressed artifact references.

## 2. Migration rules

- Add new forward-only goose migration files under the existing AO migration directory.
- Use a clearly reserved migration-number range only after inspecting the pinned source and agreeing an upstream-collision policy.
- Never edit an applied AO or Square migration.
- Migration source hashes are recorded in `square_schema_registry` or an equivalent migration-audit table.
- Backup/snapshot policy is tested before the first Square migration is shipped.
- Unknown newer schema or inconsistent Square registry fails explicitly.
- sqlc-generated files are regenerated, never hand-edited.

## 3. Logical table set

The exact columns belong to SA03 dispatch packets, but the first stable schema must cover:

```text
square_schema_registry
square_sessions
square_session_messages
square_workflow_runs
square_role_profiles
square_role_assignments
square_role_runs
square_ao_bindings
square_task_briefs
square_dispatch_previews
square_plans
square_tasks
square_task_dependencies
square_acceptance_criteria
square_context_jobs
square_context_reports
square_context_packs
square_execution_packets
square_interactions
square_decisions
square_review_packets
square_findings
square_fix_links
square_receipts
square_evidence_refs
square_events
square_idempotency_keys
square_writer_leases
square_session_layouts
square_memory_entries
square_memory_candidates
square_route_certifications
square_route_decisions
square_usage
square_exposure
square_resource_profiles
square_outcome_evaluations
```

## 4. Key relational constraints

- session belongs to one AO/Square project identity;
- workflow run belongs to one session;
- role assignment/run belongs to one workflow run and optional task;
- one current active AO binding per role run attempt;
- one current writer lease per worktree target;
- task dependency graph cannot self-reference;
- acceptance criterion IDs unique within plan/workflow version;
- receipt nonce unique and conflict-detecting;
- route decision immutable once role run starts;
- event global sequence monotonically increasing;
- stream version unique per aggregate;
- message revision/hash immutable;
- artifact bodies never stored as unbounded SQLite BLOBs.

## 5. Semantic event ledger

`change_log` is an implementation-level CDC stream. `square_events` records business meaning.

Required event fields:

- global sequence;
- event ID and schema version;
- aggregate type/ID;
- aggregate version;
- event type/version;
- occurred/recorded time;
- actor kind/reference;
- correlation/causation IDs;
- command/idempotency key;
- compact payload or content-addressed payload reference;
- policy/route/contract hashes where relevant.

Events are append-only. Update/delete is denied by store policy and, where practical, triggers/tests.

Examples:

```text
square.session.created
square.session.message_added
square.workflow.started
square.workflow.profile_resolved
square.role.route_selected
square.role.started
square.role.actual_identity_observed
square.interaction.created
square.decision.recorded
square.task.completed
square.review.finding_recorded
square.workflow.succeeded
square.memory.promotion_proposed
```

## 6. Atomicity

For every accepted mutation:

```text
validate idempotency/generation/policy
begin transaction
  append semantic event
  update current projection(s)
  update binding/lease/idempotency result as required
commit
then launch or signal external work
```

External process launch is never inside the database transaction. If launch fails, record a new failure event/projection; do not erase the accepted command.

Tests must inject failure:

- before event;
- after event before projection;
- after projection before commit;
- after commit before external launch;
- after launch before AO binding persisted;
- after receipt spool before ingestion;
- after artifact move before metadata reference.

## 7. Idempotency

Each mutating command has:

- scope/operation;
- idempotency key;
- canonical command hash;
- actor/client/correlation;
- state: `IN_PROGRESS`, `SUCCEEDED`, `FAILED_TERMINAL`;
- original result/error artifact;
- creation/expiry/retention.

Exact duplicate returns original result. Conflicting hash under same key returns typed conflict. A crash during `IN_PROGRESS` enters reconciliation, not blind replay.

## 8. Writer leases and fencing

Writer lease fields:

- protected project/repository/worktree identity;
- holder task/attempt/role run;
- generation/fencing token;
- acquired/renewed/expires;
- state/release reason.

A stale generation cannot:

- mark task/role success;
- apply receipt;
- change binding;
- publish an accepted result;
- delete/cleanup the worktree.

AO worktree lifecycle remains authoritative for actual workspace creation, but Square writer authority is a separate durable contract.

## 9. AO bindings

Binding persistence must permit restart classification:

```text
BOUND_AND_ALIVE
BOUND_BUT_TERMINAL
AO_SESSION_MISSING
WORKTREE_MISSING
RUNTIME_HANDLE_STALE
CONTROLLER_GENERATION_STALE
AMBIGUOUS
```

Square never silently creates a replacement process when classification is ambiguous. It records a blocker/gate or creates an explicit new role attempt.

## 10. Artifact store

Recommended Square-specific data root, isolated from official AO:

```text
%LOCALAPPDATA%\SquareOrchestrator\
  data\
  artifacts\sha256\ab\<hash>
  spool\
  terminal\
  cache\
  logs\
  electron\
```

Actual paths must follow the fork's Windows path conventions and portable tests.

Write protocol:

1. create unique spool file;
2. stream bytes and compute SHA-256/length;
3. flush according to artifact criticality;
4. close;
5. atomically move/deduplicate to hash path;
6. transactionally add metadata/reference;
7. reconcile orphan spool/hash files on startup.

Artifact media types include:

- Task Brief, Preview, Plan, Task/Execution/Review packets;
- Context Report/Pack;
- validation output;
- diff/patch snapshot;
- receipt/evidence manifest;
- handover;
- bounded terminal/Chat chunks;
- screenshots/browser evidence.

## 11. Terminal and conversation history

- raw terminal streams remain outside SQLite;
- SQLite stores sequence ranges, chunk hashes, timestamps, role/AO binding, truncation/retention facts;
- hidden/closed UI panes do not affect stream retention;
- session conversation is a product record and remains queryable after provider transcript retention;
- provider transcripts may be retained under explicit policy but are not the canonical Square conversation or memory.

## 12. CDC and read models

Square table mutations should enter AO's CDC pipeline through the established trigger pattern.

SSE events carry lightweight invalidation/replay information:

- change sequence;
- resource type/ID;
- operation;
- schema version;
- correlation.

Clients fetch authoritative read models. Do not place large artifacts or full terminal output in CDC.

## 13. Recovery sequence

On daemon start:

1. acquire single-instance/data-directory ownership;
2. validate database/migration state;
3. reconcile spool and artifact store;
4. load nonterminal Square workflows/role runs/interactions/leases/bindings;
5. query AO session/runtime/worktree facts;
6. classify each binding;
7. apply exact once receipts/artifacts;
8. renew/release/fence leases;
9. produce recovery events and owner gates;
10. resume deterministic scheduling only for unambiguous states.

Never silently rerun a role because its process is absent.

## 14. Required tests

- creation and every migration path;
- AO baseline database upgraded with Square tables;
- existing migration immutability/collision detection;
- unsupported-newer/inconsistent registry refusal;
- event/projection atomicity;
- monotonic event and stream versions;
- append-only enforcement;
- idempotent duplicate and conflict;
- lease/fence concurrency;
- binding generation/reconciliation;
- artifact crash points/hash mismatch/dedup/orphan recovery;
- session/message/history round trip;
- database restart with active AO session fixture;
- CDC replay and client read-model refresh;
- corruption/backup/restore fixtures.
