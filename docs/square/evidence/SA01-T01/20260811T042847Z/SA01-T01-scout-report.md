# SA01-T01 — Desktop-Lifetime Detachment and Reconnect Scout Report

Status: planning-only reconnaissance; no product implementation dispatched.

Date: 2026-08-11

Reviewed source: `6972e30e21103227941863a42bb2c62760793077` (`square/main`)

A0 status: `BLOCKED`, per `docs/square/receipts/SA00-T05.json`. This report is not an A0 acceptance or task-completion receipt.

## Inputs

- `docs/superpowers/plans/2026-08-09-fork-agent-orchestrator/square-session-first-implementation-pack/plans/tasks/SA01-T01.md`
- `docs/square/authority/00-architecture-amendment.md`
- `docs/square/authority/01-master-session-first-plan.md`
- `docs/square/receipts/SA00-T05.json`

The requested authority files are present in the worktree but are not committed at the reviewed source commit. Product source paths were inspected at the reviewed HEAD; no product source was changed.

## Exact source map

### Electron lifecycle and daemon process

- `frontend/src/main.ts`
  - `createWindow`: BrowserWindow creation, renderer URL loading, `render-process-gone`, and `mainWindow.on("closed")` cleanup.
  - menu handlers: `view.reload`, `window.close`, and `app.quit`.
  - `startDaemon`, `startDaemonInner`, `readDaemonProbe`, `inspectExistingDaemon`, `refreshDaemonStatus`, and `reportBoundPort`.
  - `killDaemon`, `stopDaemon`, and `restartDaemon`.
  - `app.on("before-quit")`, `app.on("window-all-closed")`, and `process.on("exit")`.
  - `daemonEnv` sets `AO_OWNER`; `establishSupervisorLink` connects the Electron liveness socket.
- `frontend/src/main/daemon-owner.ts`
  - `keepDaemonAlive`: `AO_KEEP_DAEMON` environment opt-out.
  - `shouldLinkOnAttach`: links only to an `app`-owned daemon.
- `frontend/src/main/supervisor-link.ts`
  - `connectSupervisor`: retrying socket connection, data draining, and `dispose`.
- `frontend/src/main/auto-updater.ts`
  - `quitAndInstallUpdate`: updater-controlled process exit; explicitly out of this slice.

### Daemon startup, readiness, supervision, and termination

- `backend/internal/daemon/daemon.go`
  - `Run`: config/data-root setup, stale run-file ownership probe, SQLite/session/runtime wiring, reconciliation, supervisor setup, and HTTP server lifetime.
  - Daemon shutdown deliberately does not tear down sessions; restart reconciliation adopts durable/live runtime state.
- `backend/internal/daemon/supervisor/supervisor.go`
  - `New`, `Serve`, client tracking, and the five-second last-client grace callback.
- `backend/internal/daemon/supervisor/listen_windows.go`
  - Windows named-pipe address derived from the run-file path.
- `backend/internal/httpd/server.go`
  - `NewWithDeps`, `Run`, run-file write/removal, listening, graceful HTTP shutdown, and `RequestShutdown`.
- `backend/internal/httpd/router.go`
  - `/healthz`, `/readyz`, and loopback-gated `POST /shutdown`.
- `backend/internal/daemon/stale.go`
  - `runFileOwnerServing`.

### Run-file and endpoint discovery

- `backend/internal/runfile/runfile.go`
  - `Info`, `Read`, `Write`, `RemoveIfOwned`, and `CheckStale`.
  - Current fields are PID, port, start time, owner, browser token, and browser address.
- `frontend/src/shared/daemon-discovery.ts`
  - `RunFileInfo`, `parseRunFile`, `defaultRunFilePath`, and listen-port log scanning.
- `frontend/src/shared/daemon-attach.ts`
  - `parseDaemonProbe`, `resolveDaemonFromRunFile`, `resolveDaemonFromPort`, and readiness/identity checks.
- `frontend/src/shared/daemon-launch.ts`
  - `resolveDaemonLaunch` for dev and packaged daemon commands.
- `frontend/src/shared/daemon-takeover.ts`
  - `shouldReplacePortHolder`, used by current wedged-orphan replacement behavior.

### Renderer reconnect and event state

- `frontend/src/renderer/lib/api-client.ts`
  - `setApiBaseUrl`, `subscribeApiBaseUrl`, and `runtimeFetch`.
