# M1 Dry-Run Foundation State

State records evidence; it does not grant authority.

## Cold start

Read root `AGENTS.md`, `CLAUDE.md`, `SPEC.md`, `STATUS.md`, and `HANDOVER.md`. Read
`CLIENT-EXECUTION.md` before any route or client work. Then read this directory's `PACKET.md`,
`BUILD.md`, `BUILD-TASKS.md`, and `STATE.md`.

## Planning position

| Field | Value |
|---|---|
| Current activity | M1 planning technically complete; implementation inactive |
| Planning authorization | Owner instruction on 2026-08-05 |
| Starting HEAD | `0c6351e` — `docs: record client playbook context` |
| Planning commit | `44428ce` — `docs: plan m1 dry-run foundation` |
| Verified file count | 29 tracked documentation files |
| Implementation authority | none |
| Delegated agent authority | none |
| External calls / spend | `0 / $0` |
| Dependencies | `0` |
| Proposed implementation tasks | T-M1-01 through T-M1-07; all inactive |
| Live route checks | none; proposed dated routes are not availability evidence |
| Open `STOP:` items | none |
| Owner gate | Review the M1 plan, then activate or amend T-M1-01 |

## Planning verification

- Design responsibility graph matches the source projection: passed.
- Packet fields for authority, paths, budgets, stops, validation, evidence, and acceptance: passed.
- Build decisions for runtime, CLI, manifest, paths, projects, practices, SQLite, locks, and tests:
  closed.
- Six implementation task blocks plus owner-gated T-M1-07: present and inactive.
- New plan directory context pair and parent spec/plan file maps: passed.
- `rg --files`: 29 documentation files.
- `git diff --check 0c6351e..44428ce`: passed.
- Implementation-artifact guard: passed; no `sqorch/`, `tests/`, or `pyproject.toml` exists.
- External calls, spend, dependencies, client launches, and delegated agents used: `0`.
- The extra blank line at the end of `CLIENT-EXECUTION.md`: removed.

## Carry-forward

- Replace each `ACTIVATION_REQUIRED` starting commit only during an owner-authorized task amendment.
- Reverify the exact proposed route and allowance immediately before every launch; never fall back.
- M2A adoption mutation, M2B real launch/review, packaging, migrations, and stale-lock recovery stay
  parked.
