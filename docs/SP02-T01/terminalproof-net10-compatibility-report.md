# TerminalProof .NET 10 Compatibility Report

- Date: 2026-08-08
- SDK: 10.0.302
- OS: Windows x64
- Branch: main, commit bd5e4ec30c0f2e4e31fea82d48ae700cdb5f041f

## Changes made (all in TerminalProof)

### Fixed issues

1. **Struct/const name collision** — `NativeMethods.cs`: structs `JobObjectExtendedLimitInformation`, `JobObjectBasicAccountingInformation`, `JobObjectBasicAndIoAccountingInformation` renamed to `*Info` suffix. Collided with same-named `const int` in same class. NET 10 added BCL types of same name.

2. **SafeHandle.IsInvalid check** — `WindowsProcessEnvironment.cs`: `Process.GetCurrentProcess().SafeHandle` returns null/invalid in .NET 10. Replaced with raw Win32 `GetCurrentProcess()` pseudo-handle (-1) via `IsProcessInJobRaw` IntPtr overload.

3. **Console resize crash** — `FixtureProgram.cs`: `Console.WindowWidth`/`WindowHeight` throws `IOException: handle invalid` under ConPTY in .NET 10. Wrapped in try-catch returning "unknown".

4. **Evidence file lock** — `ProofEvidenceWriter.cs`: `runs.ndjson` held open during `WriteEvidenceManifestAsync` causing `FileShare.Read` conflict in .NET 10. Now disposes stream before hashing evidence files.

### Unresolved: ConPTY output capture broken

**Symptom:** `WaitForOutputAsync` times out after 20s. Output capture only records 238 bytes (`ESC[?9001h ESC[?1004h` — ConPTY escape sequences). Fixture's actual output (BURST-DATA, NORMAL-EXIT, QUIET-READY, etc.) appears in terminal but is NOT captured via ConPTY pipe.

**Root cause:** Fixture process writes to host console, not through pseudoconsole. The `PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE` attribute is set correctly via `ProcThreadAttributeList`, and `CreatePseudoConsole` succeeds. But in .NET 10, the process's console handle routing changed — `Console.WriteLine()` may bypass the pseudoconsole.

**Attempted fix:** Deferred disposal of `pseudoInputRead`/`pseudoOutputWrite` (pseudoconsole-side pipe handles) from post-CreateProcess to session disposal. No effect — output still goes to host.

**Secondary issue:** `ClosePseudoConsole` times out during cleanup (10s timeout). Likely because handles are not properly balanced after the deferred-disposal change.

**File:** `ConPtyTerminalSession.cs:95-172` (StartAsync)

### Suggested investigation points

1. Check if .NET 10 `CreateProcessW` with `PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE` requires different handle handling. Possibly `inheritHandles: false` is incorrect for pseudoconsole in .NET 10.

2. Test whether `STARTF_USESTDHANDLES` with explicit pseudoconsole pipe handles works around the routing issue.

3. Try `CreateProcess` with `PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE` and the EXACT console process creation pattern from .NET 10 source (check how `dotnet` or `pwsh` uses pseudoconsole internally).

4. The `CreatePipe` handles may need different `SECURITY_ATTRIBUTES` for inheritance in .NET 10 — `CreatePipe` second parameter is `nint.Zero` (NULL), which uses default security. May need `SECURITY_ATTRIBUTES{bInheritHandle=true}`.

### Files modified

| File | Change |
|---|---|
| `prototypes/TerminalProof/.../NativeMethods.cs` | Struct rename + IsProcessInJobRaw overload |
| `prototypes/TerminalProof/.../JobObject.cs` | Updated struct references |
| `prototypes/TerminalProof/.../WindowsProcessEnvironment.cs` | Rewritten to use raw IntPtr |
| `prototypes/TerminalProof/.../FixtureProgram.cs` | Console resize try-catch |
| `prototypes/TerminalProof/.../ProofEvidenceWriter.cs` | Dispose stream before hashing |
| `prototypes/TerminalProof/.../ConPtyTerminalSession.cs` | Defer pseudoconsole handle disposal, broad pump exception catch |

### Test results

- 13/13 unit tests (command line, manifest, options): PASS
- All 11 scenarios + warmup: FAIL (output capture broken)
- Owner crash probe: FAIL (timeout)
- Handle growth within tolerance (24)
