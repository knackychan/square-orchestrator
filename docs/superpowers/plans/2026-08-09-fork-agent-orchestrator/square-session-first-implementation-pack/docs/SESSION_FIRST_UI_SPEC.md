# Session-First Desktop UI Implementation Specification

- Status: approved direction for SA06 and later refinement
- Visual reference: `ui/square-session-workspace-rounded-reference.html`
- Foundation: AO Electron/React renderer, existing session/terminal/Chat/diff/review capabilities

## 1. Experience outcome

The user thinks in topics and outcomes, not in a global fleet of processes.

Each open tab is one Square Session. The tab preserves the complete context:

- user conversation;
- current workflow and stage;
- Task Manager timeline;
- dynamically created role docks;
- terminal/Chat/diff/evidence history;
- direct problem/solution/decision strip;
- plan and acceptance;
- review and findings;
- final receipt and result.

Switching sessions changes the visible workbench only. Background role runs continue.

## 2. Global shell

Keep the global level deliberately small:

### Top/app chrome

- product/project selector;
- active session tabs;
- New Session action;
- daemon connection/health;
- global attention count and notifications;
- theme/settings/user menu.

### Left session navigator

- search;
- filters: `All`, `Needs You`, `Active`, `Done`, `Archived`;
- session title/project/status;
- concise attention reason;
- active stage/role count;
- latest meaningful activity;
- no raw terminal log preview.

No permanent global Fleet, Plan, Resource, Approval, and Inspector maze is required for MVP. Cross-session operational views may be added later as secondary tools.

## 3. New Session

Default composer:

```text
Describe the result you want…
Project
Workflow: Auto
Quality: Balanced
Start session
```

Expandable advanced area:

- QUICK/PLANNED explicit profile;
- result mode: local/PR/auto;
- role route setup;
- trust/network/write constraints;
- resource profile;
- owner approval preferences.

Submitting creates a durable draft session/message first. Starting execution is a separate acknowledged command if policy requires preview approval.

## 4. Session workbench

### Session header

- title and stable session ID;
- project/repository;
- status and attention;
- workflow profile/stage;
- pause/resume/cancel/more actions;
- route/resource preset summary.

### Primary tabs

```text
Workbench
Conversation
Plan & Review
History
```

Tabs do not own or stop processes.

### Workbench

Contains:

1. compact conversation/task summary;
2. workflow progression strip;
3. direct attention strip, only when needed;
4. dynamic dock canvas of Task Manager/role runs;
5. compact composer for session follow-up or interaction answer.

## 5. Workflow progression strip

Display only stages relevant to the selected workflow:

QUICK example:

```text
Intake → Dispatch → Worker → Validate → Done
```

PLANNED example:

```text
Intake → Context → Plan → Approval → Implement → Verify → Review → Done
```

Stage has text/icon/state and concise blocker. It is navigational, not a progress promise.

## 6. Task Manager dock

Task Manager is not a terminal/model. It displays a structured software timeline:

- last decision/event;
- current stage and why;
- ready/waiting dependencies;
- role runs launched/completed;
- policy/route decision summaries;
- next deterministic action;
- pending owner decision;
- restart/reconciliation facts.

It may look terminal-like for consistency but must not imply an LLM process/model/token usage.

## 7. Role docks

Only instantiated roles are shown. Each dock header includes:

- role and concise task;
- state/elapsed;
- actual harness/model and permission;
- writer/read-only;
- AO session/worktree/branch or reviewer binding;
- attention indicator;
- controls: focus, terminal/Chat, diff/evidence, attach/control, checkpoint/cancel when authorized.

Dock body tabs depend on capability:

```text
Terminal
Chat
Diff
Evidence
```

Completed docks remain visible in History and optionally collapsed in Workbench.

### Dynamic hierarchy

PLANNED may display:

```text
Task Manager
├─ Secretary
├─ Scouts (tab group or row)
├─ Planner
├─ Orchestrator/Synthesizer
├─ Workers (split/tab group)
└─ Reviewer
```

Hierarchy is a visual relationship within the session; Task Manager remains the workflow authority.

## 8. Attention strip

When user action is required, show one direct card/strip near the top of the session:

- exact problem;
- why it matters;
- proposed solution or options;
- affected role/task;
- safe default/expiry;
- direct authorized actions.

Examples:

```text
Approval required: allow one dependency addition
Problem: requested model route is unavailable
Suggested solution: use approved fallback route
Blocker: plan changes a public API outside accepted scope
Auth required: attach manually to provider CLI
```

Do not require opening Inspector → Task → Gate → Approval pages for ordinary decisions. Deeper evidence remains expandable.

## 9. Conversation

Conversation is the durable Square conversation, not the provider transcript.

Show:

- user messages;
- Task Manager summaries;
- role artifacts as compact linked messages;
- questions/decisions;
- result/receipt;
- scope changes and supersession.

Raw role terminal/Chat history remains in the role dock/history.

## 10. Plan & Review

This tab may contain sub-tabs:

