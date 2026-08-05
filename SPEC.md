# Square Orchestrator — Product Specification

- Revision: `0.1-draft`
- Date: 2026-08-05
- State: planning only

## 1. Purpose

Square Orchestrator will be a globally installed, terminal-first control plane for coordinating
bounded work by existing coding-agent clients across multiple repositories. A primary session or
human operator supplies judgement and project authority; the application supplies deterministic
validation, launch mechanics, scheduling, locks, state, and review evidence.

## 2. Goals

The planned product will:

1. be callable from Claude Code, Codex CLI, OpenCode, Command Code, or an ordinary shell;
2. create a coherent planning baseline for a new project or propose a safe adoption plan for an
   existing repository;
3. describe a project's responsibilities and one-directional dependencies as a graph before
   generating its file and folder arrangement;
4. register multiple independent repositories without moving authority out of them;
5. validate a task against versioned repository documents before launch;
6. select and record one exact client/model route under a versioned project policy;
7. open each worker in a project-approved visible foreground terminal;
8. coordinate dependencies and safe parallel work;
9. halt visibly on `STOP:`, route unavailability, budget exhaustion, or authority drift;
10. review exact commits, changed paths, validation results, and budget evidence;
11. learn candidate practices from opted-in project evidence and explicitly sourced research;
12. suggest versioned workflow, template, guardrail, skill, and tool improvements without applying
    them automatically; and
13. provide machine-readable status for calling agent sessions and readable terminal status for
    the owner.

## 3. Non-goals for the first implementation

The first implementation will not:

- provide a web application or graphical workflow editor;
- call model-provider APIs directly;
- store or broker provider credentials;
- invent plans, owner decisions, packet content, or `STOP:` resolutions;
- automatically switch clients or models;
- run hidden, detached, cloud, or background workers;
- permit simultaneous writers on the same repository;
- merge worktrees or resolve merge conflicts;
- require ACP, MCP, a VS Code extension, or a daemon;
- replace repository-specific `AGENTS.md`, `SPEC.md`, `STATUS.md`, packets, or acceptance records;
- treat one project's convention as universally correct without context and evidence;
- upload or centralize private repository contents as learning data;
- silently mutate its own code, global workflow, project templates, or practice rankings; or
- generate speculative shared libraries, abstraction layers, plugin systems, or empty directory
  forests for possible future use.

## 4. Actors and authority

### 4.1 Owner

The owner activates milestones, approves policy, resolves questions not answered by repository
authority, and accepts results.

### 4.2 Primary orchestrating session

The primary Claude, Codex, OpenCode, or Command Code session authors packets, decomposes work,
selects justified routes, reviews worker output, and requests owner decisions when needed.

### 4.3 Square Orchestrator

The application deterministically validates and executes an already-authorized plan. It is not an
authority and does not use a model to interpret whether permission exists.

### 4.4 Worker

A worker is one installed coding-agent client running one bounded task through one exact route in
one visible terminal.

### 4.5 Reviewer

A reviewer inspects an immutable commit or diff and produces findings. Review is read-only unless a
separate fix task grants write authority.

## 5. Authority chain

The planned validation order is:

```text
repository STATUS
    -> active packet/build guide/task list
    -> hash-bound execution manifest
    -> route and budget preflight
    -> visible launch
    -> immutable diff and checks
    -> primary-session/owner acceptance
```

A lower layer cannot widen a higher one. Any mismatch fails closed.

## 6. Execution manifest

The machine-readable manifest will be a projection of canonical human documents. It will record at
least:

- schema and profile version;
- repository canonical path and expected starting commit;
- hashes of the status, packet, build guide, and task list;
- task ID, role, dependencies, and read/write mode;
- exact allowed and forbidden paths;
- exact validation commands and expected commit message;
- external-call, spend, request, turn, and token ceilings when applicable;
- risk class, selected client, exact model ID, and escalation rationale when required;
- terminal-surface requirement;
- evidence destination, stop conditions, and acceptance authority.

Changing any canonical document invalidates the compiled manifest until it is regenerated and
reviewed.

## 7. Project foundry

The planned project-foundry capability has two modes:

