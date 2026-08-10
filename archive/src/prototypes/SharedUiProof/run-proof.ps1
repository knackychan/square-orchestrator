[CmdletBinding()]
param(
    [string] $EvidenceRoot = "",
    [ValidateSet("Debug", "Release")]
    [string] $Configuration = "Release"
)

# The proof must also run from the Windows PowerShell 5.1 console installed by default on Windows.
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$ProofRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepositoryRoot = (Resolve-Path (Join-Path $ProofRoot "..\..")).Path

if ($env:OS -ne "Windows_NT") { throw "SP00-T04 acceptance requires Windows." }
if (-not [Environment]::Is64BitOperatingSystem -or -not [Environment]::Is64BitProcess) {
    throw "SP00-T04 acceptance currently requires an x64 PowerShell process on Windows x64."
}

$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = New-Object -TypeName Security.Principal.WindowsPrincipal -ArgumentList $identity
if ($principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw "SP00-T04 acceptance must run as the normal non-elevated interactive user."
}

function Assert-ExactVersion {
    param(
        [Parameter(Mandatory = $true)][string] $Name,
        [Parameter(Mandatory = $true)][string] $Actual,
        [Parameter(Mandatory = $true)][string] $Expected
    )

    $normalized = $Actual.Trim()
    if ($normalized -ne $Expected) {
        throw "$Name must be $Expected for acceptance evidence; found $normalized. Activate the repository-pinned toolchain and run the proof again."
    }
    return $normalized
}

function Write-Utf8NoBomFile {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][AllowEmptyString()][string] $Content
    )

    $encoding = New-Object -TypeName System.Text.UTF8Encoding -ArgumentList $false
    [IO.File]::WriteAllText($Path, $Content, $encoding)
}

function Quote-NativeArgument {
    param([Parameter(Mandatory = $true)][AllowEmptyString()][string] $Value)

    if ($Value.Length -gt 0 -and $Value -notmatch '[\s"]') { return $Value }
    # Proof-generated paths never end in a slash or contain a literal quote. Escaping the latter keeps
    # this helper safe if that constraint changes while preserving paths containing spaces.
    return '"' + $Value.Replace('"', '\"') + '"'
}

function Join-NativeArguments {
    param([Parameter(Mandatory = $true)][string[]] $Values)
    return (($Values | ForEach-Object { Quote-NativeArgument $_ }) -join ' ')
}

function Restore-EnvironmentVariable {
    param(
        [Parameter(Mandatory = $true)][string] $Name,
        [AllowNull()][string] $PreviousValue
    )

    if ($null -eq $PreviousValue) {
        Remove-Item -Path ("Env:" + $Name) -ErrorAction SilentlyContinue
    }
    else {
        Set-Item -Path ("Env:" + $Name) -Value $PreviousValue
    }
}

$NodeCommand = Get-Command node -ErrorAction Stop
$CorepackCommand = Get-Command corepack -ErrorAction Stop
$DotNetCommand = Get-Command dotnet -ErrorAction Stop
$CodeCommand = Get-Command code.cmd -ErrorAction SilentlyContinue
if ($null -eq $CodeCommand) { $CodeCommand = Get-Command code -ErrorAction SilentlyContinue }
if ($null -eq $CodeCommand) { throw "VS Code's 'code' command is required for the VS Code host proof." }

$NodeVersion = Assert-ExactVersion "Node.js" (& $NodeCommand.Source --version) "v24.19.0"
if ($LASTEXITCODE -ne 0) { throw "node --version failed with exit code $LASTEXITCODE." }
$PnpmVersion = Assert-ExactVersion "pnpm" (& $CorepackCommand.Source pnpm --version) "11.20.0"
if ($LASTEXITCODE -ne 0) { throw "corepack pnpm --version failed with exit code $LASTEXITCODE." }
$DotNetVersion = Assert-ExactVersion ".NET SDK" (& $DotNetCommand.Source --version) "10.0.302"
if ($LASTEXITCODE -ne 0) { throw "dotnet --version failed with exit code $LASTEXITCODE." }

