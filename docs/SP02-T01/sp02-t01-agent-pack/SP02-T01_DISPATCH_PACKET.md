# SP02-T01 — SQLite Schema and Migration Runner

## Implementation dispatch packet

- **Task ID:** `SP02-T01`
- **Title:** SQLite schema and migration runner
- **Packet revision:** `1.0-draft`
- **Prepared:** 2026-08-07
- **Status:** implementation-ready specification; **dispatch blocked until prerequisites pass**
- **Normal commit title:** `SP02-T01: add SQLite schema and migration runner`
- **Architectural owner:** persistence leaf, `Square.Persistence.Sqlite`
- **Primary acceptance authority:** owner-approved task contract plus automated persistence/security evidence

---

## 1. Objective

Implement the first production persistence leaf for Square Orchestrator using SQLite. The task must deliver:

1. an explicitly admitted and reproducibly locked SQLite provider/native package graph;
2. a safe database-open/bootstrap path;
3. an ordered, forward-only migration catalogue and runner;
4. the complete initial table/index/trigger schema required by the sliced plan;
5. append-only lifecycle events and transactionally updated current projections;
6. backup-before-migration;
7. explicit refusal of an unsupported newer schema, an unrecognized database, migration-history drift, or corruption; and
8. a deterministic persistence test suite and evidence set.

This task creates **persistence infrastructure only**. It does not implement daemon RPC, CLI commands, workflow handlers, terminal hosting, artifact file contents, recovery orchestration, UI behavior, provider adapters, or model-facing workflows.

---

## 2. Dispatch gate and prerequisites

### 2.1 Mandatory prerequisites

The dispatcher must fill and verify every item below before a worker edits production code:

| Requirement | Required state | Evidence |
|---|---|---|
| G0 architecture gate | Superseding decision is `ACCEPTED` or explicitly permits SP01/SP02 | Gate record path/hash |
| SP01-T06 | Accepted; public contracts frozen at the version named by the gate | Receipt path/hash |
| Repository | Clean Git worktree at an exact commit | `git status --porcelain=v1` empty; commit SHA |
| Authority documents | Hashes match owner-approved baseline | SHA-256 manifest |
| SDK | Exact version from `global.json` | `dotnet --version` |
| Dependency proof | Qualifying PASS for the exact package graph in this packet | Dependency-admission report/evidence hash |
| Architecture tests | Baseline pass before edits | command and log hash |
| Domain/contract tests | Baseline pass before edits | command and log hash |

### 2.2 Current bootstrap warning

The latest generated bootstrap snapshot known when this packet was prepared still records G0 as blocked and SP01-T01/T02 as draft/partial. Therefore the packet is a forward implementation contract, not permission to skip the gate sequence.

### 2.3 Mandatory STOP

Stop before implementation when any prerequisite is absent, stale, ambiguous, or contradicted by the local repository. Report the exact missing record; do not synthesize a replacement contract.

---

## 3. Authority hierarchy

When materials conflict, use this order:

1. owner amendment or decision attached to this dispatch;
2. this filled dispatch packet;
3. `square-orchestrator-sliced-implementation-plan.md`;
4. `square-orchestrator-technical-architecture.md`;
5. accepted SP01 contracts and completion receipt;
6. repository-wide build/dependency rules;
7. UI design and UI handover for cross-boundary constraints only;
8. existing implementation details and comments.

The worker may not reinterpret a lower authority to override a higher one.

### 3.1 Authority hashes from the prepared bootstrap snapshot

These hashes are included as a comparison aid. The dispatcher must recalculate them from the actual local checkout and either confirm them or attach an owner-approved amendment.

```text
135fc5449998cea9b2bc3b9ddbc8bb8848e60c54ddb25dec34bc232016f0f65f  square-orchestrator-sliced-implementation-plan.md
5f13c89330470104a1263ef7f371128c6ab5a41199a121c258785775aa448aad  square-orchestrator-technical-architecture.md
b55546c1587bbc2fc1cb06524a8702a4f271bfe646bd5584d638f43ddf8650f9  square-orchestrator-ui-design.md
24e23bff559624a10eb603d0d4d3518a91c78a6e98c07203237fb31f180d0021  square-orchestrator-ui-handover-readme.md
```

---

## 4. Locked decisions

The following are non-discretionary for this task.

### 4.1 Architecture

- SQLite remains the metadata, lifecycle-event, projection, lease, receipt, usage, and exposure store.
- Content-addressed artifact **bytes** remain outside SQLite; this task creates artifact metadata only.
- The daemon will be the only authoritative writer. This project must make that ownership possible but must not implement daemon lifecycle here.
- UI, CLI, VS Code, and desktop code never open or query this database directly.
- Persistence remains a leaf implementation. No SQLite/provider type may leak into Domain, Contracts, or Application public APIs.
- The implementation uses direct ADO.NET-style SQL through `Microsoft.Data.Sqlite.Core`; do not introduce EF Core, Dapper, an ORM, a generic repository framework, or a second database abstraction package.
- Migrations are ordered and forward-only. There is no production downgrade runner.
- Lifecycle events are append-only. Current tables are projections and are updated in the same transaction as the event that caused the change.
- Database compatibility is fail-closed. A newer schema, changed historical migration checksum, unknown database, or corruption is not silently repaired or overwritten.
- A complete, verified backup is created before any schema-changing migration begins.
- Migration SQL is static, embedded, reviewed source. No migration SQL comes from user input, configuration, network content, or model output at runtime.

### 4.2 Dependency candidate

Only the following candidate graph may be evaluated by this packet:

```text
Direct:
  Microsoft.Data.Sqlite.Core          10.0.10
  SQLitePCLRaw.config.e_sqlite3       3.0.5
  SourceGear.sqlite3                  3.53.4

Expected transitive:
  SQLitePCLRaw.provider.e_sqlite3     3.0.5
  SQLitePCLRaw.core                   3.0.5
```

The `Microsoft.Data.Sqlite` meta-package is not used. `SQLitePCLRaw.lib.e_sqlite3` must not appear anywhere in the resolved closure. Any version substitution requires a new dependency review, even when it is newer.

### 4.3 SQLite runtime baseline

For this task, the qualifying runtime probe must report exactly:

```text
sqlite_version() = 3.53.4
```

