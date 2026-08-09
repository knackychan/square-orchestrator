# Square — Rounded Session Workspace Reference

## Purpose

This revision keeps the session-first product model intact while applying the visual language of the attached original Square reference.

The global application remains simple:

- project selector;
- persistent session list;
- open session tabs;
- New Session;
- daemon and global attention status.

Each session still owns its own:

- conversation and original task;
- Task Manager, Planner, Orchestrator, workers, and reviewer;
- docked process terminals;
- attention/decision state;
- plan and acceptance criteria;
- durable history.

## Visual changes

- Default light theme with a white canvas.
- Soft neutral panels derived from the original reference palette.
- Blue selected-session and active-state treatment.
- Black primary action buttons in light mode and white primary buttons in dark mode.
- 16px rounding for the main sidebar, workspace, docks, and content cards.
- 10px rounding for buttons, inputs, tabs, terminal wells, and message blocks.
- Pill-shaped state labels, workflow stages, and filters.
- White terminal wells inside pale gray dock panels in light mode.
- Matching near-black, white, gray, and pale-blue dark theme.
- The full-width decision strip is contained as a rounded session-attention panel.

## Behavior retained

- Switch between durable sessions.
- Open multiple sessions as tabs.
- Create a new session from a chat-style task.
- View Workbench, Conversation, Plan & Review, and History.
- Inspect session-specific docked agent processes.
- Resolve a session decision inline.
- Focus an individual terminal.
- Toggle light/dark themes.
- Closing or changing presentation does not represent stopping daemon-owned work.

## Files

- `square-session-workspace-rounded-reference.html` — self-contained interactive reference.
- `square-session-workspace-rounded-reference-validation.txt` — structural and interaction validation.
- `previews/rounded-light.png` — default light appearance.
- `previews/rounded-dark.png` — dark appearance.
- `previews/rounded-plan-light.png` — session Plan & Review appearance.
- `square-session-workspace-rounded-reference.html.sha256` — HTML integrity checksum.

## Usage

Open the HTML directly in a modern browser. No server, package manager, build step, font download, or network connection is required.

The file is an interaction and visual reference, not production Electron/React code.