if ([string]::IsNullOrWhiteSpace($EvidenceRoot)) {
    $stamp = [DateTimeOffset]::UtcNow.ToString("yyyyMMddTHHmmssZ")
    $EvidenceRoot = Join-Path $ProofRoot "evidence\run-$stamp"
}
$EvidenceRoot = [IO.Path]::GetFullPath($EvidenceRoot)
if (Test-Path -LiteralPath $EvidenceRoot) {
    if (@(Get-ChildItem -Force -LiteralPath $EvidenceRoot).Count -gt 0) {
        throw "The SP00-T04 evidence directory must be empty: $EvidenceRoot"
    }
}
else {
    New-Item -ItemType Directory -Force -Path $EvidenceRoot | Out-Null
}

Push-Location $RepositoryRoot
try {
    & $CorepackCommand.Source pnpm install --frozen-lockfile
    if ($LASTEXITCODE -ne 0) { throw "pnpm install failed with exit code $LASTEXITCODE." }

    $TypeScriptVersion = Assert-ExactVersion "TypeScript" (& $CorepackCommand.Source pnpm exec tsc --version) "Version 6.0.3"
    if ($LASTEXITCODE -ne 0) { throw "TypeScript version probe failed with exit code $LASTEXITCODE." }

    & $NodeCommand.Source (Join-Path $RepositoryRoot "build\verify-repository.mjs")
    if ($LASTEXITCODE -ne 0) { throw "Repository architecture verification failed." }

    & $NodeCommand.Source (Join-Path $ProofRoot "scripts\build.mjs")
    if ($LASTEXITCODE -ne 0) { throw "Shared UI asset build failed." }

    & $NodeCommand.Source (Join-Path $ProofRoot "validate-source.mjs")
    if ($LASTEXITCODE -ne 0) { throw "Shared UI source validation failed." }

    $TestFiles = @(Get-ChildItem -Path (Join-Path $ProofRoot "test") -Filter "*.test.mjs" -File |
        Sort-Object Name | ForEach-Object { $_.FullName })
    if ($TestFiles.Count -eq 0) { throw "Shared UI deterministic test files were not found." }
    & $NodeCommand.Source --test @TestFiles
    if ($LASTEXITCODE -ne 0) { throw "Shared UI deterministic tests failed." }

    & $DotNetCommand.Source build (Join-Path $ProofRoot "SharedUiProof.slnx") --configuration $Configuration
    if ($LASTEXITCODE -ne 0) { throw "WebView2 proof host build failed." }
}
finally {
    Pop-Location
}

$WebViewEvidence = Join-Path $EvidenceRoot "webview2.json"
$VsCodeEvidence = Join-Path $EvidenceRoot "vscode.json"
$ComparisonEvidence = Join-Path $EvidenceRoot "comparison.json"
$ScratchRoot = Join-Path ([IO.Path]::GetTempPath()) ("SquareOrchestrator-SharedUiProof-" + [Guid]::NewGuid().ToString("N"))
$WebViewUserData = Join-Path $ScratchRoot "webview2-user-data"
$VsCodeUserData = Join-Path $ScratchRoot "vscode-user-data"
$VsCodeExtensions = Join-Path $ScratchRoot "vscode-extensions"
New-Item -ItemType Directory -Force -Path @($WebViewUserData, $VsCodeUserData, $VsCodeExtensions) | Out-Null

$SourceManifestHash = (Get-FileHash -Algorithm SHA256 -Path (Join-Path $ProofRoot "source-manifest.sha256")).Hash.ToLowerInvariant()
$DispatchHash = (Get-FileHash -Algorithm SHA256 -Path (Join-Path $ProofRoot "dispatch.packet.json")).Hash.ToLowerInvariant()
$ScenarioHash = (Get-FileHash -Algorithm SHA256 -Path (Join-Path $ProofRoot "scenario-manifest.json")).Hash.ToLowerInvariant()
$FixtureHash = (Get-Content -Raw -Path (Join-Path $ProofRoot "fixtures\canonical-state.sha256")).Trim()
$BenchmarkHash = (Get-FileHash -Algorithm SHA256 -Path (Join-Path $ProofRoot "fixtures\benchmark-manifest.json")).Hash.ToLowerInvariant()
$CodeVersion = @(& $CodeCommand.Source --version 2>&1 | ForEach-Object { ([string] $_).Trim() })
if ($LASTEXITCODE -ne 0) { throw "VS Code version probe failed with exit code $LASTEXITCODE." }

