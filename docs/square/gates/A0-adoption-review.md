# A0 — Adoption gate review

- Task: SA00-T05 — Adoption gate A0
- Decision: **BLOCKED**
- Reviewed commit: `6972e30e21103227941863a42bb2c62760793077`
- Branch: `square/main`
- Reviewed UTC: `2026-08-11T04:16:13Z`
- Method: independent raw Git-object, manifest, source-default, targeted-test, and privacy review; receipt claims were not accepted without recomputation.

The fork cannot enter SA01 — Windows lifecycle and AO platform hardening. Critical
source-control, safety-default, authority, and evidence-integrity criteria fail at
the reviewed commit.

## Criterion table

| Criterion | Result | Independent result |
|---|---|---|
| A0-01 — Exact upstream identity | PASS | `v0.12.1^{commit}` and `square-base-v0.12.1^{commit}` both resolve to `1df40e93772c2c48e916870d9c3ddf8f29a69f84`; canonical upstream tag agrees. |
| A0-02 — Controlled Git/remotes/history | FAIL | History is a clean descendant of the baseline, but the required `upstream` remote is absent and the pre-review tree is dirty. No remote `square/main` or baseline tag was observed at origin. |
| A0-03 — Unchanged Windows baseline | FAIL | The committed product boundary is unchanged and command classifications are plausible, but SA00-T02's manifest fails for 13/42 committed entries and no single checkout/blob representation verifies it. The baseline is therefore not independently reproducible as an integrity-bound evidence set. |
| A0-04 — No hidden product/dependency drift | FAIL | The reviewed commit has zero product/dependency/generated diff, but the review workspace contains an unclassified 25-line `frontend/package-lock.json` modification. |
| A0-05 — Apache attribution | PASS | Apache-2.0 is preserved, no upstream project NOTICE exists, and modified-file, third-party notice/SBOM, About, installer/archive, provenance, and trademark duties are recorded. |
| A0-06 — Telemetry disabled by default | FAIL | Packaged startup still supplies the official AO PostHog defaults and packaged daemon remote telemetry defaults to PostHog. No Square hard-off/explicit-opt-in implementation exists. |
| A0-07 — Updater disabled until SA14 | FAIL | The preference defaults off, but packaged startup wires the updater and its fallback feed remains `AgentWrapper/agent-orchestrator`; manual/opt-in checks can reach the official AO feed. |
| A0-08 — AO/Square identity isolation | FAIL | Product/app ID, CLI/env namespace, data/run paths, Electron userData, executable/process identity, and ports remain AO values, including `~/.ao`. |
| A0-09 — Owner-accepted session-first architecture | FAIL | Explicit owner wording exists and is internally clear in the untracked SA00-T04 evidence, including deterministic Task Manager and requested/resolved/actual model identity. It is absent from the reviewed commit and cannot establish committed authority. |
| A0-10 — Authority hashes/index | FAIL | The current untracked authority manifest and index recompute cleanly (21 manifest entries, 20 indexed authority entries, no mismatch), but neither exists in the reviewed commit. |
| A0-11 — Supersession register | FAIL | The untracked register accurately separates preserved research, retired .NET/WPF/global-dashboard paths, and the current AO/session-first direction, but it is absent from the reviewed commit. |
| A0-12 — Complete, privacy-safe evidence | FAIL | Privacy scan found no credential or user-profile value, and T01/T04 manifests verify in their applicable representations. T02 and T03 manifests do not verify against committed bytes (13/42 and 12/16 failures respectively); T04 is uncommitted. Completeness/integrity therefore fails. |

## Raw verification commands and results

| Command | Result |
|---|---|
| `git rev-parse HEAD` | `6972e30e21103227941863a42bb2c62760793077` |
| `git rev-parse "v0.12.1^{commit}"` | `1df40e93772c2c48e916870d9c3ddf8f29a69f84` |
| `git rev-parse "square-base-v0.12.1^{commit}"` | `1df40e93772c2c48e916870d9c3ddf8f29a69f84` |
| `git merge-base --is-ancestor square-base-v0.12.1 HEAD` | exit `0` |
| `git remote -v` | only `origin`; no `upstream` |
| `git status --short --branch` before gate outputs | 3 modified paths and 6 untracked groups; includes `frontend/package-lock.json` and all SA00-T04 authority artifacts |
| `git diff --name-status square-base-v0.12.1..6972e30e -- backend frontend packages package.json package-lock.json .github` | no output |
| `git diff --stat -- frontend/package-lock.json` | 25 insertions |
| `git cat-file -e 6972e30e:<SA00-T04 critical path>` | exit `128` for receipt, authority index, ADR, and supersession register |
| `git ls-remote --heads --tags origin ...` | origin has `main`; no `square/main` or baseline tag |
| raw Git-blob SA00-T01 manifest recomputation | 7/7 pass |
| raw Git-blob SA00-T02 manifest recomputation | 29/42 pass; 13 fail |
| raw Git-blob SA00-T03 manifest recomputation | 4/16 pass; 12 fail |
| worktree SA00-T04 manifest recomputation | 6/6 pass, but input is uncommitted |
| worktree authority manifest/index recomputation | 21/21 hashes pass; 20/20 indexed entries agree, but input is uncommitted |
| source-pack manifest recomputation | 108/108 pass, but source pack is uncommitted |
| `go test ./internal/config -run TestLoadDefaults -count=1 -v` | PASS |
| targeted updater/telemetry Vitest run | 3 files, 77 tests PASS |