### 7.1 New project

It asks for the product boundary, owner, repositories, languages, deployment context, external
effects, data sensitivity, expected scale, and acceptance authority. From those answers it proposes:

- root `AGENTS.md` and `CLAUDE.md` context;
- canonical `SPEC.md` and `STATUS.md` authority;
- a planning workspace with design specs, bounded packets, build guides, task lists, and state;
- decision and evidence registers where the project needs them;
- a responsibility graph and permitted dependency direction;
- the smallest source/test/script directory graph required by the first active vertical slice;
- validation, exact-path staging, review, `STOP:`, budget, credential, and external-action guards;
  and
- clean-code rules appropriate to the selected language and project size.

The owner reviews a complete preview before files are created. The generator creates no application
source merely because a future directory appears in the proposed graph.

### 7.2 Existing project adoption

Adoption begins read-only. It inventories context files, authority, architecture, dependencies,
tests, scripts, module edges, duplicate responsibilities, shared code, and operational guardrails.
It emits a proposed graph and patch plan. It does not rearrange, rename, or rewrite the existing
repository until the owner activates a bounded migration packet.

### 7.3 Graph and clean-code rules

- Nodes represent cohesive responsibilities, data/effect boundaries, applications, or packages;
  edges are permitted dependencies.
- Dependencies are one-directional and cycles are defects unless an accepted project decision
  documents why the cycle is unavoidable.
- A module may contain multiple tightly related functions. Split when responsibilities have
  different reasons to change.
- Domain rules live once above adapters; leaf clients and platform adapters remain boring.
- Shared components and functions are extracted after genuine reuse is observed, or when a packet
  identifies a single rule that would otherwise be duplicated across multiple consumers.
- `utils`, `common`, `shared`, plugin, factory, and interface layers require named consumers and a
  stable contract; they are not default folders.
- Each directory's context map states ownership, public contracts, allowed dependencies, tests, and
  evidence destinations at the detail justified by that project.

## 8. Task lifecycle

The minimum planned states are:

```text
DRAFT -> AUTHORIZED -> READY -> RUNNING -> REVIEW -> ACCEPTED
                                  |           |
                                  +-> STOP    +-> FIX -> REVIEW
```

`FAILED`, `CANCELLED`, and `ROUTE_UNAVAILABLE` are terminal or gated outcomes, not synonyms for
`STOP:`. A `STOP:` records an exact unresolved question and preserves the task boundary.

## 9. Concurrency

- Different repositories may have write tasks running concurrently.
- One repository permits one writer by default, regardless of path claims.
- Multiple read-only reviewers may inspect the same immutable commit concurrently.
- Canonical packet or plan publication has one owning writer; research agents may contribute
  non-authoritative findings in parallel.
- Worktree-based concurrent writers and a serial integration queue are parked until evidence shows
  the one-writer rule is a material bottleneck.

## 10. Routing policy

Routes are configuration, not core code. A versioned project profile maps declared task risk to an
exact client and model. The Sticker Generator reference profile is planned to encode DEC-047:

- price-first DeepSeek Flash for ordinary bounded implementation;
- DeepSeek Pro for silent-failure work;
- a recorded task-specific reason for any approved escalation;
- one exact client/model route per attempt;
- live route/allowance verification before launch; and
- deliberate recorded reselection, never automatic fallback.

That profile applies to another project only when the other project adopts it.

## 11. Terminal lifecycle

The first planned launcher targets Windows Terminal because it can be invoked from any current
session and can immediately run a selected client as the visible foreground command. Closing the
hosting tab must terminate the worker process tree.

A thin VS Code extension is a later adapter for creating and observing integrated terminals. A
hidden daemon is not required for the first vertical slice.

## 12. Practice lab and project taste

“Taste” is a versioned, inspectable set of preferences derived from evidence and owner choices, not
opaque model memory. A practice record will include:

- stable ID, category, statement, and proposed scope;
- source type and provenance reference;
- observed project context and outcome, without copied secret or client content;
- trade-offs, counterexamples, confidence, and review date;
- state: `OBSERVED`, `CANDIDATE`, `TRIAL`, `ADOPTED`, `REJECTED`, or `DEPRECATED`;
- the approving authority and the template/profile versions affected; and
- a way to trace which projects have opted into the practice.

