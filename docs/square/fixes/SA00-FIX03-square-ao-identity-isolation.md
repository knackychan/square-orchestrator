# SA00-FIX03 — Square/AO identity and state isolation

- Status: owner-authorized
- Purpose: prevent official AO and Square from sharing state, process identity, endpoint identity, or release identity.
- Owner authorization: `authorize everything`, 2026-08-11
- Prerequisite: SA00-FIX01 committed candidate; coordinate with SA00-FIX02 where configuration surfaces overlap

## Scope

Implement the accepted SA00-T03 identity values only:

- Square Orchestrator product/app identity and `dev.square-orchestrator.desktop`.
- Separate `square` CLI command; no `ao` compatibility alias.
- Square data/config/cache/log/worktree roots under `~/.square` and dev roots under `~/.square/dev`.
- `SQUARE_*` environment namespace for Square-owned configuration and child-process identity.
- Square run-file, named-pipe, executable, process, and updater-cache identities.
- Square loopback defaults `127.0.0.1:3101` packaged and `127.0.0.1:3102` development.
- Authenticated LAN listener remains disabled by default; reserved port is `3111` only when explicitly enabled.
- Preserve AO `~/.ao`, `ao`, AO ports, and AO runtime identities for official AO coexistence.
- Update focused tests and documentation needed to prove coexistence.

## Forbidden

- No Square Session/domain/API/migration/workflow feature.
- No updater activation or telemetry opt-in.
- No arbitrary process killing or stale endpoint takeover redesign; that remains SA01-T02.
- No worktree cleanup redesign; that remains SA01-T05.
- No modification of applied migrations or generated files by hand.

## Acceptance

- AO and Square can run side by side without shared default data, run-file, process, pipe, port, worktree, or environment identity.
- Explicit test overrides remain available and canonicalized.
- LAN remains off unless explicitly enabled and authenticated.
- Tests prove the separation without valuable repositories or credentials.

## Evidence/receipt

Create `docs/square/evidence/SA00-FIX03/<UTC-stamp>/` and
`docs/square/receipts/SA00-FIX03.json`. After PASS, request a fresh independent SA00-T05 review; do not dispatch SA01 directly.
