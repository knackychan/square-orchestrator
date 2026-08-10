# Owner-Input Notifications Build Guide

This guide closes only the parked notification decisions. It cannot activate work.

## Decision register

**N-001 - Status before transport.** First expose pending owner input through local `sqorch status`
or `sqorch status --watch`. A notification transport should read already-recorded run state rather
than invent its own event source.

**N-002 - Notification is not authorization.** A notification can say that input is needed. It
cannot approve a command, resolve a `STOP:`, switch a route, widen paths, or accept a result.

**N-003 - Redacted fixed payload.** The payload contains run ID, task ID, event reason, and the next
local command. It excludes prompts, source, diffs, logs, credential values, environment values, and
free-form worker text.

**N-004 - Local Windows first.** Prefer no-dependency local options first: terminal bell or clear
watch output, then a Windows-specific notification adapter. A polished toast is allowed only when
the activated packet accepts the implementation cost.

**N-005 - Remote is opt-in.** Phone or remote notification is a later adapter. Configuration uses
environment variable names only, records the provider name and redacted delivery result, and sends
the same fixed payload.

**N-006 - No always-on daemon in the first slice.** A foreground watch command is enough until real
run receipts prove a daemon is necessary.

## Event contract

Events are derived from stored run or attempt state. An event is notify-worthy only when it moves
into a blocked, gated, or review-ready state. Repeated polling must not spam the same event.

Minimum dedupe key:

```text
<run-id>|<task-id>|<event>|<attempt-id>
```

## Parked until evidence requires them

- Remote approval from phone.
- Push-notification subscriptions.
- A daemon or tray app.
- Rich notification bodies.
- Per-project notification templates.
- Multiple provider plugins.
