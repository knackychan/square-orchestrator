# SP00 first Windows run — hotfix record

- Date: 2026-08-07
- Baseline archive: `square-orchestrator-sp00-t05.zip`
- Baseline SHA-256: `f164c8daaea07e05e6cfdc2d297eb703f10cfcaec9400d1adbf145462b28bea3`
- Status: source hotfix applied; Windows rerun required
- Gate effect: G0 remains blocked

## Reported Windows environment

| Component | Reported value | Proof expectation |
|---|---|---|
| .NET SDK | `10.0.302` | `10.0.302` |
| Node.js | `v26.2.0` | `v24.19.0` for acceptance evidence |
| pnpm | `11.20.0` | `11.20.0` |
| VS Code | `1.132.0`, x64 | available x64 command |
| PowerShell host | Windows PowerShell behavior observed | runner must support Windows PowerShell 5.1 and PowerShell 7 |

## Failures reproduced from the report

### SP00-T02

`dotnet build TerminalProof.slnx --runtime win-x64` failed with `NETSDK1134` because a RuntimeIdentifier
cannot be applied to a solution build. The individual projects already target x64. The runner now builds
the solution without a solution-level RID and resolves the normal framework output paths.

### SP00-T03

The first C# compile stopped at `CS0509`: `FrameSizeException` attempted to inherit from the sealed
`.NET 10` `InvalidDataException` type. Two additional private protocol exceptions had the same problem
and would have failed next. All three transport exceptions now inherit from `IOException`; the oversized
frame test asserts the specific `FrameSizeException` type.

### SP00-T04

The runner used the PowerShell 7 automatic variable `$IsWindows` under strict mode. Windows PowerShell
5.1 does not define that variable, so execution stopped before the intended toolchain check. The runner
now uses `$env:OS`, avoids PowerShell-7-only `utf8NoBOM`, records the actual PowerShell/tool versions,
quotes paths containing spaces for native host launches, and handles absent environment variables safely.

## Additional guard

`prototypes/check-proof-toolchain.ps1` now reports all declared acceptance prerequisites in one table.
It is intentionally fail-closed. In the reported environment it should identify only Node.js as the known
toolchain mismatch before the proof rerun.

## Required rerun

From the repository root, in a normal non-administrator PowerShell:

```powershell
./prototypes/check-proof-toolchain.ps1
./prototypes/TerminalProof/run-proof.ps1
./prototypes/PipeProof/run-proof.ps1
./prototypes/SharedUiProof/run-proof.ps1
```

The full SP00-T04 acceptance runner still requires the repository-pinned Node.js `v24.19.0`. Node.js
`v26.2.0` is not silently accepted as equivalent because the proof records exact toolchain identity.

## Validation available in the hotfix creation environment

- TerminalProof source-contract validator: passed.
- PipeProof source-contract validator: passed after manifest regeneration.
- SharedUiProof source-contract validator: passed after manifest regeneration.
- Node framing/protocol and shared-UI deterministic tests: passed.
- Repository architecture and G0 fail-closed validators: passed.
- C# compilation, PowerShell execution, and Windows runtime behavior: not available in the creation host;
  the Windows rerun is the authoritative next check.
