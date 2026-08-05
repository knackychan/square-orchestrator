# M0 Bootstrap State

State records progress; it does not grant authority.

## Cold start

Read root `AGENTS.md`, `SPEC.md`, `STATUS.md`, then this directory's `PACKET.md`, `BUILD.md`,
`BUILD-TASKS.md`, and `STATE.md`.

## Current position

| Field | Value |
|---|---|
| Current task | T-M0-02 complete; M0 awaits owner acceptance |
| T-M0-01 commit | `b5e7a85` — `docs: establish square orchestrator planning baseline` |
| HEAD at last update | `b5e7a85` before this state-record commit |
| Verified file count | 20 tracked planning files |
| Context pairs | passed: all five created documentation directories plus root carry both files |
| Implementation artifacts | none: no `src/`, `tests/`, `pyproject.toml`, dependency, installer, extension, or runtime file |
| Open `STOP:` items | none |
| Owner gate | M0 acceptance pending after technical verification |

## Verification

- `rg --files`: 20 files.
- Recursive context-pair check: five created documentation directories passed; root pair present.
- Implementation-artifact guard: passed.
- Required project-foundry, practice-lab, routing, concurrency, and responsibility-graph terms:
  present in the canonical documents.
- Sticker Generator worktree: unchanged and clean at bootstrap entry.

## Carry-forward

- M1 is planned but inactive. Do not write its executable packet or create implementation files.
- Manifest format, SQLite schema, packaging, receipt mechanics, VS Code, MCP, ACP, worktrees, and
  TUI dependencies remain decisions for the first active task that needs them.
- Blueprint inheritance, practice scoring, opt-in outcome metrics, research allow-lists, and
  proposal-promotion thresholds also remain future packet decisions.