- `frontend/src/renderer/lib/daemon-status.ts`
  - `applyDaemonStatus` changes the renderer API base only after readiness.
- `frontend/src/renderer/hooks/useDaemonStatus.ts`
  - IPC status polling and lifecycle of event transport.
- `frontend/src/renderer/lib/event-transport.ts`
  - SSE connection, retry, endpoint rebinding, and query invalidation.
- `frontend/src/renderer/lib/terminal-mux.ts`
  - `/mux` WebSocket, ping, close/error state, and pool replacement.
- `frontend/src/renderer/hooks/useTerminalSession.ts`
  - terminal attach, detach, replay, and scheduled reattach after WebSocket closure.

### Session, controller, and runtime persistence

- `backend/internal/domain/session.go`
  - durable `SessionRecord`; derived `Session` read model.
- `backend/internal/session_manager/manager.go`
  - `Spawn`, `Kill`, `RestoreWithMode`, `ResumeAgentWithMode`, `Reconcile`, `RestoreAll`, `Send`, and `SaveAndTeardownAll`.
- `backend/internal/service/session/service.go`
  - session reads, status derivation, spawn/restore/resume/kill delegation, and durable writes.
- `backend/internal/httpd/controllers/sessions.go`
  - HTTP request façade; it is not the process owner.
- `backend/internal/storage/sqlite/store/session_store.go`
  - session create/update/get/list and runtime/agent/workspace identity persistence.
- `backend/internal/storage/sqlite/queries/sessions.sql`
  - session persistence queries; no derived status column.
- `backend/internal/adapters/runtime/conpty/runtime.go`
  - runtime host create/attach/destroy and `IsAlive` behavior.
- `backend/internal/daemon/lifecycle_wiring.go`
  - runtime messenger lookup through persisted runtime handle IDs.

## Current process ownership behavior

The current topology is:

```text
Electron main
  ├─ BrowserWindow / renderer
  ├─ detached AO daemon
  │    ├─ SQLite and session manager
  │    ├─ terminal manager
  │    └─ ConPTY/runtime and agent processes
  └─ supervisor socket to daemon
```

The current close path is:

1. Electron launches the daemon with `AO_OWNER=app` and connects the supervisor socket.
2. Closing the last window calls `app.quit()` on Windows/Linux.
3. Electron exits and the supervisor socket reaches EOF.
4. The backend supervisor waits five seconds, then calls `RequestShutdown`.
5. The daemon exits gracefully and removes its run-file.
6. Durable session/runtime records remain available for later reconciliation.

Thus, sessions and runtime handles are durable, but the daemon process is currently coupled to desktop lifetime. This fails the SA01-T01 requirement that closing the desktop release only UI ownership.

Reload and renderer crash do not normally close the Electron process or supervisor socket. Renderer SSE and terminal WebSocket clients are detached and then reconnect. Terminal attachment close is not runtime destruction.

Explicit `daemon:stop` currently calls `killDaemon`, which terminates the daemon process group. The existing `/shutdown` route is not used as a safe, fact-based stop operation, and no typed safe-idle status currently exposes active session/controller/interaction ownership.

`AO_KEEP_DAEMON` and the five-second supervisor grace are current implementation details, not a proposed solution. This plan proposes neither an arbitrary keep-alive timer nor process-name matching/killing.

## Proposed non-overlapping file scope

### SA01-T01-owned paths after A0

- `frontend/src/main.ts`: desktop lifecycle separation and renderer-only cleanup.
- `frontend/src/main/supervisor-link.ts`: only if the Electron socket must stop representing desktop lifetime.
- Renderer reconnect files, only where tests demonstrate a gap:
  - `frontend/src/renderer/lib/daemon-status.ts`
  - `frontend/src/renderer/lib/event-transport.ts`
  - `frontend/src/renderer/hooks/useDaemonStatus.ts`
  - `frontend/src/renderer/lib/terminal-mux.ts`
  - `frontend/src/renderer/hooks/useTerminalSession.ts`
- Matching frontend unit tests and real Electron lifecycle evidence.

### Excluded from SA01-T01

The following belong to SA01-T02 or require a separately accepted combined amendment:

- `frontend/src/shared/daemon-attach.ts`
- `frontend/src/shared/daemon-discovery.ts`
- `frontend/src/shared/daemon-takeover.ts`
- `frontend/src/main/daemon-owner.ts`
- backend run-file, identity, stale-owner, takeover, and daemon ownership changes
- migrations, storage schema, session-manager semantics, ConPTY/runtime behavior
- updater, telemetry, worktree cleanup, and UI redesign

