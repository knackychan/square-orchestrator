# Owner-Input Notifications State

State records evidence. It does not grant authority.

## Planning position

| Field | Value |
|---|---|
| Current activity | parked plan only |
| Planning authorization | owner request on 2026-08-05 to add a dedicated plan |
| Implementation authority | none from this plan |
| Delegated agent authority | none |
| External calls / spend | `0 / $0` |
| Dependencies | `0` |
| Open `STOP:` items | none |

## Planning evidence

- Dedicated plan created for owner-input notifications.
- Scope is notification only, not remote command approval.
- First slice is local status/watch so notification transport has deterministic state to read.
- Local Windows notification is second.
- Phone or remote notification is third and remains opt-in, redacted, and initially preview-only.

## Carry-forward

- Activate no task from this plan until `STATUS.md` records the exact task and starting commit.
- Keep notification payloads fixed and redacted.
- Add remote delivery only after local notification behavior is accepted.
