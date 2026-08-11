# SA00-T02-EA01 — Correct and Persist Baseline Evidence

- Status: **draft; not activated**
- Nature: documentation and evidence only
- Target worktree: `D:\WORK\10 - AI\AI TOOLS\square-orchestrator-work-square-main`
- Expected product-code delta: none
- Required commit: `docs: amend SA00-T02 baseline evidence`

## Objective

Correct the two review findings in the existing SA00-T02 baseline record, preserve its evidence in Git, and leave a clean `square/main` checkout ready for an owner acceptance decision. This task classifies evidence; it does not rerun, repair, or change Agent Orchestrator.

## Entry conditions

The owner must record, before a worker edits:

1. activation of `SA00-T02-EA01` at `square/main` commit `8e2769553f4f0e456dbe4b04fe1f0813e1cf7c8b`;
2. one exact worker client/model route, visible-terminal surface, budget, and no-fallback setting; and
3. authorization for the documentation-only commit below.

At preflight, `git status --porcelain=v1` in the target worktree must contain only:

```text
?? docs/square/evidence/SA00-T02/
?? docs/square/receipts/SA00-T02.completion.json
```

Any other tracked, staged, or untracked change is `STOP: target worktree is not the reviewed SA00-T02 state`.

## Read authority

- target checkout `AGENTS.md`, `CLAUDE.md`, `README.md`, `docs/README.md`, and `frontend/package.json`;
- `plans/tasks/SA00-T02.md`;
- this packet, `SA00-T02-EA01-BUILD.md`, and `SA00-T02-EA01-TASKS.md`;
- the existing SA00-T02 receipt, command matrix, summary, manifest, and final-status evidence.

## Allowed writes

```text
docs/square/evidence/SA00-T02/20260810T053611Z/**
docs/square/receipts/SA00-T02.completion.json
```

## Forbidden actions

- No `backend/**`, `frontend/**`, package, lockfile, generated-source, migration, resource, or workflow edits.
- No dependency installation, package/build/test/daemon rerun, application launch, publish, signing, remote action, credential inspection, or model/session prompt.
- No `git add -A`, `git add .`, `git add -u`, `git commit -a`, reset, checkout, restore, or deletion outside the allowed paths.
- Do not alter the observed command outcomes, source hashes, baseline commit, package hashes, or prior failure classifications.

## Acceptance criteria

- The receipt truthfully reports a non-clean final task tree containing only the allowed evidence and receipt.
- The frontend-build requirement has an explicit evidence row explaining that no `scripts.build` exists and that the recorded Forge/Vite packaging result is the applicable build evidence.
- Every changed evidence file is included in a regenerated `manifest.sha256`; the manifest excludes itself and verifies with zero mismatches.
- All JSON evidence parses; source/package paths remain byte-identical to `8e2769553f4f0e456dbe4b04fe1f0813e1cf7c8b`.
- Exactly one documentation-only commit has the required message and changes only the allowed paths.
- The target worktree is clean after the commit.

## Stop conditions

Stop for owner direction if the required start state differs, a correction needs a new baseline command, a receipt field cannot express the fact without rewriting history, a manifest/JSON validation fails, a secret-pattern scan reports a value, or the commit would include anything outside the allowlist.

## Owner handoff

The commit is a persistence amendment, not acceptance. After independent review, the owner must explicitly accept, amend, or reject **SA00-T02 — Capture the unchanged Windows build/test/package baseline** and have that decision recorded in canonical status authority before **SA00-T03 — License, attribution, identity, telemetry, updater, and data isolation design** dispatch. SA00-T02 acceptance alone does not pass the **A0 — Adoption gate** or authorize product code.
