# Execution Plan Context

Plans are bounded work packets. Root `STATUS.md` must name a plan before any of its tasks may run.
Packet, build guide, task list, and state are separate so recorded progress cannot create authority.

## File map

| Path | Purpose |
|---|---|
| `AGENTS.md`, `CLAUDE.md` | Directory context pair |
| `2026-08-09-fork-agent-orchestrator/` | Active plan: Square Orchestrator as a maintained downstream fork of Agent Orchestrator `v0.12.1` |
| `2026-08-09-fork-agent-orchestrator/square-session-first-implementation-pack/plans/SA00-T02-EA01-*`, `KICKOFF_PROMPT_SA00-T02-EA01.md` | Draft packet, build guide, ordered task list, and kickoff prompt for the SA00-T02 evidence-only amendment; not activated |
| `2026-08-09-fork-agent-orchestrator/square-session-first-implementation-pack/plans/SA00-T02-EA01-FIX-01-*`, `KICKOFF_PROMPT_SA00-T02-EA01-FIX-01.md` | Activated bounded fix packet, build guide, ordered task list, and kickoff prompt for the EA01 evidence-integrity correction |
| `2026-08-09-fork-agent-orchestrator/square-session-first-implementation-pack/plans/SA00-T02-EA01-FIX-02-*`, `KICKOFF_PROMPT_SA00-T02-EA01-FIX-02.md` | Activated bounded fix packet, build guide, ordered task list, and kickoff prompt for the manifest-encoding correction |
