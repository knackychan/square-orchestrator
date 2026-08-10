# Low-Tier Research Delegation Packet

## Authority state

This is a parked plan. It does not activate implementation, launch a worker, or permit web
requests. `STATUS.md` must name one exact task before any source, test, launch, or research work
begins.

## Objective

Use cheaper, lower-tier model workers for bounded file and web research, then transfer only a
compact evidence report to the higher-tier model so expensive context is spent on judgement rather
than scanning.

## Measurable outcome

- A primary session can create an explicit read-only research brief.
- A low-tier worker can inspect only named files, globs, repositories, or web domains under a hard
  budget.
- The worker writes a structured report with sources, findings, uncertainty, and token-saving
  summary.
- A higher-tier model receives the brief plus report path and exact source references, not raw
  search output or whole-file dumps.
- The orchestrator records the model catalog snapshot, selected route, budget, source count, and
  report hash for audit.

## Model catalog and worker class

Use the cheapest approved route that can follow a literal research prompt. `codex` /
`gpt-5.4-mini` is only an example candidate for small mechanical read-only research while it is
available and reverified. The higher-tier primary session may select another exact low-tier route
from a project-approved catalog, including Command Code, Claude Code, OpenCode, or Codex CLI
models, when that route better fits the research task and budget.

The catalog is a fast-readable snapshot of exact client/model IDs, rough class, permitted task
types, last verification time, and known launch profile. It is evidence for selection, not
authority. The selected route must still be exact, currently available, adopted by the active
project profile or packet, and recorded before launch. Aliases, automatic fallback, and worker-side
route choice remain forbidden.

## Research types

| Type | Scope |
|---|---|
| File research | Read-only search, source mapping, API tracing, dependency inventory, and evidence collection inside allowed paths |
| Web research | Bounded external research with explicit domains, request ceilings, retrieval date, and source quality rules |
| Comparison research | Read-only comparison of candidate approaches, libraries, commands, or prior project decisions |

## Handoff rule

The low-tier worker produces a report. The higher-tier model consumes the report and may open
specific cited sources when needed. The higher-tier prompt must not receive raw whole-repository
context merely because a lower-tier worker could gather it.

## Forbidden behavior

- Hidden internal sub-agents or background workers.
- Letting the research worker approve commands, resolve `STOP:`, change routes, or decide scope.
- Source or test edits by a research task.
- Credential reads, secret persistence, raw prompt persistence, or full log persistence.
- Web requests without an activated task, explicit domains or source rules, and request ceilings.
- Copying private project content into a global catalogue.
- Treating a report as truth without higher-tier or primary-session review.

## Budgets

| Budget | Ceiling |
|---|---|
| Source edits | `0` |
| Destructive actions | `0` |
| File research external requests | `0` |
| Web research external requests | task-specific hard ceiling |
| Spend | task-specific hard ceiling |
| Report size | target `2000` words or less unless activated task amends it |
| Evidence excerpts | short cited excerpts only, never full files or articles |
| Concurrent repository writers | read-only only |

## Acceptance authority

The primary session reviews the report against the brief. The report is advisory evidence only. Any
implementation or decision that follows requires its own active packet or owner decision.