This is an admission identity, not a permanent policy that forbids a later reviewed upgrade.

### 4.4 Conservative initial connection profile

Use this correctness-first profile for SP02-T01:

```text
Data Source=<absolute path>
Mode=ReadWriteCreate
Cache=Private (or omitted; never Cache=Shared)
Pooling=false during bootstrap/migration/backup tests
Foreign Keys=true
Default Timeout=5 seconds
```

After schema initialization, verify and record:

```sql
PRAGMA foreign_keys = ON;
PRAGMA journal_mode = WAL;
PRAGMA synchronous = FULL;
PRAGMA busy_timeout = 5000;
PRAGMA trusted_schema = OFF;
```

Rules:

- Check the value returned by each relevant pragma; do not assume assignment succeeded.
- Do not mix `Cache=Shared` with WAL.
- Do not claim that these are final SSD/performance settings. Later measured tasks may amend checkpoint cadence or synchronous policy.
- Leave SQLite's default WAL auto-checkpoint threshold unless the task's measured tests justify and record another value. Record the actual value from `PRAGMA wal_autocheckpoint`.

### 4.5 Database identity

Define and document one stable SQLite `application_id` for Square Orchestrator:

```text
0x53514F52  // ASCII "SQOR"
```

`schema_registry` is authoritative for migration history. `application_id` is a fast identity guard, not a replacement for migration-history verification.

---

## 5. Global invariants relevant to SP02-T01

The implementation and tests must preserve all of these:

1. no UI/client direct database access;
2. one authoritative writer owner;
3. accepted mutations can be persisted as event plus projection atomically;
4. idempotency keys are durable and can distinguish same-key/same-input from same-key/conflicting-input;
5. public IDs and schema versions use accepted SP01 canonical forms;
6. all timestamps come from an injected clock or explicit input, never implicit SQL `CURRENT_TIMESTAMP` for domain/audit records;
7. lifecycle event order is monotonic and durable;
8. corruption or compatibility uncertainty fails closed;
9. no raw terminal transcript, diff, context pack, screenshot, or large artifact body is stored in ordinary projection rows;
10. no destructive repair, migration, or replacement occurs without a typed outcome and evidence;
11. database errors must not expose secrets or unbounded SQL/data in public problem details; and
12. cancellation or process interruption cannot leave a migration falsely recorded as complete.

---

## 6. Scope boundaries

### 6.1 Required read paths

The worker may read:

```text
docs/authority/**
docs/gates/**
docs/adr/**
docs/receipts/SP01-*.json
Directory.Build.props
Directory.Packages.props
NuGet.Config
THIRD_PARTY.md
global.json
SquareOrchestrator.slnx
src/Square.Domain/**
src/Square.Contracts/**
src/Square.Application/**
src/Square.Persistence.Sqlite/**
tests/Domain.Tests/**
tests/Contract.Tests/**
tests/Architecture.Tests/**
tests/TestKit/**
tests/Persistence.Tests/**
build/**
test.ps1
```

Read-only inspection of daemon/CLI/UI projects is permitted solely to verify that they do not reference SQLite directly. Do not edit them.

### 6.2 Allowed write paths

The normal write set is limited to:

```text
Directory.Build.props                         # NuGet audit properties only, when absent
Directory.Packages.props                      # admitted package versions only
NuGet.Config                                  # audit source/signature policy only, when justified
THIRD_PARTY.md                                # dependency review record
src/Square.Persistence.Sqlite/**
tests/Persistence.Tests/**
tests/fixtures/Square.SqliteProbe/**          # only if the dedicated publish probe is needed
tests/test-suites.json                        # add Persistence category
build/test.ps1                                # add Persistence category
build/dependencies/**                         # repeatable dependency proof/validators
docs/dependencies/SP02-T01-*.md
docs/dependencies/SP02-T01-*.json
docs/validation/SP02-T01-*.txt
docs/receipts/SP02-T01*.json
```

A solution/project file outside those paths may be changed only if the already-declared project/test fixture is missing from the solution. Record this as exercised discretion.

### 6.3 Forbidden write paths

Do not modify:

```text
src/Square.Domain/**
src/Square.Contracts/**
src/Square.Application/**
src/Square.ControlPlane/**
src/Square.Daemon/**
src/Square.Cli/**
src/Square.Desktop/**
src/Square.Platform.Windows/**
src/Square.Artifacts/**
src/Square.Adapters.*/**
ui/**
vscode/**
prototypes/TerminalProof/**
prototypes/PipeProof/**
prototypes/SharedUiProof/**
docs/authority/**
docs/gates/**                     # except a separate owner gate task
```

### 6.4 Explicit non-goals

- no JSON/file-store replacement;
- no daemon process;
- no named-pipe server/client;
- no CLI commands;
- no artifact file writes or retention;
- no terminal chunks;
- no route/provider behavior;
- no repository/worktree filesystem lock implementation;
- no background model calls;
- no UI data access;
- no encryption-at-rest decision;
- no schema auto-repair;
- no downgrade migrations;
- no final WAL/SSD optimization policy;
- no backup retention/deletion policy beyond preserving the pre-migration backup.

---

## 7. Sequential checkpoints

This task may use the following internal checkpoints. They remain one atomic task and normally one final commit.

### SP02-T01-ST01 — Dependency admission

- Execute the separate dependency-admission proof guide.
- Produce a qualifying PASS for the exact package graph.
- Update central versions, package references, lock files, audit configuration, and `THIRD_PARTY.md` only after PASS.
- STOP on any unresolved vulnerability, license, signature, provenance, native-load, architecture, or graph issue.

### SP02-T01-ST02 — Runtime and connection boundary

- Initialize SQLitePCLRaw exactly once and safely under concurrency.
- Add the connection options/factory and runtime identity probe.
- Ensure provider types stay internal to the persistence leaf.
- Prove x64 native loading, canonical connection settings, and exact runtime version.

### SP02-T01-ST03 — Migration catalogue and initial schema

- Add immutable embedded SQL migrations.
- Implement deterministic discovery/order/checksum validation.
- Implement empty bootstrap, compatibility detection, migration transaction, and history recording.
- Create all required tables/indexes/triggers.

### SP02-T01-ST04 — Event/projection transaction primitive

