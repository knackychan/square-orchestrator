# SA00-FIX03 — Square/AO identity and state isolation

- Status: implementation complete; awaiting independent A0 review
- Owner authorization: `authorize everything`, 2026-08-11
- Candidate implementation commit: `9af0955471022ff5256f48b497f0631e3a01c26d`
- Worktree: isolated `square/sa00-fix02-03`

## Result

Square uses product identity `Square Orchestrator`, app ID
`dev.square-orchestrator.desktop`, the `square` CLI/executable, service name
`square-orchestrator-daemon`, and independent `SQUARE_*` supervisor/runtime
variables. Packaged defaults are loopback `127.0.0.1:3101`; development uses
`127.0.0.1:3102`. State, Electron user data, logs, run files, named pipes,
PTY registry, and daemon launch roots resolve under `~/.square` and
`~/.square/dev`.

The official AO defaults remain separate at their prior identities. No AO
compatibility command alias was added. LAN behavior was not enabled or
expanded; reserved port 3111 remains outside this fix and requires the later
authenticated listener work.

## Verification

- `go build ./...` — PASS
- focused Go config/daemon-meta/HTTP/browser tests — PASS
- focused frontend identity/telemetry/updater tests — PASS (86 tests)
- `npm run typecheck` — PASS
- Windows supervisor package test on the host — PASS

This fix does not redesign daemon identity, stale endpoint takeover, PID
fencing, or safe-stop semantics; those remain SA01-T02/combined-amendment
concerns as required by the SA01 planning boundary.
