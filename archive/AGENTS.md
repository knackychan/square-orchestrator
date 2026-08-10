# Archive — Frozen Pre-Fork Line

This directory holds the repository's documents, plans, source, and tooling exactly as they stood
before the 2026-08-09 pivot to a maintained downstream fork of Agent Orchestrator. It is
trace/history only.

## Rules

- Nothing here is live product behavior, and nothing here is authoritative for the fork.
- Do not port code from this tree into the fork. `docs/superpowers/plans/2026-08-09-fork-agent-orchestrator/docs/UPSTREAM_GOVERNANCE.md`
  requires porting *behavior/findings*, not source, from a research line into the AO-based tree.
- Do not resume implementation work in this tree. `STATUS.md` governs current authority, not this
  directory's contents or its own subordinate `AGENTS.md`/`CLAUDE.md` files.
- Treat this content the same way the fork plan treats the earlier .NET/WPF research line: inputs
  and findings (terminal ownership, process containment, evidence patterns) remain useful reference;
  the code itself is not continued.

## File map

| Path | Purpose |
|---|---|
| `AGENTS.md`, `CLAUDE.md` | This context pair |
| `docs/` | All pre-fork documentation, evidence, validation records, and inventories |
| `plans/` | Superseded plans, specifications, and the completed archive-tidy packet |
| `src/` | Former implementation trees, grouped under `dotnet/`, `sqorch/`, `contracts/`, `prototypes/`, `ui/`, `vscode/`, `tests/`, and `build/` |
| `tooling/` | Former root toolchain files, scripts, and `.github/` CI |
| `artifacts/` | Former gitignored local test-run output, kept unchanged and untracked for trace only |

Each moved directory retains its own nested `AGENTS.md`/`CLAUDE.md` context pair from before the
move; those still describe their own subtree accurately, just now under `archive/`.
