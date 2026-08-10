# Square Orchestrator — Initial Design

- Date: 2026-08-05
- Status: draft design; no implementation authority
- Parent contract: `../../../SPEC.md`

## Design outcome

Square Orchestrator is a deterministic project foundry and control plane around existing agent
clients, not another general-purpose coding agent. The primary session supplies judgement.
Repository documents supply authority. The application supplies project-blueprint proposals,
validation, state, locks, launch mechanics, evidence, and versioned practice suggestions.

## Layering

```text
CLI / later MCP
       |
application workflow
       |
project foundry --- authority validator --- scheduler --- review
       |                 |                |             |
practice catalogue  repository reader  state/locks   git/checks
       |
client adapters --- terminal adapters
```

Dependencies point downward. Client and terminal adapters contain command-specific mechanics but no
authorization or scheduling decisions.

## Planned core records

### Project

Canonical repository path, display name, policy profile, authority-document locations, and project
concurrency limit.

### Task

Project, canonical task ID, role, dependencies, read/write mode, path bounds, validations, budgets,
risk class, route selection, evidence destination, and acceptance authority.

### Attempt

Immutable launch record tying a task to one expected starting commit, one document-hash set, one
exact client/model route, one terminal, and one outcome.

### Gate

A typed reason work cannot advance: authority drift, `STOP:`, route unavailable, budget exhausted,
validation failure, unexpected diff, or owner acceptance required.

### Finding

A reviewer observation tied to an immutable commit and severity. Findings never edit; an accepted
finding becomes input to a separate fix task.

### Practice

Immutable revision describing one contextual engineering preference, its provenance, evidence,
trade-offs, lifecycle state, approval, and affected project profiles or blueprints.

### Project graph

Versioned nodes for cohesive responsibilities and boundaries, plus directed permitted dependencies.
The file tree is a projection of the accepted graph and first active slice, not a universal
template copied into every project.

## Roles

| Role | Default access | Parallel rule |
|---|---|---|
| Research | read-only | Parallel against a pinned commit |
| Plan contribution | read-only/non-canonical output | Parallel; one primary session publishes |
| Documentation | write | One repository writer by default |
| Implementation | write | One repository writer by default |
| Review | read-only | Parallel against an immutable commit |
| Fix | write | Sequential after findings are consolidated |
| Amendment | authority-controlled | Never automatic |

## First vertical slice

The smallest useful implementation is not a swarm. It is a dry-run CLI that proves:

1. a new project blueprint and dependency graph can be previewed without writing;
2. an existing repository can be inventoried without mutation;
3. a practice record can be validated with provenance and lifecycle state;
4. a repository can be registered;
5. authority documents and hashes can be checked;
6. one task can be compiled into a manifest;
7. an exact route can be selected and displayed without launching it;
8. a repository write lock prevents a second writer; and
9. all output is available as human text and stable JSON.

Only after that slice is accepted should a real visible client be launched.

## Parked decisions

- exact manifest serialization (`TOML` versus `JSON`);
- exact SQLite schema;
- distribution mechanism and executable packaging;
- Windows Terminal completion-receipt mechanism;
- VS Code extension protocol;
- MCP method names;
- ACP adoption per client;
- worktree merge queue; and
- interactive full-screen TUI library.
- blueprint catalogue and inheritance rules;
- practice scoring and evidence thresholds;
- local cross-project metrics eligible for opt-in learning; and
- research source allow-list and proposal promotion thresholds.

Each is decided in the packet that first needs it, from observed local capabilities rather than
speculation.

## Practice evolution loop

```text
opted-in project outcome / bounded sourced research
        -> provenance-preserving observation
        -> candidate practice with context and trade-offs
        -> owner-selected trial profile
        -> measured project result and review
        -> adopt / reject / deprecate
        -> versioned blueprint, guardrail, skill, or tool proposal
```

No arrow is automatic. Raw project content does not enter the catalogue, and an adopted practice
does not rewrite projects that have not opted into its profile revision.

## Reference-project observations

The initial taste is informed by these observed Sticker Generator practices, not copied as
universal law:

- repository authority and explicit implementation activation;
- packet/build-guide/task-list/state separation;
- graph-shaped cohesive modules with one-directional dependencies;
- leaf adapters separated from domain rules;
- multiple tightly related functions allowed in one module;
- shared helpers only for genuine protocol or domain reuse;
- literal tests, exact commits, and diff review;
- first-class `STOP:` and fail-closed ambiguity;
- visible foreground delegated sessions;
- exact client/model attribution and no silent fallback; and
- price-first DeepSeek routing with recorded task-specific escalation.

Every imported observation begins as `OBSERVED` or `CANDIDATE`; owner acceptance in Sticker
Generator does not automatically make it `ADOPTED` for Square Orchestrator.
