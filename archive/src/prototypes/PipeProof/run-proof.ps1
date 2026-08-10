[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [string]$EvidenceDirectory,
    [switch]$Quick
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
if ($env:OS -ne 'Windows_NT') {
    throw 'SP00-T03 requires Windows.'
}
if (-not [Environment]::Is64BitOperatingSystem -or -not [Environment]::Is64BitProcess) {
    throw 'SP00-T03 must run as an x64 process on x64 Windows.'
}

$solution = Join-Path $PSScriptRoot 'PipeProof.slnx'
$targetFramework = 'net10.0-windows10.0.17763.0'
$serverProject = Join-Path $PSScriptRoot 'Square.PipeProof.Server\Square.PipeProof.Server.csproj'
$clientProject = Join-Path $PSScriptRoot 'Square.PipeProof.DotNetClient\Square.PipeProof.DotNetClient.csproj'
$harnessProject = Join-Path $PSScriptRoot 'Square.PipeProof.Harness\Square.PipeProof.Harness.csproj'
$testsProject = Join-Path $PSScriptRoot 'Square.PipeProof.Tests\Square.PipeProof.Tests.csproj'
$server = Join-Path $PSScriptRoot "Square.PipeProof.Server\bin\$Configuration\$targetFramework\Square.PipeProof.Server.dll"
$client = Join-Path $PSScriptRoot "Square.PipeProof.DotNetClient\bin\$Configuration\$targetFramework\Square.PipeProof.DotNetClient.dll"
$harness = Join-Path $PSScriptRoot "Square.PipeProof.Harness\bin\$Configuration\$targetFramework\Square.PipeProof.Harness.dll"
$nodeFixture = Join-Path $PSScriptRoot 'node-client\fixture.mjs'
$dispatch = Join-Path $PSScriptRoot 'dispatch.packet.json'
$manifest = Join-Path $PSScriptRoot 'scenario-manifest.json'

if ([string]::IsNullOrWhiteSpace($EvidenceDirectory)) {
    $stamp = [DateTimeOffset]::UtcNow.ToString('yyyyMMddTHHmmssZ')
    $EvidenceDirectory = Join-Path $repositoryRoot "artifacts\test-results\SP00-T03\$stamp"
}
$EvidenceDirectory = [IO.Path]::GetFullPath($EvidenceDirectory)
if (Test-Path $EvidenceDirectory) {
    if (Get-ChildItem -Force -LiteralPath $EvidenceDirectory | Select-Object -First 1) {
        throw "The SP00-T03 evidence directory must be empty: $EvidenceDirectory"
    }
} else {
    New-Item -ItemType Directory -Force -Path $EvidenceDirectory | Out-Null
}

Push-Location $repositoryRoot
try {
    & node (Join-Path $PSScriptRoot 'validate-source.mjs')
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    $nodeTests = Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'node-client\test') -Filter '*.test.mjs' |
        Sort-Object Name |
        ForEach-Object { $_.FullName }
    if ($nodeTests.Count -eq 0) { throw 'No PipeProof Node tests were found.' }
    & node --test @nodeTests
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    & dotnet build $solution --configuration $Configuration
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    & dotnet run --no-build --configuration $Configuration --project $testsProject
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    foreach ($artifact in @($server, $client, $harness)) {
        if (-not (Test-Path -LiteralPath $artifact)) {
            throw "Expected build artifact was not produced: $artifact"
        }
    }

    $arguments = @(
        $harness,
        '--server', $server,
        '--dotnet-client', $client,
        '--node', (Get-Command node).Source,
        '--node-fixture', $nodeFixture,
        '--output', $EvidenceDirectory,
        '--source-root', $repositoryRoot,
        '--dispatch', $dispatch,
        '--manifest', $manifest
    )
    if ($Quick) { $arguments += '--quick' }

    & dotnet @arguments
    $exitCode = $LASTEXITCODE
    Write-Host "SP00-T03 evidence: $EvidenceDirectory"
    exit $exitCode
} finally {
    Pop-Location
}
