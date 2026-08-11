# Role Routing and Model Selection Contract

- Status: contract draft for SA02/SA04/SA12
- Principle: users may choose the model/agent route per role, but Square persists and certifies a complete route rather than assuming a model name is sufficient

## 1. Route definition

A `Route` is the executable path through which a bounded role run is launched:

```text
Agent harness/adapter
+ provider/account boundary
+ requested model or agent mode
+ actual observed model identity
+ interface mode (Chat/terminal/reviewer)
+ permission mode
+ runtime/workspace capabilities
+ adapter and executable version/hash
+ certification state
```

Examples are illustrative only:

```text
claude-code / planning-model / structured-chat / read-only
codex / coding-model / terminal / writer
opencode / fast-read / terminal / read-only
reviewer harness / independent model family / read-only
```

## 2. Role kinds

Initial role vocabulary:

```text
SECRETARY
SCOUT
PLANNER
ORCHESTRATOR
WORKER
REVIEWER
TERMINAL_TRIAGE
```

Optional specialist tags do not create new authority semantics. A worker may have specialist `UI_ENGINEERING`; a reviewer may have specialist `SECURITY_REVIEW`.

Task Manager is excluded because it is deterministic software.

## 3. Selection modes

### AUTO

Square chooses the highest-ranked eligible certified route and records all candidates and rejection reasons.

### PREFERRED

Square tries an ordered allow-list. It may fall back only within that list and only when policy permits. Every fallback is visible and durable.

### PINNED

Square must use the exact certified route. Missing, unhealthy, uncertified, permission-incompatible, or unverifiable route blocks the role run. No silent fallback.

## 4. Configuration scopes and precedence

Resolution order:

```text
1. Task/role-run override
2. Current Square Session override
3. Project role profile
4. Global role profile
5. Automatic routing policy/defaults
```

Hard constraints override every preference:

- route not installed or executable changed;
- authentication unavailable;
- adapter/model override unsupported;
- trust/network/path policy mismatch;
- required Chat/terminal/review capability absent;
- reviewer-independence violation;
- context/output/tool budget incompatibility;
- quarantine/circuit breaker;
- concurrency/resource capacity unavailable;
- required actual-model verification unavailable for a pinned route.

## 5. RoleRoutingProfile

Required fields:

- stable ID/version/scope;
- role kind;
- selection mode;
- ordered route IDs or required route class;
- allowed harnesses/providers/model families;
- required capabilities;
- permission mode;
- read/write/network authority;
- context/input/output/tool budget;
- timeout and handover policy;
- fallback policy;
- independence policy;
- usage/cost/resource preferences;
- created/updated/provenance;
- enabled/deprecated state.

Example:

```yaml
version: square.role-routing/v1
scope: project
roles:
  secretary:
    selection: auto
    routeClass: economical-reasoning
    permissions: read-only

  scout:
    selection: preferred
    routes:
      - codex/fast-read
      - claude-code/light-read
    permissions: read-only

  planner:
    selection: pinned
    route: claude-code/strong-planning
    permissions: read-only
    contextBudget: 120000

  orchestrator:
    selection: preferred
    routes:
      - claude-code/strong-synthesis
      - codex/high-reasoning
    permissions: read-only

  worker:
    selection: auto
    routeClass: coding-writer
    permissions: writer

  reviewer:
    selection: auto
    routeClass: independent-review
    permissions: read-only
    requireDifferentModelFamilyFrom:
      - worker
```

## 6. Route certification

A route is not eligible merely because AO can launch its adapter.

Certification record includes:

- adapter/harness ID and exact version;
- executable path/hash/version;
- provider/account boundary identifier without secrets;
- supported requested model syntax;
- actual-model observability level;
- permission modes and actual behavior;
- terminal/Chat/reviewer modes;
- worktree compatibility;
- resume/checkpoint/cancel support;
- prompt/approval/auth signatures;
- structured event or activity support;
- completion artifact/receipt support;
- context/token/usage telemetry support;
- Windows path/Unicode/space behavior;
- certification fixture hashes and date;
- health/quarantine state;
- supported Square roles.

Certification states:

```text
DISCOVERED
PROBING
CERTIFIED
DEGRADED
QUARANTINED
UNAVAILABLE
EXPIRED
```

## 7. Requested versus resolved versus actual

Every role run persists:

```text
requested_selection_mode
requested_route_id
requested_model
resolved_route_id
resolved_model
actual_harness
actual_model
actual_provider/account boundary when observable
adapter version
executable path/hash/version
fallback reason
identity confidence/source
```

UI rules:

- display actual observed identity when available;
- show requested and actual side-by-side when they differ;
- show `UNVERIFIED` rather than copying the requested value into actual;
- pinned route with required verification fails closed;
- preferred fallback is highlighted in Dispatch Preview and role dock;
- automatic route shows concise rationale and expandable decision evidence.

## 8. Ranking for AUTO

Eligibility is binary and evaluated first. Ranking may then consider:

1. role capability fit;
2. certification/health freshness;
3. permission/trust match;
4. context/tool/output headroom;
5. historical acceptance/finding/retry outcomes in comparable cohorts;
6. current availability and concurrency limits;
7. project/user preference;
8. model-family exposure balancing;
9. estimated cost and subscription allocation;
10. FAST/NORMAL/SLOW resource profile.

Exposure, cost, or novelty never makes an unsafe or unsuitable route eligible.

The route decision is an immutable artifact containing candidates, scores/inputs, rejected reasons, selected route, policy version, and owner overrides.

## 9. Reviewer independence

Policy options:

```text
NONE
DIFFERENT_ROUTE
DIFFERENT_MODEL
DIFFERENT_MODEL_FAMILY
DIFFERENT_PROVIDER_OR_ACCOUNT_BOUNDARY
OWNER_REVIEW_REQUIRED_IF_UNAVAILABLE
```

A requirement cannot silently downgrade. If no independent eligible route exists, create a gate or use explicit owner review according to the accepted policy.

## 10. Model change during a session

Before a role starts, the assignment may be amended and re-resolved.

After the AO process starts, Square never hot-swaps identity. It must:

1. checkpoint or cancel the current role run;
2. create a handover artifact containing decisions, progress, changed files, evidence, blockers, and exact baseline;
3. end or supersede the old role run;
4. create a new RoleAssignment/RoleRun attempt;
5. create/reuse worktree only under explicit writer-safety policy;
6. launch a new AO session/controller generation;
7. retain both terminal histories.

## 11. UI requirements

### New Session

Default remains simple:

```text
Describe the result you want…
Workflow: Auto
Quality: Balanced
Start session
```

Expandable `Role setup` exposes role assignments and presets:

- Project default
- Economy
- Balanced
- Quality
- Manual

### Dispatch Preview

Show each planned role:

- role and purpose;
- requested mode;
- selected harness/model;
- actual-ready/verification status;
- permission and workspace authority;
- fallback/independence policy;
- estimated context/cost/resource confidence.

### Role dock

Header displays:

```text
Role · task
Actual harness/model · permission
state · elapsed
```

Details expose requested/resolved/actual identity, certification, attempt history, worktree, process/controller, and route-decision evidence.

## 12. Adapter enforcement

Square must audit each AO adapter's actual launch command and model forwarding. A typed `AgentConfig.Model` value alone is not proof that the CLI received or honored it.

Required adapter/model conformance cases:

- requested model option emitted correctly;
- unsupported model rejected before or during startup with typed result;
- actual model observable or explicitly unverified;
- permissions map to native flags/behavior;
- auth prompt recognized;
- resume behavior documented;
- cancellation does not leave descendants/worktree locks;
- writer/read-only behavior matches role contract;
- version drift invalidates or degrades certification.

## 13. Required tests

- precedence across task/session/project/global/default;
- AUTO eligibility and deterministic decision artifact;
- PREFERRED approved fallback only;
- PINNED fail-closed behavior;
- requested/actual mismatch;
- unverified actual identity;
- reviewer-independence matrix;
- quarantined/expired route denial;
- adapter version/executable replacement invalidation;
- active model change creates new attempt/handover;
- UI shows actual identity and fallback;
- only-eligible route warning;
- exposure/cost cannot override safety.
