# M0 Build Tasks

## T-M0-01 — Establish the planning baseline

Create every file named in the root and nested context maps. Initialize Git with branch `main`.
The baseline must cover both the orchestration control plane and the project-foundry/practice-lab
capabilities without creating implementation artifacts.

Acceptance assertions:

```powershell
assert (Test-Path SPEC.md)
assert (Test-Path STATUS.md)
assert -not (Test-Path src)
assert -not (Test-Path tests)
assert -not (Test-Path pyproject.toml)
assert (Select-String -Path SPEC.md -Pattern 'Project foundry')
assert (Select-String -Path SPEC.md -Pattern 'Practice lab')
```

PowerShell does not provide a built-in `assert`; these lines are literal contract assertions. The
executable validation in `PACKET.md` implements the same conditions with `if (...) { throw ... }`.

Stage exact paths only. Commit message:

```text
docs: establish square orchestrator planning baseline
```

## T-M0-02 — Record verified bootstrap state

After T-M0-01, run the full packet validation. Update `STATE.md` with the T-M0-01 commit, file
count, context-pair result, implementation-artifact result, and open gates. Update `STATUS.md` to
state that M0 is technically complete and awaits owner acceptance.

Literal state assertions:

```powershell
assert (Select-String -Path STATE.md -Pattern 'T-M0-01 commit.*[0-9a-f]{7,40}')
assert (Select-String -Path STATE.md -Pattern 'Implementation artifacts.*none')
assert (Select-String -Path ../../../../STATUS.md -Pattern 'technically complete')
```

Stage only `STATUS.md` and `STATE.md`. Commit message:

```text
docs: record m0 bootstrap completion
```

## T-M0-03 — Add the cold-start workflow handover

Create root `HANDOVER.md` with the exact reading order, authority precedence, role boundaries,
planning/packet loop, manual visible-terminal delegation, test/document discipline, `STOP:` handling,
diff review, state handoff, and planned-versus-shipped boundary. Add it to the root context maps and
required reading order. Update the M0 packet and D-018 so the file has a recorded contract.

Literal documentation assertions:

```powershell
assert (Test-Path HANDOVER.md)
assert (Select-String -Path AGENTS.md -Pattern 'HANDOVER.md')
assert (Select-String -Path CLAUDE.md -Pattern 'HANDOVER.md')
assert (Select-String -Path HANDOVER.md -Pattern 'Cold-start reading order')
assert (Select-String -Path HANDOVER.md -Pattern 'STOP:')
assert (Select-String -Path HANDOVER.md -Pattern 'Boundary review')
assert (Select-String -Path HANDOVER.md -Pattern 'Technical completion is not owner acceptance')
```

Stage only `HANDOVER.md`, `README.md`, `AGENTS.md`, `CLAUDE.md`, `SPEC.md`, `PACKET.md`, `BUILD.md`,
and `BUILD-TASKS.md`. Commit message:

```text
docs: add cold-start workflow handover
```

## T-M0-04 — Record the handover amendment

Run the amended M0 packet validation. Update `STATE.md` with T-M0-03's commit, the new file count,
and handover checks. Update `STATUS.md` with the amended technical result; M1 remains inactive.

Stage only `STATUS.md` and `STATE.md`. Commit message:

```text
docs: record handover amendment
```
