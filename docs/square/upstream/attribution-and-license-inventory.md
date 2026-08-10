# Attribution and License Inventory

- Task: SA00-T03
- Date: 2026-08-10
- Upstream: Agent Orchestrator `v0.12.1` commit `1df40e93772c2c48e916870d9c3ddf8f29a69f84`
- Fork: `square/main` at `d61ec3322c48d842b1dd71e3809c0f393acc69f4`

## 1. Upstream License

### 1.1 Primary License

- **License:** Apache License 2.0
- **Full text:** Present at `LICENSE` in repository root
- **Copyright notice:** `Copyright 2026 Untrivial`
- **LICENSE SHA-256:** Recorded in evidence (`license-sha256.txt`)

### 1.2 License Applicability

The Apache 2.0 license covers the entire upstream repository as a Work. The copyright holder is
Untrivial. The frontend `package.json` declares license "MIT" for the Electron package, but the
root `LICENSE` file is Apache 2.0 and governs the project. The RPM maker configuration (`forge.config.ts:180`)
setting `license: "MIT"` is a build artifact, not a superseding license declaration.

For Square: the fork retains Apache 2.0 for the modified Work. Square adds its own copyright line
and follows §4 for modified-file notices.

## 2. NOTICE File Status

### 2.1 Project-Level NOTICE

**No NOTICE file exists at the repository root.** Apache 2.0 §4(d) requires preservation of a
NOTICE file only if one is included in the original Work. Since the upstream does not ship one,
Square has no obligation to create one. Square may add its own Square NOTICE file as an addendum
(permitted by §4(d) last sentence).

### 2.2 Third-Party NOTICE Files

The following NOTICE files exist only inside `node_modules/` directories and are part of
third-party dependency distributions, not the project's own attribution surface:

| File | Owner | License |
|---|---|---|
| `frontend/node_modules/@playwright/test/NOTICE` | Microsoft | Apache 2.0 |
| `frontend/node_modules/playwright/NOTICE` | Microsoft | Apache 2.0 |
| `frontend/node_modules/playwright-core/NOTICE` | Microsoft | Apache 2.0 |
| `frontend/src/landing/node_modules/@fastify/*/NOTICE` | OpenJS Foundation | MIT |
| `frontend/src/landing/node_modules/@prisma/*/NOTICE` | Prisma | Apache 2.0 |

These are preserved as shipped by their respective upstreams. Square must not remove or alter them.

## 3. Third-Party Attribution

### 3.1 Shipped Third-Party Notices

The upstream project does not currently ship a consolidated `THIRD_PARTY.md` or `THIRD_PARTY_NOTICES`
file at the repository root. Dependency attribution is implicit through:

- `package.json` and `package-lock.json` (npm dependencies with their declared licenses)
- `frontend/package.json` and `frontend/package-lock.json`
- `go.mod` and `go.sum` (Go module dependencies)

### 3.2 Key Third-Party Dependencies (License Context)

#### Go Backend (selection)
| Module | License |
|---|---|
| chi router | MIT |
| sqlc codegen | MIT |
| golangci-lint | GPL 3.0 (dev tool only, not runtime) |
| cobra CLI | Apache 2.0 |
| SQLite (via CGO or modernc) | Public Domain |

