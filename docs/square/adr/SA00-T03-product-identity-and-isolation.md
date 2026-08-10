# ADR: Square Product Identity and Isolation Design

- Status: proposed
- Date: 2026-08-10
- Task: SA00-T03
- Target: `square/main` at `d61ec3322c48d842b1dd71e3809c0f393acc69f4`

## 1. Context

Square Orchestrator is a maintained downstream fork of Agent Orchestrator (AO) `v0.12.1`, licensed
under Apache 2.0 (copyright 2026 Untrivial). The fork must:

1. Comply with Apache 2.0 obligations (license copy, modified-file notices, NOTICE preservation,
   trademark separation).
2. Prevent Square development and future releases from colliding with official AO installations on
   the same machine.
3. Establish a clear product identity distinct from AO while preserving upstream provenance.
4. Default to safe behavior: telemetry off, LAN listener off, updater off, crash reporting off.

This ADR inventories every identity surface that later implementation tasks must change and
proposes exact Square values or marks owner decisions still pending.

## 2. Observed Identity Surfaces (Current AO Defaults)

### 2.1 Product Name and Branding

| Surface | Observed Value | Source |
|---|---|---|
| Product name | "Agent Orchestrator" | `frontend/package.json:3`, `frontend/forge.config.ts:125,144,155` |
| Window title | "Agent Orchestrator" | `frontend/src/main.ts:93` (`app.setName`) |
| Electron productName | "Agent Orchestrator" | `frontend/package.json:3`, all makers |
| Electron appBundleId | "dev.agent-orchestrator.desktop" | `forge.config.ts:33` |
| macOS appId | "dev.agent-orchestrator.desktop" | `forge.config.ts:124,143,154` |
| Windows AppUserModelId | "dev.agent-orchestrator.desktop" | `frontend/src/main.ts:100` |
| Linux appId | "dev.agent-orchestrator.desktop" | `forge.config.ts:154` |
| Executable name | "agent-orchestrator" | `forge.config.ts:16`, all three OS |
| npm package name | "agent-orchestrator" | `package.json:2`, `frontend/package.json:2` |
| npm published package | "@aoagents/ao" | `README.md:158`, frozen at 0.10.0 |
| GitHub repository | "Untrivial-ai/agent-orchestrator" | `README.md:8-9`, docs |
| Release repository (CI) | "AgentWrapper/agent-orchestrator" | `forge.config.ts:10`, `frontend-release.yml` |
| Description | "Agent Orchestrator" | `frontend/package.json:6` |
| Author | "Agent Orchestrator" | `frontend/package.json:7` |
| Maintainer | "Agent Orchestrator" | `forge.config.ts:169` (deb) |
| Homepage | "https://github.com/aoagents/agent-orchestrator" | `frontend/package.json:9` |
| Twitter/X | "@aoagents" (x.com/aoagents) | `README.md:10` |
| Discord | "discord.com/invite/UZv7JjxbwG" | `README.md:11` |

### 2.2 Data, State, and Runtime Paths

| Surface | Observed Default | Source / Env |
|---|---|---|
| Data directory | `~/.ao/data` | `backend/internal/config/config.go:363` |
| Run file | `~/.ao/running.json` | `backend/internal/config/config.go:349` |
| State home | `~/.ao/` | `backend/internal/config/config.go:366-371` |
| Electron userData | `~/.ao/electron` (packaged), `~/.ao/dev/electron` (dev) | `frontend/src/main.ts:113-115` |
| Dev daemon port | 3002 | `frontend/src/main.ts:148` |
| Dev state subdir | `~/.ao/dev/` | `frontend/src/main.ts:149` |
| Env override | `AO_DATA_DIR`, `AO_RUN_FILE` | `config.go:342,356` |
| Mobile config | `~/.ao/mobile/config.json` | `backend/internal/mobilebridge/config.go:22` |
| Mobile push devices | `~/.ao/mobile/push-devices.json` | `backend/internal/mobilebridge/pushdevices.go:41` |
| Attachments | `.ao/attachments/` | `backend/internal/session_manager/manager.go:315` |
| Preview launch config | `.ao/launch.json` | `backend/internal/previewserver/manager.go:31` |
| Hooks log | `$AO_DATA_DIR/hooks.log` | `backend/internal/cli/hooks.go:27` |
| App state marker | `~/.ao/app-state.json` | `backend/internal/cli/start.go:31` |
| Updater cache | `agent-orchestrator-updater` (in user cache dir) | `forge.config.ts:86` |

