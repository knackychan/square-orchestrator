# Kickoff Prompt — SA00-T02-EA01-FIX-02

Paste into a fresh visible VS Code integrated terminal. Do not launch until client and target
preflight passes.

```text
You are executing SA00-T02-EA01-FIX-02 — Manifest Encoding Correction.

Exact route:
opencode / opencode-go/deepseek-v4-pro / high
Automatic fallback is disabled. Do not substitute a client or model.

Planning authority root:
D:\WORK\10 - AI\AI TOOLS\square-orchestrator

Target worktree:
D:\WORK\10 - AI\AI TOOLS\square-orchestrator-work-square-main

Target starting branch and HEAD:
square/main
feabb34679d0494419dd5b30ab43765404c6bf42

Read the authority packet, build guide, ordered task list, STATUS.md, CLIENT-EXECUTION.md, the
existing evidence manifest, the existing receipt, and all JSON evidence before editing.

Before editing, report exact HEAD, branch, clean status, route, OpenCode catalogue and allowance,
visible terminal, budgets, no-fallback setting, and STOP conditions. Stop without editing if any
fact differs.

The only defect to correct is the unexpected UTF-8 BOM at the beginning of:
docs/square/evidence/SA00-T02/20260810T053611Z/manifest.sha256

Allowed writes are only:

- docs/square/evidence/SA00-T02/20260810T053611Z/manifest.sha256
- docs/square/receipts/SA00-T02.completion.json

Remove only the BOM bytes. Preserve every manifest hash line and every historical receipt fact.
Update the receipt manifest SHA-256 and append a concise FIX-02 amendment-history entry.
Preserve the existing PostHog telemetry-risk wording. Do not rerun baseline commands, launch the
app, perform provider/model smoke, make network calls, inspect credentials, or modify any other path.

Validate the raw manifest prefix, all 42 hashes, JSON parsing, credential-pattern counts without
printing matches, git diff --check, the empty product-source diff against
8e2769553f4f0e456dbe4b04fe1f0813e1cf7c8b, and the exact two-path commit boundary.

Stage only the two allowed paths and commit exactly:

docs: fix SA00-T02 manifest encoding

After commit, verify clean target worktree, no push, and report the actual receipt SHA-256. Do not
claim SA00-T02 acceptance.
```