The safe-stop requirement cannot be implemented safely in frontend-only scope because the current daemon API has no authoritative safe-idle query. That contract must be supplied by T02 or approved through a combined amendment.

## Test and evidence matrix

| Boundary | Existing coverage | Required SA01 evidence |
|---|---|---|
| Window close/quit | `frontend/e2e/smoke-t0.spec.ts` uses a fake bridge; no real main lifecycle test | Real Windows Electron fixture proving daemon/session/runtime identities remain alive after close and quit |
| Reload/crash | Fake renderer reload coverage | Separate real reload and renderer-crash cases |
| Existing daemon reopen | `frontend/src/shared/daemon-attach.test.ts`, `daemon-discovery.test.ts` | Same compatible endpoint; no duplicate daemon/session/process/worktree |
| Ambiguous/stale endpoint | `frontend/src/shared/daemon-takeover.test.ts` covers current behavior | T02-owned proof that ambiguous holders are never killed |
| Renderer SSE reconnect | `event-transport.test.ts`, `useDaemonStatus.test.tsx` | Reconnect and authoritative query invalidation after close/reopen/readiness transition |
| Terminal reconnect | `terminal-mux.test.ts`, `useTerminalSession.test.tsx`, backend terminal tests | WS detach/reattach preserves runtime/session identity; silence and probe errors do not terminate work |
| Session persistence | session-manager, service, and SQLite tests cover reconciliation/mappings | E2E identity comparison across close/reopen |
| Safe daemon stop | No safe-idle status test; current stop is process-group termination | Typed safe-stop operation, refusal while live work exists, safe cleanup after idle |
| Startup/stdio | Existing readiness and real-app boot tests | Readiness, drained/redirected stdout/stderr, and no orphan pipes |

Required evidence artifact names from the task packet:

`environment.json`, `before-process-topology.json`, `lifecycle-cases.ndjson`, `identity-comparison.json`, `stdout-stderr-drain.json`, `tests.json`, `summary.json`, and `manifest.sha256`.

`test/e2e-pod/real-app.spec.ts` is the closest existing real Electron harness. It currently proves boot/readiness/run-file ownership only; it does not prove close/reopen continuity.

## SA01-T02 overlap risks

1. `runfile.Info.Owner` and frontend `AO_OWNER` are currently ownership signals, but T02 requires product, protocol, user, data-root, and instance-generation identity.
2. `daemon-attach.ts` accepts PID/path/readiness evidence that is insufficient for safe reuse.
3. `daemon-takeover.ts` and the `main.ts` orphan-replacement path can terminate a process group based on ambiguous evidence.
4. `/healthz` and `/readyz` do not expose instance generation, protocol compatibility, or safe-stop counts.
5. Simultaneous reopen/spawn deduplication is an ownership/identity concern, not a renderer concern.
6. `AO_KEEP_DAEMON` and supervisor-link semantics encode the old app-vs-persistent owner model and must not be independently redefined by T01.

Recommendation: freeze the T02 identity/run-file contract before dispatching lifecycle implementation, or approve a combined amendment assigning the safe-stop contract. T01 may only consume that contract; it must not recreate identity or takeover logic.

## STOP conditions

Stop if:

- A0 remains blocked.
- Identity compatibility requires PID-only, process-name, or arbitrary kill heuristics.
- The change modifies T02-owned discovery, takeover, run-file, or daemon identity behavior.
- Safe shutdown requires an arbitrary timer or unconditional process-group kill.
- The real fixture cannot prove the same daemon, session, runtime process, and worktree after reopen.
- Renderer close/reload requires recreating a durable session.
- Required evidence falls outside the approved path scope without owner approval.
- Updater, OS-service/elevation, credential cleanup, or worktree cleanup becomes necessary.
- Detached child stdout/stderr cannot be safely drained or redirected.

## Post-A0 dispatch brief

Before dispatching implementation, record:

- accepted A0 receipt and authority hashes;
- clean baseline and exact implementation commit;
- accepted T02 identity/readiness contract, including instance generation and compatible endpoint rules;
- explicit ownership of the safe-status and safe-stop API;
- approved T01 path list and real-Electron test harness location.

Then dispatch SA01-T01 as a frontend lifecycle/reconnect slice with real Windows evidence. Keep daemon identity, run-file takeover, stale/foreign classification, and process termination ownership in SA01-T02.
