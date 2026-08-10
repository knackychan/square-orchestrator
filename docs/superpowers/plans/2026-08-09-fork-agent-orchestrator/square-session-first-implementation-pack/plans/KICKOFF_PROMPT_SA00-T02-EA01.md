# Kickoff Prompt — SA00-T02-EA01 Evidence-Only Amendment

Fill the route placeholder from the owner-recorded activation before launching a worker in a visible foreground terminal.

```text
You are executing Square task SA00-T02-EA01 — Correct and Persist Baseline Evidence.

This is a documentation/evidence amendment only. It is not SA00-T03 and it does not authorize any Square product implementation.

Target worktree:
D:\WORK\10 - AI\AI TOOLS\square-orchestrator-work-square-main

Planning repository and packet source:
D:\WORK\10 - AI\AI TOOLS\square-orchestrator

Exact approved worker route:
<OWNER MUST REPLACE WITH CLIENT / NATIVE MODEL ID / VISIBLE TERMINAL PROFILE>

Do not use automatic fallback or substitute a model/client. Do not launch nested agents or run a model/session smoke.

Read completely before editing:

1. Target worktree AGENTS.md and CLAUDE.md.
2. Planning repository AGENTS.md, CLAUDE.md, SPEC.md, STATUS.md, HANDOVER.md, and CLIENT-EXECUTION.md.
3. Planning repository docs/superpowers/plans/2026-08-09-fork-agent-orchestrator/square-session-first-implementation-pack/plans/tasks/SA00-T02.md.
4. Planning repository docs/superpowers/plans/2026-08-09-fork-agent-orchestrator/square-session-first-implementation-pack/plans/SA00-T02-EA01-PACKET.md.
5. The matching SA00-T02-EA01-BUILD.md and SA00-T02-EA01-TASKS.md files.
6. In the target worktree, docs/square/receipts/SA00-T02.completion.json and the complete docs/square/evidence/SA00-T02/20260810T053611Z directory.

Before writing, report the following and stop if any is wrong:

- target branch is square/main and HEAD is exactly 8e2769553f4f0e456dbe4b04fe1f0813e1cf7c8b;
- target status contains only these untracked paths:
  ?? docs/square/evidence/SA00-T02/
  ?? docs/square/receipts/SA00-T02.completion.json
- the owner has activated SA00-T02-EA01, recorded the exact route/budget/no-fallback setting, and authorized one documentation-only commit;
- the existing evidence manifest verifies with zero mismatches;
- the original receipt, command matrix, summary, and JSON evidence parse.

If any check fails, write STOP: <exact reason> and do not edit, clean, stash, reset, restore, or delete anything.

Allowed writes only:

- docs/square/evidence/SA00-T02/20260810T053611Z/**
- docs/square/receipts/SA00-T02.completion.json

Forbidden:

- all backend, frontend, package, lockfile, generated-source, migration, resources, and workflow changes;
- dependency installation; baseline command reruns; packaging; daemon/app launch; publishing; signing; remote actions; credential inspection;
- changing previous command outcomes, baseline hashes, source identity, package hashes, or pre-existing failure classifications;
- git add -A, git add ., git add -u, git commit -a, reset, checkout, restore, or any deletion outside the allowlist.

Perform only the ordered EA01 task list:

1. Add a read-only frontend-build-script-availability evidence log proving frontend/package.json has no scripts.build and identifying the already-recorded Forge/Vite package result as the applicable build substitute.
2. Add matching NOT_APPLICABLE entries to command-matrix.json and summary.json; preserve every existing row and classification.
3. Replace git-status-after.txt with a final capture taken after both the evidence and receipt exist, showing both allowlisted untracked paths.
4. Amend the receipt: working_tree_clean_at_end must be false, with a note that only the allowed evidence and receipt paths existed. Preserve the original SA00-T02 execution commit and commit.created false. Add a concise amendment-history entry.
5. Regenerate manifest.sha256 from all files inside the timestamped evidence directory except manifest.sha256 itself.
6. Validate every manifest hash, every JSON file, the empty product-source diff relative to 8e2769553f4f0e456dbe4b04fe1f0813e1cf7c8b, and credential-pattern counts without printing matches.
7. Review the full diff and git diff --check. Stage only the two allowed paths, inspect the cached diff, and commit exactly:
   docs: amend SA00-T02 baseline evidence
8. Verify exactly one commit, a clean target worktree, no product-source diff, and no push.

Return:

1. starting and resulting HEAD;
2. exact route and reported token/budget use;
3. changed paths and the one commit;
4. manifest and receipt SHA-256 values;
5. validation results;
6. unchanged pre-existing failures and environment blockers;
7. any STOP item.

Do not claim SA00-T02 accepted. Owner acceptance must be recorded separately in canonical status authority before SA00-T03 can start.
```
