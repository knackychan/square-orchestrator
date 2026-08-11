# Archive Research Line — Reusable Findings and Behavior Index

- Status: planning reference for this pack; not authority
- Prepared: 2026-08-09 (repository reorganization pass)
- Source scope: frozen pre-fork research under `/archive/` and superseded planning history under
  `archive/plans/specs/` and `docs/superpowers/plans/2026-08-0[57]-*/`
- Policy: per `UPSTREAM_GOVERNANCE.md`, port **behavior/findings**, not source. Nothing in this
  document licenses copying .NET, C#, WPF, Win32, Node, or Python implementation code into the fork.

## 1. Why this document exists

The pre-fork line (.NET 10 daemon/CLI, WPF/WebView2, Windows named pipes, Python M1 CLI, proofs) was
archived on 2026-08-09 and is frozen research. Parts of it encode durable behavior the fork will need
to re-express under the Go/SQLite/Electron/AO stack. This index routes those findings so a later
implementation slice can consult the exact archived source instead of re-deriving the behavior.

Every entry gives an absolute-repo-relative path, the category, what the behavior is, and where it
should land in the fork. All paths are relative to the repository root; `archive/` is the frozen
pre-fork content.

## 2. Domain rules and state machines

| Path | Behavior to reproduce | Fork target | Do NOT port |
|---|---|---|---|
| `archive/src/sqorch/authority.py` | Exact task-block field set (schema version, ceilings, route, paths, evidence destination, acceptance authority); alias route/model rejection; no automatic fallback | Go packet/task validators (SA02/SA08) | Python `tomllib`/regex plumbing |
| `archive/src/sqorch/practices.py` | Practice record exact field set; `ADOPTED`/`REJECTED`/`DEPRECATED` require an approving authority | Square practice/evidence layer (later) | Python dataclass defaults |
| `archive/src/sqorch/projects.py` | Closed blueprint field set + acyclic dependency ordering; cycle => `INVALID_INPUT` | Go project graph / dependency check | Kahn implementation detail |
| `archive/src/dotnet/Square.Domain/Terminals/TerminalLifecycleState.cs` | 17-state terminal lifecycle vocabulary (incl. `LOST_PROCESS`, `HARD_STOPPED`, `QUIET_ACTIVE` distinct from stall) | Square role-run/binding state machine (aligns with `docs/SESSION_DOMAIN_MODEL.md`) | C# enum syntax |
| `archive/src/dotnet/Square.Domain/Terminals/TerminalLifecycleReducer.cs` | Idempotent event application; out-of-order events rejected; final states immutable | Go session/workflow reducers | C# records/discriminated unions |
| `archive/src/dotnet/Square.Domain/Authority/TaskContractValidator.cs` | Required task fields for packet validation | Go packet validator | None |
| `archive/src/dotnet/Square.Domain/Authority/PathValidation.cs` | Reject `..`, absolute prefixes, duplicate allowed/forbidden overlap, platform separators | Go path-ACL layer (Windows-aware) | Literal backslash/posix-only checks |
| `archive/src/dotnet/Square.Application/Authority/ManifestCompiler.cs` | Fail-closed authority projection: read STATUS, extract exactly one active marker, hash every claimed file, produce canonical manifest | Go manifest compiler + `scripts/capture-authority-hashes.ps1` | C# reporting plumbing |
| `archive/src/dotnet/Square.Contracts/Serialization/SquareJson.cs` | Strict unknown-field rejection; canonical JSON bytes for hashing | Go JSON codec / `--json` envelope | `System.Text.Json` defaults |
| `archive/src/dotnet/Square.Contracts/Rpc/CliExitCode.cs` | Versioned numeric exit/error contract | Go CLI/API error table (see §8) | None |
| `archive/src/dotnet/Square.Contracts/Rpc/RpcContracts.cs` | `square.rpc` envelope: id, method, idempotencyKey, params, protocol/version | Go daemon API envelope | None |
| `archive/src/dotnet/Square.Domain/Primitives/StrongIds.cs`, `UtcInstant.cs`, `ContentHash.cs` | Crockford-32 26-char IDs with type prefix, fixed UTC instants, `sha256:` content hashes | Go strong-ID/hash primitives (`SESSION_DOMAIN_MODEL.md` §2) | C# generic `StrongId<T>` |

## 3. Terminal and process hosting

