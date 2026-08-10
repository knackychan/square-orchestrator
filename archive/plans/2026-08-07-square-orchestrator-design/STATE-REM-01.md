# REM-01 State Ledger

Evidence only. Never authority.

## Preflight (2026-08-07)

```
Task: REM-01
Risk class: silent-failure
Client: cmdc (recorded); opencode (actual, owner-authorized deviation due to cmdc weekly allowance exhausted)
Exact model ID: deepseek/deepseek-v4-pro (recorded); deepseek-v4-flash (actual, owner-authorized deviation)
Selection reason: reverts a security-driven architecture deviation where a plausible-looking defect could silently keep the file store framed as accepted behavior
Escalation condition: not applicable
Catalogue observed: 2026-08-07, cmdc 1.14.1, 52 models available including deepseek/deepseek-v4-pro
Allowance observed: cmdc weekly limit off; owner directed opencode/deepseek-v4-flash inline
Automatic fallback: disabled
Visible terminal: Windows Terminal (initially opened, then owner took over inline)
Starting HEAD: 66ea781da9974fa993ab519e82096fbfb0e73bd1
Token rotation: not measured (owner-executed inline session)
```

## Activation

Owner activated REM-01 on 2026-08-07. Recorded route: cmdc / deepseek/deepseek-v4-pro (silent-failure).
Actual route (owner-authorized deviation): opencode / deepseek-v4-flash.
Starting HEAD: 66ea781. Expected commit message:
`fix: revert dependency-free state store and re-scope M1 as non-persistent`.

## Worker result

- Resulting HEAD: b7cd5de03d8b91ea111fcfe01655b2d637ff9201
- Commit message: `fix: revert dependency-free state store and re-scope M1 as non-persistent`
- One commit, parent 66ea781.
- Changed paths (10):
  - `Directory.Packages.props` (blocker/evidence note)
  - `build/SquareOrchestrator.Core.slnx` (remove Persistence.Tests entry)
  - `build/test.ps1` (remove Persistence.Tests run)
  - `build/verify-repository.mjs` (remove Square.Persistence.Sqlite from CLI deps)
  - `src/Square.Cli/Program.cs` (remove project add, remove run, remove persistence refs)
  - `src/Square.Cli/Square.Cli.csproj` (remove Square.Persistence.Sqlite reference)
  - `src/Square.Persistence.Sqlite/StateStore.cs` (deleted)
  - `tests/Persistence.Tests/Persistence.Tests.csproj` (restore placeholder)
  - `tests/Persistence.Tests/Program.cs` (deleted)
  - `tests/test-suites.json` (remove Persistence.Tests command)
- Validation by owner: build 0 warnings/0 errors; Domain.Tests 41/41; Contract.Tests 4/4; Architecture.Tests 3/3; verify-repository.mjs passed; no StateStore/StateConflictException/ProjectRegistration refs remain; forbidden phrases absent from Directory.Packages.props; git diff --check clean.
- Independent primary validation by takeover session: all above checks passed again.

## Route deviation

The recorded REM-01 route was `cmdc / deepseek/deepseek-v4-pro`. The owner executed the task via
`opencode / deepseek-v4-flash` (inline, no delegated terminal) because the cmdc weekly allowance was
exhausted. This is recorded as an owner-authorized exception for this one attempt. It is not a
precedent for later tasks. The deviation is noted in root `STATUS.md`.

## Findings / carry-forward

None. The task met its packet contract.

## Acceptance

Primary technical review accepts REM-01 on 2026-08-07. Owner accepted REM-01 on 2026-08-07. `SP02-T01`
remains inactive. No application implementation is authorized.
