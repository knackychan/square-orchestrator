# SP02-T01 — SQLite Dependency-Admission Proof Guide

- **Guide revision:** `1.0-draft`
- **Prepared:** 2026-08-07
- **Applies to:** Windows x64, .NET SDK 10.0.302, `Square.Persistence.Sqlite`
- **Decision type:** fail-closed package/runtime admission
- **Result values:** `PASS`, `REJECT`, or `BLOCKED`

---

## 1. Purpose

This proof determines whether the exact managed and native SQLite dependency graph is acceptable for production use in SP02-T01. It is intentionally separate from schema implementation so the worker cannot conceal a package/security failure behind working application code.

A package is not admitted because it is popular, published by a known vendor, restores successfully, or appears to fix one known CVE. Admission requires a complete, reproducible package graph and evidence for:

- identity and exact versions;
- transitive closure;
- source/provenance and signatures;
- license/redistribution;
- known-vulnerability audit;
- package content inspection;
- native Windows x64 loading;
- exact runtime SQLite version;
- self-contained publish behavior;
- required SQLite capabilities; and
- committed lock files and locked restore.

The proof must fail closed. It must not replace SQLite with another persistence design when the candidate fails.

---

## 2. Current candidate and rationale

### 2.1 Direct package set

Evaluate exactly:

| Package | Version | Role | Expected license |
|---|---:|---|---|
| `Microsoft.Data.Sqlite.Core` | `10.0.10` | Managed ADO.NET provider without an implicit native SQLite binary | MIT |
| `SQLitePCLRaw.config.e_sqlite3` | `3.0.5` | Cross-platform configuration/provider initialization for native library base name `e_sqlite3` | Apache-2.0 |
| `SourceGear.sqlite3` | `3.53.4` | Native SQLite binaries, including Windows x64 `e_sqlite3` | SQLite Public Domain license file |

Expected transitive closure on `net10.0`:

| Package | Expected version | Reason |
|---|---:|---|
| `SQLitePCLRaw.provider.e_sqlite3` | `3.0.5` | Native provider selected by the config package |
| `SQLitePCLRaw.core` | `3.0.5` | SQLitePCLRaw core required by provider and managed SQLite layer |

The lock file—not this table—is the authoritative statement of the actual resolved graph. A graph difference is a review event, not something the worker may wave through.

### 2.2 Why this explicit trio is selected

- `Microsoft.Data.Sqlite.Core` keeps the managed ADO.NET provider separate from the native-binary choice.
- SQLitePCLRaw v3 defines `config.e_sqlite3` specifically so the native package can be selected independently.
- SQLitePCLRaw v3 replaced the old `SQLitePCLRaw.lib.e_sqlite3` native package with `SourceGear.sqlite3`.
- `SourceGear.sqlite3` versions identify the SQLite version in their first three components, making the native SQLite identity explicit.
- The configuration package supports `SQLitePCL.Batteries_V2.Init()` and the expected `e_sqlite3` provider.

### 2.3 Why not the broad `Microsoft.Data.Sqlite` meta-package

The meta-package is convenient, but this proof needs an explicit direct native-binary decision. A minimum transitive requirement is not the same as a reviewed, directly pinned native version. The explicit trio makes the architecture and package review visible in `Directory.Packages.props`, `Square.Persistence.Sqlite.csproj`, the lock file, and `THIRD_PARTY.md`.

### 2.4 Clarification about `SQLitePCLRaw.bundle_e_sqlite3`

Version `3.0.5` of the bundle is not automatically rejected. SQLitePCLRaw v3 describes it as a compatibility convenience that brings in `config.e_sqlite3` plus `SourceGear.sqlite3`. This packet nevertheless chooses the explicit trio so the native package/version is a first-class reviewed dependency. Using the bundle instead requires an owner amendment and a new report, even if it resolves to the same closure.

### 2.5 Known CVE boundary

The proof must reject any graph containing:

