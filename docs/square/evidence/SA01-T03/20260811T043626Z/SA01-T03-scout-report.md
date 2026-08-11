# SA01-T03 — Windows ConPTY, input/output/resize/cancel, and descendant cleanup

Status: planning-only scout; implementation not dispatched.

Reviewed source: `6972e30e21103227941863a42bb2c62760793077` (`square/main`)

Pinned AO baseline: `v0.12.1`, commit `1df40e93772c2c48e916870d9c3ddf8f29a69f84`.

A0 status: `BLOCKED` per `docs/square/receipts/SA00-T05.json`.

## Findings

- The inspected Windows runtime paths remain byte-identical to pinned AO.
- Windows uses AO's detached pty-host plus `github.com/aymanbagabas/go-pty` v0.2.3 over direct Win32 ConPTY.
- Input, output, and resize have explicit terminal paths. Viewer/WebSocket closure closes only its attachment and does not destroy the runtime.
- `Runtime.Interrupt` sends Ctrl-C, but `Session Manager.Kill` calls `Runtime.Destroy` directly; graceful interrupt is currently skipped by the kill path.
- No Windows Job Object or process-tree enumeration exists. Cleanup targets the direct ConPTY child and pty-host PID only, so descendant containment is not guaranteed.
- Registry recovery records PID but not process creation identity, leaving PID-reuse risk before force kill.
- No new dependency is needed: `golang.org/x/sys/windows` v0.44.0 already exposes Job Object and process-identity APIs.
- Relevant licenses are clear: AO Apache-2.0; go-pty MIT; x/sys and x/crypto BSD-3-Clause; coder/websocket ISC; xterm packages MIT.
- Targeted ConPTY, ptyexec, terminal, and HTTP tests pass. The full Windows backend suite retains previously recorded unrelated SA00 baseline failures.
- No files were edited, committed, or dispatched by this scout.

## Recommended post-A0 implementation scope

1. Start from a clean isolated branch/worktree from the accepted A0 tip.
2. Add a standalone deterministic Go fixture with `normal_exit`, `unicode`, `ansi`, `burst`, `quiet_active`, `stdin_question`, `resize`, `crash`, `graceful_cancel`, `ignore_cancel`, `nested_children`, and `long_running_for_viewer_close` modes.
3. Add real Windows integration coverage for discrete argv/env/cwd, spaces and Unicode, parent stdout/stderr isolation, input, resize, crash classification, viewer detach, graceful cancel, hard stop, descendants, and startup cleanup.
4. Create one unnamed Job Object per pty-host, assign the host before launching the ConPTY workload, and use `JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE` so children inherit containment.
5. Fence PID operations with Windows process creation time.
6. Attempt Ctrl-C and bounded natural exit before separately classified authorized hard stop.
7. Prove ConPTY close and slow-client paths cannot block output draining.
8. Run sequential and concurrency resource matrices, recording exact process, handle, goroutine, and thread plateaus without arbitrary thresholds.
9. Emit ADR-SA01-003, the required evidence bundle, manifest, and receipt.

## Non-overlap boundaries

SA01-T01 owns Electron/window/daemon lifetime and desktop reopen E2E; it should consume this task's long-running fixture.

SA01-T05 owns worktree safety and cleanup classification; it should consume this task's definitive process-death facts.

SA01-T03 must not change Square domain/API/migrations/UI, Electron lifecycle, worktree cleanup, or ports without a STOP and owner-approved scope expansion.

## Active and mandatory STOP conditions

Active STOP conditions:

- A0 has not passed.
- The current shared checkout is dirty; implementation requires a clean isolated worktree.

Stop implementation for failed Job assignment, escaped descendants, ambiguous PID identity, blocked terminal close, parent-output leakage, linear resource growth, natural cases requiring hard cleanup, scope expansion, unclear dependency security/license, or tests requiring credentials, valuable repositories, elevation, or shell-string execution.

## Dispatch decision

Do not dispatch SA01-T03 implementation. After A0 passes, dispatch fixture-first evidence and runtime repair only within the task packet's terminal/runtime scope.
