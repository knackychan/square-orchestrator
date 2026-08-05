# M0 Packet — Planning Bootstrap

## Objective

Create an independent Git repository whose documents define Square Orchestrator's purpose,
authority model, minimal architecture, first implementation boundary, and cold-start workflow.

## Measurable outcome

- Root authority consists of `AGENTS.md`, `CLAUDE.md`, `SPEC.md`, and `STATUS.md`.
- Every created directory has an accurate `AGENTS.md`/`CLAUDE.md` context pair.
- A design spec separates the deterministic core from repository, client, and terminal adapters.
- The spec covers project creation/adoption, responsibility graphs, clean-code guardrails, opt-in
  cross-project practice evidence, and proposal-only workflow/tool evolution.
- This packet has a build guide, ordered task list, and state ledger.
- Root `HANDOVER.md` lets a cold session operate the same packet/delegation/review workflow without
  relying on chat history or describing planned automation as shipped.
- Git is initialized on `main`, exact files are committed by task, and the final worktree is clean.
- No implementation artifact exists.

## Entry conditions

- Owner instruction on 2026-08-05 to start the independent project.
- Target directory did not exist before this bootstrap.
- Sticker Generator remains unmodified.

## Allowed changes

- Files listed in the root and directory context maps.
- Local Git initialization, exact-path staging, and the four commits named in `BUILD-TASKS.md`.

## Forbidden changes

- Any file under Sticker Generator.
- `src/`, `tests/`, `pyproject.toml`, dependency files, installers, extensions, executables, runtime
  databases, caches, or generated manifests.
- Client catalogue checks, model/provider calls, agent launches, network calls, credential reads,
  background processes, or environment changes.
- An M1 implementation packet or any statement that M1 is active.
- Autonomous self-modification, automatic template promotion, unsourced practice claims, or
  cross-project content collection.

## Ordered work

Follow `BUILD-TASKS.md` T-M0-01 through T-M0-04 exactly.

## Validation

Run from the new repository:

```powershell
git status --short
rg --files
@('docs','docs/superpowers','docs/superpowers/specs','docs/superpowers/plans',
  'docs/superpowers/plans/2026-08-05-m0-bootstrap') | ForEach-Object {
    if (!(Test-Path "$_/AGENTS.md") -or !(Test-Path "$_/CLAUDE.md")) { throw "missing context pair: $_" }
  }
if ((Test-Path src) -or (Test-Path tests) -or (Test-Path pyproject.toml)) {
  throw 'implementation artifact found'
}
git log --oneline --decorate -4
```

Expected: all context pairs exist, no implementation artifacts exist, four matching commits are on
`main`, and the worktree is clean.

## Stop conditions

Stop before mutation if the target already exists, a parent context file imposes conflicting
authority, or any required artifact would need an implementation decision. Stop before committing
if unrelated files appear or exact-path staging cannot isolate M0.

## Evidence destination

`STATE.md` records the commit IDs and validation result.

## Acceptance authority

Technical completion is recorded by the primary session. Project-owner acceptance is a separate
gate and is not inferred from technical completion.