```text
SQLitePCLRaw.lib.e_sqlite3 <= 2.1.11
```

That package line is affected by CVE-2025-6965 and has no patched version in the affected package series. The advisory recommends SQLite 3.50.2 or later. The selected native candidate is SQLite 3.53.4, but its version alone is not enough: the actual loaded runtime and package closure must be proven.

---

## 3. Preconditions

### 3.1 Environment

Run on a normal non-administrator Windows x64 shell from a clean checkout.

Required:

```text
Windows x64
PowerShell 7 preferred; scripts must remain Windows PowerShell compatible where repository policy requires
.NET SDK 10.0.302
NuGet source: nuget.org only, unless an owner-approved mirror is recorded
Internet access for initial clean restore and advisory lookup
No uncommitted repository changes
```

Node, pnpm, VS Code, WebView2, and agent CLIs are not required for this dependency proof.

### 3.2 Repository/gate state

For production admission and merge, G0 and SP01-T06 must be accepted. An owner may authorize an earlier isolated proof branch, but that branch must not merge production package references or implementation code before normal prerequisites.

### 3.3 Capture baseline

Before editing:

```powershell
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

git status --porcelain=v1
git rev-parse HEAD
git diff --no-ext-diff --exit-code
dotnet --version
dotnet --info
Get-ComputerInfo | Select-Object WindowsProductName, WindowsVersion, OsBuildNumber, OsArchitecture
```

Save the exact output to the proof evidence directory.

---

## 4. Evidence directory

Create one unique directory outside source-controlled paths:

```powershell
$runId = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffffffZ')
$evidence = Join-Path $PWD "artifacts\test-results\SP02-T01-dependency\$runId"
$packages = Join-Path $evidence 'global-packages'
$nupkgs = Join-Path $evidence 'nupkgs'
$expanded = Join-Path $evidence 'expanded'
$logs = Join-Path $evidence 'logs'
New-Item -ItemType Directory -Force -Path $evidence, $packages, $nupkgs, $expanded, $logs | Out-Null
```

Use an isolated package cache for this proof:

```powershell
$env:NUGET_PACKAGES = $packages
```

Do not clear the user's global NuGet caches. Isolation is enough and is safer.

---

## 5. Repository changes proposed by an admitted result

Do not commit these changes until the proof has passed and the reviewer accepts the report.

### 5.1 `Directory.Build.props`

Add explicit audit policy if not already present:

```xml
<PropertyGroup>
  <NuGetAudit>true</NuGetAudit>
  <NuGetAuditMode>all</NuGetAuditMode>
  <NuGetAuditLevel>low</NuGetAuditLevel>
</PropertyGroup>
```

The repository already treats warnings as errors, so audit warnings should fail the build/restore. Do not suppress `NU1901`–`NU1904`.

### 5.2 `Directory.Packages.props`

Replace the empty dependency group with exact centrally managed versions:

```xml
<Project>
  <ItemGroup>
    <PackageVersion Include="Microsoft.Data.Sqlite.Core" Version="10.0.10" />
    <PackageVersion Include="SQLitePCLRaw.config.e_sqlite3" Version="3.0.5" />
    <PackageVersion Include="SourceGear.sqlite3" Version="3.53.4" />
  </ItemGroup>
</Project>
```

Do not enable floating versions. Do not add `Microsoft.Data.Sqlite`, EF Core, a bundle, or a transitive package as a direct reference unless the accepted report explicitly says so.

### 5.3 `Square.Persistence.Sqlite.csproj`

Add direct references without local version attributes:

```xml
<PropertyGroup>
  <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
</PropertyGroup>

<ItemGroup>
  <PackageReference Include="Microsoft.Data.Sqlite.Core" />
  <PackageReference Include="SQLitePCLRaw.config.e_sqlite3" />
  <PackageReference Include="SourceGear.sqlite3" />
</ItemGroup>
```

Do not use `PrivateAssets=all`; these are runtime dependencies of the persistence leaf and must flow to executable roots.

