# SP00-T04 proof record — shared terminal UI in WebView2 and VS Code

- Task: `SP00-T04`
- Source status: implemented
- Empirical status: Windows x64 host execution pending
- Architecture gate: not accepted; G0 remains blocked

## Question under proof

Can one host-neutral TypeScript workspace, one xterm.js binding, one fixture/state model, and one strict
message vocabulary render the same terminal-oriented operations workspace inside both WPF/WebView2 and a
VS Code webview without giving either host ownership of terminal or workflow state?

The proof is intentionally isolated under `prototypes/SharedUiProof/`. It does not connect to the daemon,
open ConPTY sessions, persist production layouts, mutate repository state, or define the final docking
library. Every terminal and event is deterministic fixture data.

## Shared implementation boundary

Both hosts load the same compiled files from `dist/`:

- `src/web/main.js` and its TypeScript module graph;
- the xterm.js 6.0.0 terminal renderer and addon-fit 0.11.0 copied from the pinned workspace lock;
- `index.template.html` and `styles.css`;
- the canonical eight-terminal fixture;
- the render benchmark manifest; and
- the same strict bridge protocol.

Host code is limited to local asset loading, validated message transport, evidence writing, and host
lifecycle. The WPF shell and VS Code extension do not contain alternate workspace stores or terminal
rendering rules.

## Canonical fixture and presentation states

`fixtures/canonical-state.json` contains eight stable terminal identities and the written controller modes
`VIEW`, `CONTROL`, and `CONTROLLED ELSEWHERE`. The initial Operations layout shows two terminals. The same
workspace can switch among Operations, Focus Agent, Plan, Review, and Resources without rebuilding fixture
state or changing terminal sequence positions.

The fixture covers running, quiet-active, input, approval, and other attention states with role, route,
task, state, and controller text in accessible labels. It is canonicalized and bound to
`fixtures/canonical-state.sha256` before either host may run the benchmark.

The UI handover refers to `square-orchestrator-interactive-reference.html`, but that HTML was not supplied.
The proof therefore follows the written UI design and handover contracts and does not claim pixel parity
with an unavailable visual reference.

## Terminal rendering and hidden-pane behavior

Each pane owns a bounded presentation queue, not an authoritative terminal buffer. Frames must be positive,
strictly contiguous sequences. Duplicate frames are idempotent; gaps and byte-capacity overflow are explicit
failures rather than silent loss.

Visible panes schedule a write on the next animation frame. Hidden panes retain the same accepted frames but
use the declared slower timer. Revealing a pane cancels the slow timer, schedules visible rendering, and
must catch up to the last accepted sequence. xterm.js write callbacks advance `renderedThroughSequence` only
after the emulator confirms the batch.

The canonical benchmark renders 262,144 bytes per terminal in 8,192-byte frames at one, four, and eight
active terminals. It records duration, bytes, batches, sequence state, and available JavaScript heap data for
every dark/light/high-contrast and 100/150/200-percent matrix cell. Host evidence also records available
process memory. These are observations for SP00-T05 review, not pre-accepted product budgets.

## Controller, input, focus, and accessibility

xterm input is disabled unless the fixture mode is `CONTROL`. Input and resize messages contain only a
terminal ID, proof lease ID, and bounded typed data; the bridge has no arbitrary shell-command operation.
Controller buttons expose textual state and an accessible name. The benchmark verifies keyboard focus moves
between terminals, every terminal region/output/controller has a unique accessible name, xterm screen-reader
mode is enabled, the minimum contrast ratio is configured, and all declared theme/scaling states retain the
same labels.

## Bridge and host security

The TypeScript bridge rejects incompatible versions, unknown message types, unknown fields, missing fields,
and invalid values in both directions.

The VS Code host:

- owns all extension-host access;
- gives the webview only the compiled `dist/` local-resource root;
- uses a nonce-protected import map and module script;
- denies network, object, base, form, and frame sources through CSP;
- allows xterm's generated style attributes without broadly allowing inline stylesheets; and
- never imports process-launch APIs or exposes an arbitrary terminal/shell command.

The WebView2 host:

- maps one local virtual origin with `DenyCors` access;
- rejects navigation away from the generated local proof document;
- blocks new windows, downloads, permissions, host objects, developer tools, and built-in error pages;
- validates every web message in C# before reading it; and
- uses the same CSP/template and compiled assets as the VS Code host.

Closing either host destroys only the fixture view. No process, terminal, lock, or workflow exists for the
host to stop.

## Evidence and fail-closed runner

`run-proof.ps1` requires a normal, non-elevated Windows x64 session with .NET SDK 10.0.302, Node.js 24.19.0,
pnpm 11.20.0, TypeScript 6.0.3, and VS Code. It verifies the repository, proof source manifest, locked
inputs, deterministic tests, and builds before launching either host in isolated temporary user-data
directories.

A qualifying evidence directory contains:

```text
environment.json
webview2.json
vscode.json
comparison.json
evidence-manifest.sha256
```

Manual host runs can produce diagnostic records but cannot independently mark a result acceptance-eligible.
The comparison requires both hosts, identical fixture and benchmark identities, passing shared semantics,
and acceptance-eligible host records. The environment file binds the run to the source, dispatch, scenario,
fixture, benchmark, and exact toolchain versions.

## Provisional conclusion

The source implementation preserves the locked architecture boundary: one shared TypeScript/xterm.js UI can
be hosted by WPF/WebView2 and VS Code while host code remains a validated transport/lifecycle leaf. This is
not yet an empirical architecture conclusion. The creation environment could not execute:

- the pinned TypeScript 6.0.3 build;
- the .NET 10/WPF/WebView2 compilation;
- local WebView2 loading and security behavior;
- a VS Code extension-host/webview run;
- actual xterm.js rendering with restored vendor packages;
- one/four/eight-terminal latency and memory measurements;
- Windows scaling, high-contrast, keyboard, and screen-reader checks; or
- the cross-host evidence comparison.

Until a normal-user Windows x64 run produces reviewed acceptance-eligible evidence, SP00-T04 remains
implemented as source but not accepted. Prototype code must not move into production projects before
SP00-T05 records the architecture decisions and G0 result.
