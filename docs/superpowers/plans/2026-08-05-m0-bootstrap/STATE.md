# M0 Bootstrap State

State records progress; it does not grant authority.

## Cold start

Read root `AGENTS.md` and `CLAUDE.md`, then `SPEC.md`, `STATUS.md`, `HANDOVER.md`, and this
directory's `PACKET.md`, `BUILD.md`, `BUILD-TASKS.md`, and `STATE.md`.

## Current position

| Field | Value |
|---|---|
| Current task | T-M0-05 complete; M0 accepted; no active task |
| T-M0-01 commit | `b5e7a85` — `docs: establish square orchestrator planning baseline` |
| T-M0-03 commit | `d475094` — `docs: add cold-start workflow handover` |
| T-M0-04 commit | `5a6f60b` — `docs: record handover amendment` |
| T-M0-05 starting HEAD | `5a6f60b` — `docs: record handover amendment` |
| Verified file count | 21 tracked planning files |
| Context pairs | passed: all five created documentation directories plus root carry both files |
| Implementation artifacts | none: no `src/`, `tests/`, `pyproject.toml`, dependency, installer, extension, or runtime file |
| Open `STOP:` items | none |
| Owner gate | M0 accepted by the owner on 2026-08-05; M1 not activated |

## Verification

- `rg --files`: 21 files.
- Recursive context-pair check: five created documentation directories passed; root pair present.
- Implementation-artifact guard: passed.
- Required project-foundry, practice-lab, routing, concurrency, and responsibility-graph terms:
  present in the canonical documents.
- Cold-start handover assertions: reading order, context-map references, `STOP:`, boundary review,
  and technical-versus-owner acceptance wording all passed.
- Ledger reconciliation: the tracked-file count and required cold-start order now match repository
  reality.
- Owner acceptance: explicitly granted on 2026-08-05 and recorded without activating M1.
- Sticker Generator worktree: unchanged and clean at bootstrap entry.

## Carry-forward

- M0 is accepted. M1 is planned but inactive; no task is currently active. Do not write its packet
  or create implementation files until the owner records a new bounded authorization.
- Manifest format, SQLite schema, packaging, receipt mechanics, VS Code, MCP, ACP, worktrees, and
  TUI dependencies remain decisions for the first active task that needs them.
- Blueprint inheritance, practice scoring, opt-in outcome metrics, research allow-lists, and
  proposal-promotion thresholds also remain future packet decisions.
