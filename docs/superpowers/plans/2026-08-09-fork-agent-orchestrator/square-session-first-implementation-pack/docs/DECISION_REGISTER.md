# Square session-first decision register

- Status: planning register; owner acceptance required
- Updated: 2026-08-09

## Accepted direction represented by this pack

| Decision | Current value |
|---|---|
| Platform foundation | Maintained downstream fork of Agent Orchestrator |
| Initial pin | `v0.12.1`, commit prefix `1df40e9` pending local full-hash verification |
| Target | Windows x64, per-user local application |
| Top-level UX object | Square Session about one topic/outcome |
| Session contents | conversation, workflow, role runs, docked terminal/Chat history, attention, plan, review, history, receipt |
| Workflow owner | deterministic Go Task Manager |
| Model roles | bounded Secretary, Scout, Planner, Orchestrator/Synthesizer, Worker, Reviewer, optional Triage |
| Route modes | AUTO, PREFERRED, PINNED |
| Route precedence | task → session → project → global → automatic default |
| Identity rule | requested, resolved, and actual route/model stored separately |
| Fallback rule | no silent fallback for PINNED; approved-list-only for PREFERRED |
| Review rule | independence is explicit policy; no silent downgrade |
| First workflow | QUICK before PLANNED |
| Desktop direction | approved rounded session-focused UI reference |
| Local transport | retain AO REST/SSE/WebSocket for MVP; harden later |
| Telemetry | disabled by default |
| Updater | official AO updater disabled; Square feed postponed to release work |
| Previous .NET code | archived research, not current production implementation |

## Decisions deliberately postponed

| Decision | Earliest task |
|---|---|
| owner Git hosting/origin URL | SA00-T01 input |
| final Windows app ID, publisher, installer GUIDs, signing identity | SA14-T04 |
| Square update feed and channel policy | SA14-T05 |
| loopback API install token/authentication | SA14-T02 |
| exact first real adapter/model route | SA07-T07 after SA04 certification |
| default role routes/models | SA12-T01 after route discovery |
| exact model names in UI | runtime route registry, never hard-coded as authority |
| project/global memory retention defaults | SA09 |
| terminal-history retention | SA03-T05/SA13-T05 |
| VS Code inclusion in MVP vs post-MVP | SA12-T05 owner review |
| advanced docking library | only when basic session docks prove insufficient |
| cost/exposure formulas | SA13 |
| resource thresholds | SA13 measured evidence |
| parallel writers | SA15, disabled by default |

## Decision rule

An implementation agent may not resolve a postponed item because it is convenient. It must stop or use a task-packet-approved conservative prototype value that is visibly non-production.
