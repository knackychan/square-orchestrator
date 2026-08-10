# Low-Tier Research Delegation Build Tasks

These tasks are inactive unless root `STATUS.md` activates one exactly.

## T-RESEARCH-00 - Preview the available model catalog

Create a read-only catalog projection for exact approved client/model routes. Do not query live
providers or launch clients in this task.

<!-- sqorch:task v1 -->
```toml
schema = 1
id = "T-RESEARCH-00"
role = "IMPLEMENT"
mode = "write"
starting_commit = "ACTIVATION_REQUIRED"
allowed_paths = ["sqorch/", "tests/"]
forbidden_paths = [".git/", ".env", "STATUS.md", "HANDOVER.md", "CLIENT-EXECUTION.md"]
validation = ["python -m unittest discover -s tests -v"]
expected_commit_message = "feat: preview model route catalog"
external_call_limit = 0
spend_limit_usd = 0
turn_limit = 100
token_rotation_limit = 150000
client = "cmdc"
model = "deepseek/deepseek-v4-flash"
automatic_fallback = false
evidence_destination = "docs/superpowers/plans/2026-08-05-low-tier-research-delegation/STATE.md"
acceptance_authority = "primary session technical review"
```
<!-- /sqorch:task -->

Required assertions:

```python
assert catalog["generated_at"]
assert catalog["entries"][0]["exact_model_id"]
assert catalog["entries"][0]["cost_class"] in {"low", "standard", "high"}
assert catalog["entries"][0]["client"] in {"cmdc", "opencode", "claude", "codex"}
assert catalog["selection_performed"] is False
```

Commit message: `feat: preview model route catalog`.

## T-RESEARCH-01 - Validate research briefs and reports

Implement validation for explicit research briefs and report metadata. Do not launch workers or
perform web requests.

<!-- sqorch:task v1 -->
```toml
schema = 1
id = "T-RESEARCH-01"
role = "IMPLEMENT"
mode = "write"
starting_commit = "ACTIVATION_REQUIRED"
allowed_paths = ["sqorch/", "tests/"]
forbidden_paths = [".git/", ".env", "STATUS.md", "HANDOVER.md", "CLIENT-EXECUTION.md"]
validation = ["python -m unittest discover -s tests -v"]
expected_commit_message = "feat: validate research reports"
external_call_limit = 0
spend_limit_usd = 0
turn_limit = 100
token_rotation_limit = 150000
client = "cmdc"
model = "deepseek/deepseek-v4-flash"
automatic_fallback = false
evidence_destination = "docs/superpowers/plans/2026-08-05-low-tier-research-delegation/STATE.md"
acceptance_authority = "primary session technical review"
```
<!-- /sqorch:task -->

Required assertions:

```python
assert validate_brief(file_research).ok
assert validate_brief(web_without_request_ceiling).error.code == "INVALID_INPUT"
assert validate_report(missing_sources).error.code == "INVALID_INPUT"
assert validate_report(raw_secret_excerpt).error.code == "INVALID_INPUT"
```

Commit message: `feat: validate research reports`.

## T-RESEARCH-02 - Dry-run a read-only file research assignment

Preview the exact prompt and route record for a low-tier file research worker. Do not launch a
real client in this task. The preview may choose from a supplied approved model catalog, but it
must record that selection as a proposal only.

<!-- sqorch:task v1 -->
```toml
schema = 1
id = "T-RESEARCH-02"
role = "IMPLEMENT"
mode = "write"
starting_commit = "ACTIVATION_REQUIRED"
allowed_paths = ["sqorch/", "tests/"]
forbidden_paths = [".git/", ".env", "STATUS.md", "HANDOVER.md", "CLIENT-EXECUTION.md"]
validation = ["python -m unittest discover -s tests -v"]
expected_commit_message = "feat: preview file research delegation"
external_call_limit = 0
spend_limit_usd = 0
turn_limit = 100
token_rotation_limit = 150000
client = "cmdc"
model = "deepseek/deepseek-v4-flash"
automatic_fallback = false
evidence_destination = "docs/superpowers/plans/2026-08-05-low-tier-research-delegation/STATE.md"
acceptance_authority = "primary session technical review"
```
<!-- /sqorch:task -->

Required assertions:

```python
assert preview["launch_performed"] is False
assert preview["mode"] == "read-only"
assert preview["external_call_limit"] == 0
assert preview["selected_route"]["exact_model_id"] in approved_catalog_model_ids
assert "full repository dump" not in preview["handoff_prompt"].lower()
```

Commit message: `feat: preview file research delegation`.

## T-RESEARCH-03 - Add web research preview with hard budgets

Preview a web research assignment with explicit source rules and request ceilings. Do not contact
the network in this task.

<!-- sqorch:task v1 -->
```toml
schema = 1
id = "T-RESEARCH-03"
role = "IMPLEMENT"
mode = "write"
starting_commit = "ACTIVATION_REQUIRED"
allowed_paths = ["sqorch/", "tests/"]
forbidden_paths = [".git/", ".env", "STATUS.md", "HANDOVER.md", "CLIENT-EXECUTION.md"]
validation = ["python -m unittest discover -s tests -v"]
expected_commit_message = "feat: preview web research delegation"
external_call_limit = 0
spend_limit_usd = 0
turn_limit = 100
token_rotation_limit = 150000
client = "cmdc"
model = "deepseek/deepseek-v4-flash"
automatic_fallback = false
evidence_destination = "docs/superpowers/plans/2026-08-05-low-tier-research-delegation/STATE.md"
acceptance_authority = "primary session technical review"
```
<!-- /sqorch:task -->

Required assertions:

```python
assert preview["delivery_performed"] is False
assert preview["network_performed"] is False
assert preview["request_ceiling"] > 0
assert preview["domains"] == expected_domains
```

Commit message: `feat: preview web research delegation`.

## T-RESEARCH-04 - Compile higher-tier handoff context

Compile a compact higher-tier handoff from one or more accepted research reports. Do not include
uncited raw context.

<!-- sqorch:task v1 -->
```toml
schema = 1
id = "T-RESEARCH-04"
role = "IMPLEMENT"
mode = "write"
starting_commit = "ACTIVATION_REQUIRED"
allowed_paths = ["sqorch/", "tests/"]
forbidden_paths = [".git/", ".env", "STATUS.md", "HANDOVER.md", "CLIENT-EXECUTION.md"]
validation = ["python -m unittest discover -s tests -v"]
expected_commit_message = "feat: compile research handoff"
external_call_limit = 0
spend_limit_usd = 0
turn_limit = 100
token_rotation_limit = 150000
client = "cmdc"
model = "deepseek/deepseek-v4-flash"
automatic_fallback = false
evidence_destination = "docs/superpowers/plans/2026-08-05-low-tier-research-delegation/STATE.md"
acceptance_authority = "primary session technical review"
```
<!-- /sqorch:task -->

Required assertions:

```python
assert handoff["report_hash"] == expected_hash
assert handoff["raw_context_included"] is False
assert handoff["source_count"] == len(report_sources)
assert len(handoff["summary"].split()) <= 500
```

Commit message: `feat: compile research handoff`.
