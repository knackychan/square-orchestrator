# SA00-T02-EA01-FIX-02 — Manifest Encoding Correction

- Status: **owner-activated on 2026-08-10; dispatchable only after route and target preflight**
- Nature: documentation and evidence integrity only
- Target worktree: `D:\WORK\10 - AI\AI TOOLS\square-orchestrator-work-square-main`
- Starting branch: `square/main`
- Starting commit: `feabb34679d0494419dd5b30ab43765404c6bf42`
- Required commit: `docs: fix SA00-T02 manifest encoding`
- Exact route: `opencode` / `opencode-go/deepseek-v4-pro`, `high`, visible VS Code integrated terminal, automatic fallback disabled
- Budget: 100 worker turns, 150000 reported input-token rotation, zero spend, one writer

## Objective

Remove the unexpected UTF-8 BOM from the first byte of the evidence `manifest.sha256` while
preserving every hash line and every historical evidence fact. Update the receipt's manifest hash
and append the FIX-02 amendment history entry.

## Allowed writes

- `docs/square/evidence/SA00-T02/20260810T053611Z/manifest.sha256`
- `docs/square/receipts/SA00-T02.completion.json`

No other path may change. The telemetry risk remains documented and is not modified by this fix.

## Forbidden

- changing any manifest hash line other than removing the BOM byte;
- changing evidence, logs, classifications, source hashes, package hashes, or telemetry wording;
- rerunning baseline commands, packaging, daemon/app launch, provider/model smoke, or network calls;
- modifying source, dependencies, generated files, or any path outside the two-path allowlist;
- `git add -A`, `git add .`, `git add -u`, `git commit -a`, reset, checkout, restore, deletion, push, or fallback.

## Acceptance criteria

- `manifest.sha256` begins with the ASCII hexadecimal hash character and has no UTF-8 BOM.
- Every manifest entry recomputes correctly, with 42 entries and zero mismatches.
- All JSON evidence and the receipt parse.
- Receipt references the actual manifest SHA-256 and contains a FIX-02 history entry.
- `git diff --check` and `git show --check` pass.
- Product-source diff against `8e2769553f4f0e456dbe4b04fe1f0813e1cf7c8b` is empty.
- Exactly the two allowed paths are committed with the required message.
- The target worktree is clean after the commit.

## Mandatory STOP conditions

Stop before editing if the starting HEAD, branch, target status, route, allowance, terminal,
budget, or no-fallback record differs. Stop on any hash mismatch, JSON failure, unexpected byte or
path, credential-pattern finding, source diff, or need to rerun any baseline command.

This fix does not accept SA00-T02 or resolve the separate telemetry-risk owner decision.