### 2.3 Process and Runtime Identity

| Surface | Observed Value | Source |
|---|---|---|
| Daemon PID file | `~/.ao/running.json` (PID, Port, StartedAt, Owner) | `config.go:347-349` |
| Windows named pipe | `\\.\pipe\ao-supervise` (or `ao-supervise-<dirname>` for dev) | `backend/internal/daemon/supervisor/listen_windows.go:17-18` |
| Unix socket | `<dir(runFilePath)>/supervise.sock` | `backend/internal/daemon/supervisor/listen_unix.go` |
| CLI binary | `ao` (Cobra command tree) | `backend/internal/cli/root.go` |
| Agent env exports | `AO_PORT`, `AO_DATA_DIR`, `AO_RUN_FILE`, `AO_AGENT`, `AO_APP_RUN_ID` | `backend/internal/session_manager/manager.go:85-99` |
| Default agent | "claude-code" | `config.go:37` |
| Daemon spawn mode | `AO_OWNER=app` or `persistent` | `frontend/src/main.ts:528` |

### 2.4 Listeners and Ports

| Surface | Observed Value | Source |
|---|---|---|
| Loopback host | `127.0.0.1` (hardcoded, no env var) | `config.go:25` |
| Loopback port | 3001 | `config.go:27` |
| Dev loopback port | 3002 | `main.ts:148` |
| LAN listener bind | `0.0.0.0:<port>` (opt-in only) | `backend/internal/httpd/lan_listener.go:117` |
| LAN listener default port | 3011 (per architecture.md) | `docs/architecture.md` |
| Preview server bind | `127.0.0.1:<dynamic>` | `backend/internal/previewserver/manager.go:734` |
| ConPTY host bind | `127.0.0.1:0` (OS-assigned) | `backend/internal/adapters/runtime/conpty/host_main.go:19` |
| Browser runtime | Loopback TCP via daemon-to-Electron bridge | `main.ts:67-68` |
| No-auth (loopback-only) | All REST, terminal mux, health, readyz | `config.go:2-4` |
| LAN auth | Bearer-password `authMiddleware` | `lan_listener.go` |

### 2.5 URL Protocol / Scheme

| Surface | Observed Value | Source |
|---|---|---|
| OS-level protocol | None registered | Not found in source |
| Internal renderer scheme | `app://renderer` (Electron custom scheme) | `main.ts:161-178` |

### 2.6 Telemetry

| Surface | Observed Default | Source |
|---|---|---|
| Telemetry events | `on` (packaged builds) | `main.ts:440` |
| Remote export | `posthog` (packaged), `off` (dev) | `main.ts:441` |
| PostHog key | `DEFAULT_POSTHOG_PROJECT_KEY` (compiled constant) | `main.ts:64,442` |
| PostHog host | `https://us.i.posthog.com` | `config.go:40`, `main.ts:443` |
| Renderer SDK | `posthog-js` (frontend direct) | `frontend/package.json:85` |
| Daemon sink | `backend/internal/adapters/telemetry/posthog.go` | Go PostHog adapter |
| Kill switch | `AO_TELEMETRY_DISABLED_EVENTS` | `main.ts:448-450` |
| Version stamp | `AO_TELEMETRY_APP_VERSION` | `main.ts:446` |
| Install ID | Anonymous, no person profiles | `docs/telemetry.md:84-89` |
| Rate limits | 5/min burst, 200/day hard | `docs/telemetry.md:77-83` |
| Session recording | Disabled | `docs/telemetry.md:39-40` |
| Feature flags | Disabled | `docs/telemetry.md:45-46` |
| Surveys | Disabled | `docs/telemetry.md:45-46` |
| Disable by env | Set `VITE_AO_POSTHOG_KEY` to empty string | `README.md:194` |

### 2.7 Updater

