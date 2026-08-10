# Official source checklist for SP02-T01 dependency review

Checked while preparing this packet on 2026-08-07. The executing agent must re-check them on the actual proof date.

## Package identities

- Microsoft.Data.Sqlite.Core 10.0.10  
  `https://www.nuget.org/packages/Microsoft.Data.Sqlite.Core/10.0.10`
- SQLitePCLRaw.config.e_sqlite3 3.0.5  
  `https://www.nuget.org/packages/SQLitePCLRaw.config.e_sqlite3/3.0.5`
- SQLitePCLRaw.provider.e_sqlite3 3.0.5  
  `https://www.nuget.org/packages/SQLitePCLRaw.provider.e_sqlite3/3.0.5`
- SQLitePCLRaw.core 3.0.5  
  `https://www.nuget.org/packages/SQLitePCLRaw.core/3.0.5`
- SourceGear.sqlite3 3.53.4  
  `https://www.nuget.org/packages/SourceGear.sqlite3/3.53.4`

## Provider/native selection

- Microsoft.Data.Sqlite — custom SQLite versions  
  `https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/custom-versions`
- SQLitePCLRaw v3 release notes  
  `https://github.com/ericsink/SQLitePCL.raw/blob/main/v3.md`
- SQLitePCLRaw repository package/configuration explanation  
  `https://github.com/ericsink/SQLitePCL.raw`

## Security, package integrity, and lock files

- GitHub Advisory Database — CVE-2025-6965 / GHSA-2m69-gcr7-jv3q  
  `https://github.com/advisories/GHSA-2m69-gcr7-jv3q`
- NuGet package auditing  
  `https://learn.microsoft.com/en-us/nuget/concepts/auditing-packages`
- PackageReference lock files / locked mode  
  `https://learn.microsoft.com/en-us/nuget/consume-packages/package-references-in-project-files`
- `dotnet nuget verify`  
  `https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-nuget-verify`
- NuGet signed-package verification  
  `https://learn.microsoft.com/en-us/dotnet/core/tools/nuget-signed-package-verification`

## SQLite runtime behavior

- Microsoft.Data.Sqlite online backup  
  `https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/backup`
- `SqliteConnection.BackupDatabase` API  
  `https://learn.microsoft.com/en-us/dotnet/api/microsoft.data.sqlite.sqliteconnection.backupdatabase`
- Microsoft.Data.Sqlite connection strings / shared-cache caution  
  `https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/connection-strings`
- SQLite Online Backup API  
  `https://sqlite.org/backup.html`
- SQLite WAL  
  `https://sqlite.org/wal.html`

## Current facts to verify again

- `Microsoft.Data.Sqlite.Core` contains the managed provider and requires an explicitly installed/initialized native SQLite path.
- SQLitePCLRaw v3 introduces `config.e_sqlite3`, moves the native package from `SQLitePCLRaw.lib.e_sqlite3` to `SourceGear.sqlite3`, and defines the compatibility bundle as config + SourceGear native package.
- `SourceGear.sqlite3` 3.53.4 identifies a SQLite 3.53.4 native build.
- CVE-2025-6965 affects SQLite before 3.50.2 and the old `SQLitePCLRaw.lib.e_sqlite3` package through 2.1.11.
- The actual lock graph, downloaded package files, signatures, advisory state, and loaded runtime—not this checklist—determine admission.
