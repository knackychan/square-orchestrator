# Square Orchestrator — Cold-Start Handover

This is the operational guide for a new primary, worker, or reviewer session. It explains how to
use the repository workflow; it does not grant authority. `STATUS.md` always controls what may run.

## 1. Current project boundary

At this handover revision, Square Orchestrator is planning-only. M0 documentation is technically
complete and awaits owner acceptance. No application implementation, dependency installation,
client launch, external call, or M1 executable packet is authorized.

Never translate a planned command or interface in `SPEC.md` into shipped behavior.

## 2. Cold-start reading order

From the repository root:

1. Read root `AGENTS.md` and `CLAUDE.md`; stop if they conflict.
2. Read the nearest directory-level `AGENTS.md` and `CLAUDE.md` for every path you may change.
3. Read `SPEC.md` for durable product intent and planned/shipped truth.
4. Read `STATUS.md` for current authority and the active subplan.
5. Read this `HANDOVER.md` for the operating loop.
6. Read only the active packet named by `STATUS.md`, in this order:
   `PACKET.md`, `BUILD.md`, `BUILD-TASKS.md`, `STATE.md`.
7. Inspect repository reality:

   ```powershell
   git status --short
   git log --oneline --decorate -5
   ```

8. Before editing, state the exact active task, allowed paths, forbidden actions, route/budget
   bounds, starting `HEAD`, open `STOP:` items, and acceptance authority.

If `STATUS.md` names no active task matching the request, do not infer one from a plan or from
`STATE.md`. Answer read-only questions normally; ask the owner to activate or amend the exact work
before mutation.

### Reusable cold-start prompt

```text
Read AGENTS.md and CLAUDE.md, then SPEC.md, STATUS.md, HANDOVER.md, and only the active packet.
Inspect git status and recent history. Before editing, report the exact authority, active task,
allowed paths, forbidden actions, budgets, selected route if any, starting HEAD, open STOP items,
and acceptance authority. Do not infer implementation permission from planned work or STATE.md.
```

## 3. Authority precedence

| Source | Owns | Cannot do |
|---|---|---|
| Owner instruction recorded in `STATUS.md` | Activates exact work and owner gates | Remove credential or external-call safeguards |
| `SPEC.md` | Durable product contract and planned/shipped distinction | Activate a task |
| Active `PACKET.md` | Objective, paths, budgets, stops, validation, acceptance | Widen `STATUS.md` |
| Active `BUILD.md` | Decisions, contracts, vocabularies, forbidden patterns | Add tasks or authority |
| Active `BUILD-TASKS.md` | Exact order, artifacts, assertions, commits | Resolve an unanswered design choice |
| Active `STATE.md` | Progress, HEAD, checks, budgets, stops, carry-forward | Grant permission |
| This handover | Operating procedure | Override any authority above |

Where two sources genuinely conflict, stop and report the exact clauses. Do not silently select the
more convenient one.

## 4. Identify the session role

### Primary orchestrating session

Owns decomposition, packet authoring, decision closure, route recommendation, boundary review,
`STOP:` resolution from authority, state updates, and owner handoff. Packet authoring is never
delegated because the packet constrains delegation.

### Worker session

Receives one exact task, reads its authorities, writes only allowed paths, runs literal checks,
commits with the exact message, updates the task evidence destination, and stops on any real gap.
It does not redesign, widen scope, choose an unrecorded dependency, change route, or self-approve.

### Reviewer session

Reads an immutable commit or diff and produces findings. It is read-only unless a separate fix task
explicitly grants writes. It reviews the diff and assertions, not the worker's summary.

### Research or planning contributor

May gather evidence or propose options in a named non-authoritative destination. One primary session
reconciles contributions into canonical plans. Research does not become a decision merely because
several agents agree.

## 5. The project workflow

```text
owner request
   -> authority/status check
   -> primary session closes design decisions
   -> packet + build guide + literal task list
   -> owner activates exact implementation boundary
   -> route/terminal/budget preflight
   -> one bounded worker
   -> deterministic checks and immutable diff review
   -> findings -> bounded fix -> review
   -> primary technical result
   -> owner acceptance
   -> STATE and STATUS handoff
```

### 5.1 Planning and packet authoring

1. Search `docs/superpowers/specs/` and `docs/superpowers/plans/` before creating another plan.
2. Separate observed facts, assumptions, proposed decisions, and accepted decisions.
3. Draw cohesive responsibilities and permitted dependency edges before proposing source folders.
4. Keep one-directional dependencies. Avoid monoliths and speculative one-function-per-file trees.
5. Extract shared components or helpers only for genuine consumers or a single rule that would
   otherwise be duplicated.
6. Write the packet with objective, measurable outcome, entry conditions, exact inputs, allowed and
   forbidden paths/actions, budgets, ordered steps, contracts, stops, validation, evidence, and
   acceptance authority.
7. Put every design fork in the build guide with a numbered rationale.
8. Write tasks with literal assertions and exact commit messages. If that cannot be done, the
   missing decision belongs in the build guide before delegation.
9. Keep future inactive milestones parked. Do not write an executable packet whose existence might
   be mistaken for permission unless `STATUS.md` authorizes that packet boundary.

