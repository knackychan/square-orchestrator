# ADR-0003 — Shared UI hosts: reject production promotion at G0

- Status: `REJECTED_FOR_PROMOTION`
- Date: 2026-08-07
- Task: `SP00-T05`
- Candidates retained: WPF/WebView2 desktop leaf and VS Code extension-host/webview leaf
- Revisit condition: reviewed acceptance-eligible SP00-T04 cross-host Windows evidence

## Context

SP00-T04 implements one shared TypeScript workspace and bridge vocabulary hosted by WPF/WebView2 and
VS Code. Host code is limited to lifecycle, local resource loading, validated transport, and evidence.
It does not own workflow or terminal state. The proof also defines strict CSP/navigation restrictions,
controller-mode messaging, layout states, accessibility labels, and cross-host comparison.

The source boundary and deterministic tests passed in the creation environment, but neither host was
executed on Windows and no qualifying `comparison.json` exists.

## Evidence reviewed

| Evidence | SHA-256 | Result available |
|---|---|---|
| `docs/proofs/shared-ui.md` | `bfad88cd9e4cb5fc23f8e5ca6aa41a4de34337c8b4c81ccb46c11219d79f53ef` | Source/proof contract only |
| `docs/receipts/SP00-T04.prototype-receipt.json` | `e0b9fb7e37b0705cdfb729a461753ea91cb9d71ac5a2b45534c27033e9f9c2fb` | Windows execution pending |
| `docs/validation/sp00-t04-host-neutral-validation.txt` | `7fcad78404d3251a668d9ba7cfeeb8a6cf0e1dbb1b1655ffb9868dd52535b4a6` | Host-neutral PASS |
| `prototypes/SharedUiProof/dispatch.packet.json` | `adf912a15aa1e60783d788bd85faac55eea7f1869058b72b40e9540ae18a6439` | Canonical input |
| `prototypes/SharedUiProof/scenario-manifest.json` | `5fef3b4cf97c2587a461346bc5ecdbdf5fadc0fcf3d0483a1588139b741d6c1f` | Canonical host scenarios |
| Acceptance-eligible cross-host evidence | not present | **Missing** |

## Decision

Do **not** promote the WPF/WebView2 or VS Code proof hosts into production host projects at G0. Keep
both as current leaf candidates because their source organization preserves the intended authority
boundary, but require empirical host evidence before commitment.

The following host-neutral decisions remain mandatory:

- desktop and VS Code consume the same versioned state and command contracts;
- the extension host owns pipe access; a webview never does;
- closing a view releases only subscriptions/controller state;
- host messages are allow-listed and schema validated in both directions;
- terminal output cannot invoke host commands;
- external navigation, unnecessary host objects, downloads, and arbitrary process launch stay denied;
  and
- presentation/layout state is not workflow authority.

## Why promotion is rejected

No reviewed run proves WPF compilation, WebView2 local-origin behavior, VS Code extension/webview CSP,
host reload/closure behavior, theme/scaling/high-contrast behavior, keyboard and screen-reader paths,
memory, render latency, or semantic parity between hosts.

The handover names `square-orchestrator-interactive-reference.html`, but that file was not supplied.
This does not invalidate the written host contract, but it prevents any pixel-parity conclusion.

## Consequences

- SP06-T01 and SP07-T03 remain blocked by G0 and their later dependencies.
- The final docking library and VS Code Pseudoterminal strategy remain parked decisions.
- No host-specific store or alternate workflow model may be introduced to bypass the proof.
- A later decision must supersede this ADR after evidence review.

## Evidence required to reconsider

A normal, non-elevated Windows x64 run of:

```powershell
./prototypes/SharedUiProof/run-proof.ps1
```

must produce acceptance-eligible `webview2.json`, `vscode.json`, and `comparison.json` records bound to
the same fixture, benchmark, source, scenario, and toolchain identities. Both hosts must pass the same
semantic, security, accessibility, and lifecycle checks.
