# Square Orchestrator — Client Execution Playbook

This document gives cold sessions reproducible context for working with Command Code (`cmdc`),
OpenCode (`opencode`), Claude Code (`claude`), and Codex CLI (`codex`). It is an operating reference,
not project authority. `STATUS.md` and the active target-repository packet must adopt an exact route
before any launch.

## Mandatory execution-session gate

This playbook is required at the start of every activated implementation, fix, or delegated review
task. The current conversation remains the primary orchestrating session. Before worker edits, the
primary must select and record one exact route, complete preflight, and launch one separate worker
as the foreground process in a visible VS Code terminal or Windows Terminal tab/window.

Use the command family assigned by the active route:

| Assigned client | Command type | Full profile |
|---|---|---|
| Command Code | `cmdc ... "<bounded-prompt>"` | Section 7.1 |
| OpenCode | `opencode run ... "<bounded-prompt>"` | Section 7.2 |
| Claude Code | `claude ... "<bounded-prompt>"` | Section 7.3 |
| Codex CLI | `codex exec ... "<bounded-prompt>"` | Section 7.4 |

Do not perform the assigned worker edits in the primary session and do not use an internal
sub-agent, hidden/minimized process, background job, cloud executor, or different CLI. The only
exception is an exact owner instruction recorded in `STATUS.md` for that task. Planning, packet
authoring, ordinary read-only answers, and primary boundary review do not launch a worker unless
their packet explicitly assigns one.

Before the visible launch, the record must contain task ID, risk class, client, exact model ID,
selection reason, catalogue and allowance evidence, disabled fallback, terminal surface, starting
HEAD, token rotation, and budgets. If any item is absent or the visible surface cannot be opened,
record `STOP:` or `ROUTE_UNAVAILABLE`; do not edit.

## 1. Scope and provenance

The reference matrix and commands below were observed and accepted for Sticker Generator on
2026-08-05 through DEC-046 and DEC-047. They seed a reusable price-first profile; they are not
automatically binding on Square Orchestrator or another project.

Before every real launch, the primary session must verify the selected client is installed, its
exact model ID is still present in the authenticated live catalogue, its allowance is available,
and its current CLI accepts the recorded flags. If the active project does not pin how to perform a
catalogue or allowance check, record `STOP:` instead of guessing a command or trusting an old
observation.

The source decisions are retained in Sticker Generator at:

```text
docs/plan/2026-07-29_decathlon-nutrition-sticker-generator/decisions/
  DEC-046-delegated-client-model-routing.md
  DEC-047-price-first-deepseek-and-selective-model-escalation.md
```

## 2. Orchestrator and worker clients are independent

The primary session may itself be Claude Code, Codex CLI, OpenCode, or Command Code. Its own client
does not determine the worker route. For example, a Codex primary session may author and review a
packet whose implementation worker is OpenCode on DeepSeek V4 Flash.

The primary session:

- writes the packet, build guide, and task list;
- selects and records the justified route;
- launches the worker in a separate approved visible terminal;
- does not edit the worker's claimed paths while it runs;
- reviews the resulting commit and assertions; and
- resolves authority questions or asks the owner.

The worker executes one exact task and route. A reviewer is a separate read-only task unless a fix
packet grants writes.

## 3. Dated DEC-047 reference route matrix

| Role | Command Code exact ID | OpenCode Go exact ID |
|---|---|---|
| Ordinary bounded implementation | `deepseek/deepseek-v4-flash` | `opencode-go/deepseek-v4-flash` |
| Silent-failure implementation | `deepseek/deepseek-v4-pro` | `opencode-go/deepseek-v4-pro` |
| Recorded Qwen escalation | `qwen/qwen3.8-max` | `opencode-go/qwen3.8-max` |
| Recorded Kimi escalation | `moonshotai/kimi-k3` | `opencode-go/kimi-k3` |
| Recorded GLM escalation | `zai-org/glm-5.2` | `opencode-go/glm-5.2` |

Additional exact routes retained by DEC-046:

| Client | Exact model ID | Use boundary |
|---|---|---|
| Claude Code | `claude-sonnet-5` | Recorded high-capability or judgement-heavy alternative |
| Codex CLI | `gpt-5.5` | Recorded complex, cross-cutting, or judgement-heavy OpenAI route |
| Codex CLI | `gpt-5.4-mini` | Small, mechanical, low-risk task with explicit assertions only |

GPT models are Codex CLI-only in this reference profile. Do not launch them through Command Code or
OpenCode merely because an ID appears in another catalogue. GPT-5.4-mini must be reverified before
every use and is not approved after its documented 2026-08-31 ChatGPT-authenticated Codex retirement
without a new observed availability record and owner decision.

## 4. Route-selection procedure

### 4.1 Ordinary work

Use DeepSeek V4 Flash for a small, bounded task whose defects are normally visible through literal
tests, compilation, or diff review.

### 4.2 Silent-failure work

Use DeepSeek V4 Pro where a plausible-looking defect could silently:

- mutate evidence or provenance;
- weaken identity or attribution checks;
- weaken a budget, spend, or request ceiling;
- corrupt a schema or immutable lifecycle contract; or
- pass tests while changing the meaning of accepted data.

### 4.3 Qwen, Kimi, or GLM escalation

Select one escalation route only when at least one condition is recorded:

1. repository evidence shows DeepSeek lacks a concrete capability required by the task;
2. a bounded DeepSeek attempt reaches a repeatable quality `STOP:` not caused by a bad packet or
   missing owner decision;
3. the work is unusually cross-cutting or judgement-heavy and likely remediation would cost more
   than the escalation; or
4. the owner explicitly requests that family for the task.

No standing quality ranking exists between Qwen, Kimi, and GLM. If current evidence does not
distinguish them, retain DeepSeek rather than choosing arbitrarily.

### 4.4 Claude or GPT selection

Claude Sonnet 5 or GPT-5.5 may be selected for a complex, cross-cutting, or judgement-heavy task
with a one-line rationale in the packet or `STATE.md`. GPT-5.4-mini is never eligible for a
silent-failure task.

## 5. Required route record

Before launch, record this information in the active packet or `STATE.md`:

```text
Task: <exact-task-id>
Risk class: <ordinary | silent-failure | escalation>
Client: <cmdc | opencode | claude | codex>
Exact model ID: <client-native-id>
Selection reason: <rule and task fact>
Escalation condition: <1-4 | not applicable>
Catalogue observed: <UTC timestamp and evidence path>
Allowance observed: <UTC timestamp and safe result; no credential value>
Automatic fallback: disabled
Visible terminal: <VS Code terminal | Windows Terminal tab/window>
Starting HEAD: <sha>
Token rotation used: <current reported input tokens / 150000>
```

Generic phrases such as “use a stronger model,” “latest,” or “auto” are insufficient.

## 6. Availability preflight

Check only the selected client. A universal executable-presence check on Windows is:

```powershell
Get-Command <cmdc|opencode|claude|codex> -ErrorAction Stop
```

Then use the target project's pinned, client-specific catalogue and allowance procedure. Record
the exact command and redacted result in its evidence destination. Do not run a version/catalogue
command known to auto-update a client unless the active packet explicitly permits that environment
change.

Preflight fails closed when:

- the executable is absent;
- the exact model ID is absent;
- authentication or allowance cannot be confirmed;
- the installed client no longer accepts the pinned launch flags;
- the check would expose a credential; or
- a client attempts to substitute or fall back automatically.

## 7. Visible foreground launch profiles

Run these from a visible VS Code integrated terminal or visible Windows Terminal tab/window. The
selected client remains the terminal's foreground command. Closing that terminal surface must end
the session.

### 7.1 Command Code

```powershell
cmdc --trust --permission-mode auto-accept --no-auto-update `
  --model <assigned-command-code-model> --effort high --max-turns 100 `
  --name "<subplan>-<milestone>" "<bounded-prompt>"
```

Use one of the exact Command Code IDs from the adopted route profile. A milestone mixing Flash and
Pro assignments uses separate sessions at task boundaries; do not weaken Pro work to keep a Flash
session alive.