### 5.2 Activation and preflight

Before launching a worker, the primary session verifies:

- `STATUS.md` activates the exact milestone/task;
- the packet, build guide, task list, and context pairs exist;
- the starting `HEAD` and dirty worktree are recorded;
- allowed paths do not overlap another writer;
- request, spend, token, dependency, destructive-action, and credential bounds are explicit;
- one exact installed client/model route is recorded and currently available;
- any escalation has the required task-specific rationale;
- no automatic fallback or alias is enabled; and
- the selected visible terminal surface satisfies the target project's policy.

If no exact route is recorded, stop. The Sticker Generator DEC-047 profile is a reference candidate,
not automatic authority for Square Orchestrator or another project.

### 5.3 Manual delegation until `sqorch` ships

Until the application implements this workflow, the primary session performs the mechanics
manually:

1. Prepare a bounded prompt naming repository, task, required reading, starting commit, selected
   client/model, allowed paths, checks, commit message, stop format, and handoff destination.
2. Launch the worker as the foreground command of an approved visible terminal. Do not use a
   hidden/minimized runner, background job, detached process, service, scheduler, terminal
   multiplexer, or silent client fallback.
3. Run one write milestone per repository at a time. Other repositories and read-only reviews of an
   immutable commit may run in parallel when their packets permit it.
4. Let the worker finish or raise `STOP:`. Do not edit concurrently in its claimed paths.
5. Review at the task or milestone boundary before advancing.

Suggested bounded worker prompt shape:

```text
Execute only <task-id> from <packet-path> in <repository> at starting HEAD <sha>.
Read the required context and active packet in order. Use exact route <client>/<model>.
Allowed paths: <paths>. Forbidden: <actions/paths>. Budgets: <ceilings>.
Follow the literal failing assertions and exact commit message in BUILD-TASKS.md.
On a real gap, write `STOP: <exact question>` and make no invented decision.
Return the commit, checks, budget use, changed paths, and open stops.
```

## 6. Test-first and document-task discipline

For code tasks:

1. Add the literal failing assertion.
2. Run it and observe failure for the specified reason.
3. If it passes on first run, stop: the test is ineffective or behavior already exists.
4. Implement the smallest cohesive change.
5. Run focused checks, then the packet's full validation.
6. Review the assertion for weakened behavior.
7. Commit with the exact message.

For document tasks, replace the red/green loop with exact artifact criteria, field lists,
invariants, worked examples, greppable prohibitions, link/path checks, and executable validation
where possible. Document drift is still a defect.

## 7. `STOP:` handling

1. Preserve the stopped worker's files, terminal output, starting commit, and question.
2. Record the exact question in `STATE.md`; do not paraphrase away the uncertainty.
3. Search `STATUS.md`, `SPEC.md`, accepted decisions, the packet, and the build guide.
4. If authority already answers it, cite that source and create a recorded continuation attempt.
5. If authority does not answer it, take the question to the owner.
6. If the answer changes scope, routes, budgets, contracts, or paths, amend the appropriate
   authority and revalidate before relaunch.
7. Never solve a `STOP:` by weakening a test, guessing a default, adding an unapproved dependency,
   or switching clients/models silently.

## 8. Boundary review

Review the repository, not the agent narrative:

```powershell
git status --short
git log --oneline <starting-head>..HEAD
git show --name-only --format=fuller <commit>
git diff --check <starting-head>..HEAD
git diff <starting-head>..HEAD -- <allowed-paths>
```

Then verify:

- one expected commit per task, with the exact message;
- no path outside the packet;
- context pairs and file maps for every added/moved/removed file;
- the literal assertion failed first for the intended reason and now passes;
- focused and full validation are green;
- no credential, unsafe shell interpolation, alias, fallback, or unrecorded external action;
- no weakened test or speculative abstraction; and
- `STATE.md` truthfully records HEAD, checks, budgets, stops, and carry-forward findings.

Fix accepted findings through a bounded fix task before advancing. Stage exact paths only; never use
`git add -A`, `git add .`, `git add -u`, or `git commit -a` in a shared repository.

## 9. Milestone completion and owner handoff

At every milestone boundary, update `STATE.md` with:

- current milestone and last completed task;
- starting and resulting commit IDs;
- verified test/check count and exact commands;
- budget consumed against every ceiling;
- open `STOP:` items and owner gates;
- unexpected findings and bounded fixes;
- carry-forward items, striking resolved entries rather than deleting them; and
- the next cold-start reading order.

Update `STATUS.md` only to record truthful authority, technical completion, owner acceptance, or the
next exact activation. Technical completion is not owner acceptance. A cold session must be able to
continue from repository files without relying on chat history.

## 10. When Square Orchestrator begins to ship

The future application may automate document hashing, manifest compilation, route and allowance
checks, locks, visible terminal launch, event receipts, diff checks, and dashboards. It must not
automate owner authority, packet judgement, `STOP:` invention, practice adoption, or acceptance.

Until accepted implementation evidence exists, follow the manual procedure above and describe all
`sqorch` commands as planned.

