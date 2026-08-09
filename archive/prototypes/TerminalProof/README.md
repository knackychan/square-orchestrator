# SP00-T02 — ConPTY and Job Object lifecycle proof

This isolated Windows x64 prototype implements the architecture-proof task from the sliced plan. It is
not referenced by a production project and does not authorize promotion into `src/`.

The complete proof note is `docs/proofs/conpty-job-object.md`. The task boundaries, source hashes,
budgets, acceptance IDs, discretion, and `STOP` conditions are frozen in `dispatch.packet.json`; each
run hashes that packet into its environment evidence.

## Components

- `Square.TerminalProof.Native` — narrow Win32 interop and lifecycle owner for synchronous ConPTY
  pipes, a dedicated blocking output thread, `STARTUPINFOEX`, suspended process launch, Job Object
  assignment, resize, UTF-8 input/output, Ctrl+C input, Job Object termination, accounting, and
  deterministic teardown.
- `Square.TerminalProof.Fixture` — deterministic child CLI scenarios for Unicode, ANSI, burst output,
  a quiet descendant, stdin, resize, normal exit, crash, graceful cancellation, ignored cancellation,
  and nested descendants.
- `Square.TerminalProof.Harness` — manifest-driven reliability and 1/4/8-session scale runner. It
  writes append-only NDJSON run evidence, summaries, environment/tool hashes, handle checkpoints, and
  an evidence checksum manifest.
- `Square.TerminalProof.CrashOwner` — creates a nested process tree, writes each observed Job Object
  PID plus process start time atomically, and terminates its owner without disposal so the parent can
  verify `JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE` without a PID-reuse false result.
- `Square.TerminalProof.Tests` — dependency-free contract tests run before the empirical proof.

## Run

From a normal, non-elevated x64 PowerShell session with the pinned .NET SDK:

```powershell
./prototypes/TerminalProof/run-proof.ps1
```

The full run executes every scenario 100 times, then records one scale group for each scenario at 1,
4, and 8 concurrent sessions. A developer smoke run is available without claiming proof completion:

```powershell
./prototypes/TerminalProof/run-proof.ps1 -Quick
```

Target one scenario while developing:

```powershell
./prototypes/TerminalProof/run-proof.ps1 -Quick -Scenario resize -FailFast
```

Run the host-neutral boundary check independently:

```text
node prototypes/TerminalProof/validate-source.mjs
```

Evidence is written under `artifacts/test-results/SP00-T02/<UTC stamp>/` unless overridden. The
harness creates `environment.json`, `manifest.snapshot.json`, `runs.ndjson`,
`owner-crash-ready.json`, `summary.json`, and `evidence-manifest.sha256`. The console log is stored
beside the evidence directory as `<directory>.harness.log`.

The task can be accepted only from a full non-elevated Windows x64 run whose `summary.json` status is
`PASS`, with the checked-in manifest and dispatch-packet hashes, all canonical scenarios, the 100-run
minimum, 1/4/8 scale groups, owner-crash containment, zero leaked descendants, and handle growth inside
the declared tolerance. `DIAGNOSTIC_PASS` is useful for development but is never G0 evidence.

## Deliberate constraints

- ConPTY communication uses synchronous anonymous-pipe handles as required by the selected API path;
  output is read exactly once on a dedicated long-running thread to avoid blocking process launch or
  coupling input and output servicing.
- The root process is created suspended and assigned to the Job Object before it can create children.
- Closing an observer is outside this prototype. The owner controls the process tree; stopping output
  observation is not represented as cancellation.
- A lack of output is never treated as failure. `quiet_child` must remain alive and contained throughout
  its declared quiet window.
- This leaf design has no surviving broker or transferable ConPTY handles. Owner failure is expected to
  close the Job Object and terminate the observed tree; recovery must report a lost/terminated terminal
  rather than claim live PTY reattachment.
