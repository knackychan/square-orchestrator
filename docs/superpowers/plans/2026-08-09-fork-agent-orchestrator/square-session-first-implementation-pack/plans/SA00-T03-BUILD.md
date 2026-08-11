# SA00-T03 Build Guide — License, attribution, identity, telemetry, updater, and data isolation design

## Route and execution boundary

- Exact client/model: `opencode` / `opencode-go/deepseek-v4-pro`
- Variant: `high`
- Visible surface: foreground process in a visible VS Code integrated terminal
- Automatic fallback and model substitution: disabled
- Risk class: `silent-failure` because identity, updater, telemetry, and state-isolation mistakes can remain invisible until release or coexistence
- Budget: 100 worker turns, 150000 reported input-token rotation, zero spend, one writer
- Target worktree: `D:\WORK\10 - AI\AI TOOLS\square-orchestrator-work-square-main`
- Starting branch and commit: `square/main` at `d61ec3322c48d842b1dd71e3809c0f393acc69f4`
- Required commit: `docs: design SA00-T03 product identity isolation`

The planning/authority checkout is `D:\WORK\10 - AI\AI TOOLS\square-orchestrator` and is read-only
during worker execution. The worker writes only the allowlisted paths in the packet inside the
target worktree. No product source, package metadata, dependency, generated runtime source,
installer, updater, telemetry configuration, or behavior may change.

## Evidence and design decisions

1. Read the packet, authority documents, and pinned Agent Orchestrator source before any write.
2. Use read-only content searches, source inspection, and file hashes to inventory every identity,
   state, listener, telemetry, updater, attribution, and coexistence surface named by the packet.
3. Record proposed Square values separately from owner decisions still pending. Do not present a
   proposal as shipped behavior or silently resolve a legal, trademark, privacy, or security STOP.
4. Treat README claims as leads only. Source, build configuration, release workflows, and package
   metadata are authoritative for observed behavior.
5. Do not call model providers, publish, sign, push, install dependencies, launch the AO app, or
   alter remote services. Network access is not required for this design inventory.
6. Never read, print, persist, or stage credentials, tokens, private keys, or secret-like values.

## Required outputs

The worker must produce:

- `docs/square/adr/SA00-T03-product-identity-and-isolation.md`
- `docs/square/upstream/attribution-and-license-inventory.md`
- `docs/square/upstream/identity-surface-inventory.json`
- `docs/square/upstream/telemetry-updater-network-inventory.json`
- timestamped evidence under `docs/square/evidence/SA00-T03/<UTC>/`
- `docs/square/receipts/SA00-T03.completion.json`

Evidence must include the exact search commands and redacted results, source/symbol inventory
hashes, license and NOTICE hashes, isolation and network matrices, ADR review checklist, command
matrix, summary, manifest, and exact product-source diff proving no source behavior changed.

## Validation contract

Before commit, verify:

- all required JSON parses;
- every evidence manifest hash recomputes with zero mismatches;
- credential-pattern scans report counts without printing matches;
- `git diff --check` passes;
- the product-source diff against `d61ec3322c48d842b1dd71e3809c0f393acc69f4` is empty;
- changed paths are exactly the packet allowlist;
- the receipt states pending owner decisions and whether SA00-T04 may proceed;
- the target worktree is clean after the exact commit and no push occurred.

Stop before editing if the target, route, visible terminal, allowance, budget, no-fallback setting,
source pin, or write scope differs. Stop for ambiguous license, trademark, telemetry, updater,
listener, or coexistence obligations instead of inventing an answer.
