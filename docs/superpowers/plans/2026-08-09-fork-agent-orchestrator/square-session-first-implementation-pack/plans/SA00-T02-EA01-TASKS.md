# SA00-T02-EA01 Ordered Tasks

1. Preflight the target worktree, branch, exact starting commit, and two-path untracked state. Confirm the assigned route, visible terminal, budget, and no-fallback record exist. Otherwise stop without edits.
2. Read the existing receipt and evidence. Verify the original manifest before changing any evidence; verify all JSON parses and record only counts/results in the amendment log.
3. Capture `frontend-build-script-availability.log` with a read-only inspection of `frontend/package.json`. It must prove `scripts.build` is absent and identify the recorded Forge package command as the renderer-build substitute.
4. Add the corresponding `NOT_APPLICABLE` matrix and summary entries. Do not change any existing command outcome or failure classification.
5. Capture the final `git status --porcelain=v1` after the receipt and evidence files are present. Update the receipt to set `working_tree_clean_at_end` false, explain the exact allowlisted final delta, and add an amendment-history entry.
6. Regenerate `manifest.sha256` from every evidence file except itself. Validate all manifest hashes, all JSON, the baseline product-source diff, and credential-pattern counts. Stop on any mismatch or unexpected path.
7. Review `git diff --check` and the full diff. Stage only:

```powershell
git add -- docs/square/evidence/SA00-T02/20260810T053611Z docs/square/receipts/SA00-T02.completion.json
```

8. Inspect `git diff --cached --check` and `git diff --cached --name-only`; commit exactly:

```text
docs: amend SA00-T02 baseline evidence
```

9. Verify the resulting one-commit boundary, clean target worktree, manifest/JSON integrity, empty product-source diff, and no push. Hand the commit and receipt hash to the owner for an explicit `ACCEPT`, `AMEND`, or `REJECT` decision.

## Required handoff

Report starting/resulting HEAD, exact route and reported usage, changed paths, manifest and receipt SHA-256 values, validation results, the unchanged pre-existing failure list, and open stops. Do not claim SA00-T02 accepted until the owner records the decision in canonical status authority.
