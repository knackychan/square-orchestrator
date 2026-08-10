# ADR-0001 — Terminal hosting: reject production promotion at G0

- Status: `REJECTED_FOR_PROMOTION`
- Date: 2026-08-07
- Task: `SP00-T05`
- Candidate retained: Win32 ConPTY plus one Job Object/process tree per attempt
- Revisit condition: reviewed acceptance-eligible SP00-T02 Windows evidence

## Context

The locked architecture proposes one ConPTY and one Job Object/process tree per attempt. SP00-T02
implements that candidate under `prototypes/TerminalProof/`, including suspended process creation,
Job assignment before resume, Unicode/ANSI/input/resize/cancellation fixtures, nested descendants,
100-run reliability defaults, 1/4/8-session scale groups, owner-crash containment, and resource/handle
evidence.

The proof record explicitly states that the creation environment could not compile or execute the
Windows implementation. No acceptance-eligible `summary.json` exists in the reviewed repository.

## Evidence reviewed

| Evidence | SHA-256 | Result available |
|---|---|---|
| `docs/proofs/conpty-job-object.md` | `0c0c24ce38d946de69d9badbc2ff61de24eb6bd1d094d8478e7db06b485db548` | Source/proof contract only |
| `docs/receipts/SP00-T02.prototype-receipt.json` | `44bcd4c13ed77a1071bc5a681553fcc727d4e855e51c513a6dd47b3a0127d31c` | Windows execution pending |
| `docs/validation/sp00-t02-host-neutral-validation.txt` | `dca78dc10484107a8b25f5b838dafbdce2ac326a0838880a99c1a7d0c8448910` | Host-neutral PASS |
| `prototypes/TerminalProof/dispatch.packet.json` | `4e5b73b4384f1a1346d50648d180b9b198834b8f37e1c94608a032a4a0a9fcd1` | Canonical input |
| `prototypes/TerminalProof/scenarios.json` | `f206d7899c2c5e2eef374f7f7016970afa7653333e82d966f985fa95dd15a405` | 11 scenarios |
| Acceptance-eligible Windows evidence | not present | **Missing** |

## Decision

Do **not** promote the ConPTY/Job Object implementation into production projects at G0. The candidate
remains the preferred leaf for the next empirical run, but source review and host-neutral validation
are not a substitute for Windows lifecycle evidence.

The following conservative boundaries are retained regardless of the later leaf decision:

- one daemon-owned terminal/process container per attempt;
- one output reader assigns the authoritative byte sequence;
- graceful checkpoint/interrupt precedes authorized hard stop;
- exact process identity, not PID alone, is used during reconciliation;
- no live PTY reattachment is claimed by the current owner-process design; and
- owner failure must never silently rerun work.

## Why promotion is rejected

The gate lacks measurements for ConPTY creation, Ctrl+C, resize, close ordering, burst output,
quiet-child behavior, Job Object containment, nested descendants, handle growth, process leakage,
CPU, working set, latency, and write volume on the pinned Windows toolchain. These are the risks that
SP00-T02 exists to burn down.

## Consequences

- `Square.Platform.Windows` must not copy or reference TerminalProof code yet.
- SP03-T03 remains blocked.
- Existing prototype evidence and source stay intact.
- A later decision must supersede this ADR; it must not silently change this status.

## Evidence required to reconsider

A normal, non-elevated Windows x64 run of:

```powershell
./prototypes/TerminalProof/run-proof.ps1
```

must produce canonical `PASS` evidence with all 11 scenarios, at least 100 reliability repetitions,
all 1/4/8 scale groups, the owner-crash probe, zero surviving exact descendant identities, and handle
growth within the currently declared tolerance. Any failed STOP condition requires a leaf-alternative
amendment rather than a threshold increase without review.
