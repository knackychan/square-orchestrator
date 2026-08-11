# Source templates — placement aids, not patches

These files are deliberately suffixed `.template`. They illustrate the intended Square boundaries but are **not ready-to-apply production source**.

Why they remain templates:

- SA00/SA01 must first prove the exact pinned AO source and Windows lifecycle;
- SA02 freezes names, IDs, reducers, and JSON/OpenAPI contracts;
- migration sequence must be selected from the accepted checkout and migrations are immutable;
- generated sqlc/OpenAPI/TypeScript output must come from AO's tools;
- exact Go package imports, frontend router/query patterns, and store interfaces must match the inspected fork;
- copying them before the proper gate would create unauthorized product code.

Included examples:

```text
backend/internal/domain/square_session.go.template
backend/internal/domain/square_role.go.template
backend/internal/domain/square_routing.go.template
backend/internal/service/squaresession/service.go.template
backend/internal/storage/sqlite/migrations/NNNN_square_session_foundation.sql.template
frontend/src/renderer/features/square/sessions/SquareSessionPage.tsx.template
```

A coding agent may use them as a review checklist only after its task packet names the actual source files, symbols, migration number, tests, and accepted contract hashes.
