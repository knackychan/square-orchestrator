# Repository Tidy and Archive Cleanup Plan

- Status: planning only; the owner will execute from another session
- Prepared: 2026-08-09
- Owner take-over required before any file moves; this document is the execution manifest for that session
- Related reuse extraction: `docs/superpowers/plans/2026-08-09-fork-agent-orchestrator/square-session-first-implementation-pack/docs/ARCHIVE_RESEARCH_REUSE.md`

## 1. Objective

Make the repository ready for a clean fork-era start by:

1. extracting the reusable research findings into the active pack (done, see the reuse document); and
2. tidying every pre-fork artifact into a sorted `archive/` (documents together, plans together, source
   together, tooling together) and cleaning the root `docs/` tree so only the active fork plan remains
   there.

Nothing in this plan authorizes application implementation, package metadata, dependencies, worker
launches, or `git push`.

## 2. Global rules for the executing session

- Move tracked files with `git mv` only. Stage exact paths; never `git add -A`, `git add .`, `git add -u`,
  or `git commit -a`.
- After every move, update the nearest context pair (`AGENTS.md`/`CLAUDE.md`) file maps.
- Do not read, stage, or commit `.commandcode/**` or `__pycache__/**`. They are runtime state.
- Do not delete or rewrite pre-fork content; it is trace/history.
- Commit as small logical steps with exact messages, then run the verification section.

## 3. Target archive layout

```text
archive/
  AGENTS.md, CLAUDE.md          context pair (file map updated)
  docs/                         ALL pre-fork documentation, evidence, records
    adr/  authority/  dispatch/  gates/  proofs/  receipts/  reference/  SP02-T01/
    validation/                  (moved from docs/validation)
    IMPLEMENTATION_STATUS.md     (moved from docs/)
    repository-inventory.json    (moved from docs/)
  plans/                        ALL superseded planning + old specs
    specs/
      2026-08-05-square-orchestrator-design.md
      2026-08-05-m1-dry-run-foundation-design.md
    2026-08-05-m0-bootstrap/
    2026-08-05-m1-dry-run-foundation/
    2026-08-05-low-tier-research-delegation/
    2026-08-05-owner-input-notifications/
    2026-08-07-square-orchestrator-design/
    2026-08-08-fork/
  src/                          ALL former source/implementation trees
    dotnet/  sqorch/  contracts/  prototypes/  ui/  vscode/  tests/  build/
  tooling/                      former root toolchain + scripts + CI
    .github/
    .editorconfig  .nvmrc  Directory.Build.props  Directory.Packages.props  NuGet.Config
    SquareOrchestrator.slnx  global.json  package.json  pnpm-lock.yaml  pnpm-workspace.yaml
    tsconfig.base.json  tsconfig.json  THIRD_PARTY.md
    build.ps1  dev.ps1  format.ps1  package.ps1  test.ps1
  artifacts/                    stays as gitignored scratch (untracked, not committed)
```

After the move, the repository root holds only:

```text
AGENTS.md  CLAUDE.md  CLIENT-EXECUTION.md  HANDOVER.md  README.md  SPEC.md  STATUS.md
archive/  docs/  .gitattributes  .gitignore  .commandcode/  .git/
```

And `docs/` holds only:

```text
docs/
  AGENTS.md  CLAUDE.md
  superpowers/
    AGENTS.md  CLAUDE.md
    plans/
      AGENTS.md  CLAUDE.md
      2026-08-09-fork-agent-orchestrator/     (the active pack, untouched apart from reuse additions)
    specs/
      AGENTS.md  CLAUDE.md                    (kept as a context pair; empty until the fork needs a spec)
```

## 4. Move manifest (source → destination)

### 4.1 Move pre-fork docs out of `docs/` into `archive/docs/`

```text
docs/IMPLEMENTATION_STATUS.md                 -> archive/docs/IMPLEMENTATION_STATUS.md
docs/repository-inventory.json                -> archive/docs/repository-inventory.json
docs/validation/*                             -> archive/docs/validation/
```

### 4.2 Move superseded plans out of `docs/superpowers/plans/` into `archive/plans/`

```text
docs/superpowers/plans/2026-08-05-m0-bootstrap/                 -> archive/plans/2026-08-05-m0-bootstrap/
docs/superpowers/plans/2026-08-05-m1-dry-run-foundation/        -> archive/plans/2026-08-05-m1-dry-run-foundation/
docs/superpowers/plans/2026-08-05-low-tier-research-delegation/ -> archive/plans/2026-08-05-low-tier-research-delegation/
docs/superpowers/plans/2026-08-05-owner-input-notifications/    -> archive/plans/2026-08-05-owner-input-notifications/
docs/superpowers/plans/2026-08-07-square-orchestrator-design/   -> archive/plans/2026-08-07-square-orchestrator-design/
docs/superpowers/plans/2026-08-08-fork/                         -> archive/plans/2026-08-08-fork/
```

