# SP02-T01 Agent Pack

This pack contains an implementation-ready dispatch packet and a separate dependency-admission proof guide for:

```text
SP02-T01 — SQLite schema and migration runner
```

Files:

- `SP02-T01_DISPATCH_PACKET.md` — task authority, scope, implementation contract, schema baseline, tests, evidence, STOP conditions, and reviewer checklist.
- `SP02-T01_DEPENDENCY_ADMISSION_PROOF_GUIDE.md` — exact package candidate, clean-room restore/audit/signature/native-runtime proof, admission criteria, and evidence format.
- `templates/SP02-T01.dispatch.json` — dispatcher fields that must be filled from the local Git checkout.
- `templates/SP02-T01.dependency-admission-report.json` — machine-readable dependency decision template.
- `templates/SP02-T01.completion-receipt.json` — completion receipt template.
- `templates/SP02-T01.evidence-summary.json` — test/evidence summary template.

## Gate warning

This packet does **not** authorize bypassing the repository's release order. Production implementation is dispatchable only after:

1. G0 has been superseded and accepted with Windows evidence; and
2. SP01-T06 has frozen and accepted the domain/contracts baseline consumed by persistence.

The dependency proof may be performed earlier only as an explicitly owner-authorized, isolated research activity. It must not merge production package references or persistence code before the prerequisites are met.

## Package candidate frozen by this packet

The dependency proof evaluates exactly:

```text
Microsoft.Data.Sqlite.Core          10.0.10
SQLitePCLRaw.config.e_sqlite3       3.0.5
SourceGear.sqlite3                  3.53.4
```

Expected transitive closure:

```text
SQLitePCLRaw.provider.e_sqlite3     3.0.5
SQLitePCLRaw.core                   3.0.5
```

No package is admitted merely because it appears in this document. Admission occurs only after the proof produces a qualifying PASS and the owner/reviewer records approval.
