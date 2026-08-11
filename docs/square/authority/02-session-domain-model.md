# Square Session Domain Model

- Status: contract draft for SA02
- Scope: pure domain vocabulary and reducers; no SQLite, HTTP, Electron, AO adapter, filesystem, clock lookup, or process I/O

## 1. Core distinction

### Square Session

The durable user-facing topic and working context. It is stable across workflow runs, role attempts, daemon restarts, desktop closure, and model changes.

### Workflow Run

One attempt to advance the session's goal using a selected workflow profile and immutable starting baseline. A user may start another run in the same session after a result, correction, changed scope, or failure.

### Role Run

One bounded invocation of a logical role. A role run has a requested route, resolved route, actual observed identity, input packet, output artifact or blocker, and optional AO execution binding.

### AO Execution Binding

The generation-fenced relationship between a Square Role Run/Task Attempt and one AO session, runtime controller, terminal/Chat controller, worktree, branch, and process/runtime identity.

## 2. Strong identifiers

Define distinct IDs even when the initial encoding is shared:

```text
SquareSessionID
SessionMessageID
WorkflowRunID
RoleProfileID
RoleAssignmentID
RoleRunID
SquareRequestID
TaskBriefID
DispatchPreviewID
PlanID
SquareTaskID
TaskAttemptID
AcceptanceCriterionID
ContextJobID
ContextReportID
ContextPackID
InteractionID
DecisionID
FindingID
ReceiptID
EvidenceID
MemoryEntryID
MemoryCandidateID
RouteID
RouteCertificationID
AOExecutionBindingID
WriterLeaseID
SemanticEventID
CorrelationID
IdempotencyKey
```

ID parsing, serialization, equality, and ordering are deterministic. Public IDs are opaque. AO IDs are stored as external references and never reused as Square IDs.

## 3. Primary records

### SquareSession

Required fields:

- `id`
- `schema_version`
- `project_id`
- `title`
- `status`
- `created_at`
- `updated_at`
- `created_by`
- `active_workflow_run_id?`
- `last_message_id?`
- `attention_state`
- `retention_policy_id`
- `version`

Session status:

```text
DRAFT
READY
RUNNING
WAITING_FOR_USER
BLOCKED
PAUSED
VERIFYING
REVIEWING
SUCCEEDED
FAILED
CANCELLED
ARCHIVED
```

A completed session may receive new messages and start another workflow run. `ARCHIVED` is presentation/retention state, not deletion.

### SessionMessage

A durable conversation item independent of provider transcripts:

- stable ID/session ID;
- author kind: `USER`, `TASK_MANAGER`, `ROLE`, `SYSTEM`;
- author reference;
- content media type;
- text or artifact reference;
- created time;
- correlation/causation IDs;
- whether it changes scope;
- optional interaction/decision reference;
- immutable revision/hash.

Edits are represented as new revisions or correction messages, not in-place history rewrites.

### WorkflowRun

- session/project IDs;
- immutable baseline commit/tree/state reference;
- requested and resolved workflow profile;
- quality/resource preset;
- status;
- current stage;
- Task Brief/Preview/Plan references;
- route-profile snapshot;
- started/finished times;
- retry/fix budgets;
- terminal outcome and final receipt;
- version/fencing generation.

Workflow status:

```text
CREATED
TRIAGING
PREVIEW_READY
APPROVAL_REQUIRED
QUEUED
CONTEXT_BUILDING
PLANNING
PLAN_APPROVAL_REQUIRED
DISPATCHING
IMPLEMENTING
VERIFYING
REVIEWING
FIXING
FINALIZING
SUCCEEDED
FAILED
CANCELLED
PAUSED
BLOCKED
```

### RoleProfile

A reusable logical-role policy, separate from any specific model:

- role kind;
- purpose;
- selection mode;
- route preferences;
- permission policy;
- required capabilities;
- context/output/tool budgets;
- artifact contract;
- allowed project/read/write scope class;
- independence constraints;
- handover and stop policy;
- version/source/scope.

### RoleAssignment

The frozen decision for one workflow stage/task:

- workflow/task and role kind;
- effective profile resolution chain;
- requested route(s);
- selected route and rationale;
- rejected candidates/reasons;
- permission mode;
- artifact contract;
- budget/deadline;
- independence constraints;
- owner override, if any.

### RoleRun

- assignment ID;
- attempt number;
- state;
- requested/resolved/actual route identity;
- AO binding ID;
- input artifact/hash;
- output artifact/hash;
- blocker/interaction;
- start/end times;
- context/token/usage fields with source/confidence;
- termination reason;
- handover reference;
- receipt/evidence references;
- version.

Role-run state:

```text
CREATED
WAITING_ROUTE
WAITING_CAPACITY
STARTING
RUNNING
QUIET_ACTIVE
WAITING_FOR_INPUT
WAITING_FOR_APPROVAL
AUTH_REQUIRED
BLOCKED
SUSPECTED_STALL
COMPLETING
CANCELLING
SUCCEEDED
FAILED
CANCELLED
HARD_STOPPED
LOST_PROCESS
SUPERSEDED
```

### AOExecutionBinding

- binding ID;
- role run/task attempt;
- AO project ID;
- AO session ID;
- AO controller generation;
- runtime kind/handle identity;
- terminal or Chat mode;
- worktree path/reference;
- branch/PR references;
- writer lease/fencing token;
- executable/adapter identity;
- created/released times;
- current binding state;
- reconciliation result.

Binding state:

```text
RESERVED
CREATING_WORKSPACE
CREATING_SESSION
BOUND
DETACHED
RECONCILING
RELEASED
LOST
QUARANTINED
```

## 4. Workflow records

### TaskBrief

- user-visible goal and outcome;
- current facts and constraints;
- in/out of scope;
- ambiguities/questions;
- risk dimensions;
- candidate profile;
- relevant project/memory references;
- source session message IDs;
- immutable content hash.

### DispatchPreview

- selected profile and reason;
- stages and role assignments;
- requested/actual-ready routes;
- write/worktree scope;
- expected approvals;
- validation/review strategy;
- resource/usage estimates with confidence;
- escalation conditions;
- owner changes and approval state.

### Plan and SquareTask

Plan includes a stable task DAG, decisions, integration strategy, acceptance map, budgets, stop boundaries, and immutable version/hash.

Each task includes:

- dependencies;
- role/specialist;
- exact read/write paths;
- task contract;
- route assignment;
- validation commands;
- acceptance references;
- discretion;
- forbidden decisions;
- resource and retry budgets;
- completion receipt requirements.

### Interaction and Decision

Interaction types:

```text
QUESTION
PERMISSION
APPROVAL
AUTH_TAKEOVER
BLOCKER
PLAN_APPROVAL
SCOPE_CHANGE
ROUTE_UNAVAILABLE
UNKNOWN_TERMINAL_STATE
FORCE_STOP_GATE
```

An interaction records exact authority, requester, affected session/workflow/role/task, redacted evidence, expiry, safe default, possible responses, and one accepted response.

A Decision records the owner or policy outcome, rationale, scope, version, actor, and event chain. The UI never invents available actions; the daemon returns capabilities.

## 5. Reducer rules

All reducers are pure:

```text
(current state, typed command/event, explicit timestamp/facts)
    -> next state + emitted domain events
    OR typed denial
```

Required reducer families:

- Square Session;
- Workflow Run;
- Role Run;
- AO binding;
- task and attempt;
- interaction;
- route certification;
- memory lifecycle;
- writer lease;
- circuit breaker/resource gate.

Properties:

- duplicate events are idempotent;
- final attempt states cannot silently reopen;
- a new attempt/run is explicit;
- session can start another workflow after terminal workflow outcome;
- scope-changing user message pauses or supersedes current work according to policy;
- `QUIET_ACTIVE` is distinct from `SUSPECTED_STALL`;
- UI closure is not a domain event;
- model-route change after process start creates a new role run/attempt;
- owner approval is bound to exact hashes/versions;
- stale writer/controller generations cannot commit.

## 6. Role hierarchy inside a session

The visible hierarchy is logical, not process ownership:

```text
Square Session
└─ Workflow Run
   ├─ Task Manager (software timeline)
   ├─ Secretary Role Run(s)
   ├─ Scout Role Run(s)
   ├─ Planner Role Run(s)
   ├─ Orchestrator/Synthesizer Role Run(s)
   ├─ Worker Task Attempts
   └─ Reviewer/Fix Role Runs
```

An Orchestrator role may create a plan/dispatch proposal, but only Task Manager software persists and advances the workflow.

## 7. History and retention

A session history is assembled from:

- durable session messages;
- Square semantic events;
- role-run records;
- AO bindings and lifecycle facts;
- terminal/Chat chunk references;
- artifacts and receipts;
- interactions and decisions;
- Git/worktree/PR results.

History is append-oriented. Retention may compact terminal payloads but must preserve:

- sequence ranges and truncation markers;
- role/task/state metadata;
- final output/receipt/evidence;
- decisions and interactions;
- hashes/provenance;
- accepted memory links.

## 8. Required deterministic tests

- ID parsing/round trip and cross-type rejection;
- legal/illegal state transition tables;
- duplicate event idempotency;
- session restart/new workflow behavior;
- scope amendment/supersession;
- role route change creates new attempt;
- stale binding/lease/controller generation denial;
- waiting interaction persists through restart;
- final receipt binds exact run/task/commit/artifact hashes;
- UI/view events cannot alter domain state;
- QUICK does not instantiate absent role runs;
- PLANNED role hierarchy remains valid when optional roles are skipped or fail.
