# SA00-FIX04 — Complete Square runtime namespace closure and supersede baseline evidence

Status: **PROPOSED — owner authorization required**

## Why this fix exists

The superseding SA00-T05 review at `de5e7754` passes the source-control and
authority portions of A0 but remains blocked by four criteria. The runtime
rename was incomplete in active CLI/session/preview/mobile/frontend seams, the
retired updater tooling still contains official AO feed references, and the
committed SA00-T02/T03 evidence manifests are not reproducible from raw Git
blobs.

## Surgical scope

1. Close active Square namespace seams only: use the accepted `square` command,
   `SQUARE_*` environment namespace, `~/.square` state root, `.square` workspace
   metadata, Square release/app identity, and reserved port contracts in the
   active paths identified by the review:
   `backend/internal/cli/{start,hooks,preview,dev,browser,spawn,launch}.go`,
   `backend/internal/session_manager/manager.go`,
   `backend/internal/previewserver/manager.go`,
   `backend/internal/mobilebridge/{config,pushdevices}.go`, and the affected
   frontend main/state/settings/import seams.
2. Retire or neutralize unused updater/feed tooling and remove official AO feed
   and release references from executable or release-participating Square
   surfaces. Do not re-enable updater behavior; it remains disabled through
   SA14. Preserve historical upstream documentation only when clearly marked
   non-executable and outside the Square release path.
3. Add boundary tests for Square env/path/release identity and an explicit scan
   proving no active Square product surface resolves to AO state, process,
   app, port, telemetry, or updater identity. Do not alter AO compatibility
   behavior in an official AO checkout.
4. Regenerate superseding SA00-T02 and SA00-T03 evidence manifests from the
   committed Git bytes with a documented line-ending policy. Preserve all old
   manifests, receipts, and evidence; use new superseding evidence and receipt
   references.
5. Emit a new FIX04 report, manifest, receipt, and exact changed-file list.

## Explicit non-scope

- No SA01 lifecycle, daemon identity/readiness/takeover/safe-stop, ConPTY,
  Job Object, or process-tree implementation.
- No telemetry opt-in, updater opt-in, LAN listener enablement, signing, npm
  publication, trademark clearance, UI redesign, API migration, or database
  migration.
- No arbitrary keep-alive timer and no process-name killing.

## Required verification and STOP conditions

- Clean isolated worktree from the accepted candidate.
- `go build ./...`, focused backend tests, frontend typecheck, and focused
  Square safety tests pass or are explicitly classified.
- Raw Git-blob recomputation reports zero failures for the new T02/T03
  superseding manifests.
- Active-surface scan finds no AO defaults or official AO feed path.
- STOP on scope expansion, unclassified AO compatibility behavior, changed
  authority decisions, credential/network use, or any SA01 lifecycle change.

## Acceptance

Owner must explicitly authorize this packet before implementation dispatch.
After its receipt is committed, dispatch only a fresh independent SA00-T05 A0
review. SA01 remains blocked until all A0-01 through A0-12 pass.