Each moved plan folder keeps its own nested `AGENTS.md`/`CLAUDE.md` context pair.

### 4.3 Move superseded specs into `archive/plans/specs/`

```text
docs/superpowers/specs/2026-08-05-square-orchestrator-design.md -> archive/plans/specs/
docs/superpowers/specs/2026-08-05-m1-dry-run-foundation-design.md -> archive/plans/specs/
```

The `docs/superpowers/specs/` context pair (AGENTS.md/CLAUDE.md) remains in place.

### 4.4 Reorganize `archive/` internals (current → target)

All documentation records move into `archive/docs/`, all implementation trees move into `archive/src/`,
and toolchain/CI move into `archive/tooling/`. To avoid an `archive/src/src` nesting, the former `.NET`
implementation tree (currently `archive/src/`) is renamed to `archive/src/dotnet/` so the group name
`src/` stays clean.

```text
archive/.github/    -> archive/tooling/.github/
archive/adr/        -> archive/docs/adr/
archive/authority/  -> archive/docs/authority/
archive/build/      -> archive/src/build/
archive/contracts/  -> archive/src/contracts/
archive/dispatch/   -> archive/docs/dispatch/
archive/gates/      -> archive/docs/gates/
archive/proofs/     -> archive/docs/proofs/
archive/prototypes/ -> archive/src/prototypes/
archive/receipts/   -> archive/docs/receipts/
archive/reference/  -> archive/docs/reference/
archive/SP02-T01/   -> archive/docs/SP02-T01/
archive/sqorch/     -> archive/src/sqorch/
archive/src/        -> archive/src/dotnet/    (former .NET 10 implementation tree)
archive/tests/      -> archive/src/tests/
archive/ui/         -> archive/src/ui/
archive/vscode/     -> archive/src/vscode/
```

Resulting `archive/src/`:

```text
archive/src/
  dotnet/        (was archive/src/)
  sqorch/        (was archive/sqorch/)
  contracts/     (was archive/contracts/)
  prototypes/    (was archive/prototypes/)
  ui/            (was archive/ui/)
  vscode/        (was archive/vscode/)
  tests/         (was archive/tests/)
  build/         (was archive/build/)
```

### 4.5 Move toolchain files into `archive/tooling/`

```text
archive/.editorconfig  archive/.nvmrc  archive/Directory.Build.props  archive/Directory.Packages.props
archive/NuGet.Config  archive/SquareOrchestrator.slnx  archive/global.json  archive/package.json
archive/pnpm-lock.yaml  archive/pnpm-workspace.yaml  archive/tsconfig.base.json  archive/tsconfig.json
archive/THIRD_PARTY.md  archive/build.ps1  archive/dev.ps1  archive/format.ps1  archive/package.ps1
archive/test.ps1
```

### 4.6 Artifacts

`archive/artifacts/` stays in place and stays untracked (gitignored scratch). Do not stage it.

## 5. Context pair updates (mandatory after each move step)