| Surface | Observed Value | Source |
|---|---|---|
| Updater library | `electron-updater` 6.8.9 | `frontend/package.json:80` |
| Update feed format | `latest.yml`, `latest-mac.yml`, `latest-linux.yml` | `frontend/scripts/feed.mjs` |
| Feed generator | `frontend/scripts/feed.mjs` | Custom script |
| Feed deployment | GitHub Release assets | `.github/workflows/frontend-release.yml` |
| Provider | GitHub (`publisher-github` in forge config) | `forge.config.ts:187-200` |
| Default release repo | `AgentWrapper/agent-orchestrator` | `forge.config.ts:10` |
| Override env | `AO_RELEASE_REPO` | `forge.config.ts:196` |
| Updater cache dir | `agent-orchestrator-updater` | `forge.config.ts:86` |
| App update manifest | `app-update.yml` (baked at build) | `forge.config.ts:72-89` |
| Check on launch | Yes (packaged builds only) | `main.ts:1571-1577` |
| Periodic checks | Yes (electron-updater default interval) | via `startAutoUpdates` |
| Feature channel | `pr<N>` prereleases | `.github/workflows/feature-release.yml` |
| Release workflow | `frontend-release.yml` (gated, signed, notarized) | `.github/workflows/frontend-release.yml` |
| Dev mode | Updater disabled | `main.ts:1572` |
| macOS zip required | Zip permanently required for MacUpdater | `forge.config.ts:134-136` |
| Pre-release env | `AO_RELEASE_PRERELEASE` | `forge.config.ts:197` |

### 2.8 Installer Identity

| Surface | Observed Value | Source |
|---|---|---|
| Windows installer | NSIS (`agent-orchestrator` executable name, Start Menu "Agent Orchestrator") | `makers/maker-nsis.ts` |
| macOS DMG | "Agent Orchestrator" product name | `makers/maker-dmg.ts` |
| Linux AppImage | "Agent Orchestrator" product name | `makers/maker-appimage.ts` |
| Linux DEB | "agent-orchestrator" bin, "Agent Orchestrator" maintainer | `forge.config.ts:160-173` |
| Linux RPM | license: "MIT", homepage: aoagents | `forge.config.ts:174-184` |
| Signing (macOS) | Apple notarization | `forge.config.ts:56-69` |
| Signing (Windows) | Authenticode (NotSigned in SA00-T02 test build) | SA00-T02 evidence |

### 2.9 Release / Distribution Identity

| Surface | Observed Value | Source |
|---|---|---|
| Release artifact naming | `agent-orchestrator-<platform>.<ext>` | `README.md:143-148` |
| Windows download | `agent-orchestrator-win32-x64.exe` | `README.md:145` |
| macOS ARM download | `agent-orchestrator-darwin-arm64.zip` | `README.md:143` |
| macOS Intel download | `agent-orchestrator-darwin-x64.zip` | `README.md:144` |
| Linux AppImage | `agent-orchestrator-linux-x64.AppImage` | `README.md:146` |
| Linux DEB | `agent-orchestrator-linux-x64.deb` | `README.md:147` |
| Linux RPM | `agent-orchestrator-linux-x64.rpm` | `README.md:148` |
| Gated release env | `release` (GitHub environment with reviewer approval) | `frontend-release.yml` |
| Stable channel guard | `release-latest-guard.yml` | `.github/workflows/release-latest-guard.yml` |

## 3. License and Attribution

### 3.1 Upstream License

- **License:** Apache License 2.0
- **Copyright holder:** Untrivial (Copyright 2026 Untrivial)
- **LICENSE file:** Present at repository root; SHA-256 recorded in evidence
- **NOTICE file at project root:** Not present (Apache 2.0 does not require one to exist, only
  that if one exists in the original Work, it must be preserved in Derivative Works)

### 3.2 Third-Party Notices

