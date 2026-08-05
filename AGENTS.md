# Agent Instructions — Square Orchestrator

## Project state

This repository is in documentation and planning mode. Do not create application code, package
metadata, dependencies, installers, extensions, runtime databases, or launch delegated agents
unless `STATUS.md` explicitly names an active implementation milestone.

The project owner may activate an exact milestone at any time. Agents may not infer implementation
authority from a completed plan, an unblocked dependency, or the existence of a task list.

## Required reading order

1. This file.
2. The nearest `AGENTS.md` in the directory being changed.
3. `SPEC.md`.
4. `STATUS.md`.
5. The active packet named by `STATUS.md`.

`CLAUDE.md` files carry equivalent context for Claude-based sessions. If a context pair conflicts,
stop and report the conflict rather than choosing one silently.

## Global invariants

- A target repository remains the authority for its own scope, budgets, accepted results, and
  permitted routes. The orchestrator never grants project authority.
- Human-readable authority documents remain canonical. A machine manifest is a hash-bound
  execution projection and cannot widen them.
- Core scheduling and policy enforcement are deterministic. No model decides whether a task is
  authorized, whether paths overlap, or whether a budget may be exceeded.
- Cross-project learning is opt-in and provenance-bearing. Project content is not copied into a
  global catalogue merely because the orchestrator can read it.
- A learned practice is a candidate until reviewed. The application may propose a template,
  workflow, guardrail, refactor, skill, or tool; it may not adopt, install, or impose one silently.
- Architecture generation starts from responsibilities and dependency direction. It must avoid
  both monoliths and speculative one-function-per-file fragmentation.
- Shared components or libraries require genuine consumers and one-directional ownership; a
  generated project must not create speculative `common`, `utils`, or plugin layers.
- Every run selects one exact client/model route before launch. Aliases, silent substitution, and
  automatic fallback are prohibited.
- A route change is deliberate, justified, and recorded before the replacement edits.
- `STOP:` is a first-class halted state. It is resolved only from repository authority or by the
  owner; another agent must not invent the answer.
- No credential value may enter source control, prompts, manifests, logs, databases, or receipts.
- The core launches installed clients; it does not call model-provider APIs directly.
- Same-repository write concurrency defaults to one. Read-only work against immutable commits may
  run in parallel. Worktree concurrency is a later opt-in feature, not a default assumption.
- A project that requires visible foreground terminals must fail closed if no approved visible
  surface is available.
- Plans and proposed interfaces must never be described as shipped behavior.
- Every project-owned directory contains both `AGENTS.md` and `CLAUDE.md`. Adding, moving, or
  removing a file requires updating the nearest context pair's file map.

## Repository map

| Path | Purpose |
|---|---|
| `README.md` | Short project introduction and current boundary |
| `SPEC.md` | Canonical planned product and safety contract |
| `STATUS.md` | Current authority and milestone state |
| `docs/` | Project documentation, with its own context pair |
| `docs/superpowers/` | Specs and execution plans, with its own context pair |
| `docs/superpowers/specs/` | Design specifications |
| `docs/superpowers/plans/` | Bounded execution packets |

There is deliberately no `src/`, `tests/`, extension, installer, or package configuration yet.

## Work protocol

No delegated work begins without:

1. a packet defining objective, authority, allowed and forbidden changes, budgets, validation,
   stops, and acceptance;
2. a build guide recording decisions, contracts, vocabularies, and forbidden patterns; and
3. an ordered task list naming exact artifacts, checks, and commit messages.

The primary orchestrating session writes those documents. Workers execute only their bounded task.
At task boundaries, review the commit and full diff rather than the worker summary. Stage exact
paths only; `git add -A`, `git add .`, `git add -u`, and `git commit -a` are prohibited.
