# SA00-T02-EA01-FIX-01 — Evidence Integrity Correction

- Status: **owner-activated on 2026-08-10; dispatchable only after route and target preflight**
- Nature: documentation and evidence integrity only
- Target worktree: `D:\WORK\10 - AI\AI TOOLS\square-orchestrator-work-square-main`
- Starting branch: `square/main`
- Starting commit: `4379badfbd38ba33f1fd614d082f5965112882cf`
- Required commit: `docs: fix SA00-T02 evidence integrity`
- Exact route: `opencode` / `opencode-go/deepseek-v4-pro`, `high`, visible VS Code integrated terminal, automatic fallback disabled
- Budget: 100 worker turns, 150000 reported input-token rotation, zero spend, one writer

## Objective

Correct the one `git diff --check` defect found during review of SA00-T02-EA01 and keep the
evidence provenance coherent. Historical baseline facts, failure classifications, source identity,
package results, and telemetry risk must remain unchanged.

## Allowed writes

- `docs/square/evidence/SA00-T02/20260810T053611Z/daemon-cli-smoke.log`
- `docs/square/evidence/SA00-T02/20260810T053611Z/manifest.sha256`
- `docs/square/receipts/SA00-T02.completion.json`

The receipt may change only to update its manifest hash and append a concise FIX-01 amendment
history entry. Preserve the original baseline commit and `commit.created: false` facts.

## Required correction

1. Remove only the extra blank line at EOF in `daemon-cli-smoke.log`. Do not alter any logged
   command, output, timestamp, redaction, or classification.
2. Regenerate `manifest.sha256` over every evidence file except the manifest itself.
3. Update the receipt's referenced manifest hash and append the fix history.
4. Preserve the existing PostHog telemetry risk wording. Do not rerun the packaged app or perform
   a network test.

## Forbidden

- all backend, frontend, package, lockfile, generated-source, migration, resource, and workflow changes;
- rerunning baseline commands, packaging, daemon/app launch, provider/model smoke, or network calls;
- changing historical outcomes, source hashes, package hashes, failure classifications, or telemetry facts;
- changing any path outside the three allowed paths;
- `git add -A`, `git add .`, `git add -u`, `git commit -a`, reset, checkout, restore, deletion, push, or fallback.

## Acceptance criteria

- `git diff --check` and `git show --check` pass.
- Manifest recomputation has zero mismatches and excludes itself.
- All JSON evidence and the receipt parse.
- Product-source diff against `8e2769553f4f0e456dbe4b04fe1f0813e1cf7c8b` is empty.
- The commit changes only the three allowed paths and uses the exact required message.
- The target worktree is clean after the commit.
- The handoff reports the actual receipt hash.
- Telemetry remains an explicit recorded risk for owner acceptance.

## Mandatory STOP conditions

Stop before editing if the starting HEAD, branch, target status, route, allowance, terminal,
budget, or no-fallback record differs from this packet. Stop on any manifest mismatch, JSON parse
failure, unexpected path, credential-pattern finding, source diff, or need to rerun a baseline
command.

This fix does not accept SA00-T02 or pass A0. Owner acceptance remains a separate status decision.