- Third-party NOTICE files exist only inside `node_modules/` (e.g., Playwright's bundled NOTICE)
- The project ships no consolidated `THIRD_PARTY.md` or `THIRD_PARTY_NOTICES` at the root level
  beyond what each dependency's own package carries
- RPM maker declares license "MIT" for the Electron frontend package
- No software bill of materials (SBOM) generation workflow detected

### 3.3 Apache 2.0 Obligations for the Fork

| Obligation | AO Status | Square Action | Task |
|---|---|---|---|
| 4a: Copy of License | LICENSE at root | Retain; add Square copyright line | SA01 |
| 4b: Modified-file notices | Not present | Add `MODIFIED:` notice on every materially changed file | SA01-SA14 |
| 4c: Preserve attribution notices | README, CONTRIBUTING retain origin | Preserve; add Square attribution | SA01 |
| 4d: NOTICE preservation | No project NOTICE file | If upstream adds one, Square must preserve it | SA14 |
| Trademark separation (§6) | AO trademarks not explicitly declared | Square rebrand satisfies separation | SA01 |
| Fork identification | `<version>` only | Add Square version suffix, About provenance | SA01 |
| Source offer/link | GitHub public repo | Square repo clearly identifies fork origin | SA01 |

## 4. Proposed Square Values

### 4.1 Product Identity (Proposed)

| Surface | Proposed Square Value | Owner Decision |
|---|---|---|
| Product name | Square Orchestrator | **PENDING OWNER** |
| Short name | Square | **PENDING OWNER** |
| CLI command | `square` | **PENDING OWNER** |
| Electron productName | Square Orchestrator | Follows product name |
| Electron appBundleId / appId | `dev.square-orchestrator.desktop` | **PENDING OWNER** |
| Windows AppUserModelId | `dev.square-orchestrator.desktop` | Follows appId |
| Executable name | `square-orchestrator` | **PENDING OWNER** |
| npm package name | `square-orchestrator` (root), TBD scope | **PENDING OWNER** |
| GitHub repository | `knackychan/square-orchestrator` | Already set |
| Release repository | `knackychan/square-orchestrator` | Same as repo |
| Description | "Square Orchestrator — maintained downstream fork of Agent Orchestrator" | **PENDING OWNER** |
| Author | Square Orchestrator | **PENDING OWNER** |
| Homepage | `https://github.com/knackychan/square-orchestrator` | Existing |

### 4.2 Data and State Paths (Proposed)

| Surface | Proposed Square Value | Collision Prevention |
|---|---|---|
| Data directory default | `~/.square/data` | Separate from `~/.ao/data` |
| Run file default | `~/.square/running.json` | Separate from `~/.ao/running.json` |
| State home | `~/.square/` | Separate from `~/.ao/` |
| Electron userData | `~/.square/electron` (packaged), `~/.square/dev/electron` (dev) | Separate from `~/.ao/electron` |
| Dev daemon port | 3102 (offset from AO's 3002) | No collision with AO dev |
| Env namespace | `SQUARE_DATA_DIR`, `SQUARE_RUN_FILE` (no reuse of `AO_*`) | **Env isolation** |
| Mobile config | `~/.square/mobile/` | Separate |
| Updater cache | `square-orchestrator-updater` | Separate from AO updater |
| Agent env exports | `SQUARE_PORT`, `SQUARE_DATA_DIR`, `SQUARE_RUN_FILE`, `SQUARE_AGENT`, `SQUARE_APP_RUN_ID` | No export of `AO_*` vars |
| Default agent | `claude-code` (unchanged agent adapter) | Same adapter IDs, different env |
| Windows named pipe | `\\.\pipe\square-supervise` | Separate from `ao-supervise` |
| Unix socket | `<dir(runFilePath)>/supervise.sock` | Already relative; isolated by dir change |

### 4.3 Listeners and Ports (Proposed)

| Surface | Proposed Square Value | Rationale |
|---|---|---|
| Loopback host | `127.0.0.1` (unchanged) | Same local-only binding; no auth needed |
| Loopback port | 3101 (offset from AO's 3001) | Different default prevents collision |
| Dev loopback port | 3102 (offset from AO's 3002) | Different default prevents dev collision |
| LAN listener | Disabled (opt-in only per own policy) | Same opt-in model; different default port TBD |
| Preview server bind | `127.0.0.1:<dynamic>` (unchanged) | OS-assigned dynamic port per session |

### 4.4 Telemetry (Proposed)

| Surface | Proposed Square Value | Owner Decision |
|---|---|---|
| Telemetry events | `off` by default | **PENDING OWNER** — accept off-by-default? |
| Remote export | `off` by default | **PENDING OWNER** |
| PostHog key | Empty string by default (no transmission) | **PENDING OWNER** — Square telemetry policy? |
| Upstream PostHog | Not configured in Square | Square must not send to AO PostHog project |
| Opt-in policy | Explicit owner opt-in required before any telemetry | **PENDING OWNER** |
| Renderer SDK | Retain but not wired (key = empty) | **PENDING OWNER** |

### 4.5 Updater (Proposed)

| Surface | Proposed Square Value | Owner Decision |
|---|---|---|
| Updater default | Disabled until SA14 | **PENDING OWNER** — accept disabled default? |
| Update feed | Not published until SA14 | **PENDING OWNER** |
| Update repo | `knackychan/square-orchestrator` | Existing; not wired yet |
| Feed format | `latest.yml` / `latest-mac.yml` / `latest-linux.yml` | Same format when enabled |
| Signing (macOS) | TBD — need Square signing identity | **PENDING OWNER** |
| Signing (Windows) | TBD — need Square signing certificate | **PENDING OWNER** |
| AO update isolation | Square must NEVER fetch from `AgentWrapper/agent-orchestrator` or `Untrivial-ai/agent-orchestrator` | Critical safety gate |

### 4.6 Installer Identity (Proposed)

| Surface | Proposed Square Value | Owner Decision |
|---|---|---|
| Windows Start Menu | Square Orchestrator | Following product name |
| Windows installer name | `square-orchestrator-setup.exe` | **PENDING OWNER** |
| macOS bundle | `Square Orchestrator.app` | Following product name |
| Linux AppImage | `square-orchestrator-<platform>.AppImage` | TBD |
| DEB package | `square-orchestrator` | TBD |
| RPM package | `square-orchestrator` | TBD |

### 4.7 Version and Provenance (Proposed)

| Surface | Proposed Value |
|---|---|
| Square version format | `<upstream-version>-square.<n>` (e.g., `0.10.3-square.1`) |
| About dialog | `Square Orchestrator v0.10.3-square.1 — Fork of Agent Orchestrator v0.10.3 (Apache 2.0)` |
| CLI version flag | `square version` shows fork provenance and upstream lineage |
| Diagnostics | Include fork metadata, upstream tag, Square revision |
| npm legacy | `@aoagents/ao` frozen; no Square equivalent unless needed |

## 5. Isolation Matrix — Official AO vs Square Development vs Square Release

| Artifact | Official AO | Square Development | Square Release | Collision Prevention |
|---|---|---|---|---|
| Data directory | `~/.ao/data` | `~/.square/data` (dev: `~/.square/dev/data`) | `~/.square/data` | Different base directory |
| Electron userData | `~/.ao/electron` | `~/.square/dev/electron` | `~/.square/electron` | Different base directory |
| Daemon run file | `~/.ao/running.json` | `~/.square/dev/running.json` | `~/.square/running.json` | Different path |
| SQLite database | `~/.ao/data/<db>` | `~/.square/data/<db>` (dev subdir) | `~/.square/data/<db>` | Different base directory |
| Worktree root | `~/.ao/worktrees/` | `~/.square/worktrees/` (dev subdir) | `~/.square/worktrees/` | Different base directory |
| Logs | `~/.ao/hooks.log` | `~/.square/hooks.log` (dev subdir) | `~/.square/hooks.log` | Different base directory |
| Cache | `~/.ao/*` | `~/.square/dev/*` | `~/.square/*` | Different base directory |
| Crash dumps | `~/.ao/electron/<Crashpad>` | `~/.square/dev/electron/<Crashpad>` | `~/.square/electron/<Crashpad>` | Encapsulated by userData |
| Process/executable name | `agent-orchestrator` / `ao` | `square-orchestrator` / `square` | `square-orchestrator` / `square` | Different process names |
| Loopback listener | `127.0.0.1:3001` | `127.0.0.1:3102` | `127.0.0.1:3101` | Different default ports |
| LAN listener | `0.0.0.0:3011` (opt-in) | Opt-in only; Square default port TBD | Opt-in only; Square default port TBD | Different default port |
| OS protocol handler | None | None | None (future: TBD) | No protocol collision |
| Auto-update feed | `AgentWrapper/agent-orchestrator` | Disabled (dev) | Disabled until SA14; then `knackychan/square-orchestrator` | Different repo; AO feed never checked |
| Telemetry project | AO PostHog project | Off; no PostHog key | Off by default; Square project only if owner opts in | Different PostHog key or empty |
| Installer app ID | `dev.agent-orchestrator.desktop` | Not applicable | `dev.square-orchestrator.desktop` | Different appId |
| Start Menu entry | "Agent Orchestrator" | Not applicable | "Square Orchestrator" | Different name |
| Windows named pipe | `\\.\pipe\ao-supervise` | `\\.\pipe\square-supervise-dev` | `\\.\pipe\square-supervise` | Different pipe name |
| Unix supervisor socket | `~/.ao/dev/supervise.sock` | `~/.square/dev/supervise.sock` | `~/.square/supervise.sock` | Different base directory |
| Env namespace | `AO_*` | `SQUARE_*` | `SQUARE_*` | Different prefix prevents propagation |

### Isolation Guarantees

1. **No shared data directory:** Official AO and Square use completely separate `~/.ao/` vs
   `~/.square/` trees. No implicit data migration or reading.
2. **No shared process namespace:** Different executable names, different named pipes, different
   run files. AO and Square daemons cannot discover or attach to each other.
3. **No cross-update:** Square never reads from AO's update feeds. Updater is disabled by default.
   When enabled, Square fetches only from `knackychan/square-orchestrator`.
4. **No cross-telemetry:** Square does not send to AO's PostHog project. Telemetry is off by
   default.
5. **No env collision:** Square uses `SQUARE_*` env namespace exclusively. AO uses `AO_*`. A
   process spawned with both sets is unambiguous.
6. **Same machine coexistence:** Both applications can run simultaneously without port conflict
   (different defaults), data corruption (different paths), or process interference (different
   identities).
7. **CLI coexistence:** `ao` and `square` are separate commands. An `ao` compatibility alias
   policy is an owner decision, not a default.

## 6. Pending Owner Decisions

The following decisions require owner input before implementation tasks SA01 and beyond:

1. **Product name:** "Square Orchestrator" proposed; confirm or specify alternative.
2. **Short name:** "Square" proposed; confirm.
3. **CLI command:** `square` proposed; confirm. AO compatibility alias policy (none, `ao` symlink,
   `square ao` subcommand)?
4. **appId:** `dev.square-orchestrator.desktop` proposed; confirm or specify.
5. **Telemetry default:** OFF proposed for all Square builds. Accept? If opt-in, what is the
   Square PostHog project/key?
6. **Updater default:** DISABLED until SA14 proposed. Accept?
7. **Updater feed:** `knackychan/square-orchestrator` GitHub Releases. Accept? When to activate?
8. **Signing:** Square needs signing identities for macOS (Apple Developer) and Windows
   (Authenticode certificate). Procure before SA14?
9. **Fork version scheme:** `<upstream>-square.<n>` proposed. Confirm?
10. **npm publication:** Square does not publish to npm initially. Confirm? If publish to npm,
    scope?
11. **Crash reporting:** Disabled by default. Confirm? If enabled, what backend?
12. **Mobile/LAN listener:** Disabled by default. Confirm? Square-specific default LAN port?
13. **Trademark and legal review:** Apache 2.0 §6 trademark separation. Advise owner to review
    Square name with legal counsel.

## 7. Enforcing Implementation Tasks

| Identity Surface | Enforcing Task |
|---|---|
| Product name, appId, executable, CLI | SA01 (Square rebrand and codebase isolation) |
| Data paths, env namespace | SA01 (config rewrite to `SQUARE_*`) |
| Telemetry off-by-default | SA01 (env defaults; PostHog key empty) |
| Updater disable | SA01 (remove AO feed wiring) |
| Updater Square feed | SA14 (release pipeline and signing) |
| LAN listener policy | SA01 (default disabled; Square port) |
| License, NOTICE, attribution | SA01 (Square copyright/notice additions) |
| Installer identity | SA01 (forge config maker updates) |
| Logo, branding assets | SA01 (Square logo replacement) |
| Version and provenance | SA01 (About dialog, CLI version) |
| Release pipeline | SA14 (GitHub Actions, signing, notarization) |
| Isolation verification | SA01 (coexistence tests) |
| Third-party notice bundle | SA01 (SBOM or THIRD_PARTY.md generation) |

## 8. Review Checklist

- [x] Every observed identity surface catalogued
- [x] Proposed Square values listed or marked owner-pending
- [x] Isolation matrix covers all runtime artifacts
- [x] Collision prevention verified for every artifact
- [x] Telemetry disposition: off by default proposed
- [x] Updater disposition: disabled until SA14 proposed
- [x] LAN listener disposition: disabled by default proposed
- [x] License obligations mapped to implementation tasks
- [x] Product source unchanged (verified empty diff)
- [x] No credentials or secrets in this document
- [ ] Owner review: product name, appId, signing, telemetry policy, updater policy