- Add the internal persistence primitive that appends one or more events and applies corresponding projection mutations inside the same SQLite transaction.
- Prove rollback and uniqueness conflict behavior.
- Do not expose generic SQL execution to Application or hosts.

### SP02-T01-ST05 — Backup, compatibility, and corruption behavior

- Back up before migration using SQLite's database backup API.
- Verify the backup before migration continues.
- Add newer-schema, unrecognized database, checksum-drift, interrupted migration, locked database, and corrupt database behavior.

### SP02-T01-ST06 — Validation, evidence, and receipt

- Run the full package, build, architecture, persistence, and deterministic suites.
- Generate schema/dependency/runtime evidence.
- Produce the completion receipt.
- Do not mark PASS when any required test/evidence is missing or diagnostic-only.

---

## 8. Expected production structure

Private names may vary when this improves clarity, but the boundaries and responsibilities may not.

```text
src/Square.Persistence.Sqlite/
  Square.Persistence.Sqlite.csproj
  AssemblyInfo.cs
  SqliteRuntime.cs
  SqliteDatabase.cs
  SqliteDatabaseOptions.cs
  SqliteConnectionFactory.cs
  SqliteConnectionProfile.cs
  SqliteOpenReport.cs
  SqliteProblemCodes.cs
  Diagnostics/
    SqliteRuntimeInspector.cs
    SqliteDatabaseInspector.cs
    SqliteSchemaSnapshot.cs
  Backup/
    SqliteBackupService.cs
    SqliteBackupManifest.cs
  Migrations/
    SqliteMigration.cs
    SqliteMigrationCatalog.cs
    SqliteMigrationRunner.cs
    SqliteMigrationPlan.cs
    SqliteMigrationReport.cs
    Sql/
      0001_schema_identity_and_registry.sql
      0002_workflow_projections.sql
      0003_eventing_coordination_and_breakers.sql
      0004_artifacts_receipts_usage_and_exposure.sql
  Transactions/
    SqliteWriteSession.cs
    StoredEvent.cs
    ProjectionMutation.cs
  Internal/
    SqliteCommandExtensions.cs
    SqliteExceptionMapper.cs
    CanonicalSqliteValues.cs
```

Expected tests:

```text
tests/Persistence.Tests/
  Persistence.Tests.csproj
  Program.cs
  TestDatabase.cs
  TestClock.cs
  TestMigrationFaultInjector.cs
  DependencyAdmissionContractTests.cs
  RuntimeIdentityTests.cs
  EmptyDatabaseTests.cs
  SchemaCatalogTests.cs
  MigrationPathTests.cs
  MigrationHistoryTests.cs
  MigrationInterruptionTests.cs
  NewerSchemaTests.cs
  UnrecognizedDatabaseTests.cs
  BackupTests.cs
  CorruptionTests.cs
  EventProjectionAtomicityTests.cs
  AppendOnlyEventTests.cs
  IdempotencySchemaTests.cs
  ConcurrentInitializationTests.cs
  WalAndReopenTests.cs
```

The repository currently uses dependency-free console test executables rather than an external test framework. Follow the accepted repository convention unless a separate dependency review authorizes a test framework.

---

## 9. Required types and behavioral contracts

### 9.1 SQLite runtime initializer

Provide one process-wide initializer with these semantics:

```csharp
internal static class SqliteRuntime
{
    public static void Initialize();
}
```

Requirements:

- Calls `SQLitePCL.Batteries_V2.Init()` exactly once.
- Multiple concurrent callers succeed without double-initialization races.
- Initialization failure is cached/reported consistently; it is not swallowed.
- A test-only inspection hook may expose whether initialization occurred, but production callers receive no mutable global handle.

### 9.2 Database options

Provide an immutable options type equivalent to:

```csharp
public sealed record SqliteDatabaseOptions(
    string DatabasePath,
    string BackupDirectory,
    TimeSpan BusyTimeout,
    bool EnableWriteAheadLog = true);
```

Validation:

- paths must be absolute after canonicalization;
- database and backup directories cannot be the same file path;
- path length/invalid-character failures become typed problems;
- database path may contain spaces and Unicode;
- caller cannot inject connection-string fragments through the path;
- busy timeout must be bounded by packet policy (recommended 1–30 seconds; default 5 seconds).

### 9.3 Database composition root

Provide one public persistence-leaf entry point equivalent to:

```csharp
public sealed class SqliteDatabase : IAsyncDisposable
{
    public static Task<Result<SqliteOpenReport>> OpenAsync(
        SqliteDatabaseOptions options,
        IClock clock,
        CancellationToken cancellationToken);
}
```

Adjust the `Result`/clock signature to the accepted SP01 primitives. Requirements:

- Public signatures may depend on accepted Domain/Application ports.
- Public signatures must not contain `SqliteConnection`, `SqliteCommand`, `SqliteTransaction`, or SQLitePCLRaw types.
- Opening performs identity, compatibility, migration, post-migration validation, and connection-profile verification.
- `OpenAsync` returns an immutable report including old/new schema version, applied migrations, backup path/hash when created, runtime SQLite version, journal/synchronous/foreign-key settings, and warnings.
- Disposing releases owned pooled/non-pooled connections. It never deletes the database or backup.

### 9.4 Migration definition

Use an immutable definition equivalent to:

```csharp
internal sealed record SqliteMigration(
    int Version,
    string Name,
    string ResourceName,
    ContentHash Sha256);
```

Rules:

- versions are positive, contiguous integers beginning at 1;
- names are stable lower-snake-case identifiers;
- checksum is SHA-256 of the exact embedded UTF-8 SQL bytes;
- duplicate version, name, resource, or checksum definition is a startup error;
- applied history must match current source checksums exactly;
- an applied migration file is immutable; changes require a new migration.

### 9.5 Migration runner

Provide behavior equivalent to:

```csharp
internal sealed class SqliteMigrationRunner
{
    public Task<Result<SqliteMigrationReport>> MigrateAsync(
        SqliteConnection connection,
        SqliteDatabaseOptions options,
        IClock clock,
        CancellationToken cancellationToken);
}
```

SQLite types remain internal. The algorithm is locked by Section 13.

### 9.6 Backup service

Provide behavior equivalent to:

```csharp
internal interface ISqliteBackupService
{
    Task<Result<SqliteBackupManifest>> CreatePreMigrationBackupAsync(...);
}
```

