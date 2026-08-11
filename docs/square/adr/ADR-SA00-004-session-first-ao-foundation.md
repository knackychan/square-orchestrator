# ADR-SA00-004: Session-first AO foundation

- Status: accepted
- Date: 2026-08-11
- Task: SA00-T04
- Gate: A0
- Accepted at: `2026-08-11T02:52:52Z`
- Base: Agent Orchestrator `v0.12.1`

## Decision

Square adopts the accepted session-first implementation authority imported under
`docs/square/authority/`. The maintained downstream fork keeps upstream history and
provenance while making the Square Session the top-level product object.

The authority hierarchy is:

1. This ADR and later accepted Square ADRs control fork-level architecture and policy.
2. `00-architecture-amendment.md` controls the session-first architectural pivot.
3. `01-master-session-first-plan.md` controls sequencing and task scope.
4. The numbered domain, routing, persistence, API, governance, and release documents
   control their respective behavior and security contracts.
5. The HTML reference is visual direction only; written contracts override it.
6. Schemas and fixtures are draft contract material until promoted by a later accepted
   contract task.

Implementation workers may not amend accepted authority files. Amendments require a new
owner decision, a new ADR or versioned authority file, and a new hash manifest.

## Accepted A0 architecture

- AO `v0.12.1` is the pinned base.
- Square is a maintained downstream fork with preserved history and upstream provenance.
- The product is session-first.
- Task Manager is deterministic Go software, not a model.
- Secretary, Scout, Planner, Orchestrator, Worker, and Reviewer are bounded model roles.
- QUICK is delivered before PLANNED.
- Routing is per role.
- Requested, resolved, and actual model identity remain separate.
- The rounded session-first UI with docked role histories is the approved visual direction.
- AO REST/SSE/WebSocket transport is retained for the MVP.
- Windows x64 is first.

## Accepted Square identity and isolation policy

- Product name: Square Orchestrator.
- Short name and primary CLI: Square / `square`.
- `ao` and `square` remain separate commands; there is no AO compatibility alias.
- Electron/application ID: `dev.square-orchestrator.desktop`.
- Telemetry and crash reporting are disabled by default.
- Any Square PostHog project/key is deferred until explicit owner opt-in.
- The updater is disabled until SA14.
- macOS signing identity and Windows signing certificate must be procured before SA14.
- Version format: `<upstream-version>-square.<n>`.
- No npm publication initially.
- LAN/mobile listener is disabled by default. If explicitly enabled, the authenticated
  Square LAN listener reserves port `3111`.
- Trademark clearance is a required pre-release/legal gate.
- The frontend license is Apache-2.0, consistent with the governing upstream license and
  attribution requirements.

## Consequences

The imported authority is the controlling source for undispatched Square work. The prior
.NET/WPF/global-dashboard direction remains historical research and is not silently deleted
or allowed to control implementation. Product code is not changed by SA00-T04; enforcement
of identity, isolation, telemetry, updater, licensing, and listener decisions belongs to later
implementation and release tasks.

## Acceptance evidence

The owner acceptance wording is recorded verbatim in:

`docs/square/evidence/SA00-T04/20260811T025726Z/owner-decision.json`

The imported bytes and their source-pack provenance are recorded in
`docs/square/authority/AUTHORITY_INDEX.json` and
`docs/square/authority/AUTHORITY_MANIFEST.sha256`.
