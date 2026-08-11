# Square API, Events, and AO Execution Facade

- Status: contract draft for SA04/SA05/SA07+
- Principle: HTTP/CLI/Electron translate protocols; Square services own behavior; AO session manager owns concrete session/worktree/runtime mutations

## 1. Package placement

Expected backend areas, following AO conventions:

```text
backend/internal/domain/                  Square shared vocabulary
backend/internal/service/squaresession/   controller-facing session use cases/read models
backend/internal/service/squareworkflow/  workflow commands and read models
backend/internal/service/squarerouting/   role profiles/routes/certification
backend/internal/service/squarememory/    memory use cases
backend/internal/square/taskmanager/      deterministic workflow engine
backend/internal/square/compiler/         Task Brief/Preview/Plan/Packet/Review compilers
backend/internal/square/execution/        AO execution facade and reconciliation
backend/internal/storage/sqlite/...       migrations, queries, stores
backend/internal/httpd/...                thin Square controllers and OpenAPI operations
backend/internal/cli/...                  thin `square` commands over HTTP
backend/internal/daemon/...               dependency wiring only
```

Exact paths must be reconciled with the pinned checkout before each task. Do not create generic `manager` or `utils` dumping grounds.

## 2. API namespace

Use additive versioned routes:

```text
/api/v1/square/sessions
/api/v1/square/sessions/{sessionId}
/api/v1/square/sessions/{sessionId}/messages
/api/v1/square/sessions/{sessionId}/workflow-runs
/api/v1/square/sessions/{sessionId}/start
/api/v1/square/sessions/{sessionId}/pause
/api/v1/square/sessions/{sessionId}/resume
/api/v1/square/sessions/{sessionId}/cancel
/api/v1/square/sessions/{sessionId}/history
/api/v1/square/sessions/{sessionId}/layout
/api/v1/square/workflow-runs/{runId}
/api/v1/square/workflow-runs/{runId}/preview
/api/v1/square/workflow-runs/{runId}/approve
/api/v1/square/workflow-runs/{runId}/plan
/api/v1/square/workflow-runs/{runId}/tasks
/api/v1/square/role-runs/{roleRunId}
/api/v1/square/role-runs/{roleRunId}/cancel
/api/v1/square/role-runs/{roleRunId}/checkpoint
/api/v1/square/role-runs/{roleRunId}/route
/api/v1/square/interactions
/api/v1/square/interactions/{interactionId}/respond
/api/v1/square/role-profiles
/api/v1/square/routes
/api/v1/square/routes/{routeId}/probe
/api/v1/square/routes/{routeId}/certify
/api/v1/square/memory
/api/v1/square/memory/{memoryId}/promote
/api/v1/square/resources
/api/v1/square/evaluations
```

## 3. Command contract

Every mutation accepts or receives:

- schema version;
- idempotency key;
- correlation ID;
- expected resource version/generation where relevant;
- actor/client identity;
- exact command payload.

Response envelope contains:

- result or typed error;
- resource version;
- emitted event sequence/reference;
- correlation ID;
- pending interaction/gate where applicable.

Typed error families:

```text
validation
not_found
conflict
stale_version
stale_generation
policy_denied
interaction_required
approval_required
auth_required
route_unavailable
route_unverified
resource_busy
unsupported_version
daemon_unavailable
timeout
reconciliation_required
internal
```

## 4. Read models

### Session list item

Optimized for the left session navigator:

- session ID/title/project;
- session status;
- active workflow stage/profile;
- attention type/count/summary;
- active role count;
- latest meaningful message/activity;
- completion/result summary;
- updated time;
- open/archived state.

### Session detail

- session/conversation;
- active and prior workflow runs;
- role hierarchy and role-run summaries;
- direct attention item;
- plan/review/history references;
- terminal/Chat docks and retained history;
- route assignments;
- available authorized actions.

### Role dock read model

- role/title/task;
- requested/resolved/actual route;
- state/elapsed/last activity;
- permission/writer state;
- AO session/worktree/branch/PR/controller;
- terminal/Chat/diff/evidence availability;
- blocker/interaction;
- progress/artifact summary;
- available actions.

