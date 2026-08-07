param(
    [ValidateSet('Debug','Release')][string]$Configuration = 'Release',
    [switch]$SkipUi,
    [switch]$IncludeWindows
)
. (Join-Path $PSScriptRoot 'common.ps1')
$root = Get-SquareRepositoryRoot
Push-Location $root
try {
    Assert-SquareDotNetSdk
    Assert-SquareNodeVersion
    & node build/verify-repository.mjs
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    & dotnet restore 'build/SquareOrchestrator.Core.slnx'
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    & dotnet build 'build/SquareOrchestrator.Core.slnx' --no-restore --configuration $Configuration
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    if ($IncludeWindows) {
        $runningOnWindows = if (Test-Path variable:IsWindows) { [bool]$IsWindows } else { $env:OS -eq 'Windows_NT' }
        if (-not $runningOnWindows) {
            throw '-IncludeWindows must run on Windows.'
        }
        & dotnet restore 'SquareOrchestrator.slnx'
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
        & dotnet build 'SquareOrchestrator.slnx' --no-restore --configuration $Configuration
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    }

    if (-not $SkipUi) {
        Invoke-SquarePnpm install --frozen-lockfile
        Invoke-SquarePnpm run check
    }
} finally {
    Pop-Location
}
