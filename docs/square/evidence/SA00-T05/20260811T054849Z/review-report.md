# SA00-T05 — Adoption gate A0 superseding independent review

- Decision: **BLOCKED**
- Reviewed commit: `de5e7754a18b1f5bcea44dbfa0a654730bb7dde7`
- Reviewed branch: `square/a0-review-20260811`
- Review UTC: `2026-08-11T05:48:49Z`
- Method: isolated clean worktree; raw Git-object checks; source-default and
  namespace inspection; manifest recomputation; focused backend build/tests.

The prior blocked review and all prior evidence/receipts are preserved. This is
a superseding review of the committed candidate after SA00-FIX02 and
SA00-FIX03.

## Criterion results

| Criterion | Result | Independent finding |
|---|---|---|
| A0-01 — Exact upstream identity | PASS | `v0.12.1` and `square-base-v0.12.1` resolve to `1df40e93772c2c48e916870d9c3ddf8f29a69f84`; candidate is a descendant. |
| A0-02 — Controlled Git/remotes/history | PASS | Candidate worktree is clean and the canonical `upstream` remote is configured. |
| A0-03 — Unchanged Windows baseline | FAIL | The source diff is intentionally bounded, but the committed SA00-T02 evidence manifest still verifies only 29/42 entries against raw Git blobs. |
| A0-04 — No hidden product/dependency drift | PASS | Clean candidate; the intended Square source/lockfile changes are committed and no unclassified worktree delta remains. |
| A0-05 — Apache attribution | PASS | Apache-2.0 is present in the accepted authority and frontend package metadata; no contrary license change was found. |
| A0-06 — Telemetry disabled by default | PASS | Square packaged defaults have no PostHog key/host, backend defaults are remote-off, and Electron stamps telemetry off. Remaining PostHog test fixtures are non-production tests. |
| A0-07 — Updater disabled until SA14 | FAIL | Runtime updater surfaces are disabled, but retired feed scripts and AO updater/release references remain in the committed frontend tree and require an explicit retirement boundary. |
| A0-08 — AO/Square identity isolation | FAIL | Active product paths still use AO namespace defaults: `backend/internal/cli/start.go`, `cli/hooks.go`, `cli/preview.go`, `cli/dev.go`, `session_manager/manager.go`, `previewserver/manager.go`, mobile state, and frontend import/state seams. |
| A0-09 — Owner-accepted session-first architecture | PASS | Accepted authority and owner decisions are committed in `docs/square/authority/` and ADR-SA00-004. |
| A0-10 — Authority hashes/index | PASS | Authority manifest and index are committed and verify in the candidate tree. |
| A0-11 — Supersession register | PASS | The committed supersession register is present and readable. |
| A0-12 — Complete, privacy-safe evidence | FAIL | Privacy/source review is clean, but T02/T03 evidence integrity is incomplete: T02 is 29/42 and T03 is 4/16 against committed Git blobs. |

## Manifest recomputation

- SA00-T02: 29 pass, 13 fail of 42 entries.
- SA00-T03: 4 pass, 12 fail of 16 entries.
- SA00-FIX02 manifest: source entries match their declared candidate hashes.
- SA00-FIX03 manifest: source entries match their declared candidate hashes.
- The failed T02/T03 entries are historical evidence artifacts whose declared
  hashes do not equal the raw Git blob bytes at the reviewed commit. No prior
  evidence was edited or deleted.

## Identity findings requiring correction

The following are active product seams, not merely historical documentation:

- `cli/start.go` still defaults to the official AO release repository and AO app
  marker/bundle identity.
- `cli/hooks.go`, `cli/preview.go`, `cli/dev.go`, `cli/browser.go`, and
  `cli/spawn.go` retain AO environment/path contracts.
- `previewserver/manager.go` and session-manager attachment/dev paths retain
  `.ao` workspace/state contracts.
- frontend import-folder and app-state/settings seams retain `.ao` checks or
  AO-only environment references.
- retired feed/release scripts still document or encode the official AO update
  path even though the runtime updater facade is disabled.

## Verification executed

| Check | Result |
|---|---|
| clean review worktree | PASS |
| canonical `upstream` remote | PASS |
| baseline ancestry and tag identity | PASS |
| `go build ./...` | PASS |
| focused backend packages | PASS |
| frontend typecheck | NOT RUN — isolated review worktree has no `node_modules`; source candidate typecheck had passed in FIX02/FIX03 evidence |

## Decision and next action

This is a hard BLOCKED decision. SA01 and all later milestones remain
unauthorized. Create and owner-authorize `SA00-FIX04 — Complete Square runtime
namespace closure and supersede baseline evidence`, then commit its receipt.
After that receipt is committed, rerun only SA00-T05 as a fresh independent
superseding review.

