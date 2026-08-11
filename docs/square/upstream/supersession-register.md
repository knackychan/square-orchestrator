# Square supersession register

- Task: SA00-T04
- Accepted at: `2026-08-11T02:52:52Z`
- Governing ADR: `docs/square/adr/ADR-SA00-004-session-first-ao-foundation.md`

This register prevents historical planning from silently controlling new Square work.
Historical material remains in the repository for research and provenance.

## Preserved behavioral research

The following principles remain useful and are preserved as research inputs:

- The daemon owns work.
- Closing the UI is harmless to running work.
- Terminal output is untrusted evidence, not state authority.
- Lifecycle classification is deterministic.
- Interactions are typed.
- Controller and writer authority is explicit.
- Model sessions are bounded.
- Acceptance, evidence, and memory principles remain first-class.
- The original docked-terminal design language informs the accepted rounded session UI.

## Retired production implementation decisions

The following decisions are superseded for Square production:

| Retired line | Disposition |
|---|---|
| Custom C#/.NET daemon | Superseded by the AO Go daemon and service boundaries. |
| WPF/WebView2 shell | Superseded by the AO Electron supervisor. |
| Custom named-pipe RPC as the immediate production path | Superseded by AO REST/SSE/WebSocket for the MVP. |
| Custom .NET SQLite layer | Superseded by AO SQLite storage and service boundaries. |
| Custom ConPTY stack as the application foundation | Superseded by AO runtime/terminal adapters. |
| Global fleet/resource dashboard as the primary UX | Superseded by the Square Session as the top-level object. |

## Current production direction

Square production follows:

- AO Go/Electron/SQLite/ConPTY/worktree platform.
- Square Session as the top-level object and outcome boundary.
- Deterministic Task Manager inside each session.
- Secretary, Scout, Planner, Orchestrator, Worker, and Reviewer model roles inside each
  session, with bounded runs ending at artifacts or blockers.
- Dynamic docked role histories.
- Per-role model routing with requested/resolved/actual identity separation.
- Explicit Square identity and isolation: separate `ao`/`square` commands, Square data and
  runtime paths, telemetry and crash reporting off by default, updater disabled until SA14,
  and authenticated LAN listener only when explicitly enabled on reserved port `3111`.

## Known historical sources

The repository's pre-pivot design material remains under the historical planning and archive
areas, including the dated `docs/plans/` documents and the preserved implementation history.
The source pack also records the research lineage in `docs/ARCHIVE_RESEARCH_REUSE.md` and
`docs/PREVIOUS_COMBINED_PLAN_SUPERSEDED.md`; those documents are not imported as production
authority. The accepted authority files under `docs/square/authority/` take precedence for
undispatched work.

## Amendment rule

An implementation worker must stop on a contradiction between accepted authority files. The
owner must accept an amendment or versioned replacement before implementation resumes. No
historical document may be deleted merely because it is superseded.
