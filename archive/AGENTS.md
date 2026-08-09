# Archive — Frozen Pre-Fork Implementation

This directory holds the repository's implementation content exactly as it stood before the
2026-08-09 pivot to a maintained downstream fork of Agent Orchestrator. It is trace/history only.

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
| `src/` | Former .NET 10 module source (domain, contracts, application, control plane, persistence, adapters, hosts) |
| `ui/` | Former TypeScript shared UI packages |
| `vscode/` | Former VS Code extension shell |
| `prototypes/` | Former isolated architecture proofs (TerminalProof, PipeProof, SharedUiProof) |
| `contracts/` | Former draft public contracts and schemas |
| `build/` | Former PowerShell/Node build, test, format, package scripts |
| `tests/` | Former .NET test projects and retained Python M1 suite |
| `sqorch/` | Former standard-library Python M1 CLI source |
| `.github/workflows/windows-ci.yml` | Former CI workflow (dotnet/pnpm build+test); no longer wired to any live CI |
| `artifacts/` | Former gitignored local test-run output, moved here unchanged for trace only |
| `.editorconfig`, `.nvmrc`, `Directory.Build.props`, `Directory.Packages.props`, `NuGet.Config`, `SquareOrchestrator.slnx`, `global.json`, `package.json`, `pnpm-lock.yaml`, `pnpm-workspace.yaml`, `tsconfig.base.json`, `tsconfig.json`, `build.ps1`, `dev.ps1`, `format.ps1`, `package.ps1`, `test.ps1`, `THIRD_PARTY.md` | Former root .NET/Node/pnpm toolchain configuration and convenience scripts |
| `SP02-T01/` | Former `docs/SP02-T01/` dependency-admission planning/evidence packet, moved here 2026-08-09 |
| `adr/` | Former `docs/adr/` Architecture Decision Records from the pre-fork proof gate review, moved here 2026-08-09 |
| `authority/` | Former `docs/authority/` 2026-08-07 redesign source documents and their hash manifest, moved here 2026-08-09 |
| `dispatch/` | Former `docs/dispatch/` SP00-T02 dispatch draft, moved here 2026-08-09 |
| `gates/` | Former `docs/gates/` G0 architecture-proof gate records, moved here 2026-08-09 |
| `proofs/` | Former `docs/proofs/` SP00 proof records (ConPTY/Job Object, named pipe, shared UI), moved here 2026-08-09 |
| `receipts/` | Former `docs/receipts/` SP00 task and proof receipts, moved here 2026-08-09 |
| `reference/` | Former `docs/reference/` preserved interactive UI reference, moved here 2026-08-09 |

Each moved directory retains its own nested `AGENTS.md`/`CLAUDE.md` context pair from before the
move; those still describe their own subtree accurately, just now under `archive/`.