$environmentJson = [ordered]@{
    schemaVersion = "1.0"
    taskId = "SP00-T04"
    createdAtUtc = [DateTimeOffset]::UtcNow.ToString("O")
    powershellVersion = $PSVersionTable.PSVersion.ToString()
    osVersion = [Environment]::OSVersion.VersionString
    processArchitecture = [Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture.ToString()
    elevated = $false
    dotnetSdk = $DotNetVersion
    nodeVersion = $NodeVersion
    pnpmVersion = $PnpmVersion
    typescriptVersion = $TypeScriptVersion.Replace("Version ", "")
    vscodeVersionOutput = $CodeVersion
    sourceManifestSha256 = $SourceManifestHash
    dispatchPacketSha256 = $DispatchHash
    scenarioManifestSha256 = $ScenarioHash
    fixtureSha256 = $FixtureHash
    benchmarkFileSha256 = $BenchmarkHash
} | ConvertTo-Json -Depth 8
Write-Utf8NoBomFile -Path (Join-Path $EvidenceRoot "environment.json") -Content ($environmentJson + [Environment]::NewLine)

$WebViewExecutable = Get-ChildItem -Path (Join-Path $ProofRoot "webview2-host\Square.SharedUiProof.WebView2\bin\$Configuration") `
    -Filter "Square.SharedUiProof.WebView2.exe" -File -Recurse | Select-Object -First 1
if ($null -eq $WebViewExecutable) { throw "Built WebView2 proof executable was not found." }

$PreviousAutorun = $env:SQUARE_SHARED_UI_PROOF_AUTORUN
$PreviousAcceptance = $env:SQUARE_SHARED_UI_PROOF_ACCEPTANCE
$PreviousEvidence = $env:SQUARE_SHARED_UI_PROOF_EVIDENCE
try {
    $webArguments = Join-NativeArguments @(
        "--autorun",
        "--acceptance",
        "--evidence", $WebViewEvidence,
        "--user-data", $WebViewUserData
    )
    $webProcess = Start-Process -FilePath $WebViewExecutable.FullName -ArgumentList $webArguments -PassThru -Wait
    if ($webProcess.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $WebViewEvidence)) {
        throw "WebView2 host did not produce passing evidence. Exit code: $($webProcess.ExitCode)."
    }

    $env:SQUARE_SHARED_UI_PROOF_AUTORUN = "1"
    $env:SQUARE_SHARED_UI_PROOF_ACCEPTANCE = "1"
    $env:SQUARE_SHARED_UI_PROOF_EVIDENCE = $VsCodeEvidence
    $arguments = @(
        "--extensionDevelopmentPath=$ProofRoot",
        "--user-data-dir=$VsCodeUserData",
        "--extensions-dir=$VsCodeExtensions",
        "--disable-workspace-trust",
        "--skip-welcome",
        "--skip-release-notes",
        "--new-window",
        "--wait",
        $ProofRoot
    )
    $codeArgumentLine = Join-NativeArguments $arguments
    $codeProcess = Start-Process -FilePath $CodeCommand.Source -ArgumentList $codeArgumentLine -PassThru -Wait
    if ($codeProcess.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $VsCodeEvidence)) {
        throw "VS Code host did not produce evidence. Exit code: $($codeProcess.ExitCode)."
    }

    & $NodeCommand.Source (Join-Path $ProofRoot "scripts\compare-evidence.mjs") $WebViewEvidence $VsCodeEvidence $ComparisonEvidence
    if ($LASTEXITCODE -ne 0) { throw "Cross-host evidence comparison failed." }
}
finally {
    Restore-EnvironmentVariable -Name "SQUARE_SHARED_UI_PROOF_AUTORUN" -PreviousValue $PreviousAutorun
    Restore-EnvironmentVariable -Name "SQUARE_SHARED_UI_PROOF_ACCEPTANCE" -PreviousValue $PreviousAcceptance
    Restore-EnvironmentVariable -Name "SQUARE_SHARED_UI_PROOF_EVIDENCE" -PreviousValue $PreviousEvidence
    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue -Path $ScratchRoot
}

$manifest = @(Get-ChildItem -Path $EvidenceRoot -Filter "*.json" -File | Sort-Object Name | ForEach-Object {
    $hash = Get-FileHash -Algorithm SHA256 -Path $_.FullName
    "{0}  {1}" -f $hash.Hash.ToLowerInvariant(), $_.Name
})
Write-Utf8NoBomFile -Path (Join-Path $EvidenceRoot "evidence-manifest.sha256") `
    -Content (($manifest -join [Environment]::NewLine) + [Environment]::NewLine)
Write-Host "SP00-T04 architecture proof: PASS"
Write-Host "Evidence: $EvidenceRoot"
