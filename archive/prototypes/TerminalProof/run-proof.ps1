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

# Keep the streamed console log beside, rather than inside, the evidence directory. The harness
# requires an empty destination and hashes all files it creates there before returning.
$logPath = "$EvidenceDirectory.harness.log"
if (Test-Path $logPath) {
    throw "The SP00-T02 harness log already exists: $logPath"
}

Push-Location $repositoryRoot
try {
    & dotnet build $solution --configuration $Configuration
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    & $tests
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    $harnessArguments = @(
        '--manifest', $manifest,
        '--fixture', $fixture,
        '--crash-owner', $owner,
        '--working-directory', $PSScriptRoot,
        '--evidence-dir', $EvidenceDirectory
    )
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

    & $harness @harnessArguments *>&1 | Tee-Object -FilePath $logPath
    $exitCode = $LASTEXITCODE
    Write-Host "SP00-T02 evidence: $EvidenceDirectory"
    Write-Host "SP00-T02 console log: $logPath"
    exit $exitCode
} finally {
    Pop-Location
}
