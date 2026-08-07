# Low-Tier Research Delegation Build Guide

This guide closes the parked design for cheap read-only research workers and report handoff. It
cannot activate work.

## Decision register

**R-001 - Lower-tier models do grunt work.** Use cheaper approved routes for bounded search,
inventory, and source extraction. Reserve higher-tier models for judgement, architecture, review,
and final synthesis.

**R-002 - Reports are the context transfer.** The worker writes a compact report. The higher-tier
model receives the report path, report hash, and exact source references instead of broad raw
context.

**R-003 - Briefs are explicit.** Every research task starts from a bounded prompt with the exact
question, allowed sources, forbidden sources, source count ceiling, output schema, and stop
conditions.

**R-004 - Research is read-only.** A research worker may inspect and report. It may not edit source,
run destructive commands, approve commands, install dependencies, or resolve owner questions.

**R-005 - Web is separately budgeted.** File research uses zero external requests. Web research must
name domains or source classes, request ceilings, spend ceilings, retrieval date, and citation
requirements.

**R-006 - Evidence beats confidence.** The report must separate cited facts, inference, uncertainty,
and unanswered questions. Unsupported conclusions are rejected.

**R-007 - Low-tier route is catalog-driven.** `gpt-5.4-mini` is an example candidate, not the
default forever. The higher-tier primary may choose an exact low-tier route from the approved model
catalog when the active packet permits it.

**R-008 - The catalog is evidence, not authority.** A model catalog records exact IDs, client,
rough cost class, supported task classes, launch profile, and verification age. It cannot widen a
packet, enable fallback, or select a model by itself.

## Research brief schema

```text
Question: <one exact question>
Purpose: <why the primary needs this>
Research type: <file | web | comparison>
Allowed sources: <paths, globs, commits, domains, or source classes>
Forbidden sources: <paths, domains, secrets, generated/runtime state>
Budget: <max files, max URLs, max commands, max external requests, max spend>
Output: <report path and schema version>
Stop when: <ambiguity, missing access, budget limit, conflicting evidence>
```

## Model catalog schema

```text
Generated at:
Source profile:
Entries:
  - client:
    exact_model_id:
    cost_class: low | standard | high
    task_classes: file-research | web-research | comparison | implementation | review
    launch_profile:
    last_verified_at:
    availability_evidence:
    caveats:
```

The catalog is optimized for quick reading by the primary or higher-tier model. It should be short,
sorted by task class and cost class, and include only exact launchable routes from the adopted
project profile.

## Report schema

```text
# Research Report

Question:
Scope:
Route:
Budget used:
Commands or queries run:
Sources:
Findings:
Uncertainties:
Recommended next reads:
Token-saving summary:
STOP items:
```

Each source entry records:

```text
id:
type: file | url | command-output
locator:
retrieved_at:
why_it_matters:
```

## Higher-tier handoff prompt

```text
Use the attached research report as evidence, not authority. Answer the original task using only
the cited facts unless you inspect a cited source directly. Do not assume the research worker's
recommendation is correct. If evidence is missing, ask for another bounded research pass or record
STOP.
```

## Parked until evidence requires them

- Parallel research swarms.
- Report ranking or scoring.
- Automatic long-context packing.
- Remote web crawling.
- Persistent cross-project research catalogue.
- Automatic implementation from a research report.
