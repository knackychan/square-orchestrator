# SP00-T04 — Shared terminal UI proof

This isolated prototype loads the **same compiled TypeScript workspace, xterm.js renderer, bridge protocol,
canonical fixture, and benchmark code** in two leaf hosts:

- WPF with WebView2 under `webview2-host/`;
- a VS Code extension webview under `src/vscode/`.

It does not own terminals, processes, workflows, locks, or daemon state. Terminal bytes, input, resize, layout,
and controller requests are fixture messages only.

## What the proof exercises

- two visible xterm.js panes in the initial Operations workspace;
- one, four, and eight active terminal render measurements;
- deterministic ANSI, Unicode, and high-volume output;
- Operations, Focus Agent, Plan, Review, and Resources layout changes;
- keyboard focus movement and explicit `VIEW`, `CONTROL`, and `CONTROLLED ELSEWHERE` labels;
- dark, light, high-contrast, 100%, 150%, and 200% states;
- accessible region/output/controller names with xterm screen-reader mode enabled;
- bounded output queues and slower hidden-pane presentation without sequence loss;
- strict unknown-type/unknown-field rejection in TypeScript and C#;
- local-only WebView2 navigation and a nonce-protected VS Code webview CSP; and
- fail-closed evidence: manually launching either host can report a diagnostic result, but only the verified
  runner may mark evidence acceptance-eligible.

The handover refers to `square-orchestrator-interactive-reference.html`, but that HTML was not supplied with
the authority documents. The canonical fixture therefore follows the written UI design and handover rules;
it does not claim pixel parity with the unavailable reference.

## Host-neutral validation

```text
node prototypes/SharedUiProof/scripts/build.mjs --skip-vendor
node --test prototypes/SharedUiProof/test/*.test.mjs
node prototypes/SharedUiProof/scripts/source-manifest.mjs
node prototypes/SharedUiProof/validate-source.mjs
```

The host-neutral build intentionally omits npm vendor bytes. It verifies TypeScript compilation, fixtures,
protocol behavior, queue semantics, host source boundaries, package pins, the complete source manifest, and
proof isolation. Regenerate the source manifest only after reviewing intended proof changes:

```text
node prototypes/SharedUiProof/scripts/source-manifest.mjs --write
```

## Full Windows proof

From a clean, non-elevated Windows x64 checkout with the pinned toolchain and VS Code installed:

```powershell
./prototypes/SharedUiProof/run-proof.ps1
```

The runner requires .NET SDK 10.0.302, Node.js 24.19.0, pnpm 11.20.0, and TypeScript 6.0.3. It restores the
pinned xterm packages, verifies repository and proof source identity, builds the shared assets and WebView2
host, launches both hosts against isolated temporary user-data directories, records host evidence, compares
semantic results, and removes temporary browser/editor state. A PASS requires both hosts; one-host or manual
host evidence is diagnostic only.

## Evidence

A qualifying run writes:

```text
evidence/run-<timestamp>/
  environment.json
  webview2.json
  vscode.json
  comparison.json
  evidence-manifest.sha256
```

The environment record binds the run to the source, dispatch, scenario, fixture, benchmark, toolchain, and
VS Code version. Host records include render durations, browser/extension memory readings, bridge-event counts,
fixture identity, and the shared semantic result. Render measurements are observations, not yet product
performance budgets. SP00-T05 reviews the raw evidence and records accepted thresholds or rejects the leaf
choice.

## Recovery and ownership boundary

Closing either proof host merely destroys its fixture view. No external agent process exists and no workflow
state is mutated. Production terminal continuation and controller leases remain daemon-owned contracts; this
proof only verifies that both UI hosts can present the same state and validated command vocabulary.