Requirements:

- use `SqliteConnection.BackupDatabase`, not a raw copy of only `square.db`;
- write to a unique temporary destination;
- close and validate the destination;
- calculate SHA-256 and length;
- atomically rename the complete backup and manifest into their final names;
- fail the migration if backup creation or validation fails;
- preserve complete backups after successful migration.

### 9.7 Internal write-session primitive

SP02-T01 must prove the transaction boundary needed by SP02-T03 without exposing arbitrary SQL outside the leaf. Provide an internal abstraction that can:

1. insert ordered event rows;
2. apply typed persistence-owned projection operations;
3. store/complete an idempotency record where applicable; and
4. commit or roll back all of it together.

The test-only API may be lower-level. The production public API must not accept raw SQL strings from Application or hosts.

---

## 10. Migration SQL rules

Every migration resource must obey these rules:

- UTF-8 without BOM; LF line endings; deterministic bytes.
- Static DDL/DML only.
- No transaction statements; the runner owns transactions.
- No `VACUUM`, `ATTACH`, `DETACH`, `load_extension`, shell command, network access, or external file read.
- No user/config/model-derived identifiers or SQL fragments.
- Avoid `IF NOT EXISTS` except where the runner's bootstrap identity logic explicitly requires it; silent drift masking is forbidden.
- Use `STRICT` tables for the initial schema.
- Use foreign keys and explicit indexes.
- Use `TEXT` for canonical IDs, schema versions, hashes, states, and UTC timestamps.
- Use `INTEGER` for sequence, revision, lengths, counters, booleans, and fencing tokens.
- Use `CHECK` constraints for nonnegative values, booleans, nonempty discriminator fields, and JSON validity where appropriate.
- Do not use SQL-generated domain timestamps. Pass canonical timestamps as parameters from the accepted clock.
- Do not store secrets, credentials, unrestricted environment values, or terminal input.
- Do not store large binary bodies in projection/event tables.

---

## 11. Initial migration set

Use four migrations so the test suite can exercise real upgrade paths.

### 0001 — schema identity and registry

Creates and initializes:

- `PRAGMA application_id = 0x53514F52`;
- `schema_registry`.

Required `schema_registry` columns:

| Column | Type/constraints | Meaning |
|---|---|---|
| `migration_version` | `INTEGER PRIMARY KEY`, `> 0` | ordered migration number |
| `migration_name` | `TEXT NOT NULL UNIQUE` | immutable stable name |
| `migration_sha256` | `TEXT NOT NULL` | canonical `sha256:<lowercase hex>` source checksum |
| `applied_utc` | `TEXT NOT NULL` | canonical injected UTC instant |
| `product_version` | `TEXT NOT NULL` | assembly/product version applying it |
| `sqlite_version` | `TEXT NOT NULL` | runtime SQLite identity |

The registry row for migration 0001 must be inserted in the same transaction that creates it.

### 0002 — workflow/current projections

Creates:

```text
projects
requests
tasks
attempts
terminals
interactions
gates
routes
volume_profiles
```

### 0003 — eventing, idempotency, leases, circuit breakers

Creates:

```text
events
idempotency_keys
leases
circuit_breaker_events
```

Also creates append-only triggers for `events` and `circuit_breaker_events`.

### 0004 — artifacts, receipts, usage, exposure

Creates:

```text
artifact_metadata
receipts
usage
exposure
```

No artifact bytes are written by this migration or task.

---

## 12. Schema baseline

### 12.1 Common projection columns

Where applicable, current projection tables contain:

```text
<entity_id>                 TEXT PRIMARY KEY
schema_version              TEXT NOT NULL
state                       TEXT NOT NULL
revision                    INTEGER NOT NULL CHECK (revision >= 0)
created_utc                 TEXT NOT NULL
updated_utc                 TEXT NOT NULL
last_event_sequence         INTEGER NOT NULL CHECK (last_event_sequence > 0)
payload_json                TEXT NOT NULL CHECK (json_valid(payload_json))
```

`payload_json` contains the accepted versioned contract record. Extracted columns are query/index fields and must remain consistent with the payload inside the event/projection transaction.

### 12.2 Identity and parent relationships

Identity column names and canonical text values must match accepted SP01 IDs. Do not invent another UUID/GUID format.

Required relationships/extracted columns:

| Table | Required extracted columns and constraints |
|---|---|
| `projects` | `project_id`; `root_path`; `root_path_key UNIQUE`; `display_name`; state/revision/event columns |
| `requests` | `request_id`; `project_id FK projects`; `priority`; `workflow_profile`; state/revision/event columns |
| `tasks` | `task_id`; `request_id FK`; nullable `parent_task_id FK tasks`; stable `plan_task_key`; `write_mode`; state/revision/event columns; unique `(request_id, plan_task_key)` |
| `attempts` | `attempt_id`; `task_id FK`; nullable `route_id FK`; `attempt_ordinal`; state/revision/event columns; unique `(task_id, attempt_ordinal)` |
| `terminals` | `terminal_id`; `attempt_id FK UNIQUE`; `output_sequence`; `retained_from_sequence`; state/revision/event columns |
| `interactions` | `interaction_id`; request/task/attempt/terminal scope IDs as accepted by contract; `interaction_kind`; `required_authority`; nullable expiry/response times; state/revision/event columns |
| `gates` | `gate_id`; request/task/attempt scope IDs; `gate_kind`; `required_authority`; nullable expiry; state/revision/event columns |
| `routes` | `route_id`; stable `route_key UNIQUE`; `client_kind`; provider/model identity fields where present in accepted contract; certification/state/revision/event columns |
| `volume_profiles` | contract-defined stable key; `volume_identity`; `volume_class`; `telemetry_state`; schema/revision/event columns |

If accepted SP01 contracts materially lack a required identity or relationship, STOP and request a contract amendment. Do not hide the gap behind a persistence-only surrogate ID without owner approval.

### 12.3 Events

Required `events` columns:

