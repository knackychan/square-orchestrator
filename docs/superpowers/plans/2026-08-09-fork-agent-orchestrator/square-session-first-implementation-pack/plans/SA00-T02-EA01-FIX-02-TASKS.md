# SA00-T02-EA01-FIX-02 Ordered Tasks

1. Preflight the exact target HEAD, branch, clean status, route, visible terminal, allowance,
   budgets, fallback setting, and two-path allowlist. Stop before editing on any mismatch.
2. Verify the current manifest, receipt, and all JSON before changing anything.
3. Remove only the UTF-8 BOM from the beginning of `manifest.sha256`. Preserve all hash lines.
4. Recompute the manifest and update the receipt manifest reference plus FIX-02 history entry.
5. Validate raw manifest prefix, all hashes, JSON, credential-pattern counts, source diff, and
   `git diff --check`.
6. Stage only:

```powershell
git add -- docs/square/evidence/SA00-T02/20260810T053611Z/manifest.sha256 docs/square/receipts/SA00-T02.completion.json
```

7. Inspect the cached diff and commit exactly:

```text
docs: fix SA00-T02 manifest encoding
```

8. Verify one commit, clean target worktree, 42/42 manifest hashes, JSON validity, empty
   product-source diff, no push, and the actual receipt SHA-256.
