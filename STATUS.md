# Square Orchestrator — Project Status

> Last updated: 2026-08-05

## Current state

- Phase: T-M1-01 technically complete; implementation closed
- Specification: `0.1-draft`
- Active planning subplan: `docs/superpowers/plans/2026-08-05-m1-dry-run-foundation/`
- Application implementation authorized: **no**
- Delegated agent launch authorized: **no**
- Dependency installation authorized: **no**
- External or provider calls authorized: **none**
- M0 status: **accepted by the owner on 2026-08-05**
- Post-M0 context: **client execution playbook added at `59c3a87`; no authority widened**
- M1 planning status: **technically complete at `44428ce`; accepted by the owner on 2026-08-05**
- T-M1-01 status: **technically accepted at `2ff5e93f024f6564af0a25e63c8e34dcb4e90b37`**

## Active authority

The owner accepted the planning-only M0 baseline on 2026-08-05. M0 acceptance closes the bootstrap
milestone; it does not activate another milestone or authorize application code, package metadata,
dependencies, installers, extensions, runtime databases, background services, or worker launches.

The accepted M0 baseline contained 21 planning files. The post-M0 client playbook brought the
tracked documentation count to 22; all five created documentation directories had matching context
pairs. At that point no implementation artifact existed and M1 was inactive. Root `HANDOVER.md`
records the manual cold-start workflow and `CLIENT-EXECUTION.md` records the dated agent-client
profiles.

The owner accepted the completed M1 plan and activated T-M1-01 on 2026-08-05 at reviewed starting
commit `574249cb5f0209a30d13a8190e416be02c5e4fc9`. The owner explicitly deferred the unavailable
OpenCode Go credit/allowance check and directed the current primary Codex session to implement the
task without launching an OpenCode worker. The task produced one exact implementation commit,
`2ff5e93f024f6564af0a25e63c8e34dcb4e90b37`, which passed its focused and full validation and was
technically accepted by the primary session. That one-task execution exception is now closed.

T-M1-01 changed only `AGENTS.md`, `CLAUDE.md`, `README.md`, `sqorch/`, and `tests/`. It used zero
dependencies, zero implementation-time external/provider calls, zero spend, zero destructive
actions, and no delegated launch. Activation and result amendments in `STATUS.md`,
`BUILD-TASKS.md`, and `STATE.md` remain separate from the implementation commit.

The M1 planning result brought the repository to 29 tracked documentation files. T-M1-01 is now
technically complete; T-M1-02 through T-M1-06 and owner-gated T-M1-07 remain inactive. Every
remaining placeholder starting commit and dated route requires a separate authority amendment and
preflight or explicit owner exception before work.

## Planned but inactive

- Remaining M1 authority validation, project preview/audit, practice validation, registry, locks,
  and integrated review
- M2A reviewed project creation/adoption and graph validation
- M2B visible single-worker launcher and deterministic review
- M3 cross-repository scheduling and parallel read-only review
- M4 practice evidence, bounded research, and workflow/tool proposals
- M5 VS Code and MCP adapters
- M6 opt-in worktree concurrency

The existence of these planned milestones creates no implementation authority.

## Next owner gate

The owner may review the T-M1-01 result and separately activate T-M1-02. No later T-M1 task becomes
active automatically. Reverify OpenCode Go credit and allowance, or record another explicit owner
route exception, before any later delegated launch.