#### Node / Electron Frontend (selection)
| Package | License |
|---|---|
| electron | MIT |
| electron-updater | MIT |
| posthog-js | MIT |
| react | MIT |
| tailwindcss | MIT |
| @radix-ui/* | MIT |
| xterm | MIT |
| openapi-typescript | MIT |
| playwright | Apache 2.0 |
| vitest | MIT |

### 3.3 SBOM / Notice Generation

The upstream does not automatically generate a software bill of materials (SBOM) in its CI/CD
pipelines. Square should consider generating an SPDX SBOM or consolidated notice bundle as part of
the SA01 or SA14 release pipeline. This is an owner decision.

## 4. Files Likely to Be Materially Modified

The following files are identified as likely to require material modification (beyond simple
string replacement) for Square rebrand and isolation:

| File | Modification | Apache §4(b) Notice |
|---|---|---|
| `frontend/forge.config.ts` | Product name, appId, executable, repo, publishers | Required |
| `frontend/src/main.ts` | Product name, AppUserModelId, userData path, telemetry defaults, updater repo | Required |
| `frontend/package.json` | name, productName, description, author, homepage, repository | Required |
| `package.json` | name | Required |
| `backend/internal/config/config.go` | Env var names (AO_* → SQUARE_*), default paths (~/.ao → ~/.square) | Required |
| `backend/internal/daemon/supervisor/listen_windows.go` | Named pipe name (ao-supervise → square-supervise) | Required |
| `backend/internal/daemon/supervisor/listen_unix.go` | (path is relative to run-file dir; isolated by path change) | Required |
| `backend/internal/session_manager/manager.go` | Env var exports (AO_* → SQUARE_*) | Required |
| `README.md` | Product name, download links, repo references, logo, telemetry section | Required |
| `docs/*.md` | Product name references, architecture, URLs | Required |
| `frontend/src/main/auto-updater.ts` | Update feed repo, app name in update dialogs | Required |
| `frontend/makers/*.ts` | Product name, appId, executable name | Required |
| `frontend/scripts/feed.mjs` | Feed filename prefix (ao-* → square-*) | Required |
| `frontend/app-update.yml` | Generated at build; repo references baked from forge config | Not directly modified |
| Agent adapters (23 files) | Env var exports to spawned agents | Required |
| `frontend/e2e/*.ts` | E2E test expectations for product name, paths | Required |
| `.github/workflows/*.yml` | Release repo, artifact naming, release pipeline | Required (SA14) |

This list is an inventory, not an authorization to modify. Implementation authority is per-task.

## 5. Modified-File Notice Policy (Proposed)

Apache 2.0 §4(b) requires prominent notices on modified files stating they were changed. Proposed
Square approach:

1. **Single-line header** in every materially modified source file:
   ```
   // Modified for Square Orchestrator (https://github.com/knackychan/square-orchestrator)
   ```

2. **Commit attribution:** Every Square commit references the upstream lineage when modifying an
   upstream-origin file.

3. **Release notice:** A `NOTICE.square.md` file at the repository root containing:
   - Original Apache 2.0 copyright
   - Square copyright addition
   - Statement that this is a modified version of Agent Orchestrator
   - Link to original source
   - List of major modification categories

4. **About dialog:** Square version and provenance information clearly displayed.

## 6. Source Offer and Provenance

Apache 2.0 does not require a "source offer" (that is a (L)GPL concept), but best practice for
fork transparency recommends:

1. **README fork notice:** Clear statement in README.md that Square is a fork of AO.
2. **About provenance:** App About dialog shows fork lineage.
3. **Version provenance:** Version string includes both upstream origin version and Square
   revision.
4. **Repository link:** Square GitHub repository clearly documented.

## 7. Trademark Separation

Apache 2.0 §6 explicitly states the license does not grant trademark rights. Square must not use
"Agent Orchestrator," "AO," or "Untrivial" marks in a way that implies endorsement or official
affiliation. The Square product name, logo, and branding must be clearly distinct.

### 7.1 Trademarks to Avoid (Observed)

| Mark | Context | Square Action |
|---|---|---|
| "Agent Orchestrator" | Product name | Do not use as product name; reference only for provenance |
| "AO" | Acronym throughout codebase | Do not use as Square branding |
| "Untrivial" | Copyright holder | Do not use; Square copyright is separate |
| "@aoagents" | Twitter/X, npm scope, docs site | Do not use in Square materials |
| "aoagents.dev" | Documentation website | Do not reference as Square support |
| "AgentWrapper" | Release repository org | Do not push to or pull from |

### 7.2 Permissible References

Square may reference "Agent Orchestrator" and "Apache 2.0" for:
- Attribution and provenance in About, README, and documentation
- Reproducing the content of the LICENSE file
- Describing the fork origin in compliance with §6

These references must be factual and not imply endorsement.

## 8. Implementation Checklist

| Obligation | Status | Implementation Task |
|---|---|---|
| LICENSE file retained | License at root; Square copyright line added | SA01 |
| Modified-file notices | Add to every materially changed file | SA01-SA14 |
| Attribution notices preserved | README, upstream copyright retained | SA01 |
| NOTICE preservation (if upstream adds one) | None currently required | SA14 |
| Trademark separation | Square naming, logo, branding distinct | SA01 |
| Fork provenance in About | Add to About dialog and CLI version | SA01 |
| Third-party notice bundle | `NOTICE.square.md` or SBOM | SA01 or SA14 |
| npm scope separation | No @aoagents reuse | SA01 (or no npm publish) |
| GitHub repo separation | Existing `knackychan/square-orchestrator` | Done |
| Release artifact naming | Square-named artifacts | SA14 |

## 9. Open Legal / Owner Decisions

The following items are flagged for owner or legal review before SA01 commits:

1. **Square product name:** Confirm "Square Orchestrator" does not conflict with existing
   trademarks, registered marks, or confusingly similar product names.
2. **NOTICE file:** Owner decides whether to create a `NOTICE.square.md` addendum.
3. **SBOM generation:** Owner decides whether to add automated SBOM generation to the CI/CD
   pipeline.
4. **Frontend license field:** `frontend/package.json` currently declares "MIT"; should Square
   change this to "Apache-2.0" (matching root LICENSE) or keep as-is?
5. **npm publication scope:** If Square publishes to npm, what scope/name?

**STOP:** Legal, trademark, and licensing decisions are owner/legal resolutions. This inventory
does not provide legal advice and does not pre-approve any rebrand.