Initial taste priorities are correctness and fail-closed safety, explicit authority, simplicity,
cohesive responsibility boundaries, one-directional dependencies, reuse of genuine shared rules,
small vertical slices, deterministic verification, recoverability, and cost-aware model routing.
Projects may override a preference through a recorded local decision.

The practice lab may analyze opted-in local run outcomes, review findings, repeated `STOP:` causes,
rework, test failures, code churn, dependency additions, and owner accept/reject decisions. External
research is a separately authorized, bounded network task. It prefers primary sources, records URL,
retrieval date, license/usage boundary, and an applicability note, and never treats popularity as
proof.

Outputs are proposals: update a project blueprint, add a guardrail, revise an agent skill, create a
new deterministic check, or develop a small supporting tool. Proposal acceptance and implementation
remain separate owner gates. Rejected practices remain recorded so the same suggestion is not
repeated without new evidence.

## 13. Interfaces

The initial interface is planned as a global `sqorch` CLI with concise human output and `--json`
for calling sessions. Candidate commands are:

```text
sqorch doctor
sqorch project new --preview
sqorch project adopt <path> --audit-only
sqorch project graph <path>
sqorch project add <path>
sqorch practices list
sqorch practices suggest --project <path>
sqorch practices research --topic <topic> --dry-run
sqorch validate --project <path> --task <id>
sqorch run --project <path> --task <id> --dry-run
sqorch run --project <path> --task <id>
sqorch status [run-id] --json
sqorch review <run-id>
sqorch resolve-stop <run-id> --authority <artifact>
```

Exact command names remain a design input until the first implementation packet is activated.

An MCP interface may later expose the same core operations. ACP may later replace individual
client transports where verified support exists; neither is a first-slice dependency.

## 14. Storage and audit

The candidate first implementation uses Python's standard-library SQLite support for the global
project registry, locks, run state, route observations, and redacted events. Repository artifacts
remain the durable authority and acceptance record.

Every attempt must attribute its project, task, starting commit, exact client/model route, terminal
surface, times, exit outcome, resulting commits, validation results, and budget observations.

Practice records store abstractions and provenance, not source files, prompts, credentials, client
data, or raw conversations. A project must opt in before its outcomes contribute to global practice
evidence.

## 15. Security and safety

- Commands are constructed as argument arrays, never interpolated from untrusted shell strings.
- Paths are canonicalized and checked against declared project and task boundaries.
- Environment variable names may be recorded; values may not be read into persisted state.
- Prompts, logs, receipts, and databases must be redacted before persistence.
- Destructive or external actions require explicit packet authority.
- Manifest hashes and starting commits prevent stale authority from being launched silently.
- Project learning is local and opt-in; export requires separate explicit authority.
- Practice and template changes use immutable revisions with preview, diff, approval, and rollback.
- Research fetches have explicit domains, request ceilings, and evidence destinations.

## 16. Proposed implementation sequence

1. M1: dry-run CLI foundation, project registry, project-foundry preview, practice-record schema,
   manifest validation, exact route preview, locks, and fake client tests.
2. M2A: reviewed project creation/adoption and graph validation with fixture repositories only.
3. M2B: one real visible Windows Terminal worker, completion receipt, `STOP:` handling, and
   deterministic Git review.
4. M3: cross-repository scheduling and parallel read-only reviews.
5. M4: practice evidence, proposal comparison, bounded official-source research, and blueprint/tool
   suggestions.
6. M5: optional VS Code terminal bridge and local MCP interface.
7. M6: optional worktree write concurrency only if measured demand justifies it.

These milestones are planned, not active.

## 17. Implementation gate

No implementation begins until the owner activates an exact milestone in `STATUS.md` and its
packet, build guide, and task list exist. The owner may activate a milestone at any time. Agents may
not infer activation.

Credential values must never be persisted, and any external or paid action must carry an explicit
hard ceiling; these safeguards are required even when the owner accelerates implementation.
