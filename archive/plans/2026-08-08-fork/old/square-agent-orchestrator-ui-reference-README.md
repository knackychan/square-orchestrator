# Square on Agent Orchestrator — Interactive UI Reference

## Included file

`square-agent-orchestrator-interactive-reference.html` is a self-contained browser reference for the revised Square product built on the Agent Orchestrator platform layer.

Open it directly in Edge, Chrome, or Firefox. It requires no package manager, local server, installation, or network connection.

## What the reference demonstrates

### Reused Agent Orchestrator platform surfaces

- project/sidebar navigation;
- AO sessions and controller modes;
- Git worktrees;
- selected Chat/terminal/diff/evidence views;
- PR and review-run areas;
- terminal/session lifecycle visibility;
- Electron-style desktop shell and inspector rail.

### Added Square product surfaces

- global project-bound request composer;
- AUTO, QUICK, and PLANNED workflow preview;
- FAST, NORMAL, and SLOW resource profiles;
- Requests, Tasks, Approvals, and Memory navigation;
- Dispatch Preview with stages, route, scope, validation, memory, and gates;
- deterministic Square roles mapped to AO sessions/worktrees;
- task and acceptance contracts;
- typed terminal interaction bar;
- Plan, Review, Resources, Operations, and Focus presets;
- project/global/candidate memory with explicit owner promotion;
- semantic event timeline separated from AO CDC/lifecycle observations;
- model exposure, cost, context, and machine-resource indicators kept distinct.

## Interactive controls

- Use the top preset buttons to switch between Operations, Focus, Plan, Review, and Resources.
- Use the left navigation to inspect Requests, Tasks, Approvals, Memory, AO Sessions, Worktrees, Pull Requests, Review Runs, and Events.
- Select Terminal, Chat, Diff, or Evidence on the active session.
- Change FAST/NORMAL/SLOW to see the resource-profile state update.
- Cycle dark, light, and high-contrast themes with the half-circle button.
- Edit the request field and choose Preview to open the Dispatch Preview.
- Approve or deny the displayed typed dependency interaction.

## Product interpretation

- **Square owns workflow semantics:** requests, profiles, task contracts, acceptance, memory, evidence, policy, and deterministic scheduling.
- **Agent Orchestrator supplies the execution substrate:** sessions, Chat/TUI controllers, worktrees, terminals, adapters, PR observation, and reviewer processes.
- **The daemon remains authoritative:** the renderer never owns workflow state or mutates SQLite directly.
- **Model sessions are bounded workers:** they end at artifact boundaries rather than staying alive to monitor other agents.
- **UI closure is harmless:** closing or hiding presentation surfaces must not stop or duplicate daemon-owned work.

## Conceptual limitations

This HTML is an interaction and layout reference, not production code:

- all records, metrics, terminal lines, commits, routes, and events are fixtures;
- no real AO or Square daemon is connected;
- buttons modify local presentation only;
- terminal content is simulated rather than xterm.js;
- live SSE/WebSocket state, generated API contracts, drag docking, controller leases, and persistence are not implemented here;
- exact spacing, component library, responsive breakpoints, and final product identity remain implementation decisions inside the AO fork.
