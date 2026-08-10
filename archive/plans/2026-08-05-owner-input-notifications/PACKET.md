# Owner-Input Notifications Packet

## Authority state

This is a parked plan. It does not activate implementation and does not change the current
T-M1-03-FIX-01 boundary. `STATUS.md` must name one exact task before any source, test, dependency,
service, or notifier work begins.

## Objective

Add a minimal notification path so Square Orchestrator can alert the owner when independent agent
work needs human input or review.

## Measurable outcome

- Runs expose pending owner-input events through a deterministic local status surface.
- A local Windows notification can alert the owner without launching another agent or contacting a
  remote service.
- Remote or phone notification remains opt-in, redacted, and configured only by environment
  variable names.
- Notifications never authorize a command, resolve `STOP:`, widen a packet, or include secrets,
  source content, raw prompts, or full logs.

## Notification events

The first implementation should notify only for these states:

- `STOP`
- `ROUTE_UNAVAILABLE`
- `APPROVAL_REQUIRED`
- `BUDGET_GATE`
- `REVIEW_READY`
- `WORKER_DONE`
- `VALIDATION_FAILED`

## Payload contract

Notification text is intentionally small:

```text
Square Orchestrator needs input
Run: <run-id>
Task: <task-id>
Reason: <event>
Next: sqorch status <run-id>
```

## Entry conditions for implementation

- The active run-state schema exists or the activated task includes the smallest needed state
  fields.
- `STATUS.md` activates one exact task from `BUILD-TASKS.md`.
- The task records starting `HEAD`, route, allowed paths, validation, budgets, and acceptance.
- No other writer owns this repository.

## Forbidden behavior

- Remote command approval from a phone.
- Persisting notification secrets, tokens, credentials, prompts, code excerpts, diffs, or logs.
- Sending remote notifications by default.
- Adding third-party dependencies for the local status/watch slice.
- Treating a successful notification as owner acceptance.
- Background daemons, schedulers, or hidden workers in the first slice.

## Budgets

| Budget | Ceiling |
|---|---|
| Third-party dependencies for local status/watch | `0` |
| Third-party dependencies for Windows local notification | `0` unless an activated packet amends it |
| Remote notification providers | `0` until the remote task is explicitly activated |
| External/provider requests | `0` for local tasks |
| Spend | `$0` |
| Destructive actions | `0` |
| Concurrent repository writers | `1` |

## Acceptance authority

The primary session may record technical completion after boundary review. The owner separately
accepts whether notifications are usable enough to become default workflow behavior.
