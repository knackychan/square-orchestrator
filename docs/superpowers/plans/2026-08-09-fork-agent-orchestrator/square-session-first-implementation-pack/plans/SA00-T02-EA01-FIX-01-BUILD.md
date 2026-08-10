# SA00-T02-EA01-FIX-01 Build Guide

## Route and boundary

Use OpenCode with exact model `opencode-go/deepseek-v4-pro`, variant `high`, in a visible VS Code
integrated terminal. Automatic fallback and substitution are disabled. This is evidence-integrity
work with silent-failure risk because hashes and acceptance evidence are being corrected.

The authority repository is read-only during worker execution. The target worktree starts at
`4379badfbd38ba33f1fd614d082f5965112882cf` on `square/main`. One writer and one exact commit are
allowed. No baseline or application command may run again.

## Deterministic checks

Before editing:

```powershell
git status --porcelain=v1
git rev-parse HEAD
git branch --show-current
```

After editing and before staging:

```powershell
git diff --check
git diff --name-only 8e2769553f4f0e456dbe4b04fe1f0813e1cf7c8b -- backend frontend packages package.json package-lock.json .github resources
```

Parse every JSON file in the evidence directory and the receipt. Recompute each manifest entry,
report only mismatch counts, and scan credential patterns by count without printing matches.

Stage only the three exact allowed paths, inspect the cached path list and cached diff, and commit
exactly `docs: fix SA00-T02 evidence integrity`. Verify the resulting commit, `git show --check`,
clean worktree, empty product-source diff, and no push.

## Telemetry disposition

The existing receipt and runtime notes already record that the packaged app may transmit
anonymous PostHog telemetry. Preserve that wording and carry it into the handoff. This task does
not establish whether transmission occurred and does not authorize a new network observation.
