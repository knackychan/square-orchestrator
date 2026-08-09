[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [ValidateRange(1, 10000)]
    [int]$Repeat = 100,
    [ValidateRange(0, 1000)]
    [int]$ScaleRepeat = 1,
    [string]$Scenario,
    [string]$EvidenceDirectory,
    [switch]$Quick,
    [switch]$AllowElevated,
    [switch]$SkipOwnerCrash,
    [switch]$FailFast
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
if ($env:OS -ne 'Windows_NT') {
    throw 'SP00-T02 requires Windows.'
}
if (-not [Environment]::Is64BitOperatingSystem -or -not [Environment]::Is64BitProcess) {
    throw 'SP00-T02 must run as an x64 process on x64 Windows.'
}

$solution = Join-Path $PSScriptRoot 'TerminalProof.slnx'
$targetFramework = 'net10.0-windows10.0.17763.0'
$fixture = Join-Path $PSScriptRoot "Square.TerminalProof.Fixture\bin\$Configuration\$targetFramework\Square.TerminalProof.Fixture.exe"
$owner = Join-Path $PSScriptRoot "Square.TerminalProof.CrashOwner\bin\$Configuration\$targetFramework\Square.TerminalProof.CrashOwner.exe"
$harness = Join-Path $PSScriptRoot "Square.TerminalProof.Harness\bin\$Configuration\$targetFramework\Square.TerminalProof.Harness.exe"
$tests = Join-Path $PSScriptRoot "Square.TerminalProof.Tests\bin\$Configuration\$targetFramework\Square.TerminalProof.Tests.exe"
$manifest = Join-Path $PSScriptRoot 'scenarios.json'

if ([string]::IsNullOrWhiteSpace($EvidenceDirectory)) {
    $stamp = [DateTimeOffset]::UtcNow.ToString('yyyyMMddTHHmmssZ')
    $EvidenceDirectory = Join-Path $repositoryRoot "artifacts\test-results\SP00-T02\$stamp"
}
$EvidenceDirectory = [IO.Path]::GetFullPath($EvidenceDirectory)
if (Test-Path $EvidenceDirectory) {
    if (Get-ChildItem -Force -LiteralPath $EvidenceDirectory | Select-Object -First 1) {
        throw "The SP00-T02 evidence directory must be empty: $EvidenceDirectory"
    }
} else {
    New-Item -ItemType Directory -Force -Path $EvidenceDirectory | Out-Null
}

# Keep streamed console logs beside, rather than inside, the evidence directory. The harness
# requires an empty destination and hashes all files it creates there before returning.
$logPath = "$EvidenceDirectory.harness.log"
if (Test-Path $logPath) {
    throw "The SP00-T02 harness log already exists: $logPath"
}
$testsLog = "$EvidenceDirectory.tests.log"
if (Test-Path $testsLog) {
    throw "The SP00-T02 tests log already exists: $testsLog"
}

# SP00-T02-FIX03 separation: diagnostics and canonical acceptance run in separate harness
# processes and separate evidence directories. The canonical process starts only after every
# diagnostic process has exited.
$runDiagnostics = -not $Quick
$isolationDir = "$EvidenceDirectory-isolation"
$ownerCrashDiagDir = "$EvidenceDirectory-owner-crash"
$handleGrowthDiagDir = "$EvidenceDirectory-handle-growth"
$orchestrationPath = "$EvidenceDirectory.orchestration.json"

function New-HarnessBaseArguments {
    @(
        '--manifest', $manifest,
        '--fixture', $fixture,
        '--crash-owner', $owner,
        '--working-directory', $PSScriptRoot
    )
}

function Invoke-CanonicalHarness {
    param([string[]]$Arguments, [string]$LogPath)
    & $harness @Arguments *>&1 | Tee-Object -FilePath $LogPath | Out-Null
    return $LASTEXITCODE
}

function Read-DiagnosticSummary {
    param([string]$Directory, [string]$Name, [int]$ExitCode)
    $processId = 0
    $status = if ($ExitCode -eq 0) { 'DIAGNOSTIC_PASS' } else { 'FAIL' }
    $environmentPath = Join-Path $Directory 'environment.json'
    $summaryPath = Join-Path $Directory 'summary.json'
    try {
        if (Test-Path $environmentPath) {
            $environment = Get-Content $environmentPath -Raw | ConvertFrom-Json
            $processId = [int]$environment.process_id
        }
        if (Test-Path $summaryPath) {
            $summary = Get-Content $summaryPath -Raw | ConvertFrom-Json
            $status = $summary.status
        }
    } catch {
        $status = 'FAIL'
    }
    return [ordered]@{
        name = $Name
        process_id = $processId
        exit_code = $ExitCode
        status = $status
        evidence_directory = $Directory
    }
}

Push-Location $repositoryRoot
try {
    & dotnet build $solution --configuration $Configuration
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    & $tests *>&1 | Tee-Object -FilePath $testsLog
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    $diagnostics = @()
    if ($runDiagnostics) {
        Write-Host "Diagnostic process: standard-handle isolation regression"
        $diagArgs = New-HarnessBaseArguments
        $diagArgs += '--evidence-dir', $isolationDir
        $diagArgs += '--diag-isolation'
        $isolationLog = "$isolationDir.harness.log"
        $isolationExit = Invoke-CanonicalHarness -Arguments $diagArgs -LogPath $isolationLog
        $diagnostics += Read-DiagnosticSummary -Directory $isolationDir -Name 'isolation' -ExitCode $isolationExit

        Write-Host "Diagnostic process: owner-crash cold/post-stress probes and retention reproducer"
        $diagArgs = New-HarnessBaseArguments
        $diagArgs += '--evidence-dir', $ownerCrashDiagDir
        $diagArgs += '--diag-owner-crash'
        $ownerCrashLog = "$ownerCrashDiagDir.harness.log"
        $ownerCrashExit = Invoke-CanonicalHarness -Arguments $diagArgs -LogPath $ownerCrashLog
        $diagnostics += Read-DiagnosticSummary -Directory $ownerCrashDiagDir -Name 'owner-crash' -ExitCode $ownerCrashExit

        Write-Host "Diagnostic process: handle-growth classifier and eight-session stress rounds"
        $diagArgs = New-HarnessBaseArguments
        $diagArgs += '--evidence-dir', $handleGrowthDiagDir
        $diagArgs += '--diag-handle-growth'
        $handleGrowthLog = "$handleGrowthDiagDir.harness.log"
        $handleGrowthExit = Invoke-CanonicalHarness -Arguments $diagArgs -LogPath $handleGrowthLog
        $diagnostics += Read-DiagnosticSummary -Directory $handleGrowthDiagDir -Name 'handle-growth' -ExitCode $handleGrowthExit

        # Machine-readable orchestration record consumed by the canonical process.
        $orchestration = [ordered]@{ diagnostic_processes = @($diagnostics) }
        $orchestration | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $orchestrationPath -Encoding utf8

        # Outer standard-handle sweep: the isolated markers must never appear in any parent
        # console log (only the fixture's ConPTY-captured stream may contain them).
        $markerPattern = 'CONPTY-(STDOUT|STDERR)-MARKER:'
        $logFiles = @($testsLog, $isolationLog, $ownerCrashLog, $handleGrowthLog)
        foreach ($logFile in $logFiles) {
            if (-not (Test-Path $logFile)) { continue }
            $content = Get-Content -LiteralPath $logFile -Raw
            if ($content -match $markerPattern) {
                Write-Host "STANDARD-HANDLE ESCAPE DETECTED in $logFile"
                exit 1
            }
        }
    }

    $harnessArguments = New-HarnessBaseArguments
    $harnessArguments += '--evidence-dir', $EvidenceDirectory
    if ($Quick) {
        $harnessArguments += '--quick'
    } else {
        $harnessArguments += @('--repeat', $Repeat.ToString([Globalization.CultureInfo]::InvariantCulture))
        $harnessArguments += @('--scale-repeat', $ScaleRepeat.ToString([Globalization.CultureInfo]::InvariantCulture))
    }
    if (-not [string]::IsNullOrWhiteSpace($Scenario)) { $harnessArguments += @('--scenario', $Scenario) }
    if ($AllowElevated) { $harnessArguments += '--allow-elevated' }
    if ($SkipOwnerCrash) { $harnessArguments += '--skip-owner-crash' }
    if ($FailFast) { $harnessArguments += '--fail-fast' }
    if ($runDiagnostics) { $harnessArguments += @('--diagnostics-report', $orchestrationPath) }

    $harnessExit = Invoke-CanonicalHarness -Arguments $harnessArguments -LogPath $logPath

    foreach ($diagnostic in $diagnostics) {
        Write-Host ("SP00-T02 diagnostic {0}: status={1}, pid={2}, exit={3}, evidence={4}" -f `
            $diagnostic.name, $diagnostic.status, $diagnostic.process_id, $diagnostic.exit_code, $diagnostic.evidence_directory)
    }
    Write-Host "SP00-T02 evidence: $EvidenceDirectory"
    Write-Host "SP00-T02 console log: $logPath"
    Write-Host "SP00-T02 orchestration: $orchestrationPath"
    exit $harnessExit
} finally {
    Pop-Location
}
