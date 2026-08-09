# AO v0.12.1 Source Placement Map for Square

This map is a starting contract, not permission to create every path immediately. Each task must inspect the pinned checkout first.

## Verified upstream areas

```text
backend/cmd/ao/
backend/internal/domain/
backend/internal/ports/
backend/internal/service/
backend/internal/session_manager/
backend/internal/lifecycle/
backend/internal/observe/
backend/internal/storage/sqlite/
  migrations/
  queries/
  gen/
  store/
backend/internal/cdc/
backend/internal/httpd/
backend/internal/terminal/
backend/internal/adapters/
backend/internal/daemon/
backend/internal/config/
frontend/src/api/
frontend/src/main/
frontend/src/renderer/
frontend/src/shared/
frontend/src/styles/
```

## Intended Square additions

### Domain vocabulary

```text
backend/internal/domain/square_session.go
backend/internal/domain/square_workflow.go
backend/internal/domain/square_role.go
backend/internal/domain/square_task.go
backend/internal/domain/square_interaction.go
backend/internal/domain/square_memory.go
backend/internal/domain/square_routing.go
backend/internal/domain/square_event.go
```

Exact splitting is worker discretion if responsibilities remain pure and reviewable.

### Controller-facing services

```text
backend/internal/service/squaresession/
backend/internal/service/squareworkflow/
backend/internal/service/squarerouting/
backend/internal/service/squarememory/
backend/internal/service/squareevaluation/
```

### Deterministic workflow engine

```text
backend/internal/square/taskmanager/
backend/internal/square/compiler/
backend/internal/square/execution/
backend/internal/square/recovery/
```

Do not put these decisions in AO adapters, Electron renderer, CLI, or HTTP controllers.

### Persistence

```text
backend/internal/storage/sqlite/migrations/<new-forward-migrations>.sql
backend/internal/storage/sqlite/queries/square_sessions.sql
backend/internal/storage/sqlite/queries/square_workflows.sql
backend/internal/storage/sqlite/queries/square_roles.sql
backend/internal/storage/sqlite/queries/square_events.sql
backend/internal/storage/sqlite/queries/square_memory.sql
backend/internal/storage/sqlite/store/square_*.go
```

Generated `gen/` output is produced by `npm run sqlc` and never hand-edited.

### HTTP/OpenAPI

Follow actual controller/route registration conventions in the pinned source. Square operations live under `/api/v1/square/*` and call services.

Generated artifacts:

```text
backend/internal/httpd/apispec/openapi.yaml
frontend/src/api/schema.ts
```

Regenerate with `npm run api`.

### CLI

Preferred staged approach:

1. add Square commands under the existing Go CLI while product identity is being integrated;
2. expose a `square` executable/entrypoint without duplicating daemon logic;
3. retain `ao` compatibility only as intentionally documented.

### Frontend

```text
frontend/src/renderer/features/square/
  sessions/
  conversation/
  workflow/
  role-docks/
  attention/
  plan-review/
  history/
  routing/
  memory/
  shared/
```

Reuse generic AO terminal/Chat/diff/session primitives through adapters/components. Keep Square server state in generated API/TanStack Query patterns; use local state only for presentation.

### Documentation/evidence

```text
docs/square/authority/
docs/square/adr/
docs/square/evidence/
docs/square/receipts/
docs/square/upstream/
```

## Forbidden placements

- Square workflow transitions in `frontend/`.
- Direct SQLite calls from HTTP controllers or CLI.
- Direct agent/runtime/worktree adapter use from Square service controllers.
- Product workflow logic inside AO adapter implementations.
- Hand edits to sqlc/OpenAPI generated files.
- New migration logic that modifies old migration files.
- Generic `utils` packages containing policy or domain behavior.
