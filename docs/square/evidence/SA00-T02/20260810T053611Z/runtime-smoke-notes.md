# Runtime smoke notes — SA00-T02 baseline (2026-08-10)

## Package launch
- Packaged Windows Win32-x64 app (`npm run package`, `out/Agent Orchestrator-win32-x64/agent-orchestrator.exe`) **starts on this machine**:
  alive 20s, unbundled Electron + bundled daemon (`resources/daemon/ao.exe`) bound `127.0.0.1:3001`, sandbox data + run-file honored, clean teardown, 0 survivors.
  Upstream documented anonymous PostHog telemetry may be transmitted by the renderer during launch (README telemetry section; not disabled at build time). Short bounded window.

## daemon/CLI
- Documented developer flow reproduced with the hidden `daemon` subcommand (root.go:318) and AO_PORT/AO_DATA_DIR/AO_RUN_FILE isolation.
- Exactly one daemon instance (second start exit 1), harmless status/list operations (`status`, `doctor`, `project list`) exit 0, documented `stop` shuts down cleanly, no survivors. See daemon-cli-smoke.log.

## agent/session smoke — UNAVAILABLE
- Packet 6.8: run one minimal supported agent/session only when credentials are already available and owner-authorized.
- No provider route is usable in this environment and no credential may be probed or exported; the owner authorization for external/provider calls is "no".
- Recorded **UNAVAILABLE**; not counted as a failure of the baseline.

## E2E — ENVIRONMENT_BLOCKED
- All three `test:e2e` attempts are recorded in logs. Final blocker: an unrelated project's vite dev server (project "SAINT JACQUES") holds `::1:5173`; Playwright reuses it (non-CI) or refuses to start on `localhost:5173` (CI). Stopping another project's process is outside SA00-T02 authority.
