[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RepositoryPath,

    [string]$OutputDirectory,

    [switch]$IncludeE2E,

    [switch]$SkipPackage,

    [switch]$ContinueOnFailure,

    [string]$ExpectedBaselineTag = 'square-base-v0.12.1',

    [string]$ExpectedCommitPrefix = '1df40e9'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repo = [System.IO.Path]::GetFullPath($RepositoryPath)
if (-not (Test-Path (Join-Path $repo '.git'))) {
    throw "Not a Git repository: $repo"
}

if (-not $OutputDirectory) {
    $stamp = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
    $OutputDirectory = Join-Path $repo "docs/square/evidence/SA00-T02/$stamp"
}
$head = (& git -C $repo rev-parse HEAD 2>&1 | Out-String).Trim()
if ($LASTEXITCODE -ne 0) { throw "Unable to resolve HEAD: $head" }
$baselineCommit = (& git -C $repo rev-parse "$ExpectedBaselineTag^{commit}" 2>&1 | Out-String).Trim()
if ($LASTEXITCODE -ne 0) { throw "Unable to resolve $ExpectedBaselineTag: $baselineCommit" }
if (-not $baselineCommit.StartsWith($ExpectedCommitPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Unexpected baseline commit. Expected prefix $ExpectedCommitPrefix, got $baselineCommit."
}
$initialStatus = @(& git -C $repo status --porcelain=v1)
if ($LASTEXITCODE -ne 0) { throw 'Unable to read repository status.' }
if ($initialStatus.Count -gt 0) {
    throw "Baseline must start from a clean working tree. Found $($initialStatus.Count) changed/untracked path(s)."
}

$out = [System.IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $out -Force | Out-Null

$steps = [System.Collections.Generic.List[object]]::new()

function Add-CommandResult {
    param([string]$Name,[string]$WorkingDirectory,[string]$Executable,[string[]]$Arguments,[bool]$Required = $true)

    $safeName = ($Name -replace '[^A-Za-z0-9_.-]', '_')
    $logPath = Join-Path $out "$safeName.log"
    $started = [DateTime]::UtcNow
    $exitCode = -1
    $exceptionText = $null

    try {
        Push-Location $WorkingDirectory
        try {
            & $Executable @Arguments 2>&1 | Tee-Object -FilePath $logPath
            $exitCode = $LASTEXITCODE
        } finally {
            Pop-Location
        }
    } catch {
        $exceptionText = $_.Exception.ToString()
        $exceptionText | Set-Content -LiteralPath $logPath -Encoding utf8
        $exitCode = -1
    }

    $finished = [DateTime]::UtcNow
    $record = [ordered]@{
        name = $Name
        required = $Required
        cwd = $WorkingDirectory
        executable = $Executable
        arguments = $Arguments
        started_utc = $started.ToString('o')
        finished_utc = $finished.ToString('o')
        duration_ms = [int]($finished - $started).TotalMilliseconds
        exit_code = $exitCode
        passed = ($exitCode -eq 0)
        log = (Split-Path -Leaf $logPath)
        exception = $exceptionText
    }
    $steps.Add([pscustomobject]$record)

    if ($Required -and $exitCode -ne 0 -and -not $ContinueOnFailure) {
        throw "Required baseline step failed: $Name. See $logPath"
    }
}

function Capture-Version([string]$Name,[string]$Executable,[string[]]$Arguments) {
    try {
        $value = (& $Executable @Arguments 2>&1 | Out-String).Trim()
        return [ordered]@{ name=$Name; available=$true; value=$value; exit_code=$LASTEXITCODE }
    } catch {
        return [ordered]@{ name=$Name; available=$false; value=$_.Exception.Message; exit_code=-1 }
    }
}

$environment = [ordered]@{
    schema_version = 'square.baseline-environment/v1'
    captured_utc = [DateTime]::UtcNow.ToString('o')
    repository = $repo
    os = [System.Runtime.InteropServices.RuntimeInformation]::OSDescription
    os_architecture = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString()
    process_architecture = [System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture.ToString()
    powershell = $PSVersionTable.PSVersion.ToString()
    git = Capture-Version 'git' 'git' @('--version')
    go = Capture-Version 'go' 'go' @('version')
    node = Capture-Version 'node' 'node' @('--version')
    npm = Capture-Version 'npm' 'npm' @('--version')
    commit = $head
    baseline_tag = $ExpectedBaselineTag
    baseline_commit = $baselineCommit
    branch = (& git -C $repo branch --show-current).Trim()
    status_porcelain = @(& git -C $repo status --porcelain=v1)
    remotes = @(& git -C $repo remote -v)
}
$environment | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'environment.json') -Encoding utf8

$identityFiles = @(
    'package.json',
    'package-lock.json',
    'frontend/package.json',
    'frontend/package-lock.json',
    'backend/go.mod',
    'backend/go.sum'
)
$repositoryIdentity = foreach ($relative in $identityFiles) {
    $path = Join-Path $repo $relative
    if (Test-Path $path -PathType Leaf) {
        [ordered]@{ path=$relative; sha256=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() }
    }
}
[ordered]@{
    schema_version='square.baseline-repository/v1'
    head=$head
    baseline_tag=$ExpectedBaselineTag
    baseline_commit=$baselineCommit
    identity_files=@($repositoryIdentity)
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'repository.json') -Encoding utf8

Add-CommandResult 'root-npm-ci' $repo 'npm' @('ci')
Add-CommandResult 'backend-go-build' (Join-Path $repo 'backend') 'go' @('build','./...')
Add-CommandResult 'backend-go-test' (Join-Path $repo 'backend') 'go' @('test','./...')
Add-CommandResult 'backend-go-test-race' (Join-Path $repo 'backend') 'go' @('test','-race','./...') $false
Add-CommandResult 'root-lint' $repo 'npm' @('run','lint')
Add-CommandResult 'frontend-npm-ci' (Join-Path $repo 'frontend') 'npm' @('ci')
Add-CommandResult 'frontend-typecheck' (Join-Path $repo 'frontend') 'npm' @('run','typecheck')
Add-CommandResult 'frontend-unit' (Join-Path $repo 'frontend') 'npm' @('run','test')

if ($IncludeE2E) {
    Add-CommandResult 'frontend-e2e' (Join-Path $repo 'frontend') 'npm' @('run','test:e2e')
}
if (-not $SkipPackage) {
    Add-CommandResult 'frontend-package' (Join-Path $repo 'frontend') 'npm' @('run','package')
}

$requiredFailures = @($steps | Where-Object { $_.required -and -not $_.passed })
$optionalFailures = @($steps | Where-Object { -not $_.required -and -not $_.passed })
$summary = [ordered]@{
    schema_version = 'square.baseline-summary/v1'
    status = if ($requiredFailures.Count -eq 0) { 'PASS' } else { 'FAIL' }
    required_failures = @($requiredFailures | ForEach-Object { $_.name })
    optional_failures = @($optionalFailures | ForEach-Object { $_.name })
    steps = $steps
    final_status_porcelain = @(& git -C $repo status --porcelain=v1)
}
$summary | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $out 'summary.json') -Encoding utf8

Get-ChildItem -LiteralPath $out -File | Where-Object { $_.Name -ne 'manifest.sha256' } | Sort-Object Name | ForEach-Object {
    $hash = Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256
    "$($hash.Hash.ToLowerInvariant())  $($_.Name)"
} | Set-Content -LiteralPath (Join-Path $out 'manifest.sha256') -Encoding ascii

Write-Host "Baseline evidence: $out"
Write-Host "Status: $($summary.status)"
if ($requiredFailures.Count -gt 0) { exit 1 }
