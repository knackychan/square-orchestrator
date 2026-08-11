# Start Here — Square Session-First Fork

This package is the implementation authority pack for starting Square as a maintained downstream fork of Agent Orchestrator.

## Read in this order

1. `README.md`
2. `plans/MASTER_IMPLEMENTATION_PLAN.md`
3. `docs/ARCHITECTURE_AMENDMENT.md`
4. `plans/OWNER_DECISIONS.md`
5. `docs/SESSION_DOMAIN_MODEL.md`
6. `docs/ROLE_ROUTING_MODEL_SELECTION.md`
7. `docs/PERSISTENCE_AND_EVENTS.md`
8. `docs/API_AND_EXECUTION_FACADE.md`
9. `docs/SESSION_FIRST_UI_SPEC.md`
10. `source-map/AO_SOURCE_PLACEMENT_MAP.md`
11. `docs/UPSTREAM_GOVERNANCE.md`
12. `docs/TEST_AND_RELEASE_STRATEGY.md`
13. `plans/tasks/SA00-T01.md` — Create and pin the downstream fork (task packet)
14. `plans/KICKOFF_PROMPT_SA00-T01.md` — Create and pin the downstream fork (kickoff prompt)

## Do not start with product code

The first work is not the UI, database schema, Task Manager, or model routing. Begin by creating a pinned downstream fork and recording the unchanged upstream baseline. This lets later agents distinguish:

- existing Agent Orchestrator behavior;
- a Square adoption/hardening change;
- a new Square product feature.

## First command

From PowerShell, after reviewing the script and replacing paths:

```powershell
& "<pack>\scripts\bootstrap-square-fork.ps1" `
  -Destination "D:\WORK\10 - AI\SQUARE LAB\TOOLS\square" `
  -AuthorityPackPath "<pack>" `
  -CreatedBy "<your identity>"
```

Add the following only when a real owner repository exists:

```powershell
-OriginUrl "https://github.com/<owner>/square.git"
```

The script clones full Git history, verifies the pinned tag/commit, creates `square/main`, records a baseline, copies only the curated starter governance overlay, and produces initial evidence. It does not commit and does not push.

After reviewing and completing **SA00-T01 — Create and pin the downstream fork**, dispatch **SA00-T02 — Capture the unchanged Windows build/test/package baseline**. The baseline helper is:

```powershell
& "<pack>\scripts\verify-ao-baseline.ps1" `
  -RepositoryPath "D:\WORK\10 - AI\SQUARE LAB\TOOLS\square" `
  -ContinueOnFailure
```

`-ContinueOnFailure` is appropriate for the unchanged baseline because pre-existing failures must be captured rather than hidden. The **A0 — Adoption gate** review decides whether each finding is acceptable, repairable, or blocking.

## First deliverable

**SA00-T01 — Create and pin the downstream fork** ends with:

- a pinned full-history Git fork;
- `upstream` and optional `origin` remotes;
- `square/main`;
- `square-base-v0.12.1`;
- exact upstream baseline/evidence;
- curated downstream authority staging files;
- no Square product implementation;
- no remote push.

Do not dispatch **SA00-T02 — Capture the unchanged Windows build/test/package baseline** until the **SA00-T01 — Create and pin the downstream fork** completion receipt has been reviewed.