## 5. Event delivery

Reuse AO's shared SSE stream and `Last-Event-ID` behavior.

Square CDC event names/resources are versioned. The client reducer must handle:

- duplicate/out-of-order invalidation events;
- reconnect/replay;
- explicit replay truncation;
- unknown resource/version compatibility placeholders;
- fetch failure and stale read-model indication.

Terminal bytes remain on AO's WebSocket mux. Product state is not inferred solely from terminal text.

## 6. AO execution facade

Square services must not call adapters or manipulate worktrees/runtimes directly. A narrow facade wraps AO session-manager capabilities.

Suggested interface shape:

```go
type ExecutionFacade interface {
    Reserve(ctx context.Context, req ReserveRequest) (Reservation, error)
    Start(ctx context.Context, req StartRoleRunRequest) (ExecutionBinding, error)
    Send(ctx context.Context, req SendInputRequest) error
    Checkpoint(ctx context.Context, req CheckpointRequest) (HandoverRef, error)
    Cancel(ctx context.Context, req CancelRequest) error
    HardStop(ctx context.Context, req HardStopRequest) error
    Observe(ctx context.Context, bindingID BindingID) (ExecutionObservation, error)
    Release(ctx context.Context, req ReleaseRequest) error
}
```

The actual implementation delegates to AO service/session and session_manager APIs. It does not duplicate session creation logic.

## 7. Start sequence

1. Task Manager accepts dispatch under idempotency key.
2. Persist role assignment and `STARTING` event.
3. Acquire writer/resource reservations.
4. Execution facade requests AO workspace/session/runtime.
5. Persist AO binding and generation immediately when identifiers exist.
6. Observe actual executable/adapter/model identity.
7. Verify route/permissions against assignment.
8. Mark role `RUNNING` or create typed failure/gate.
9. Subscribe/reconcile through durable facts; no model monitor.

Compensation does not erase accepted history. Partial resource creation is recorded and safely cleaned/quarantined.

## 8. Completion sequence

Role completion requires agreement between applicable facts:

- AO process/session state;
- completion artifact/receipt;
- worktree/Git state;
- required validation artifacts;
- child/process cleanup;
- writer fence/generation;
- expected output contract.

A process exit alone is not success. A receipt alone is not success if the writer/process/worktree state conflicts.

## 9. Session conversation behavior

`POST messages` may:

- add a normal user note/question;
- answer an active interaction through a typed link;
- request status/explanation;
- amend scope;
- request another workflow run after completion;
- propose cancel/pause/route change.

The Task Manager classifies messages. Scope-changing messages never inject arbitrary text into a worker terminal without an explicit command and authority path.

## 10. CLI mapping

Initial command examples:

```text
square session new --project <id> --message "..."
square session list
square session show <id>
square session send <id> "..."
square session start <id>
square session pause|resume|cancel <id>
square session history <id>
square role list --session <id>
square role show <role-run-id>
square role route set <role> --session <id> --mode pinned --route <id>
square interaction answer|approve-once|deny <id>
square route list|probe|certify
square memory list|promote|reject
square events watch --session <id>
```

JSON mode is stable/versioned; diagnostics go to stderr; noninteractive mode never waits for UI input.

## 11. OpenAPI/type generation

Go DTOs and operation registration are authoritative for HTTP. After API changes:

```text
npm run api
```

Commit together:

- Go controller/service DTO sources;
- generated `backend/internal/httpd/apispec/openapi.yaml`;
- generated `frontend/src/api/schema.ts`;
- golden contract fixtures.

Generated files are not hand-edited.

## 12. Required tests

- controller calls service only;
- facade calls AO session command engine, not adapter internals;
- idempotent mutations and stale version/generation;
- session list/detail/dock read models;
- SSE replay, duplicate/out-of-order changes, truncation;
- start compensation at every resource boundary;
- actual route mismatch;
- process-exit/receipt/worktree conflict matrix;
- conversation scope-change behavior;
- CLI/HTTP/frontend generated type parity;
- Electron renderer cannot access arbitrary daemon route or shell command through terminal output.
