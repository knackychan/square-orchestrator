# Square Orchestrator

Square Orchestrator is a terminal-first control plane for coordinating bounded work by existing
coding-agent clients (Command Code, OpenCode, Claude Code, Codex CLI) across multiple repositories.
A primary session or human operator supplies judgement and project authority; the application
supplies deterministic validation, launch mechanics, scheduling, locks, state, and review evidence.

## Repository shape

This repository is the merged redesign base derived from the 2026-08-07 sliced plan, Windows
technical architecture draft, terminal workspace UI draft, and UI handover README (preserved under
`docs/authority/` and `docs/superpowers/plans/2026-08-07-square-orchestrator-design/`).

- **.NET 10 core** (`src/`): pure domain (strong IDs, content hashes, canonical UTC, schema
  versions, result/problem primitives, terminal lifecycle reducer), versioned contracts and strict
  JSON, application use cases, and host shells (`square` CLI, daemon, WPF desktop).
- **TypeScript shared UI** (`ui/` + `vscode/`): host-neutral design tokens, dock layout presets,
  sequence-aware terminal stream, strict host-bridge message validation, and a VS Code extension
  shell.
- **Architecture proofs** (`prototypes/`): isolated SP00 proofs for ConPTY/Job Objects
  (TerminalProof), named-pipe framing/reconnect (PipeProof), and shared UI hosting (SharedUiProof).
  These are source-complete; their Windows execution evidence is pending, and **G0 remains blocked**.
- **M1 Python CLI** (`sqorch/` + retained Python `tests/`): the proven M1 dry-run foundation
  (authority manifest compilation, practice-record validation, project preview/audit, SQLite
  registry and locks). It is being ported to the .NET modules and will be removed once the ports
  pass.
- **Governance**: `SPEC.md`, `STATUS.md`, `HANDOVER.md`, `CLIENT-EXECUTION.md`, and the
  `AGENTS.md`/`CLAUDE.md` context pairs remain authoritative.

## Toolchain

- .NET SDK: `10.0.302` (pinned by `global.json`)
- C#: `14.0`
- Node.js: `24.19.0` (pinned by `.nvmrc` and package engines)
- pnpm: `11.20.0`
- TypeScript: `6.0.3`

## First commands on Windows

```powershell
./build.ps1
./test.ps1 -Category Deterministic
./test.ps1 -Category Architecture
./test.ps1 -Category UI
./test.ps1 -Category Prototype
```

Run `./dev.ps1 -Component Cli -- --help` to launch the current CLI shell.

## Repository rule

Dependencies point inward. `Square.Domain`, `Square.Contracts`, and `Square.Application` may not
reference hosts, Windows/platform code, persistence, adapters, or UI packages. The daemon is the only
future authoritative state mutation owner; current hosts are intentionally inert.

See `docs/IMPLEMENTATION_STATUS.md` for task-level status,
`docs/gates/G0-architecture-proof-review.md` for the fail-closed gate result, and `docs/authority/`
for the exact source documents and hashes used for this bootstrap.