### 5.4 Executable test/probe lock file

The authoritative reproducible closure must be locked at an executable root. Add:

```xml
<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
```

to `tests/Persistence.Tests/Persistence.Tests.csproj`, and commit its generated `packages.lock.json`. A library lock file may also be committed for review convenience, but it does not replace the executable-root lock because a consuming executable can resolve a different closure.

When `Square.Daemon` later becomes a package-consuming executable root, its own lock file must be generated and reviewed in the task that adds that dependency path.

### 5.5 `NuGet.Config`

Keep `nuget.org` as the only package source. Add an explicit audit source when supported by the pinned toolchain:

```xml
<auditSources>
  <clear />
  <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
</auditSources>
```

A repository signature policy may be added after the signature proof. Do not add a trusted signer fingerprint by guessing it from one run; the owner must decide the durable trust policy.

### 5.6 `THIRD_PARTY.md`

Record every direct and expected transitive package, including:

- exact version;
- direct/transitive status;
- managed/native role;
- owner/project source;
- license and redistribution obligations;
- package source;
- signature result and signer type;
- local package SHA-256;
- NuGet content hash;
- runtime native DLL path/hash;
- advisory audit result;
- architectural owner `Square.Persistence.Sqlite`;
- initialization requirement; and
- upgrade/re-review rule.

---

## 6. Clean restore and graph proof

### 6.1 Restore with full audit

After applying the candidate references in a proof branch:

```powershell
$restoreProps = @(
  '-p:NuGetAudit=true',
  '-p:NuGetAuditMode=all',
  '-p:NuGetAuditLevel=low'
)

& dotnet restore .\src\Square.Persistence.Sqlite\Square.Persistence.Sqlite.csproj `
  --force-evaluate --no-http-cache @restoreProps 2>&1 |
  Tee-Object -FilePath (Join-Path $logs 'restore-persistence.log')
if ($LASTEXITCODE -ne 0) { throw "Persistence restore failed: $LASTEXITCODE" }

& dotnet restore .\tests\Persistence.Tests\Persistence.Tests.csproj `
  --force-evaluate --no-http-cache @restoreProps 2>&1 |
  Tee-Object -FilePath (Join-Path $logs 'restore-tests.log')
if ($LASTEXITCODE -ne 0) { throw "Test restore failed: $LASTEXITCODE" }
```

Reject on any `NU1901`, `NU1902`, `NU1903`, or `NU1904`. Do not add `NoWarn`, lower `NuGetAuditMode`, raise `NuGetAuditLevel`, or remove transitive auditing.

### 6.2 Produce machine-readable graph

With .NET 10 noun-first syntax:

```powershell
& dotnet package list .\tests\Persistence.Tests\Persistence.Tests.csproj `
  --include-transitive --format json --output-version 1 `
  | Set-Content -Encoding utf8 (Join-Path $evidence 'dependency-graph.json')
if ($LASTEXITCODE -ne 0) { throw 'Package graph failed' }

& dotnet package list .\tests\Persistence.Tests\Persistence.Tests.csproj `
  --include-transitive --vulnerable --format json --output-version 1 `
  | Set-Content -Encoding utf8 (Join-Path $evidence 'vulnerability-audit.json')
if ($LASTEXITCODE -ne 0) { throw 'Vulnerability report failed' }

& dotnet package list .\tests\Persistence.Tests\Persistence.Tests.csproj `
  --include-transitive --deprecated --format json --output-version 1 `
  | Set-Content -Encoding utf8 (Join-Path $evidence 'deprecated-packages.json')
if ($LASTEXITCODE -ne 0) { throw 'Deprecated report failed' }
```

If the installed CLI does not support one option combination, run separate commands and record the exact supported syntax. Do not omit the information.

### 6.3 Required closure assertion

Parse `packages.lock.json` and the graph. The set of SQLite-related package IDs must be exactly:

```text
Microsoft.Data.Sqlite.Core          10.0.10
SQLitePCLRaw.config.e_sqlite3       3.0.5
SQLitePCLRaw.provider.e_sqlite3     3.0.5
SQLitePCLRaw.core                   3.0.5
SourceGear.sqlite3                  3.53.4
```

Reject when:

- an expected package/version is absent;
- any other SQLite/native/provider/ORM package appears;
- `SQLitePCLRaw.lib.e_sqlite3` appears at any version;
- any package version is a range/floating/pre-release version;
- the graph differs between persistence library and executable test root without a documented reason; or
- a package is pruned/selected differently in a way that changes runtime identity.

### 6.4 Locked restore

After reviewing and retaining the generated lock files:

```powershell
& dotnet restore .\tests\Persistence.Tests\Persistence.Tests.csproj `
  --locked-mode --no-http-cache @restoreProps 2>&1 |
  Tee-Object -FilePath (Join-Path $logs 'restore-locked.log')
if ($LASTEXITCODE -ne 0) { throw "Locked restore failed: $LASTEXITCODE" }
```

Then change one candidate version locally without updating the lock and prove locked restore fails. Revert the deliberate change. Record this negative test.

---

## 7. Download and verify exact package archives

Restore verifies repository signatures on Windows, but the proof also retains explicit package signature/content evidence.

### 7.1 Exact package archive URLs

Download the exact direct and expected transitive `.nupkg` files from the NuGet flat container:

```powershell
$packageSet = @(
  @{ Id = 'microsoft.data.sqlite.core';          Version = '10.0.10' },
  @{ Id = 'sqlitepclraw.config.e_sqlite3';       Version = '3.0.5' },
  @{ Id = 'sqlitepclraw.provider.e_sqlite3';     Version = '3.0.5' },
  @{ Id = 'sqlitepclraw.core';                   Version = '3.0.5' },
  @{ Id = 'sourcegear.sqlite3';                  Version = '3.53.4' }
)

foreach ($p in $packageSet) {
  $file = "$($p.Id).$($p.Version).nupkg"
  $uri = "https://api.nuget.org/v3-flatcontainer/$($p.Id)/$($p.Version)/$file"
  Invoke-WebRequest -UseBasicParsing -Uri $uri -OutFile (Join-Path $nupkgs $file)
}
```

Any redirect/source difference must be recorded. Do not download from a third-party mirror for the qualifying proof.

### 7.2 Signature verification

```powershell
& dotnet nuget verify (Join-Path $nupkgs '*.nupkg') --all --verbosity detailed 2>&1 |
  Tee-Object -FilePath (Join-Path $evidence 'package-signatures.txt')
if ($LASTEXITCODE -ne 0) { throw "Package signature verification failed: $LASTEXITCODE" }
```

Record:

- verification result;
- author/repository signature type;
- certificate subjects/issuers/fingerprints shown by the tool;
- timestamp verification;
- .NET 10 content hashes; and
- exact command/tool version.

A package from nuget.org may be repository-signed rather than author-signed. Repository signing is acceptable only when verification succeeds under the approved NuGet source/trust policy. An unsigned or unverifiable package is `BLOCKED`, not automatically accepted.

### 7.3 Local archive hashes

```powershell
Get-ChildItem $nupkgs -Filter *.nupkg |
  Get-FileHash -Algorithm SHA256 |
  Select-Object Path, Algorithm, Hash |
  ConvertTo-Json -Depth 4 |
  Set-Content -Encoding utf8 (Join-Path $evidence 'package-sha256.json')