| Path | Behavior to carry into the fork | Fork target | Do NOT port as-is |
|---|---|---|---|
| `archive/docs/proofs/conpty-job-object.md` | ConPTY creation ordering (pipes → pseudoconsole → attribute list → unnamed Job Object → `CREATE_SUSPENDED` → assign → resume); quiet-but-running must not read as stall; reconcile by exact process identity, not PID; owner failure must never silently rerun | SA01-T03 ConPTY/process audit against AO's runtime | Win32 interop scaffolding |
| `archive/docs/proofs/named-pipe-protocol.md` | Framing (big-endian length-prefixed strict JSON), typed handshake/versioning, bounded queue depths, slow-subscriber handling | Only evidence for choosing between AO's loopback REST/SSE/WS and alternatives (`ARCHITECTURE_AMENDMENT` §6) | the transport itself — AO already uses loopback REST/SSE/WS |
| `archive/docs/adr/ADR-0001-terminal-hosting.md` | G0 = `REJECTED_FOR_PROMOTION`; boundaries: one daemon-owned container per attempt, single authoritative output reader, graceful checkpoint before hard stop, exact-identity reconciliation, no live PTY reattachment claim, owner failure never silently reruns | SA01 gates and `TEST_AND_RELEASE_STRATEGY.md` | the promotion decision itself (does not apply to AO's validated runtime) |
| `archive/src/prototypes/TerminalProof/` harness | The 11 ordered scenarios (unicode, ANSI, large burst, quiet child, stdin question, resize, normal/crash/cancel/forced stop, nested) | SA01-T03 scenario matrix as Go test fixtures | .NET harness scaffolding |
| `archive/src/prototypes/PipeProof/ServerCore/DurableEventJournal.cs` | Append-only NDJSON journal, retain-window cap, replay-budget refusal | SA03 event/projection journal pattern (`PERSISTENCE_AND_EVENTS.md` §5) | .NET stream flags |
| `archive/src/prototypes/PipeProof/ServerCore/AtomicJsonFile.cs` | Atomic temp-file + rename for state writes | Go artifact writes (or SQLite transactions) | None |
| `archive/src/prototypes/PipeProof/Transport.Windows/PipeSecurityDescriptor.cs` | DACL-protected SDDL, exactly two full-control ACEs, DACL-structure check | Windows security fixtures for local transports | Win32 SDDL strings |

## 4. Evidence, receipts, and manifests

| Path | Behavior | Fork target | Avoid |
|---|---|---|---|
| `archive/src/sqorch/application.py` | `--json` machine-readable output with stable exit codes | `square` CLI contract (§8) | argparse plumbing |
| `archive/src/prototypes/PipeProof/evidence.schema.json` | `PASS`/`DIAGNOSTIC_PASS`/`FAIL`, `acceptance_eligible`, `ineligibility_reasons` | Evidence templates (`templates/evidence-summary-v1.json`) | Version pinning |
| `archive/src/prototypes/PipeProof/validate-source.mjs` | Pre-run integrity + structure check (source-manifest digest, required files, marker checks) | Go pack validation | Node/mjs runner |
| `archive/src/build/validate-g0.mjs` | Gate that requires fully resolved inputs/packet/proof evidence, binds all files by path+sha256 | A-gate semantics for fork gates | Node runner |
| `archive/src/build/verify-repository.mjs` | Enforce allow-list dependency direction between modules (Domain → Contracts → Application → Cli) | Go dependency policy | Node-only evaluation |
| `archive/docs/authority/manifest.sha256` | Hash-bound source identity precedent | Pack/spec hashing | none — keep hash files |

## 5. Persistence and SQLite

| Path | Behavior | Note |
|---|---|---|
| `archive/src/sqorch/state.py` | The working SQLite schema from the pre-fork line: `PRAGMA foreign_keys=ON`, `user_version`, `projects` + `locks` tables | The .NET `Square.Persistence.Sqlite` module was an empty stub; this Python file is the real behavioral source of the DB-layer contract |
| `archive/src/sqorch/state.py` | Single-holder lock; release only by recorded holder; duplicate acquire → `LOCK_EXISTS` | One-writer/lock contract — matches `PERSISTENCE_AND_EVENTS.md` §8 writer leases and the root one-writer invariant |

## 6. What is NOT reusable

Nothing in `archive/` is authoritative for the fork. Specific exclusions:

- `.NET/C#/WPF/Win32` source: platform-bound research only (`src/`, `prototypes/`, `ui/`, `vscode/`).
- Python M1 CLI (`sqorch/`): behavior seeds only; Go re-expresses the contracts.
- Old architecture drafts in `archive/plans/2026-08-07-square-orchestrator-design/`: superseded by
  the session-first pack; use only the highlighted items in §7.
- The former root toolchain (`build/`, `Directory.*` props), receipts (`archive/docs/receipts/`), and
  SP00 prototype receipts: historical trace, no porting.

## 7. Old-plan highlights of secondary value

| Source | Keep (as reusable wording) |
|---|---|
| `archive/plans/2026-08-05-m1-dry-run-foundation/BUILD.md` | Exit-code + typed-JSON mapping (0 ok / 2 invalid / 3 validation / 4 conflict-locked); canonical-JSON-bytes projection for hashing; hashing verbatim document bytes |
| `archive/plans/2026-08-05-m1-dry-run-foundation/PACKET.md` | Activation preflight: replace `ACTIVATION_REQUIRED` with starting commit + exact route; no silent substitution; `ROUTE_UNAVAILABLE`/`STOP:` |
| `archive/plans/2026-08-07-square-orchestrator-design/dependency-securityfork-resolution.md` | Dependency-admission proof gate (run an audit of each new Go dependency: license, security, transitive scan) before admission |
| `archive/plans/2026-08-08-fork/old/` | Earlier draft plan for trace only; superseded by this pack |

## 8. Recommended minimal extraction into this pack

1. Introduce an authoritative exit-code/error table for the `square` CLI (`plain.codes` retained from
   `CliExitCode.cs` / M1 `BUILD.md`): 0 ok, 2 invalid-input, 3 validation/authority, 4 state/locking.
2. Fold the fail-closed manifest routine into pack templating: canonical-JSON bytes, harness/sustainability
   hashes, one active marker.
3. Add a per-dispatch requirement that each task packet pins "starting commit + exact route earned at
   preflight" (adopting the M1 activation discipline).
4. Keep `TerminalProof`'s scenario naming as the SA01-T03 matrix seed.
5. Retain the `PASS`/`DIAGNOSTIC_PASS`/`FAIL` evidence vocabulary in `templates/evidence-summary-v1.json`.