| File | Update required |
|---|---|
| `archive/AGENTS.md`, `archive/CLAUDE.md` | Rewrite file map to the target `docs/` + `plans/` + `src/` + `tooling/` layout; note `src/dotnet/`, `src/sqorch/`, etc. |
| `archive/src/AGENTS.md`, `archive/src/CLAUDE.md` (new) | Create a context pair for the grouped source tree that lists the eight subtrees (`dotnet/`, `sqorch/`, `contracts/`, `prototypes/`, `ui/`, `vscode/`, `tests/`, `build/`) and notes the nested pairs that move along (`sqorch/AGENTS.md`, `tests/AGENTS.md`). |
| `archive/plans/AGENTS.md`, `archive/plans/CLAUDE.md` (new) | Create a context pair for the superseded planning archive. List `specs/` and the six moved plan folders; note each `2026-08-05-*` folder carries its own nested pair. |
| `archive/docs/AGENTS.md`, `archive/docs/CLAUDE.md` (new) | Create a context pair for the documentation archive. List `adr/`, `authority/`, `dispatch/`, `gates/`, `proofs/`, `receipts/`, `reference/`, `SP02-T01/`, `validation/`, plus the two files. |
| `archive/tooling/AGENTS.md`, `archive/tooling/CLAUDE.md` (new) | Create a context pair for the toolchain/CI archive. List the root scripts, config, and `.github/`. |
| `docs/AGENTS.md`, `docs/CLAUDE.md` | Remove `validation/`, `IMPLEMENTATION_STATUS.md`, `repository-inventory.json` from the file map; point to archive for pre-fork records. |
| `docs/superpowers/AGENTS.md`, `docs/superpowers/CLAUDE.md` | Keep `plans/` and `specs/`; note that `plans/` now holds only the active pack. |
| `docs/superpowers/plans/AGENTS.md`, `docs/superpowers/plans/CLAUDE.md` | Remove all superseded dated subfolders from the file map; keep only the active pack row. |
| `docs/superpowers/specs/AGENTS.md`, `docs/superpowers/specs/CLAUDE.md` | Update to reflect that the two 2026-08-05 specs moved to `archive/plans/specs/`. |
| Root `AGENTS.md`, root `CLAUDE.md` | Repository map section: `archive/` description → "documents, plans, source, and tooling from the frozen pre-fork line, sorted by type". |
| `docs/superpowers/plans/2026-08-09-fork-agent-orchestrator/` | No move; the pack stays. It has no nested context pair of its own; the nearest pair is `docs/superpowers/plans/AGENTS.md`, which already lists the pack folder. No additional update required for the reuse doc beyond the pack README and manifest already updated. |

## 6. Reference / link updates after the moves

- `README.md`: rewrite to describe the fork-era repository (root governance + `archive/` + active pack under
  `docs/superpowers/plans/2026-08-09-fork-agent-orchestrator/`), replacing the pre-fork `.NET`/`sqorch`
  description.
- `STATUS.md`: the superseded-plan references already point at `docs/superpowers/plans/2026-08-0*/...`;
  re-point them to `archive/plans/...` where the text names the file, or mark them "see archive".
- `docs/superpowers/plans/2026-08-09-fork-agent-orchestrator/square-session-first-implementation-pack/docs/ARCHIVE_RESEARCH_REUSE.md`:
  update every `archive/...` path to the new layout per the move manifest, including the source-scope
  line and all table rows. Mapping: `archive/src/*` → `archive/src/dotnet/*`; `archive/sqorch/*` →
  `archive/src/sqorch/*`; `archive/prototypes/*` → `archive/src/prototypes/*`; `archive/build/*` →
  `archive/src/build/*`; `archive/proofs/*`, `archive/adr/*`, `archive/authority/*`,
  `archive/receipts/*` → `archive/docs/...`. After editing, regenerate the pack manifest and re-run
  `validate-pack.py`.
- Any remaining internal links inside moved docs to `docs/authority`, `docs/proofs`, `docs/receipts`,
  `docs/gates`, `docs/validation`, `docs/SP02-T01` should be understood as historical references to
  content now under `archive/docs/`; a link fixer may rewrite them but it is not mandatory for trace docs.

## 7. Validation commands (run at the end of each commit step)

```powershell
git status --short
git diff --check HEAD
git ls-tree -r HEAD archive --name-only   # inspect grouping
git ls-tree -r HEAD docs --name-only      # confirm docs/ holds only the active pack + context pairs
```

Pack integrity (the active pack must stay valid):

```powershell
python "docs/superpowers/plans/2026-08-09-fork-agent-orchestrator/square-session-first-implementation-pack/scripts/validate-pack.py"
```

Regenerate the pack manifest if any pack file changed:

```powershell
# after any edit inside the pack, rebuild MANIFEST.sha256 (see the reuse doc's history for the equivalent script)
```

## 8. Acceptance criteria

- [ ] `archive/` contains only `docs/`, `plans/`, `src/`, `tooling/`, `artifacts/`, and the context pair.
- [ ] `docs/` contains only the active fork pack, its context pairs, and the empty `specs/` pair.
- [ ] Every moved folder retains history (`git mv`) and its nearest context pair file map.
- [ ] `README.md` no longer describes the pre-fork `.NET`/`sqorch` tree.
- [ ] `STATUS.md` superseded-plan references re-point to `archive/plans/...`.
- [ ] `validate-pack.py` reports `PASS`.
- [ ] `git diff --check` clean; no `git add -A` style staging used.

## 9. Out of scope

- No application code, package metadata, dependencies, or worker launches.
- No edits to the active pack's implementation plans except the reuse additions already made.
- No deletion of pre-fork content.
- No `git push`.