```

Also preserve lock-file `contentHash` values (NuGet's package content hash) in the report. Do not confuse them with the local SHA-256 field.

---

## 8. Package-content inspection

### 8.1 Expand archives

`.nupkg` files are ZIP archives:

```powershell
Add-Type -AssemblyName System.IO.Compression.FileSystem
foreach ($pkg in Get-ChildItem $nupkgs -Filter *.nupkg) {
  $dest = Join-Path $expanded $pkg.BaseName
  if (Test-Path $dest) { Remove-Item -Recurse -Force $dest }
  [System.IO.Compression.ZipFile]::ExtractToDirectory($pkg.FullName, $dest)
}
```

### 8.2 Inventory

Generate a sorted inventory of every archive entry with path, length, and SHA-256. Review at minimum:

- `.nuspec` dependency ranges;
- license file/expression;
- repository/source metadata;
- `build`, `buildTransitive`, `tools`, `analyzers`, `contentFiles`, and install scripts;
- managed assemblies by target framework;
- native runtime assets by RID;
- PowerShell/shell/executable payloads;
- unexpected network/update/bootstrap behavior; and
- duplicate/native libraries from another package.

### 8.3 Expected content conclusions

#### `Microsoft.Data.Sqlite.Core 10.0.10`

Expected:

- managed ADO.NET provider assembly;
- no native SQLite DLL;
- MIT license;
- dependency on SQLitePCLRaw core through the resolved graph.

Reject unexplained native binaries, install scripts, or another provider.

#### `SQLitePCLRaw.config.e_sqlite3 3.0.5`

Expected:

- small managed configuration package;
- dependency on `SQLitePCLRaw.provider.e_sqlite3 >= 3.0.5` for the applicable framework;
- no native SQLite DLL;
- Apache-2.0.

#### `SQLitePCLRaw.provider.e_sqlite3 3.0.5`

Expected:

- managed provider using native base name `e_sqlite3`;
- dependency on `SQLitePCLRaw.core`;
- no bundled stale native SQLite library;
- Apache-2.0 family license.

#### `SQLitePCLRaw.core 3.0.5`

Expected:

- managed low-level SQLitePCLRaw core;
- no unrelated runtime payload;
- Apache-2.0.

#### `SourceGear.sqlite3 3.53.4`

Expected:

- native `e_sqlite3` builds for supported runtime identifiers;
- Windows x64 asset;
- no managed initialization layer;
- no package dependencies;
- SQLite Public Domain license file;
- version identity corresponding to SQLite 3.53.4.

The 26 MB multi-platform package size is expected, but every native asset and RID must still be inventoried.

### 8.4 License/redistribution review

Record the exact license file/expression from each package, not a web summary only.

Minimum conclusions to verify:

```text
Microsoft.Data.Sqlite.Core       MIT — retain notice/license in distribution/SBOM as required
SQLitePCLRaw.*                   Apache-2.0 — retain license/notice and review NOTICE files
SourceGear.sqlite3 native SQLite SQLite is Public Domain — preserve package-provided license/provenance record
```

Do not infer that all SourceGear service/build offerings have the same license as this free package. Review the package actually downloaded.

---

## 9. Runtime/native proof

### 9.1 Initialization

The probe must explicitly call:

```csharp
SQLitePCL.Batteries_V2.Init();
```

before the first managed SQLite connection. Production code wraps this in a thread-safe one-time initializer.

### 9.2 Required runtime queries

Open a new temporary database through `Microsoft.Data.Sqlite.SqliteConnection`, then record:

```sql
SELECT sqlite_version();
PRAGMA compile_options;
SELECT json_valid('{"ok":true}');
PRAGMA foreign_keys;
PRAGMA journal_mode;
PRAGMA synchronous;
PRAGMA busy_timeout;
PRAGMA trusted_schema;
PRAGMA integrity_check;
```

Qualifying results:

```text
sqlite_version() exactly 3.53.4
json_valid(...) = 1
foreign_keys = 1 after factory configuration
journal_mode can be set to wal and returns wal
synchronous can be set/read as FULL
busy_timeout reads back the configured bounded value
trusted_schema reads back 0
integrity_check returns exactly ok
```

Record all compile options, but do not fail solely because a non-required optional compile flag is absent. Use functional tests for required capabilities.

### 9.3 Transaction probe

Prove:

1. create table and insert within a transaction;
2. rollback removes the row;
3. commit persists the row after connection close/reopen;
4. unique/foreign-key violations return expected SQLite error identities;
5. `PRAGMA foreign_keys=ON` is active on every new factory connection; and
6. WAL-mode reopen retains committed data.

### 9.4 Online backup probe

Using the managed `BackupDatabase` method:

1. create source data in WAL mode;
2. leave a second read connection active;
3. back up to a separate destination database;
4. close and reopen destination read-only;
5. verify rows, schema identity, and `integrity_check`;
6. hash the destination file; and
7. prove a source change after the snapshot is not falsely included in the already completed backup.

This validates the API required by the migration runner. Do not qualify a raw copy of only the main `.db` file as the backup proof.

### 9.5 Native DLL identity

From the build/publish output, locate the loaded/copy-selected Windows x64 `e_sqlite3.dll` and record:

- full evidence-relative path;
- PE architecture (`x64`/AMD64);
- file length;
- SHA-256;
- file version/product metadata when present;
- package archive/path that supplied it; and
- runtime `sqlite_version()` result.

A DLL with the right filename but wrong package/hash/runtime result is a failure.

---

## 10. Self-contained publish proof

The product architecture intends self-contained Windows packaging, so the dependency must survive a clean publish.

Use either a dedicated `tests/fixtures/Square.SqliteProbe` executable or the executable Persistence.Tests root. Publish:

```powershell
$publish = Join-Path $evidence 'publish-win-x64'