```text
Task Brief
Plan
Acceptance
Diff
Findings
Evidence
```

- Plan/task DAG and decisions;
- immutable versions/hashes;
- exact acceptance criteria/status/evidence;
- changed files/diff;
- review findings and fix links;
- integration result.

It is not permanently visible for QUICK unless relevant.

## 11. History

Chronological session history:

- workflow runs;
- role attempts;
- route/model changes;
- terminals and Chat transcripts/chunks;
- decisions/interactions;
- artifacts/receipts;
- Git/worktree/PR state;
- restart/recovery events.

Allow filtering by workflow run, role, task, event type, and outcome.

## 12. Role setup UI

Available in New Session, Dispatch Preview, and session settings.

Each role supports:

```text
Auto
Preferred route list
Pinned route
```

Show certification/availability and actual-model verification support. Recommended presets:

- Project default;
- Economy;
- Balanced;
- Quality;
- Manual.

A pinned unavailable/unverified route shows a blocker before launch. A preferred fallback is explicit.

## 13. Dock/layout behavior

MVP may use AO's existing resizable panel/tab primitives rather than introducing a full generic docking framework.

Required:

- multiple simultaneous role docks;
- split/tab/focus/collapse;
- minimum readable sizes;
- keyboard focus/navigation;
- per-session logical layout persistence;
- layout schema version;
- missing dock compatibility placeholder;
- closing/hiding a dock releases only view/controller subscription;
- completed histories reopen without starting a process.

A full drag/drop docking library is a later measured decision.

## 14. Terminal and controller safety

- xterm.js renders untrusted terminal output;
- output cannot invoke Electron preload/host commands;
- one controller authority at a time;
- visible `VIEW`, `CONTROL`, or `CONTROLLED ELSEWHERE`;
- terminal keystrokes only when focused and controlling;
- input/answer/approval/cancel/checkpoint are distinct daemon commands;
- hidden panes throttle rendering but preserve sequence/state;
- truncation/gaps explicit;
- search/copy available;
- screen-reader mode supported.

## 15. Visual system

Follow the rounded reference:

- light-first neutral canvas;
- subtle gray surfaces;
- blue selection/focus/accent;
- black primary actions in light mode;
- near-black dark mode with restrained neutral surfaces;
- 10–16px rounding depending on component level;
- system/Geist-style UI typography and monospace for IDs/terminals;
- restrained shadows and borders;
- no gradients/glow/AI-dashboard hero cards;
- status includes text/icon/shape, never color alone.

## 16. Frontend source placement

Expected feature isolation:

```text
frontend/src/renderer/features/square/
  sessions/
  conversation/
  workflow/
  role-docks/
  attention/
  plan-review/
  history/
  routing/
  memory/
  shared/
```

Reuse existing AO components where appropriate, but avoid mixing Square workflow state into generic AO session-board components unless the abstraction is truly shared.

State/data:

- generated OpenAPI types;
- TanStack Query for server state/invalidation;
- local Zustand/component state only for presentation and unsaved editor state;
- session layout persisted through daemon API, not arbitrary renderer-only storage for authoritative settings.

## 17. Accessibility

- complete keyboard path for session switching, composer, docks, decisions, terminal control, Plan/Review, and History;
- screen-reader labels include session, role, task, model, state, and attention;
- important transitions in restrained live regions;
- terminal output not continuously announced by default;
- 200% zoom/Windows scaling;
- high contrast and reduced motion;
- deliberate focus after interaction resolution/dock closure;
- destructive actions repeat exact scope.

## 18. Performance

- virtualize long session lists, message history, event history, findings, and terminal chunk indexes;
- batch terminal frames;
- hidden docks reduce render frequency;
- do not mirror entire terminal transcript in React state;
- fetch artifacts/history on demand;
- debounce layout persistence;
- session switch should reuse cached read models and active terminal connections safely;
- reconnect uses SSE sequence and terminal stream sequence.

## 19. Fixture states

Required frontend fixtures:

- blank/new session;
- QUICK running;
- PLANNED with multiple roles;
- needs approval;
- auth takeover;
- route unavailable/fallback;
- blocked plan;
- quiet active;
- suspected stall;
- review finding/fix;
- completed with retained terminals;
- daemon reconnect/reconciliation;
- unknown future role/dock compatibility.

## 20. UI acceptance criteria

1. User can identify selected session outcome, stage, active roles, attention, and next action without more than one navigation action.
2. Switching sessions does not stop, pause, attach, detach, or duplicate role runs.
3. QUICK shows no empty Planner/Reviewer docks.
4. PLANNED can display at least four useful simultaneous role docks.
5. Each role shows role, task, actual model/harness, state, permission, and writer/worktree status.
6. Attention appears directly inside the affected session within one authoritative update.
7. Completed role terminal history remains available.
8. Pinned-route failure never appears as a silent different model.
9. Closing desktop and reopening restores the same sessions/layout/history without duplicate processes.
10. Keyboard, high contrast, 200% scaling, and bounded terminal output remain usable.