| Column | Required behavior |
|---|---|
| `sequence` | `INTEGER PRIMARY KEY AUTOINCREMENT`; global monotonic cursor |
| `event_id` | canonical Event ID; `UNIQUE NOT NULL` |
| `stream_kind` | nonempty discriminator |
| `stream_id` | canonical owning aggregate ID text |
| `stream_version` | positive integer; unique with stream kind/id |
| `event_type` | versioned nonempty event type |
| `schema_version` | accepted event schema version |
| `occurred_utc` | injected canonical UTC instant |
| `correlation_id` | canonical correlation ID |
| `causation_event_id` | nullable event ID |
| `payload_json` | strict valid JSON; bounded by application policy |

Required constraints/indexes:

```text
UNIQUE(event_id)
UNIQUE(stream_kind, stream_id, stream_version)
INDEX(sequence)
INDEX(stream_kind, stream_id, stream_version)
INDEX(correlation_id, sequence)
INDEX(occurred_utc, sequence)
```

Required triggers reject all `UPDATE` and `DELETE` operations on `events` with a stable SQLite error message/code mapping such as `events_append_only`.

### 12.4 Idempotency keys

Required columns:

```text
idempotency_key             TEXT PRIMARY KEY
operation                   TEXT NOT NULL
request_hash                TEXT NOT NULL
status                      TEXT NOT NULL
correlation_id              TEXT NOT NULL
result_schema_version       TEXT NULL
result_json                 TEXT NULL CHECK (result_json IS NULL OR json_valid(result_json))
problem_json                TEXT NULL CHECK (problem_json IS NULL OR json_valid(problem_json))
first_event_sequence        INTEGER NULL
created_utc                 TEXT NOT NULL
completed_utc               TEXT NULL
expires_utc                 TEXT NULL
```

Constraints must allow one pending or one terminal result, never both result and problem simultaneously. Same key plus different operation/request hash becomes a typed conflict; it is not overwritten.

### 12.5 Leases

Required columns:

```text
resource_kind               TEXT NOT NULL
resource_key                TEXT NOT NULL
lease_id                    TEXT NOT NULL UNIQUE
holder_id                   TEXT NOT NULL
fencing_token               INTEGER NOT NULL CHECK (fencing_token > 0)
acquired_utc                TEXT NOT NULL
renewed_utc                 TEXT NOT NULL
expires_utc                 TEXT NOT NULL
released_utc                TEXT NULL
schema_version              TEXT NOT NULL
payload_json                TEXT NOT NULL CHECK (json_valid(payload_json))
PRIMARY KEY(resource_kind, resource_key)
```

This row shape enforces one current lease slot per resource. Time/authority semantics are implemented by later application/control tasks. SP02-T01 must prove atomic fencing-token increment and unique current-slot behavior at the persistence level.

### 12.6 Artifact metadata

Required columns:

```text
artifact_id                 TEXT PRIMARY KEY
content_hash                TEXT NOT NULL UNIQUE
byte_length                 INTEGER NOT NULL CHECK (byte_length >= 0)
media_type                  TEXT NOT NULL
storage_relative_path       TEXT NULL
storage_state               TEXT NOT NULL
created_utc                 TEXT NOT NULL
schema_version              TEXT NOT NULL
payload_json                TEXT NOT NULL CHECK (json_valid(payload_json))
```

No BLOB payload column is permitted.

### 12.7 Receipts

Required columns include:

```text
receipt_id                  TEXT PRIMARY KEY
attempt_id                  TEXT NOT NULL REFERENCES attempts(attempt_id)
receipt_nonce               TEXT NOT NULL UNIQUE
content_hash                TEXT NOT NULL
status                      TEXT NOT NULL
received_utc                TEXT NOT NULL
applied_utc                 TEXT NULL
applied_event_sequence      INTEGER NULL
schema_version              TEXT NOT NULL
payload_json                TEXT NOT NULL CHECK (json_valid(payload_json))
```

Conflicting receipt reuse must be detectable by nonce/hash uniqueness.

### 12.8 Usage and exposure

Use stable entry identities from accepted SP01 contracts. At minimum extract:

- owning project/request/task/attempt/route identifiers where present;
- model family/provider/client dimensions where present;
- measured/estimated source and confidence;
- canonical units;
- recorded UTC time;
- immutable JSON payload.

Usage and exposure remain separate tables and semantics. Never merge exposure into cost or treat unknown values as zero.

### 12.9 Circuit-breaker events

This table is append-only and references the corresponding global lifecycle event when available. Extract scope kind/key, breaker kind, transition, reason code, occurred UTC, and payload. Add update/delete denial triggers.

---

## 13. Required open/migration algorithm

Implement the following order exactly, with typed outcomes at each boundary.

### 13.1 Pre-open

1. Validate options and canonicalize paths without following an untrusted alternate database path supplied through a connection string.
2. Create the state and backup directories only; do not create/overwrite an existing database file manually.
3. Initialize SQLitePCLRaw once.
4. Open a non-pooled bootstrap connection with foreign keys enabled and bounded busy timeout.
5. Query and record `SELECT sqlite_version();`.
6. Reject the runtime unless it equals the dependency-admission identity for this task.
7. Run functional capability probes required by the schema, including `json_valid`.

### 13.2 Identify database state

Classify one of:

- `EMPTY_NEW`: zero-length/nonexistent SQLite database just created by open, no schema objects;
- `SQUARE_CURRENT`: valid identity and complete expected migration history at supported maximum;
- `SQUARE_OLDER`: valid identity and a contiguous older history;
- `SQUARE_NEWER`: valid Square identity but migration version above supported maximum;
- `UNRECOGNIZED`: nonempty/schema-bearing database without valid Square identity/registry;
- `HISTORY_DRIFT`: missing, duplicate, noncontiguous, renamed, or checksum-mismatched applied migration;
- `CORRUPT`: SQLite reports corruption/not-a-database or integrity failure.

Only `EMPTY_NEW`, `SQUARE_CURRENT`, and `SQUARE_OLDER` may proceed. `SQUARE_NEWER`, `UNRECOGNIZED`, `HISTORY_DRIFT`, and `CORRUPT` fail without schema mutation.

### 13.3 Integrity and migration plan

For an existing recognized database:

1. run `PRAGMA quick_check` before ordinary no-op open;
2. run full `PRAGMA integrity_check` before a schema migration;
3. run `PRAGMA foreign_key_check`;
4. compare applied migration names/checksums with the embedded catalogue;
5. construct an immutable ordered plan from current+1 to maximum.

No plan means no backup and no schema writes.

### 13.4 Backup-before-migration

