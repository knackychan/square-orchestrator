# Implementation status

## Accepted source baseline

The four authority documents under `docs/authority/` are copied byte-for-byte from the supplied files.
`docs/authority/manifest.sha256` records their hashes. Written contracts take precedence over code
comments and visual fixtures.

## Task status

| Task | Status | Evidence in this repository |
|---|---|---|
| SP00-T01 | Implemented, execution pending Windows toolchain validation | solution/module shells, build scripts, toolchain pins, architecture tests, UI compile/test harness |
| SP00-T02 | Prototype implementation complete; Windows x64 execution pending | five isolated projects, narrow ConPTY/Job interop, eleven deterministic fixtures, 100-run plus 1/4/8-session harness, exact process-identity leak checks, owner-crash containment probe, strict evidence schema, dispatch packet, and source validator |
| SP00-T03 | Prototype source implementation complete; Windows x64 execution pending | eight isolated projects; strict .NET/Node framing and message contracts; thirteen shared fixtures; secure `CreateNamedPipeW` transport with live DACL inspection and anonymous denial; bounded queues; cancellation; durable journal/replay; transactional subscriptions; daemon restart/Node reconnect harness; nine scenarios; evidence schema, dispatch packet, source validator, and proof document |
| SP00-T04 | Prototype source implementation complete; Windows x64 host execution pending | one shared TypeScript/xterm.js workspace, WPF/WebView2 and VS Code leaf hosts, strict bridge/CSP boundaries, eight-terminal fixture, 27-cell render matrix, bounded hidden-pane queue, cross-host evidence comparison, source manifest, and fail-closed runner |
| SP00-T05 | Gate review complete; G0 rejected for production promotion and remains blocked | five ADRs, human and machine-readable gate records, evidence-hash validation, and explicit Windows rerun requirements |
| SP01-T01 | Draft implementation | IDs, clock, hash, version, result/problem primitives and deterministic tests |
| SP01-T02 | Partial draft only | terminal reducer only; Request/Task/Attempt/Gate/Interaction/Route/Resource/Breaker reducers remain |
| SP01-T03+ | Not started | intentionally blocked by G0 and task order |

## Validation status in the creation environment

The repository was structurally checked and the TypeScript source was compiled with the available
local Node.js 22.16.0 / TypeScript 5.8.3 toolchain. The SP00-T02 source contract still passes its
host-neutral validator: five isolated projects, 37 C# source files, and eleven canonical scenarios.

The SP00-T03 host-neutral checks also pass:

- eight isolated .NET projects and 49 C# sources;
- thirteen golden contract fixtures and one shared .NET/Node vector document;
- nine canonical Windows scenarios;
- twelve passing Node framing/protocol tests;
- strict source/authority/source-identity checks and production-reference isolation;
- bounded queue, durable replay, transactional subscription, ACL, negative-probe, reconnect, and
  evidence markers; and
- repository verification, TypeScript compilation, UI policy checks, and eighteen UI tests.

The SP00-T04 host-neutral checks also pass:

- one isolated WPF/WebView2 project and one isolated VS Code proof extension;
- 25 source/project files and 48 files covered by the proof source manifest;
- one canonical eight-terminal fixture and 27 benchmark cells across 1/4/8 terminals, three themes,
  and 100/150/200% scaling;
- strict two-way bridge validation, local-only CSP markers, bounded sequence-preserving output queues,
  hidden-pane throttling, host evidence normalization, and cross-host comparison; and
- twelve passing deterministic SharedUiProof tests.

The creation environment did not contain the .NET 10 SDK, PowerShell, a Windows kernel, WebView2, or
installed agent CLIs. It also lacked the pinned Node.js 24.19.0, pnpm 11.20.0, and TypeScript 6.0.3
installation. Consequently, exact pinned JavaScript validation, .NET build/test, PowerShell execution,
ConPTY/Job Object measurements, live named-pipe framing, DACL/anonymous-denial tests, restart/replay,
slow-subscriber measurements, WebView2 and VS Code host execution, xterm.js browser rendering, CSP/runtime
behavior, Windows scaling/accessibility behavior, and one/four/eight-terminal latency/memory measurements
are **not claimed as executed**.

SP00-T02 through SP00-T04 are implemented as isolated source proofs but are not accepted architecture
proofs and do not satisfy G0 until their Windows evidence is reviewed. Detailed T03 and T04 limitations
and host-neutral results are recorded under `docs/validation/`.


## SP00-T05 gate result

The available evidence has now been reviewed. No acceptance-eligible Windows evidence directory was
present for SP00-T02, SP00-T03, or SP00-T04. SP00-T05 therefore records:

- gate decision: `REJECTED_FOR_PROMOTION`;
- gate state: `BLOCKED`;
- production promotion allowed: `false`;
- SP01 dispatch/finalization allowed: `false`; and
- proof constants and provisional limits are not production performance budgets.

The candidate technologies remain in `prototypes/`; the gate decision does not claim that they are
technically disproven. Five ADRs under `docs/adr/` define the evidence needed for a superseding review.
`docs/gates/G0-architecture-proof.json` is the machine-readable result, and
`node build/validate-g0.mjs` verifies its evidence hashes and fail-closed state.

`docs/repository-inventory.json` remains the immutable SP00-T01 bootstrap snapshot; later proof tasks
do not regenerate it because that path is outside their task-local dispatch write sets.

## Next controlled dispatch

1. From a clean, normal-user Windows x64 checkout, validate SP00-T01 and run:
   - `./prototypes/TerminalProof/run-proof.ps1`;
   - `./prototypes/PipeProof/run-proof.ps1`; and
   - `./prototypes/SharedUiProof/run-proof.ps1`.
2. Preserve the complete raw evidence directories and exact source/toolchain identities.
3. Review measurements, STOP conditions, security evidence, accessibility results, and limits in a new
   recorded gate review that supersedes ADR-0001 through ADR-0005.
4. Open G0 and dispatch/finalize SP01 only if every required leaf decision is accepted with measurable
   results. Otherwise amend the rejected leaf while preserving domain/protocol boundaries.

## First Windows-run hotfix

The first owner-run Windows attempt exposed three source/runner defects before architecture evidence could
be produced: a solution-level RID build in TerminalProof, inheritance from sealed `InvalidDataException`
in PipeProof, and PowerShell 7-only assumptions in the SharedUiProof runner. These are corrected in the
post-SP00-T05 hotfix recorded at `docs/validation/sp00-first-windows-run-hotfix.md`.

The reported .NET SDK, pnpm, and VS Code prerequisites match the proof baseline. The reported Node.js
`v26.2.0` does not match the pinned acceptance version `v24.19.0`; the runner continues to fail closed
until the pinned Node version is active. This is a toolchain identity condition, not another source error.
G0 remains blocked until all three corrected proofs produce reviewable acceptance evidence.
The corresponding immutable hotfix receipt is `docs/receipts/SP00-HF01.windows-proof-hotfix.json`.
