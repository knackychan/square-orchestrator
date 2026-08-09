# SP00-T02 — ConPTY and Job Object lifecycle proof

- Date: 2026-08-07
- Status: prototype implementation complete; Windows x64 execution evidence pending
- Gate impact: G0 remains blocked until a qualifying Windows run produces `PASS`
- Prototype: `prototypes/TerminalProof/`
- Production promotion: prohibited before SP00-T05 records an accepted architecture decision

## Authority baseline

This proof implements only SP00-T02 from the sliced implementation plan and the Windows terminal
boundary from the technical architecture. The checked-in dispatch packet binds the work to these
source hashes:

| Authority document | SHA-256 |
|---|---|
| `square-orchestrator-sliced-implementation-plan.md` | `135fc5449998cea9b2bc3b9ddbc8bb8848e60c54ddb25dec34bc232016f0f65f` |
| `square-orchestrator-technical-architecture.md` | `5f13c89330470104a1263ef7f371128c6ab5a41199a121c258785775aa448aad` |

The dispatch contract, paths, budgets, stop conditions, and task-local acceptance IDs are recorded in
`prototypes/TerminalProof/dispatch.packet.json`. Every proof run hashes that packet into
`environment.json`.

## Decision being tested

The proposed leaf architecture is one ConPTY plus one kill-on-close Job Object for each terminal
attempt. The proof must establish that a normal, current-user Windows x64 process can:

1. create the ConPTY channels before process launch;
2. launch the root process suspended, assign it to the Job Object, and only then resume it;
3. read terminal output exactly once while independently accepting input and resize operations;
4. contain ordinary nested descendants and terminate the complete observed tree on an explicit hard
   stop or owner failure;
5. distinguish normal exit, crash, graceful Ctrl+C, ignored Ctrl+C, and forced termination;
6. preserve a quiet-but-running process without treating silence as failure; and
7. record CPU, working set, output latency, transfer bytes, process identities, and harness handle
   growth at one, four, and eight concurrent sessions.

This task does not decide workflow state machines, terminal health policy, prompt classification,
controller leases, persistence, or adapter behavior. Those remain in SP01 and SP03–SP04.

## Prototype layout

| Component | Responsibility |
|---|---|
| `Square.TerminalProof.Native` | Narrow Win32 interop, ConPTY handles, `STARTUPINFOEX`, suspended launch, Job Object assignment/accounting, resize, input/output, a dedicated blocking output thread, Ctrl+C input, hard stop, and bounded teardown |
| `Square.TerminalProof.Fixture` | Deterministic child executable and nested helper processes |
| `Square.TerminalProof.CrashOwner` | Creates a nested fixture tree, records exact Job Object PID/start-time identities, then deliberately terminates the owner without disposal |
| `Square.TerminalProof.Harness` | Reliability, concurrency, metrics, leak checks, owner-crash reconciliation, and evidence production |
| `Square.TerminalProof.Tests` | Dependency-free contract tests for quoting, dimensions, manifest validation, JSON strictness, and harness options |
| `validate-source.mjs` | Host-neutral structural and authority-boundary checks |
| `TerminalProof.slnx` | Isolated Windows proof solution; production solutions do not reference it |

No prototype project references `src/`, and no production project references the prototype.

## Launch and ownership sequence

The native session follows this order:

1. Create non-inheritable anonymous input and output pipes.
2. Create the pseudoconsole with the pseudoconsole-side pipe handles.
3. Build one `PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE` attribute list, passing the `HPCON` value directly
   as the attribute value.
4. Create an unnamed Job Object with `JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE`.
5. Prepare `STARTUPINFOEX` without `STARTF_USESTDHANDLES`; the pseudoconsole attribute owns the
   hosted console path, and `CreateProcessW` receives `inheritHandles=false`.
6. Call `CreateProcessW` with `EXTENDED_STARTUPINFO_PRESENT`, `CREATE_UNICODE_ENVIRONMENT`, and
   `CREATE_SUSPENDED`.
7. Assign the suspended root process to the Job Object and verify membership.
8. Close the host copies of the pseudoconsole-side pipe handles.
9. Start one dedicated long-running blocking output pump and then resume the root thread, requiring the
   prior suspend count to be exactly one.
10. On teardown, terminate remaining Job Object members only through the explicit hard-stop path,
    close input and ConPTY, drain output, and dispose safe handles.

Creating the root suspended prevents it from starting a descendant before assignment. The fixture
uses ordinary `Process.Start` descendants without a breakaway flag, allowing the proof to test the
planned containment model rather than a malicious escape mechanism.

## Scenario contract

The canonical manifest contains eleven ordered scenarios:

| Scenario | Required observation |
|---|---|
| `unicode` | UTF-8 text including accented Latin, Han, Greek, and emoji survives the ConPTY path |
| `ansi` | ANSI escape bytes and styled text are captured without host interpretation |
| `large_burst` | A declared 1 MiB burst is drained without deadlock or truncation below the requested payload |
| `quiet_child` | Root and child remain alive and contained through a no-output observation interval |
| `stdin_question` | A question marker is observed, bounded input is written, and the expected answer is echoed |
| `resize` | The fixture observes the requested 132×43 terminal dimensions after resize |
| `normal_exit` | Output and exit code zero reconcile normally |
| `crash` | A deliberate unhandled exception produces a nonzero process outcome |
| `graceful_cancel` | Ctrl+C reaches the console handler, produces `CANCEL-ACK`, and exits zero |
| `forced_termination` | Ctrl+C is deliberately ignored, followed by an explicit Job Object hard stop |
| `nested_children` | Root plus three descendants become active in the Job Object and are all removed by hard stop |

