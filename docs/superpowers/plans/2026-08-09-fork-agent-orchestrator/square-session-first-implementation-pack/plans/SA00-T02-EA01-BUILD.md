# SA00-T02-EA01 Build Guide

## Decisions

1. Preserve the original SA00-T02 execution facts. This is an amendment to evidence wording and persistence, never a rerun or a repair.
2. Keep `working_tree_clean_at_end` as `false`: its name is unambiguous, and the final state legitimately includes untracked evidence and the receipt. Add a concise note that the only final delta was the allowlisted evidence/receipt paths.
3. Do not manufacture an `npm run build` result. `frontend/package.json` has no `scripts.build`; add a zero-write availability check and classify that requirement `NOT_APPLICABLE`. Link it to the already-recorded Forge/Vite package build rather than re-running either command.
4. Retain all existing failure and environment classifications verbatim. New amendment facts must be additive and distinguishable from baseline findings.
5. The evidence manifest hashes files inside its timestamped directory only and excludes `manifest.sha256` itself. The receipt remains outside that directory and is protected by the exact Git commit.

## Required evidence edits

| Artifact | Change |
|---|---|
| `frontend-build-script-availability.log` | Capture a read-only Node inspection of `frontend/package.json`, showing that `scripts.build` is absent and that `package`/`prepackage` exist. |
| `command-matrix.json` | Add `frontend-build-script-availability`, required, zero exit, `NOT_APPLICABLE`, and its log name. Do not alter existing rows. |
| `summary.json` | Add the matching classified finding and point to the existing Forge package result as the build substitute. |
| `git-status-after.txt` | Replace the misleading final capture with one taken after both the evidence directory and receipt exist. It must name both paths. |
| `SA00-T02.completion.json` | Correct the end-tree boolean/note; record this amendment and the no-script/Forge disposition. Preserve the baseline execution commit and `commit.created: false`. |
| `manifest.sha256` | Regenerate after all files above are final. |

## Validation contract

Use read-only checks only:

```powershell
git status --porcelain=v1
git diff --name-only 8e2769553f4f0e456dbe4b04fe1f0813e1cf7c8b -- backend frontend packages package.json package-lock.json .github resources
Get-Content -Raw <each-json> | ConvertFrom-Json | Out-Null
```

Recompute every manifest entry with `Get-FileHash -Algorithm SHA256`; report filenames and mismatch counts only, never secret-like matching text. Scan the evidence and receipt for credential patterns by count, not by emitting matched values.

Stage only the two allowlisted paths explicitly, review the cached diff, and commit once. The post-commit check must show a clean worktree and an empty product-source diff relative to the baseline execution commit.

## Explicit non-goals

No attempt to make npm 11, E2E, race tests, lint, SQLC generation, or the shipped PowerShell helper pass. No E2E port intervention. No provider/model invocation. No change to the current client availability record.
