# SA00-T02-EA01-FIX-02 Build Guide

Use OpenCode with exact model `opencode-go/deepseek-v4-pro`, variant `high`, in a visible VS Code
integrated terminal. Automatic fallback and substitution are disabled. This is evidence-integrity
work with silent-failure risk.

The authority repository is read-only during worker execution. The target starts at
`feabb34679d0494419dd5b30ab43765404c6bf42` on `square/main`. One writer and one exact commit are
allowed. No baseline or application command may run.

Before editing, verify the target status, branch, exact HEAD, route, allowance, visible terminal,
budgets, and no-fallback setting. Read the manifest as bytes and prove its first three bytes are
not the UTF-8 BOM sequence `EF BB BF`.

After editing, recompute all 42 manifest entries, parse all JSON, scan credential patterns by
count without printing matches, run `git diff --check`, inspect the full diff, verify the empty
product-source diff, stage only the two allowed paths, and commit exactly:

```text
docs: fix SA00-T02 manifest encoding
```

Do not rerun any baseline or runtime command. Preserve the existing telemetry-risk wording.
