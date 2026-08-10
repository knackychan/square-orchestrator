# Kickoff Prompt — SA00-T02 Unchanged Windows Baseline

Paste this prompt into a fresh visible Command Code terminal session. The primary session must
complete the client preflight from `CLIENT-EXECUTION.md` before launch.

```text
You are executing Square task SA00-T02 — Capture the Unchanged Windows Build, Test, Package, and Runtime Baseline.

Exact client/model route:
cmdc / deepseek/deepseek-v4-flash

Repository:
D:\WORK\10 - AI\AI TOOLS\square-orchestrator

Implementation pack:
D:\WORK\10 - AI\AI TOOLS\square-orchestrator\docs\superpowers\plans\2026-08-09-fork-agent-orchestrator\square-session-first-implementation-pack

Task packet:
D:\WORK\10 - AI\AI TOOLS\square-orchestrator\docs\superpowers\plans\2026-08-09-fork-agent-orchestrator\square-session-first-implementation-pack\plans\tasks\SA00-T02.md

Read these files completely before doing task work:

1. Repository AGENTS.md
2. Repository CLAUDE.md
3. Repository SPEC.md
4. Repository STATUS.md
5. Repository HANDOVER.md
6. Repository CLIENT-EXECUTION.md
7. The pinned Agent Orchestrator source AGENTS.md
8. The pinned Agent Orchestrator README.md
9. The pinned Agent Orchestrator docs/README.md
10. The pinned Agent Orchestrator docs/architecture.md
11. The pinned Agent Orchestrator docs/STATUS.md
12. The pinned Agent Orchestrator docs/development.md
13. The pinned Agent Orchestrator docs/backend-code-structure.md
14. The pinned Agent Orchestrator root package.json scripts
15. The pinned Agent Orchestrator frontend package.json scripts
16. The repository workflows relevant to Windows, build, test, and package
17. The SA00-T02 task packet
18. The pack script scripts/verify-ao-baseline.ps1

Task authority is SA00-T02 only. Do not infer permission to start SA00-T03, modify Square
behavior, rebrand Agent Orchestrator, or implement any Square product feature.

Before any command that writes files, return a preflight report containing:

- current branch, HEAD, square-base-v0.12.1 resolution, and upstream tag resolution
- confirmation that square-base-v0.12.1 and v0.12.1 resolve to the expected 1df40e9-prefixed upstream commit
- confirmation that HEAD is the accepted SA00-T01 downstream tip on square/main and that product source is unchanged
- repository status and whether the starting tree is clean
- all running Agent Orchestrator or Square development instances
- Windows, PowerShell, Git, Go, Node, npm, Docker, architecture, disk, and memory facts
- exact task write scope
- whether E2E, packaging, daemon smoke, and harmless session smoke prerequisites exist
- every triggered STOP condition
- the exact commands you intend to run

If the branch is not square/main, the pinned tag or baseline commit is wrong, the tree is not
clean, a live AO installation would share state with this run, or any other mandatory STOP
condition is triggered, stop and report it. Do not clean, stash, reset, revert, or delete user
files to force the baseline to start.

The accepted SA00-T01 commit on square/main is a downstream documentation and evidence commit.
It is expected that this HEAD does not itself begin with 1df40e9. Do not detach to the baseline
tag. The baseline tag is the upstream identity reference, while the task starts from the accepted
square/main tip and proves that product source remains unchanged.

Objective:

Capture reproducible evidence for the unchanged pinned Agent Orchestrator v0.12.1 Windows
baseline. This is classification and evidence work, not repair. Preserve every upstream failure,
warning, generated drift, environment limitation, and skipped check exactly as observed.

Allowed repository writes are limited to:

- docs/square/evidence/SA00-T02/<UTC timestamp>/**
- docs/square/receipts/SA00-T02.completion.json

Transient dependency, build, package, and generated outputs may be created only as required by
the unchanged upstream commands. Do not retain generated source drift. Do not edit backend,
frontend, package metadata, lockfiles, Go modules, workflows, resources, migrations, generated
source, authority documents, or Square product code. Do not add dependencies, upgrade tools,
change environment requirements, suppress warnings, or patch behavior.

Run the documented baseline helper first, with ContinueOnFailure so all required evidence is
captured:

& "D:\WORK\10 - AI\AI TOOLS\square-orchestrator\docs\superpowers\plans\2026-08-09-fork-agent-orchestrator\square-session-first-implementation-pack\scripts\verify-ao-baseline.ps1" `
  -RepositoryPath "D:\WORK\10 - AI\AI TOOLS\square-orchestrator" `
  -IncludeE2E `
  -ContinueOnFailure

Supplement the helper where the packet or upstream documentation requires it, including:

- environment and Git metadata
- npm install and lockfile integrity
- go build ./...
- go test ./...
- go test -race ./...
- go vet ./...
- the documented root lint command
- frontend typecheck, unit test, build, E2E when prerequisites exist, and Windows package
- generated SQL or API drift checks in a disposable copy or with complete cleanup
- daemon and CLI smoke with isolated data paths
- one harmless Windows AO session only if an owner-authorized credential-free route exists

Never read, print, export, persist, or place credentials in evidence. Do not call model
providers, publish packages, sign artifacts with production credentials, push remotes, or alter
remote services. If no authorized provider route exists for session smoke, record UNAVAILABLE and
continue with the rest of the baseline.

The evidence directory must contain, as applicable:

- environment.json
- one log per declared command
- command-matrix.json
- git-status-before.txt
- git-status-after.txt
- git-diff-after.patch
- generated-drift report
- package inventory and SHA-256 hashes
- daemon and process inventory
- runtime smoke notes
- skipped checks with reasons
- summary.json
- manifest.sha256

Classify each result only as PASS, PRE_EXISTING_FAILURE, ENVIRONMENT_BLOCKED,
NOT_APPLICABLE, or NOT_RUN_WITH_REASON. Do not classify any result as a Square regression because
this task makes no product-source change.

At completion, report:

1. PASS, FAIL, BLOCKED, or STOPPED_FOR_OWNER_DECISION
2. starting and ending HEAD
3. exact client/model route and preflight result
4. every command, exit code, duration, and evidence log
5. required and optional pass/failure counts
6. pre-existing failures and environment blockers
7. package and generated-drift findings
8. daemon and runtime-smoke findings
9. exact product-source diff, which must be empty
10. evidence directory and manifest hash
11. completion receipt path and hash
12. whether SA00-T03 is ready

Do not claim SA00-T02 acceptance. The primary session and owner review the exact evidence and
diff before the next task is activated.
```