When a plan is nonempty:

1. create a unique backup temp path outside the live database filename;
2. use `SqliteConnection.BackupDatabase` to a destination SQLite connection;
3. close destination;
4. open the backup read-only;
5. verify `application_id`, migration history, `integrity_check`, and `foreign_key_check`;
6. calculate byte length and SHA-256;
7. atomically rename the validated backup database into its final immutable name;
8. write the JSON manifest through temp-file + flush + atomic rename;
9. verify that both final files exist and match, then and only then begin schema migration.

Recommended final name:

```text
square.pre-migration.v<old>-to-v<new>.<yyyyMMddTHHmmssfffffffZ>.<hash-prefix>.db
```

The manifest must avoid exposing a full user path in general logs; store a path hash and repository-relative/evidence-safe representation where appropriate.

### 13.5 Migration execution

1. Acquire the SQLite writer/migration lock using a bounded transaction strategy (`BEGIN IMMEDIATE` is acceptable; document the exact behavior).
2. Re-read identity/history inside the lock to close the race with another initializer.
3. For each pending migration, in order:
   - verify embedded resource checksum again;
   - execute static SQL;
   - insert its registry row using clock/product/runtime values;
   - verify the expected schema objects for that migration;
   - commit the migration transaction.
4. A migration may use one transaction per version. A failed version rolls back completely and is not recorded.
5. After all versions, run full post-migration `integrity_check`, `foreign_key_check`, expected-object/index/trigger checks, and history validation.
6. Apply/verify the runtime WAL/foreign-key/synchronous/busy/trusted-schema profile.
7. Return a complete report.

The runner must be restartable after process termination at any tested boundary. A database may remain at the last fully committed migration, never a falsely advanced version.

### 13.6 Current-schema open

For `SQUARE_CURRENT`, verify identity, history, quick integrity, foreign keys, expected objects, and runtime pragmas. Do not write a migration registry row, alter timestamps, or create a backup merely because the process opened the database.

---

## 14. Error catalogue

Map provider/SQLite exceptions into stable typed problem codes. Recommended codes:

```text
sqlite_dependency_not_admitted
sqlite_runtime_initialization_failed
sqlite_native_version_unsupported
sqlite_database_path_invalid
sqlite_database_open_failed
sqlite_database_busy
sqlite_database_locked
sqlite_database_unrecognized
sqlite_database_corrupt
sqlite_integrity_check_failed
sqlite_foreign_key_check_failed
sqlite_schema_newer_than_supported
sqlite_schema_history_missing
sqlite_schema_history_noncontiguous
sqlite_schema_checksum_mismatch
sqlite_migration_catalog_invalid
sqlite_backup_failed
sqlite_backup_validation_failed
sqlite_migration_failed
sqlite_migration_cancelled
sqlite_event_conflict
sqlite_projection_conflict
sqlite_idempotency_conflict
sqlite_lease_conflict
```

Requirements:

- Preserve SQLite primary/extended error codes in internal diagnostics/evidence.
- Public problem details contain a stable code, safe explanation, correlation ID where available, and remediation.
- Do not expose SQL statements with user data, terminal contents, secrets, or arbitrary database payloads.
- Do not convert corruption/newer schema to “empty database” or auto-create a replacement.

---

## 15. Concurrency, cancellation, and restart rules

### 15.1 Concurrent initialization

Two processes/connections racing to initialize the same path must produce one valid final schema. One may wait and then report no pending migrations. Neither may duplicate registry rows, create inconsistent projections, or overwrite the other's backup.

### 15.2 One writer assumption

The product's daemon-only writer model is a higher-level invariant. SP02-T01 must still handle accidental concurrent opens safely and return typed busy/lock outcomes. It must not depend on UI/CLI discipline for migration correctness.

### 15.3 Cancellation

- Cancellation before opening/backup/migration stops cleanly.
- Cancellation during `BackupDatabase` may produce only a temporary/incomplete backup, which must never be promoted to a valid final backup.
- Cancellation during a migration rolls back that migration and leaves history at the prior version.
- Once the underlying SQLite commit has succeeded, cancellation cannot report the mutation as uncommitted. Return/record the committed result.

### 15.4 Crash/restart

Fault injection must cover:

- before backup;
- during backup temp creation;
- after backup validation but before final rename;
- after final backup but before migration;
- after DDL but before registry insert;
- after registry insert but before migration commit;
- between migration versions;
- after final migration commit but before post-check/report.

On restart, the runner must classify and continue from durable state without fabricating completion.

---

## 16. Event/projection atomicity contract

The persistence test suite must demonstrate all of the following:

1. event insert and projection update are visible together after commit;
2. a thrown projection failure rolls back the event insert;
3. an event uniqueness failure rolls back projection work;
4. duplicate event ID is typed and does not advance global sequence with a committed row;
5. duplicate `(stream_kind, stream_id, stream_version)` is typed;
6. projection optimistic revision mismatch is typed;
7. `last_event_sequence` equals the committed event sequence;
8. update/delete against append-only event tables is denied;
9. same idempotency key and same request hash can return the prior result without a second event;
10. same idempotency key and different request hash is a conflict;
11. transaction cancellation/failure leaves neither half applied; and
12. reopen shows the same committed result.

Do not implement application command handlers in this task. Use persistence-owned test records/operations to prove the transaction boundary.

---

## 17. Required tests

### 17.1 Dependency and runtime

- exact direct/transitive package closure;
- no vulnerable/deprecated/unreviewed package;
- lock-file exactness and locked restore;
- package signature verification and hashes;
- native x64 DLL present in normal build and self-contained publish;
- runtime `sqlite_version()` exactly 3.53.4;
- `json_valid` functional capability;
- provider initialization under concurrency;
- no provider type leaks into non-leaf public API.

### 17.2 Empty creation

- nonexistent parent state directory;
- nonexistent database;
- zero-byte path behavior;
- path containing spaces;
- Unicode path;
- maximum supported normal Windows path fixture;
- all four migrations applied in order;
- exact schema registry rows/checksums;
- expected application ID;
- expected tables/indexes/triggers/foreign keys;
- current open after creation is a no-op.

### 17.3 Migration paths

Create immutable golden databases at versions 1, 2, and 3. Test:

