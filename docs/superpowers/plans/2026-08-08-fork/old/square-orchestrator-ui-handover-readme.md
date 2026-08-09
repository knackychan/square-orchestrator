# Square Orchestrator — Interactive UI Handover

## Included reference

`square-orchestrator-interactive-reference.html` is a self-contained browser reference for the
Windows/VS Code terminal workspace. It contains its styling and interaction code, does not require a
development server, and can be opened directly in a modern browser on Windows.

The reference demonstrates:

- a terminal-first operations workspace;
- docked agent terminals running simultaneously;
- persistent project/request navigation;
- an agent fleet and task-state overview;
- a right-side inspector for task, model, scope, and process state;
- approval and terminal-attention states;
- an event/status strip; and
- Operations, Focus Agent, and Review layout presets.

## How to use it

1. Extract the handover package.
2. Open `square-orchestrator-interactive-reference.html` in Edge, Chrome, or Firefox.
3. Use the three layout buttons in the top toolbar to inspect the workspace modes.
4. Resize the browser to review the responsive behavior.
5. Treat the HTML as an interaction and layout reference, not production application code.

No installation, package manager, build command, local server, or network connection is required for
the reference.

## Product interpretation

| Visible area | Intended production responsibility |
|---|---|
| Top command bar | Submit commands and change presentation layout; calls daemon operations through the host bridge |
| Left navigator | Project, request, plan, and task navigation from authoritative daemon state |
| Agent Fleet | Current attempts, roles, model routes, terminal states, writer ownership, and attention reasons |
| Terminal panes | Bounded views of daemon-owned ConPTY streams; panes do not own or stop processes |
| Terminal interaction state | Durable questions, permissions, authentication, blockers, and recovery actions |
| Right inspector | Selected attempt identity, scope, policy, budgets, health signals, artifacts, and allowed actions |
| Event strip | Sequence-numbered workflow events, daemon connectivity, locks, gates, and resource conditions |
| Layout presets | Presentation-only arrangements; changing a layout never alters orchestration state |

## Non-negotiable implementation rules

1. The daemon owns all workflow, terminal, lock, receipt, and scheduler state.
2. Closing, moving, or hiding a terminal pane must not stop its process.
3. Desktop and VS Code surfaces consume the same versioned RPC/event contracts.
4. Only one client may hold a terminal controller lease at a time.
5. Terminal output is untrusted and cannot invoke host commands through escape sequences.
6. Known terminal states are classified deterministically without a monitoring model.
7. Model exposure, estimated context pressure, and USD-equivalent usage are different indicators.
8. Missing telemetry is displayed as unavailable or degraded, never as zero or healthy.
9. `QUICK` tasks simplify the visible workflow instead of exposing every planning/review pane.
10. Layout persistence is versioned and debounced to reduce unnecessary writes.

## What is conceptual in this reference

- Displayed tasks, model names, commands, timings, token values, and events are illustrative fixtures.
- Buttons only change the local presentation; they are not connected to the Square daemon.
- Drag/drop docking, terminal input, approvals, task submission, and live event streaming are specified
  behaviors but are not implemented in this static reference.
- Exact fonts, dimensions, icons, docking library, and responsive breakpoints remain subject to the
  proof and accessibility tasks in the implementation plan.

## Production source mapping

| Reference behavior | Planned code area |
|---|---|
| Shared tokens and components | `ui/packages/design-system/` |
| Workspace and dock layouts | `ui/packages/workspace/` |
| xterm.js binding | `ui/packages/terminal/` |
| Host-neutral message contract | `ui/packages/host-contract/` |
| Windows shell and WebView2 bridge | `src/Square.Desktop/` |
| VS Code host and webviews | `vscode/square-vscode/` |
| Authoritative state and commands | `src/Square.Daemon/` and `src/Square.Application/` |
| Terminal stream/controller lease | `src/Square.ControlPlane/` and `src/Square.Platform.Windows/` |

## Companion specifications

- `square-orchestrator-ui-design.md` defines the panes, layouts, flows, visual rules, accessibility,
  responsive behavior, and UI acceptance criteria.
- `square-orchestrator-technical-architecture.md` defines the Windows processes, IPC, ConPTY hosting,
  persistence, security boundaries, desktop shell, and VS Code integration.
- `square-orchestrator-sliced-implementation-plan.md` defines the implementation order and the tasks
  `SP05`, `SP06`, and `SP07` that turn this reference into working UI code.

## Recommended handover instruction

Give the receiving agent the HTML, this README, and the three companion specifications. Instruct it to
implement only the assigned task ID, preserve the non-negotiable rules above, and stop when the task
requires a decision parked in the implementation plan. The HTML may guide placement and interaction,
but the written contracts take precedence whenever presentation and system behavior appear to conflict.

## Acceptance checklist for the eventual implementation

- All active agents expose role, model route, task, state, and writer/read-only status.
- Questions, approvals, authentication, blockers, and failures appear within one authoritative event.
- Four simultaneous terminals remain readable and responsive in the Operations layout.
- Focus Agent and Review layouts preserve the same selected task and terminal state.
- Keyboard-only users can reach docks, terminal controls, approvals, and inspector actions.
- High contrast and 200% Windows text scaling remain usable.
- Closing and reopening either host restores layout and state without duplicating a terminal process.
- High-volume terminal output is bounded, sequenced, and backpressured.
