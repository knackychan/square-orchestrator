# Kickoff Prompt — SA00-T02-EA01-FIX-01

Paste into a fresh visible VS Code integrated terminal. Do not launch until the primary has
completed the client and target preflight in `CLIENT-EXECUTION.md`.

```text
You are executing SA00-T02-EA01-FIX-01 — Evidence Integrity Correction.

Exact route:
opencode / opencode-go/deepseek-v4-pro / high
Automatic fallback is disabled. Do not substitute a client or model.

Planning authority root:
D:\WORK\10 - AI\AI TOOLS\square-orchestrator

Target worktree:
D:\WORK\10 - AI\AI TOOLS\square-orchestrator-work-square-main

Target starting branch and HEAD:
square/main
4379badfbd38ba33f1fd614d082f5965112882cf

Read completely before editing:

1. Target AGENTS.md and CLAUDE.md.
2. Authority AGENTS.md, CLAUDE.md, SPEC.md, STATUS.md, HANDOVER.md, and CLIENT-EXECUTION.md.
3. The authority packet, build guide, and ordered task list at:
   docs/superpowers/plans/2026-08-09-fork-agent-orchestrator/square-session-first-implementation-pack/plans/SA00-T02-EA01-FIX-01-PACKET.md
   docs/superpowers/plans/2026-08-09-fork-agent-orchestrator/square-session-first-implementation-pack/plans/SA00-T02-EA01-FIX-01-BUILD.md
   docs/superpowers/plans/2026-08-09-fork-agent-orchestrator/square-session-first-implementation-pack/plans/SA00-T02-EA01-FIX-01-TASKS.md
4. The complete existing evidence directory and receipt.

Before editing, report target branch, exact HEAD, clean status, exact route, OpenCode catalogue and
allowance evidence, visible terminal, budgets, no-fallback setting, and every STOP condition.
If any required fact is absent or differs, write STOP and do not edit.

This task corrects one evidence-integrity defect found in commit 4379badfbd38ba33f1fd614d082f5965112882cf:
daemon-cli-smoke.log has one extra blank line at EOF, causing git show --check to fail.

Allowed writes are only:

- docs/square/evidence/SA00-T02/20260810T053611Z/daemon-cli-smoke.log
- docs/square/evidence/SA00-T02/20260810T053611Z/manifest.sha256
- docs/square/receipts/SA00-T02.completion.json

Remove only the extra EOF blank line. Preserve all logged facts, redactions, timestamps, and
classifications. Regenerate the self-excluded evidence manifest. Update the receipt's manifest
hash and append a concise FIX-01 amendment-history entry while preserving the baseline commit and
commit.created:false facts.

Preserve the existing PostHog telemetry-risk wording. Do not rerun baseline commands, package or
launch the app, perform provider/model smoke, make network calls, inspect credentials, or modify
any other path.

Validate hashes, JSON, credential-pattern counts without printing matches, git diff --check, the
empty product-source diff against 8e2769553f4f0e456dbe4b04fe1f0813e1cf7c8b, and the exact three-path
commit boundary. Stage only the three allowed paths and commit exactly:

docs: fix SA00-T02 evidence integrity

After commit, verify the target worktree is clean, no push occurred, and report the actual receipt
SHA-256. Do not claim SA00-T02 acceptance. Owner acceptance remains separate.
```