- v1 → v4;
- v2 → v4;
- v3 → v4;
- v4 → v4 no-op;
- each backup is a valid snapshot at the old version;
- all final schemas are semantically identical;
- migrations do not depend on current working directory or culture.

### 17.4 Migration catalogue/history

- duplicate version;
- missing version/gap;
- duplicate name;
- missing embedded resource;
- source checksum mismatch;
- applied checksum mismatch;
- applied unknown migration;
- registry row with invalid timestamp/hash/version;
- database identity mismatch;
- application ID correct but registry absent;
- registry present but application ID wrong.

### 17.5 Interrupted migration

Inject failure/cancellation at every boundary in Section 15.4. Verify:

- current version remains the last committed version;
- failed migration row is absent;
- no partial table/index/trigger remains from a rolled-back migration;
- restart can continue;
- completed backup remains immutable;
- incomplete temp backup is not treated as valid.

### 17.6 Newer-schema refusal

- registry version `max + 1`;
- registry version much newer;
- valid newer history with unknown migration names;
- no backup, no pragma mutation, no migration, no file replacement;
- typed remediation says a compatible/newer application is required.

### 17.7 Corruption/unrecognized database

- random bytes;
- truncated SQLite header/page;
- SQLite database belonging to another application;
- malformed registry;
- failed `quick_check`;
- failed `foreign_key_check`;
- original bytes remain preserved;
- no empty replacement is created.

### 17.8 Backup

- no backup on empty creation unless owner explicitly requires it;
- no backup on current-schema open;
- backup before older-schema migration;
- backup failure blocks all migration writes;
- backup `integrity_check`/history validation;
- backup hash/length manifest;
- name collision resistance;
- temp-file cleanup/quarantine behavior;
- path with spaces/Unicode;
- WAL-mode source backup is consistent;
- backup cannot silently target the live database path.

### 17.9 WAL and connection profile

- `foreign_keys=1` on every factory connection;
- `journal_mode=wal` after initialization;
- `synchronous` resolves to FULL;
- `busy_timeout=5000` or configured bounded value;
- `trusted_schema=0`;
- no `Cache=Shared` in generated connection strings;
- reopen after committed write;
- rollback leaves no row;
- checkpoint/reopen does not lose committed events;
- DB/WAL/SHM behavior is recorded, not used as final SP12 performance policy.

### 17.10 Schema constraints

- invalid foreign key denied;
- duplicate normalized project root key denied;
- invalid JSON denied;
- negative byte length/revision/sequence denied;
- duplicate task key within request denied;
- duplicate attempt ordinal denied;
- multiple terminal per attempt denied when contract is one-to-one;
- lease current-slot uniqueness/fencing increment;
- receipt nonce conflict;
- artifact hash uniqueness;
- usage and exposure remain separate.

### 17.11 Event/projection atomicity

All cases in Section 16.

### 17.12 Architecture

- Domain, Contracts, and Application have no package/reference to SQLite/provider projects;
- CLI, Desktop, VS Code, and UI have no database package/reference/open call;
- only `Square.Persistence.Sqlite` directly references admitted SQLite packages;
- no production reference to prototype SQLite proof code;
- no generated/restored `bin`, `obj`, global-packages, `.db`, WAL, SHM, or evidence payload in source archive.

---

## 18. Test category and commands

Add a real `Persistence` category to the existing test scripts/manifests. The root command should be:

```powershell
./test.ps1 -Category Persistence
```

At minimum it runs:

```powershell
dotnet run --project tests/Persistence.Tests/Persistence.Tests.csproj --no-restore
```

The complete validation sequence is defined in the dependency guide and must include:

```powershell
./build.ps1
./test.ps1 -Category Architecture
./test.ps1 -Category Deterministic
./test.ps1 -Category Persistence
```

Do not add provider-account, WebView, ConPTY, or external-agent requirements to Persistence.Tests.

---

## 19. Evidence contract

Write one evidence directory:

```text
artifacts/test-results/SP02-T01/<UTC-run-id>/
```

Required contents:

```text
environment.json
authority.json
baseline.json
dependency-admission.json
dependency-graph.json
vulnerability-audit.json
package-signatures.txt
package-hashes.json
runtime-sqlite.json
schema-catalog.json
migration-matrix.json
backup-matrix.json
fault-injection-matrix.json
atomicity-matrix.json
architecture.log
build.log
deterministic-tests.log
persistence-tests.log
summary.json
evidence-manifest.sha256
```

### 19.1 `summary.json` minimum fields

```json
{
  "schema_version": "1.0",
  "task_id": "SP02-T01",
  "run_id": "<UTC id>",
  "acceptance_eligible": true,
  "outcome": "PASS",
  "starting_commit": "<sha>",
  "result_commit": "<sha>",
  "authority_hashes": {},
  "dependency_report_sha256": "sha256:<hex>",
  "runtime_sqlite_version": "3.53.4",
  "schema_version_before": 0,
  "schema_version_after": 4,
  "migration_paths_passed": ["0->4", "1->4", "2->4", "3->4", "4->4"],
  "required_tests_passed": 0,
  "required_tests_failed": 0,
  "remaining_risks": []
}
```

`PASS` is forbidden when:

- a prerequisite was bypassed;
- dependency proof is diagnostic/partial;
- a required package/audit/signature/runtime check is missing;
- any required migration/corruption/backup/atomicity test failed;
- tests ran against another package/runtime version;
- source or lock files changed after evidence collection;
- working tree is dirty except generated evidence excluded from source; or
- evidence manifest does not verify.

---

## 20. Completion receipt

Write:

```text
docs/receipts/SP02-T01.completion-receipt.json
```

It must record:

- task/packet version;
- starting and result commits;
- authority and prerequisite receipt hashes;
- exact changed paths;
- exact direct/transitive packages, licenses, signature identities, NuGet content hashes, and local SHA-256 values;
- runtime SQLite version and native DLL hash;
- migration IDs/names/source checksums;
- schema catalogue hash;
- validation commands, exit codes, and log hashes;
- evidence directory/manifest hash;
- any discretion exercised;
- remaining risks and deferred work;
- all STOP/escalation events; and
- final outcome.

The worker must not self-approve a dependency or security exception. Owner/reviewer approval is a separate field.

---

## 21. Budgets

### 21.1 Scope/output budget