Complete command output is preserved in
`docs/square/evidence/SA00-T05/20260811T041613Z/`.

## Accepted pre-existing findings

The following are accepted only as accurately classified upstream/environment
baseline findings; none waives a failed A0 criterion:

- `go build ./...`, `go vet ./...`, CLI e2e, root `npm ci`, npm-10 frontend install, frontend typecheck, package-with-documented-skip, package launch, and daemon/CLI smoke passed in the captured baseline.
- `go test ./...` has recorded Windows-specific upstream failures; root lint consequently fails before completing its full gate.
- `go test -race ./...` was blocked by `CGO_ENABLED=0` and no C toolchain.
- npm 11 rejects the upstream frontend lock while the documented npm-10 toolchain installs it.
- Frontend unit failures are recorded as Windows/macOS-environment-specific.
- Playwright e2e was blocked by an unrelated listener on `::1:5173`; the runtime-session smoke remained unavailable.
- sqlc reproduces known upstream generated drift; OpenAPI/schema generation reproduces.
- The unsigned Windows package launch and daemon smoke created isolated test-time AO state and terminated cleanly.
- Root npm audit reported one moderate and two high vulnerabilities; they were recorded and not repaired during baseline capture.

## Source diff classification

The reviewed commit is a linear descendant of the pinned baseline. Its 76 changed
files are limited to SA00-T01..T03 documentation, receipts, and evidence. There is
no committed backend, frontend, package, lock, generated, or workflow drift.

The current workspace is nevertheless unsuitable for a gate: the dependency
lockfile is modified, and SA00-T04 plus the governing implementation pack are
untracked. Those pre-review changes were preserved and were not edited by this
review.

## Conditions and blockers

This is not a conditional pass. The following critical blockers require a new,
explicitly owner-authorized `SA00-FIXxx` packet and a superseding A0 review:

1. Restore and preserve the canonical `upstream` remote; classify the absence and verify branch/tag tracking.
2. Produce a clean, committed review target containing the accepted SA00-T04 authority, ADR, supersession register, evidence, and receipt; keep unrelated dependency drift out of it.
3. Implement and test Square-safe defaults before A0: telemetry/crash reporting hard-off without explicit owner opt-in, updater hard-disabled through SA14 with no AO feed path, and Square/AO data/runtime/process/env/app identity isolation.
4. Add new superseding evidence that binds the committed bytes for SA00-T02 and SA00-T03 without editing or deleting the prior evidence/receipts; make line-ending policy deterministic.
5. Re-run the unchanged-baseline identity hashes and privacy scan from the clean candidate commit.

## Evidence hashes

- SA00-T01 receipt Git-blob SHA-256: `ab9aded18af5ba58da85e9bd7ba1013125b708d9671a83e99ccc4b2262e43c2b`
- SA00-T02 receipt Git-blob SHA-256: `b4f7a0f70308d25eb2c07e32586d0c2610a0c9aa5df813fd9b9767ae06d051d5`
- SA00-T03 receipt Git-blob SHA-256: `b0ede48dde709abd3d7fc6e8c612851b485d30012032f3c32bdfe5f129cac89b`
- SA00-T04 current-file SHA-256: `bb970904620601aabb2db6f51aa3e3cd4bf6dc4b46d2c600ee10ab5579857027`
- Authority manifest current-file SHA-256: `dfa08707a58515bab62bf959c66b2d02e5d5c361489f964242c624ead8d53dc1`
- SA00-T05 evidence manifest SHA-256: `d74579915ebb237569884bc67ff948196f858881cf159c660f3e0a79f4dcdd2c`

## Exact next authorized tasks

**None.** No SA01 task is authorized. The owner must first create and explicitly
authorize a narrowly scoped `SA00-FIXxx — <human-readable gate-fix name>` packet.
After its receipt is committed, only `SA00-T05 — Adoption gate A0` may run again
as a superseding independent review. SA01-T01, SA01-T02, SA01-T03, SA01-T05,
and all SA02+ work remain unauthorized.

