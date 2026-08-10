# G0 architecture proof review — SP00-T05

- Review date: 2026-08-07
- Gate: `G0 — Architecture proof`
- Decision: `REJECTED_FOR_PROMOTION`
- Gate state: `BLOCKED`
- Production promotion allowed: **No**
- SP01 dispatch/finalization allowed: **No**

## Decision summary

SP00-T02, SP00-T03, and SP00-T04 are source-implemented and have useful host-neutral validation, but
none has a reviewed acceptance-eligible Windows x64 result. The locked leaf stack therefore cannot
enter production implementation at this gate.

This is not a claim that ConPTY, Job Objects, named pipes, WPF/WebView2, VS Code webviews, or xterm.js
have failed technically. It is a fail-closed decision that the evidence required by the implementation
plan is absent. The candidates remain in `prototypes/` for the canonical Windows runs.

## Reviewed evidence matrix

| Proof | Source implementation | Host-neutral result | Acceptance evidence found | G0 decision |
|---|---|---|---|---|
| SP00-T02 — ConPTY/Job Object | Complete | PASS | No | Reject production promotion |
| SP00-T03 — named-pipe framing/reconnect | Complete | PASS | No | Reject production promotion |
| SP00-T04 — shared UI hosts/renderer | Complete | PASS | No | Reject production promotion |

No `summary.json` or cross-host `comparison.json` representing a canonical acceptance-eligible Windows
run was found under the proof evidence locations. The source receipts themselves say Windows execution
is pending.

## Architecture decisions

| ADR | Area | Status |
|---|---|---|
| [ADR-0001](../adr/ADR-0001-terminal-hosting.md) | Terminal hosting | `REJECTED_FOR_PROMOTION` |
| [ADR-0002](../adr/ADR-0002-local-ipc.md) | Local IPC | `REJECTED_FOR_PROMOTION` |
| [ADR-0003](../adr/ADR-0003-shared-ui-hosts.md) | WPF/WebView2 and VS Code hosts | `REJECTED_FOR_PROMOTION` |
| [ADR-0004](../adr/ADR-0004-terminal-renderer.md) | xterm.js renderer | `REJECTED_FOR_PROMOTION` |
| [ADR-0005](../adr/ADR-0005-benchmark-thresholds.md) | Benchmark thresholds | `REJECTED_FOR_PROMOTION` |

The machine-readable record is `docs/gates/G0-architecture-proof.json` and is checked by
`node build/validate-g0.mjs`.

## Gate consequences

1. No prototype code may be promoted or referenced by production projects.
2. Existing draft SP01 code remains non-authoritative and may not be frozen or dispatched as accepted
   production work.
3. No missing Windows metric may be represented as zero, healthy, or passing.
4. No proof limit may be raised after a failed run without a recorded amendment and fresh canonical run.
5. All existing prototype source, manifests, receipts, and validation records are retained.

## Required next evidence

Run all three proofs from a clean, normal-user Windows x64 checkout using their pinned toolchains:

```powershell
./prototypes/TerminalProof/run-proof.ps1
./prototypes/PipeProof/run-proof.ps1
./prototypes/SharedUiProof/run-proof.ps1
```

After complete evidence is available, perform a new recorded architecture review that supersedes the
five ADRs. Only accepted decisions and measurable results may open G0 and unblock SP01.