- One task, normally one reviewable commit.
- No restored package payloads, native binaries, databases, WAL/SHM, logs, or test evidence inside source-controlled production paths.
- Golden database fixtures should be generated deterministically by test code where possible. Any committed binary fixture requires owner approval, documented generator, SHA-256, and a justified size cap.
- No unrelated refactor.

### 21.2 Retry budget

- Maximum two corrective attempts for the same failing test after a written root-cause hypothesis.
- Do not version-hop packages. A different package/version is a new dependency decision.
- Do not suppress audit/compiler/test warnings to obtain green output.

### 21.3 Context budget

Follow global thresholds:

```text
100K tokens: warning/checkpoint
120K tokens: planned handover
150K tokens or lower route limit: hard stop
```

The handover must contain decisions, changed paths, evidence, current commit, open failures, and next action—not a full transcript.

### 21.4 Time behavior

The worker should end with evidence rather than continue indefinitely. A persistent package, migration, native-loading, corruption, or transaction failure after the retry budget is a STOP and escalation, not a reason to weaken the contract.

---

## 22. Worker discretion

The worker may choose:

- private class/file organization;
- internal helper naming;
- exact SQL formatting;
- deterministic test harness organization;
- whether test databases use temporary directories or an injected test filesystem root;
- whether migration fault injection is an interface, callback, or test-only hook;
- internal batching mechanics that do not change transaction semantics; and
- evidence log formatting beyond required fields.

The worker may not choose:

- another database/provider/ORM;
- another package version;
- another schema table set;
- public contract changes;
- direct client database access;
- event mutability;
- no-backup migration;
- silent newer-schema downgrade;
- corruption auto-repair;
- a weaker security/audit rule;
- a final SSD/checkpoint policy not supported by later measurements; or
- a new dependency without owner review.

---

## 23. Mandatory STOP and escalation conditions

STOP immediately and submit evidence when:

1. G0 or SP01-T06 is not accepted/frozen.
2. The exact dependency graph cannot restore under the pinned SDK.
3. Any `NU1901`, `NU1902`, `NU1903`, or `NU1904` vulnerability is reported.
4. `SQLitePCLRaw.lib.e_sqlite3` appears in the closure.
5. Runtime SQLite is not exactly 3.53.4 for this proof.
6. Any package is unsigned/unverifiable under the approved source policy, has an unclear/incompatible license, or contains an unexplained install/build/native payload.
7. A transitive package appears that is not listed in the approved report.
8. The native x64 library fails to load or a self-contained win-x64 publish omits it.
9. Implementation requires EF Core, another ORM, a second provider, or a system-wide service/admin rights.
10. A required schema identity/field is absent from accepted SP01 contracts.
11. A migration cannot be executed atomically or reliably resumed.
12. A valid backup cannot be created and verified before migration.
13. Event and projection cannot be committed atomically.
14. Corruption/newer schema can be mistaken for an empty database.
15. The worker needs to edit Domain, Contracts, Application, daemon, CLI, UI, or authority documents.
16. Tests require weakening append-only, idempotency, foreign-key, or compatibility rules.
17. The repository's actual baseline differs materially from this packet and no amendment exists.

A STOP report contains exact command, environment, package graph/lock, first causal error, relevant hashes, attempted fixes within budget, and proposed options. It must not contain an unapproved implementation pivot.

---

## 24. Reviewer checklist

### Dependency

- [ ] Exact direct and transitive graph matches the admitted report.
- [ ] No `Microsoft.Data.Sqlite` meta-package and no old `SQLitePCLRaw.lib.e_sqlite3`.
- [ ] Central versions and executable-root lock files are committed.
- [ ] Locked restore passes.
- [ ] Audit/signature/license/native evidence is complete.
- [ ] Runtime reports SQLite 3.53.4 and native DLL hash.

### Architecture

- [ ] SQLite types remain inside `Square.Persistence.Sqlite`.
- [ ] No UI/CLI/host direct database dependency.
- [ ] No new outward dependency from Domain/Contracts/Application.
- [ ] No artifact bodies stored in SQLite.
- [ ] No ORM/generic repository framework.

### Migrations/schema

- [ ] Four migrations are contiguous, immutable, embedded, and checksummed.
- [ ] All required tables/indexes/FKs/triggers exist.
- [ ] Historical checksum drift and newer schemas fail closed.
- [ ] Empty/current/older/unrecognized/corrupt states are distinguished.
- [ ] Current-schema open does not create backup or migration writes.
- [ ] Backup is created and verified before migration.
- [ ] Interrupted migration restarts safely.

### Transactions

- [ ] Events are append-only.
- [ ] Event/projection/idempotency changes are atomic.
- [ ] Duplicate event/stream/idempotency conflicts are typed.
- [ ] Lease slot/fencing constraints are test-covered.

### Quality/evidence

- [ ] All baseline and task tests pass from clean restore.
- [ ] Spaces/Unicode paths pass on Windows.
- [ ] Evidence manifest verifies against result commit.
- [ ] No generated package/database/log output is committed.
- [ ] Receipt accurately records remaining risks and discretion.

---

## 25. Suggested commit structure

Preferred final history:

```text
SP02-T01: add SQLite schema and migration runner
```

The commit contains dependency admission records, package/lock changes, production persistence code, tests, scripts, documentation, and receipt together because they form one atomic admitted boundary.

If repository policy requires separate review commits, use no more than:

```text
SP02-T01: admit reviewed SQLite dependency graph
SP02-T01: add schema and migration runner
```

The second commit must depend on the first, and the final receipt/evidence must identify both. Do not merge the dependency commit alone into a branch where no approved owner uses it.

---

## 26. Definition of done

SP02-T01 is complete only when:

1. all prerequisites and dependency admission are accepted;
2. a clean database reaches migration 4;
3. every supported prior migration path reaches the identical current schema;
4. newer/unrecognized/corrupt/history-drift databases fail without mutation;
5. a verified pre-migration backup exists before every older-schema change;
6. append-only event plus current projection writes are proven atomic;
7. exact package/runtime/lock/signature/audit evidence passes;
8. architecture, deterministic, build, and persistence suites pass;
9. evidence manifest binds to the result commit; and
10. reviewer/owner accepts the completion receipt.

Until all ten are true, SP02-T02 and later production persistence consumers remain blocked.