& dotnet publish .\tests\fixtures\Square.SqliteProbe\Square.SqliteProbe.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained true `
  --output $publish `
  --no-restore 2>&1 |
  Tee-Object -FilePath (Join-Path $logs 'publish-win-x64.log')
if ($LASTEXITCODE -ne 0) { throw "Publish failed: $LASTEXITCODE" }
```

If no dedicated probe exists, adapt the project path while preserving the same test.

Run the published executable from:

- a different working directory;
- a path containing spaces;
- a path containing Unicode; and
- an environment where it cannot accidentally resolve native assets from the source tree/global package cache.

The published probe must print a machine-readable report containing package expectations, native DLL hash, and SQLite 3.53.4. It must create/use/delete only its evidence-scoped temporary database.

Reject when:

- native DLL is missing;
- wrong architecture is selected;
- runtime falls back to a system SQLite;
- initialization requires an undocumented file from the developer checkout;
- self-contained publish introduces another SQLite/native package; or
- output differs from normal framework-dependent execution.

---

## 11. Security audit requirements

### 11.1 NuGet audit

A qualifying run requires:

- `NuGetAudit=true`;
- `NuGetAuditMode=all`;
- `NuGetAuditLevel=low`;
- no NU1901–NU1904 warnings;
- a saved machine-readable vulnerable-package report; and
- audit source availability recorded.

If the audit source is unavailable, the result is `BLOCKED`, not PASS.

### 11.2 Explicit advisory checks

In addition to automated audit, search official advisory sources for every package ID and the native SQLite version. Record the search date and result. At minimum verify:

- `SQLitePCLRaw.lib.e_sqlite3` CVE-2025-6965 is not present;
- no advisory applies to the selected package versions;
- no advisory applies to SQLite 3.53.4 at the proof date; and
- no package is deprecated with a security-relevant replacement instruction.

Do not treat “no results” from one database as proof that no vulnerability exists. Record sources and uncertainty.

### 11.3 Package source and signature

- initial qualifying package bytes come from nuget.org;
- restore and explicit `dotnet nuget verify` succeed;
- package hashes match the retained report;
- no local feed/source override is active;
- no unsigned replacement is accepted; and
- no package source credentials or user secrets enter evidence.

### 11.4 Native surface

This SQLite database is local and not a network listener, but untrusted project/model-derived strings may later enter event payloads. The implementation must use parameters for all data and static migration SQL. The dependency proof should include malformed input/large JSON/basic constraint tests, but fuzzing the full native library is outside SP02-T01.

---

## 12. Compatibility checks

Prove on the exact supported build target:

```text
net10.0
win-x64
.NET SDK 10.0.302
normal non-admin user
```

Required build modes:

- Debug framework-dependent;
- Release framework-dependent;
- Release self-contained win-x64.

This packet does not admit arm64. Arm64 may be reviewed later as a separate packaging target. The multi-platform native package may contain arm64 assets, but their presence does not constitute arm64 acceptance.

---

## 13. Admission report

Write:

```text
docs/dependencies/SP02-T01-sqlite-dependency-admission.json
docs/dependencies/SP02-T01-sqlite-dependency-admission.md
```

The JSON must include:

```json
{
  "schema_version": "1.0",
  "decision_id": "SP02-T01-SQLITE-DEP-001",
  "decision": "PASS",
  "reviewed_utc": "<canonical UTC>",
  "starting_commit": "<sha>",
  "sdk_version": "10.0.302",
  "target_framework": "net10.0",
  "runtime_identifier": "win-x64",
  "direct_packages": [],
  "transitive_packages": [],
  "lock_files": [],
  "nuget_audit": {},
  "signatures": [],
  "licenses": [],
  "package_hashes": [],
  "native_assets": [],
  "runtime_sqlite_version": "3.53.4",
  "capabilities": {},
  "self_contained_publish": {},
  "evidence_manifest_sha256": "sha256:<hex>",
  "known_risks": [],
  "conditions": [],
  "reviewer": "<owner/reviewer>",
  "reviewer_approval": false
}
```

The worker may set a technical recommendation, but `reviewer_approval` remains false until the owner/reviewer accepts it.

---

## 14. Decision rules

### 14.1 PASS

PASS requires all of the following:

- exact package graph and lock files;
- full audit with no advisory warnings;
- package signatures verify;
- license/provenance/redistribution review complete;
- package content matches expected roles;
- no old native package or unexpected provider;
- Windows x64 native asset present and hashed;
- managed initialization succeeds;
- runtime SQLite exactly 3.53.4;
- required JSON/foreign-key/WAL/transaction/backup behavior passes;
- framework-dependent and self-contained publish/run pass;
- locked restore passes and drift negative test fails as expected;
- evidence manifest verifies; and
- owner/reviewer records approval.

### 14.2 REJECT

Use REJECT when evidence demonstrates that the candidate cannot satisfy a locked contract, for example:

- known unremediated vulnerability in the selected graph;
- incompatible license/redistribution term;
- wrong/uncontrollable native runtime;
- native loading/publish failure intrinsic to the selected packages;
- required SQLite feature/backup/transaction behavior absent; or
- unacceptable package payload/provenance.

REJECT does not authorize a JSON store, another database, ORM, or package. It triggers an owner decision and, where necessary, a leaf-technology proof/amendment.

### 14.3 BLOCKED

Use BLOCKED when the proof is incomplete or environmental, for example:

- advisory/audit source unavailable;
- package archive/signature cannot be retrieved/verified;
- prerequisite gate missing;
- unsupported SDK/OS/architecture;
- dirty or unidentifiable source baseline;
- evidence missing; or
- package graph differs and has not been reviewed.

---

## 15. Mandatory STOP conditions

Stop and preserve evidence when:

1. any package/version differs from the packet;
2. any unexpected transitive package appears;
3. any vulnerability warning/advisory applies;
4. `SQLitePCLRaw.lib.e_sqlite3` appears;
5. exact runtime is not SQLite 3.53.4;
6. package signature verification fails or package bytes differ between restore/download evidence;
7. license is missing, inconsistent, or not acceptable for planned redistribution;
8. native DLL is absent, wrong architecture, loaded from an unexpected location, or falls back to system SQLite;
9. self-contained publish fails;
10. managed initialization is non-deterministic or requires hidden global state;
11. WAL, foreign keys, transactions, JSON, or online backup fails;
12. proof requires administrator rights or a machine-wide service;
13. a dependency warning must be suppressed to continue;
14. the worker proposes another version/package/architecture without owner amendment; or
15. evidence cannot be bound to the exact source/lock state.

---

## 16. Suggested automation script behavior

A repeatable script may be added at:

```text
build/dependencies/verify-sqlite.ps1
```

It should:

1. require repository root and clean worktree unless `-Diagnostic` is explicitly set;
2. verify SDK/OS/architecture;
3. create a unique evidence directory;
4. set isolated `NUGET_PACKAGES`;
5. restore with full audit;
6. generate/validate graph and lock files;
7. download exact package archives;
8. verify signatures and hashes;
9. inspect package inventories/licenses;
10. build and run the runtime probe;
11. publish/run self-contained win-x64 probe;
12. run locked restore and negative drift test;
13. write summary/manifest; and
14. return nonzero unless every qualifying condition passes.

`-Diagnostic` may skip clean-worktree/commit binding for troubleshooting, but its output must be marked `acceptance_eligible=false` and can never produce PASS.

---

## 17. Reviewer checklist

### Identity and graph

- [ ] Direct trio exactly matches packet.
- [ ] Transitive closure exactly matches report/lock.
- [ ] No meta-package, bundle, ORM, second provider, or old native package.
- [ ] Lock at executable root committed and locked restore passes.

### Security/provenance

- [ ] Full transitive audit ran at low threshold and is clean.
- [ ] Explicit CVE-2025-6965 exclusion is demonstrated.
- [ ] Package archives came from approved source.
- [ ] Signatures/content hashes verify.
- [ ] Package inventories contain no unexplained executable/build/install payload.

### License

- [ ] Microsoft provider MIT record retained.
- [ ] SQLitePCLRaw Apache-2.0 records retained.
- [ ] SourceGear package's SQLite Public Domain license file retained in evidence/distribution planning.
- [ ] `THIRD_PARTY.md` is accurate and distinguishes direct/transitive/native roles.

### Runtime

- [ ] `Batteries_V2.Init()` required and encapsulated exactly once.
- [ ] Runtime SQLite exactly 3.53.4.
- [ ] Native DLL is Windows x64, from SourceGear package, and hashed.
- [ ] No system SQLite fallback.
- [ ] JSON, FK, WAL, transaction, integrity, and backup probes pass.
- [ ] Self-contained publish/run passes from isolated path.

### Evidence

- [ ] Evidence bound to exact commit/authority/lock files.
- [ ] All commands and exit codes recorded.
- [ ] No package/database/native payload committed to source.
- [ ] Machine-readable and human reports agree.
- [ ] Owner/reviewer approval is explicit.

---

## 18. Upgrade policy after admission

Admission is version-specific. Later upgrades must repeat at least:

- package graph/lock diff;
- advisory audit;
- signature/hash/license review;
- package-content/native asset diff;
- runtime `sqlite_version()` and capability probe;
- framework-dependent/self-contained publish tests;
- migration/backup/atomicity regression suite; and
- owner acceptance.

No Dependabot, agent, IDE, `dotnet package update`, or central version change may merge an SQLite/provider/native upgrade automatically.

---

## 19. Official-source checklist used to prepare this guide

The dispatcher/reviewer should re-check current official sources at proof time:

- NuGet Gallery package pages for all five expected packages;
- Microsoft Learn: custom SQLite versions;
- SQLitePCLRaw v3 release notes and repository documentation;
- GitHub Advisory Database entry for CVE-2025-6965;
- Microsoft Learn: NuGet package auditing;
- Microsoft Learn: PackageReference lock files and locked mode;
- Microsoft Learn: `dotnet nuget verify`;
- Microsoft Learn: Microsoft.Data.Sqlite online backup and connection strings;
- SQLite documentation: Online Backup API and WAL.

The facts in this packet were checked on 2026-08-07. The proof must use the facts available on the actual execution date and record any new advisory or package state.
