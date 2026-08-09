# Start here — controlled project kickoff

## 1. Owner acceptance

Read and accept/amend:

1. `docs/ARCHITECTURE_AMENDMENT.md`
2. `plans/MASTER_IMPLEMENTATION_PLAN.md`
3. `docs/DECISION_REGISTER.md`
4. `docs/ROLE_ROUTING_MODEL_SELECTION.md`
5. `docs/SESSION_FIRST_UI_SPEC.md`
6. `ui/square-session-workspace-rounded-reference.html`

Use `plans/OWNER_ACCEPTANCE_CHECKLIST.md` to record the decision.

## 2. Create the fork

Open normal PowerShell:

```powershell
& "<PACK>\scripts\bootstrap-square-fork.ps1" `
  -Destination "D:\WORK\...\square-orchestrator" `
  -OriginUrl "https://github.com/<OWNER>/square-orchestrator.git" `
  -AuthorityPackPath "<PACK>"
```

The script does not commit or push.

## 3. Dispatch SA00-T01

Give the local agent:

- `plans/KICKOFF_PROMPT_SA00-T01.md`
- `plans/tasks/SA00-T01.md`
- the full pack path
- the destination path
- explicit instruction whether it may commit

Review the raw Git identity and receipt before moving on.

## 4. Baseline before product work

Run SA00-T02 against unchanged source. Do not fix failures inside the baseline task.

```powershell
& "<PACK>\scripts\verify-ao-baseline.ps1" `
  -RepositoryPath "<REPOSITORY>" `
  -IncludeE2E `
  -ContinueOnFailure
```

Then complete SA00-T03, SA00-T04, and A0.

## 5. First implementation wave

After A0:

```text
SA01-T01  detach daemon from desktop lifetime
SA01-T03  verify/harden Windows terminal runtime       ┐ parallel, separate worktrees
SA01-T05  verify/harden worktree cleanup               ┘
SA01-T02  single daemon/data ownership after T01
SA01-T04  restart/controller reconciliation after T02/T03
SA01-T06  A1 gate
```

No Square session schema, workflow code, or redesigned UI before A1/A2 according to the master plan.

## 6. Where the useful product first appears

- A6: session-focused UI against authoritative fixture/API contracts.
- A7: Square Core Alpha completes one real QUICK session.
- A11: Square MVP completes QUICK and PLANNED with memory, review, and finite repair.

The project is not expected to wait until A14 to become useful.