### 7.2 OpenCode

```powershell
opencode run --model <assigned-opencode-go-model> --variant high --auto `
  --title "<subplan>-<milestone>" "<bounded-prompt>"
```

Use the exact `opencode-go/...` ID. Do not substitute an OpenRouter or cross-client alias.

### 7.3 Claude Code

```powershell
claude --model claude-sonnet-5 --effort high --permission-mode auto `
  --name "<subplan>-<milestone>-<task>" "<bounded-prompt>"
```

Do not enable Claude background mode or `--fallback-model`.

### 7.4 Codex CLI

```powershell
codex exec --model <gpt-5.5|gpt-5.4-mini> `
  -c 'model_reasoning_effort="high"' `
  --sandbox danger-full-access --ask-for-approval never `
  -C "<repository>" "<bounded-prompt>"
```

GPT-5.4-mini is used only under its low-risk rule. Do not use Codex cloud execution for this local
foreground profile.

These permission flags authorize only ordinary terminal operations already inside the active
packet. They do not authorize new dependencies, destructive actions, credentials, external calls,
scope changes, a new milestone, or an invented `STOP:` answer. `--yolo` is not part of the profile.

## 8. Terminal lifecycle

- The terminal must be visible and interactive before the primary session leaves the launch.
- A native `wt.exe` launch is permitted only when it opens a visible tab/window that immediately
  runs the selected client in the foreground.
- Do not use `Start-Process`, `Start-Job`, PowerShell background operators, a hidden or minimized
  runner, scheduled task, service, detached process, terminal multiplexer, or remote/headless
  executor.
- Keep the terminal open at the boundary so the owner can inspect the result.
- A process-tree check may supplement visibility; it cannot replace it.
- If no approved visible surface can be opened, record a boundary and do not substitute a hidden
  worker.

## 9. Session rotation and task boundaries

A worker session rotates once its reported input tokens reach 150,000, checked between tasks. Do
not interrupt a single task solely because the threshold is crossed mid-task. At the next boundary:

1. finish and review the current task;
2. record reported input-token use in `STATE.md`;
3. close the old worker terminal;
4. launch a fresh session with the next task's exact route and cold-start prompt; and
5. confirm the new session starts from the reviewed `HEAD`.

One write milestone per repository runs at a time. Different repositories and read-only reviews of
an immutable commit may run concurrently when their packets allow it.

## 10. Route unavailable or exhausted

Unavailable allowance is a gate, not permission for automatic fallback:

1. stop before the unavailable route edits;
2. record `ROUTE_UNAVAILABLE`, the exact client/model, safe evidence, and time;
3. select another route only from the target project's approved profile;
4. record the replacement client/model and the reason before launch;
5. re-run catalogue, allowance, terminal, and starting-HEAD preflight; and
6. create a new attributable attempt.

DeepSeek exhaustion may justify another approved client, but it does not by itself prove Qwen,
Kimi, GLM, Claude, or GPT is the correct model. Never use an alias, OpenRouter substitution,
cross-client model substitution, or a client-side automatic fallback.

## 11. Worker completion and review handoff

The worker returns or records:

```text
Task and attempt ID
Starting and resulting HEAD
Exact client/model route
Reported input-token use
Changed paths and commit(s)
Focused and full validation commands/results
External-call or spend use against every ceiling
Open STOP items
Unexpected findings and carry-forward
```

The primary session then performs the `HANDOVER.md` boundary review. A model summary never replaces
`git show`, the full diff, literal assertion inspection, or the target project's validation suite.
A reviewer does not fix findings in the same unbounded step; accepted findings become a bounded fix
task, followed by review again.

## 12. Future `sqorch` responsibility

The future application may store versioned client profiles, validate exact IDs and flags, generate
safe argument arrays, open approved terminals, enforce locks, record attempts, and detect route or
token gates. It must not choose an unrecorded route, infer escalation from marketing claims, enable
fallback, weaken packet authority, or accept a result for the owner.
