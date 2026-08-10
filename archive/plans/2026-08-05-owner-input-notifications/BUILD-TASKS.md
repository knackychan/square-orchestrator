# Owner-Input Notifications Build Tasks

These tasks are inactive unless root `STATUS.md` activates one exactly.

## T-NOTIFY-01 - Expose pending owner input locally

Add the smallest local status or watch surface that reports pending input events from existing run
state. Do not send OS or remote notifications in this task.

<!-- sqorch:task v1 -->
```toml
schema = 1
id = "T-NOTIFY-01"
role = "IMPLEMENT"
mode = "write"
starting_commit = "ACTIVATION_REQUIRED"
allowed_paths = ["sqorch/", "tests/"]
forbidden_paths = [".git/", ".env", "STATUS.md", "HANDOVER.md", "CLIENT-EXECUTION.md"]
validation = ["python -m unittest discover -s tests -v"]
expected_commit_message = "feat: show pending owner input"
external_call_limit = 0
spend_limit_usd = 0
turn_limit = 100
token_rotation_limit = 150000
client = "cmdc"
model = "deepseek/deepseek-v4-flash"
automatic_fallback = false
evidence_destination = "docs/superpowers/plans/2026-08-05-owner-input-notifications/STATE.md"
acceptance_authority = "primary session technical review"
```
<!-- /sqorch:task -->

Required assertions:

```python
assert pending_events(run_state) == expected_events
assert status_json["data"]["owner_input_required"] is True
assert status_json["data"]["next_action"] == "sqorch status <run-id>"
```

Commit message: `feat: show pending owner input`.

## T-NOTIFY-02 - Add a local Windows notification adapter

Notify locally when a run enters a pending owner-input state. Prefer standard-library subprocess
argument arrays and a no-dependency Windows mechanism. Do not add a remote provider.

<!-- sqorch:task v1 -->
```toml
schema = 1
id = "T-NOTIFY-02"
role = "IMPLEMENT"
mode = "write"
starting_commit = "ACTIVATION_REQUIRED"
allowed_paths = ["sqorch/", "tests/"]
forbidden_paths = [".git/", ".env", "STATUS.md", "HANDOVER.md", "CLIENT-EXECUTION.md"]
validation = ["python -m unittest discover -s tests -v"]
expected_commit_message = "feat: notify owner on windows"
external_call_limit = 0
spend_limit_usd = 0
turn_limit = 100
token_rotation_limit = 150000
client = "cmdc"
model = "deepseek/deepseek-v4-flash"
automatic_fallback = false
evidence_destination = "docs/superpowers/plans/2026-08-05-owner-input-notifications/STATE.md"
acceptance_authority = "primary session technical review"
```
<!-- /sqorch:task -->

Required assertions:

```python
assert render_notification(event) == expected_fixed_payload
assert "secret" not in render_notification(event_with_secret_text).lower()
assert duplicate_event_is_not_sent_twice
```

Commit message: `feat: notify owner on windows`.

## T-NOTIFY-03 - Add opt-in remote notification preview

Add a dry-run or preview-only remote notifier configuration check. It must not contact a provider
until a later task explicitly activates provider delivery and external request budgets.

<!-- sqorch:task v1 -->
```toml
schema = 1
id = "T-NOTIFY-03"
role = "IMPLEMENT"
mode = "write"
starting_commit = "ACTIVATION_REQUIRED"
allowed_paths = ["sqorch/", "tests/"]
forbidden_paths = [".git/", ".env", "STATUS.md", "HANDOVER.md", "CLIENT-EXECUTION.md"]
validation = ["python -m unittest discover -s tests -v"]
expected_commit_message = "feat: preview remote owner notifications"
external_call_limit = 0
spend_limit_usd = 0
turn_limit = 100
token_rotation_limit = 150000
client = "cmdc"
model = "deepseek/deepseek-v4-flash"
automatic_fallback = false
evidence_destination = "docs/superpowers/plans/2026-08-05-owner-input-notifications/STATE.md"
acceptance_authority = "primary session technical review"
```
<!-- /sqorch:task -->

Required assertions:

```python
assert preview["delivery_performed"] is False
assert preview["secret_values_recorded"] is False
assert preview["payload"] == expected_fixed_payload
```

Commit message: `feat: preview remote owner notifications`.
