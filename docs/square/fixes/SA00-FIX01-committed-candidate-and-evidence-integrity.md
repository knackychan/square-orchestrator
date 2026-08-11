# SA00-FIX01 — Committed candidate and evidence integrity

- Status: owner-authorized
- Purpose: repair A0 source/evidence binding without changing product behavior.
- Owner authorization: `authorize everything`, 2026-08-11

## Scope

- Restore the canonical `upstream` remote to `https://github.com/Untrivial-ai/agent-orchestrator.git`.
- Preserve `origin` as the Square repository.
- Classify the existing `frontend/package-lock.json` drift as pre-existing npm-toolchain evidence; do not silently discard or include it in the candidate.
- Commit the accepted SA00-T04 authority, ADR, supersession register, evidence, receipts, and the SA00-FIX packet files.
- Create a clean candidate worktree from the resulting commit, excluding unrelated dirty paths.
- Recompute superseding fix evidence against committed Git bytes.
- Do not modify prior receipts or erase prior evidence.

## Forbidden

- No product source changes.
- No `git reset`, force checkout, or deletion of the package-lock change.
- No push or publish.
- No relabeling old evidence as committed evidence.

## Acceptance

- `upstream` resolves to the pinned AO repository.
- A clean candidate commit contains the accepted authority set.
- All new manifests verify against committed bytes.
- The package-lock drift is explicitly classified and excluded from the clean candidate.
- Prior receipts remain unchanged.

## Evidence/receipt

Create `docs/square/evidence/SA00-FIX01/<UTC-stamp>/` and
`docs/square/receipts/SA00-FIX01.json`. Next task after PASS: SA00-FIX02 and SA00-FIX03.