The owner-crash probe separately starts the nested tree, writes all observed Job Object PIDs and their
UTC process-start ticks through an atomic ready file, and calls `Environment.FailFast` without disposing
the session. The parent harness then verifies that every exact recorded process identity exits after the
owner handle closes, avoiding a false survivor caused by rapid PID reuse.

## Run shape and evidence

A full acceptance-eligible run uses the checked-in defaults:

- three `normal_exit` warm-up runs;
- 100 sequential reliability repetitions for each of the eleven scenarios;
- one scale group for every scenario at 1, 4, and 8 simultaneous sessions;
- one nested owner-crash containment probe; and
- handle checkpoints after every reliability scenario, every scale group, and at final completion.

The evidence directory starts empty and receives:

| File | Content |
|---|---|
| `environment.json` | OS/runtime/process architecture, elevation, containing-job state, initial handles, and SHA-256 identities for dispatch, manifest, harness, fixture, and crash-owner binaries |
| `manifest.snapshot.json` | Effective manifest after diagnostic overrides |
| `runs.ndjson` | One append-only record per warm-up, reliability, and scale session |
| `owner-crash-ready.json` | Atomic owner-crash PID handoff used by the parent verifier |
| `summary.json` | Scenario summaries, scale groups, handle checkpoints, limitations, failures, and owner-crash result |
| `evidence-manifest.sha256` | Hash manifest for every proof-created evidence file |

The streamed PowerShell log is deliberately stored beside the evidence directory as
`<evidence-directory>.harness.log`, allowing the harness to require an initially empty evidence set and
hash everything it creates inside it.

`summary.json` has three possible statuses:

- `PASS`: all technical checks pass and no acceptance limitation exists;
- `DIAGNOSTIC_PASS`: the exercised subset passes, but quick mode, a scenario filter, reduced repeats,
  skipped owner crash, altered canonical scenario set, or elevated execution prevents acceptance; or
- `FAIL`: a scenario, leak check, handle checkpoint, owner-crash probe, or run-shape check fails.

Only `PASS` from a normal non-elevated Windows x64 run using the checked-in manifest and dispatch
packet hashes, all canonical scenarios, at least 100 repetitions, all 1/4/8 scale groups, and the
owner-crash probe may be considered at SP00-T05.

## Reattachment boundary

This prototype deliberately keeps the ConPTY, anonymous pipe, process, and unnamed Job Object handles
only in the terminal owner process. It does not introduce a surviving broker or transferable-handle
protocol. Therefore, this selected leaf design does not claim live PTY reattachment after owner
failure. `JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE` is expected to terminate the observed tree; startup
recovery must use durable state to report a lost/terminated terminal rather than silently rerun or
claim that the old PTY was reattached.

This is a result boundary for this architecture, not a claim that every conceivable Windows broker or
handle-transfer design is impossible.

## Validation commands

Host-neutral source and authority boundary:

```text
node prototypes/TerminalProof/validate-source.mjs
```

Full Windows x64 proof from a normal, non-elevated PowerShell session with the pinned SDK:

```powershell
./prototypes/TerminalProof/run-proof.ps1
```

Development-only smoke run:

```powershell
./prototypes/TerminalProof/run-proof.ps1 -Quick -FailFast
```

The wrapper builds `TerminalProof.slnx` for `win-x64`, executes the console contract tests, and then
runs the harness. A nonempty evidence destination is rejected.

## Current validation state

The source-contract validator passes in the creation environment. JSON/XML structure, dispatch
authority hashes, canonical manifest/dispatch identities, scenario ordering, dependency isolation,
required Win32 entry points, direct `HPCON` attribute use, inherited-standard-handle isolation,
suspended-assign-resume ordering, dedicated output servicing, fixture markers, exact owner-crash process
identity capture, and harness acceptance markers have been checked.

The creation environment is Linux and does not provide the pinned .NET SDK, PowerShell, or Windows
ConPTY/Job Object APIs. It therefore cannot compile the Windows target, execute the console tests, run
any scenario, measure resources, or produce qualifying empirical evidence. A direct attempt to obtain
the exact SDK binary was blocked by the environment's artifact-download restrictions. No Windows
measurement or G0 acceptance is claimed in this repository.

## Remaining risks before SP00-T05

1. Exact .NET compile/analyzer results remain unknown until the pinned SDK runs.
2. ConPTY behavior, Ctrl+C delivery, resize reporting, close ordering, and burst throughput require
   real Windows execution.
3. Nested Job Object behavior may differ when the harness itself is launched inside a CI-owned Job
   Object; the evidence records that condition.
4. The handle-growth tolerance of 24 is provisional until repeated measurements establish a stable
   baseline. Exceeding it fails the run and requires review rather than silently raising the limit.
5. Process, working-set, and handle sampling are periodic; Job Object accounting and explicit PID
   reconciliation remain the containment authorities.
6. The prototype exercises ordinary descendants, not an adversarial process explicitly attempting a
   breakaway or privilege boundary.
7. No target provider CLI has been used. Per the plan, provider CLIs cannot become the lifecycle oracle
   before deterministic fixture gates pass.

## Promotion rule

Do not copy or reference this code from production projects based only on source review. SP00-T05 must
review the raw Windows evidence, record the terminal-hosting ADR, and either accept this leaf
implementation direction or amend it while preserving domain and protocol boundaries.
