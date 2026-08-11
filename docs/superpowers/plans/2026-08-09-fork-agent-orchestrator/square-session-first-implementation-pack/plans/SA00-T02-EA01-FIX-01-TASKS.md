# SA00-T02-EA01-FIX-01 Ordered Tasks

1. Preflight the target worktree, exact HEAD, branch, clean status, OpenCode route, visible VS Code
   terminal, allowance, budgets, fallback setting, and allowed paths. Stop without edits on any mismatch.
2. Verify the current manifest and receipt JSON before changing anything.
3. Remove only the extra blank line at EOF from `daemon-cli-smoke.log`.
4. Regenerate `manifest.sha256`, update the receipt manifest reference, and append the FIX-01 history entry.
5. Validate all hashes, JSON, credential-pattern counts, source diff, and `git diff --check`.
6. Stage only:

```powershell
git add -- docs/square/evidence/SA00-T02/20260810T053611Z/daemon-cli-smoke.log docs/square/evidence/SA00-T02/20260810T053611Z/manifest.sha256 docs/square/receipts/SA00-T02.completion.json
```

7. Inspect the cached diff and commit exactly:

```text
docs: fix SA00-T02 evidence integrity
```

8. Verify one commit, clean target worktree, valid manifest and JSON, empty product-source diff,
   no push, and the actual receipt hash.
9. Report the telemetry risk as unresolved owner-review input. Do not claim SA00-T02 acceptance.
