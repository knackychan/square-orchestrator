# SA00-FIX02 — Telemetry, crash reporting, and updater hard-off

- Status: implementation complete; awaiting independent A0 review
- Owner authorization: `authorize everything`, 2026-08-11
- Candidate implementation commit: `9af0955471022ff5256f48b497f0631e3a01c26d`
- Worktree: isolated `square/sa00-fix02-03`

## Result

Square no longer carries the upstream AO PostHog project key or host. Renderer
telemetry bootstrap is permanently withheld until a separate owner-approved
opt-in policy exists; daemon telemetry is hard-off and ignores inherited AO or
Square telemetry variables. No crash reporter was enabled.

The updater and feature-build release surfaces are replaced by disabled facades.
No Electron updater dependency, feed metadata, updater cache identity, release
feed read, scheduled check, download, or install path remains reachable from
Square startup or IPC. Legacy upstream feed tests are retained but skipped and
new fail-closed safety tests cover the disabled contract.

## Verification

- `npm run typecheck` — PASS
- `npx vitest run src/main/square-safety.test.ts src/main/feature-builds.test.ts src/shared/telemetry.test.ts --config vite.renderer.config.ts` — PASS (14 executed, 20 legacy feed tests skipped)
- `npx vitest run src/main/auto-updater.test.ts --config vite.renderer.config.ts` — skipped by explicit Square policy
- `go build ./...` — PASS
- `go test ./internal/config ./internal/daemonmeta ./internal/httpd ./internal/browserruntime` — PASS

The frontend lockfile was regenerated after removing `electron-updater`; the
pre-existing npm 11 optional dependency drift (`encoding`/`iconv-lite`) is now
classified in the candidate rather than silently discarded.
