# SA00-FIX02 — Telemetry, crash reporting, and updater hard-off

- Status: owner-authorized
- Purpose: enforce the A0 safety defaults in product/build behavior.
- Owner authorization: `authorize everything`, 2026-08-11
- Prerequisite: SA00-FIX01 committed candidate

## Scope

- Square builds send no telemetry by default and have no AO PostHog key or host activated.
- Local telemetry capture is disabled by default; explicit future opt-in remains closed until a separate owner-approved policy.
- Crash reporting is disabled by default.
- Electron updater initialization, scheduled checks, feed consumption, and install paths are disabled through SA14.
- Official AO update repositories, baked update metadata, and updater cache identity cannot be used by Square.
- Add focused unit/config/build tests proving hard-off behavior.
- Update only the relevant configuration, bootstrap, updater, telemetry, package/build, and test surfaces identified by the T03 inventory.

## Forbidden

- No Square PostHog project/key creation or network calls.
- No release feed activation.
- No broad UI/session/workflow changes.
- No SA01 daemon identity or worktree implementation.

## Acceptance

- Packaged and development Square startup cannot initialize AO telemetry or updater behavior.
- No official AO feed path is reachable through default Square code paths.
- Tests fail closed if telemetry/updater defaults become enabled.
- The change is limited to the documented surfaces and has no credentials.

## Evidence/receipt

Create `docs/square/evidence/SA00-FIX02/<UTC-stamp>/` and
`docs/square/receipts/SA00-FIX02.json`. Next task after PASS: SA00-FIX03 or a superseding A0 review if both fixes are complete.
