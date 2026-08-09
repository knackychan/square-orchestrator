param([switch]$Verify)
. (Join-Path $PSScriptRoot 'common.ps1')
$root = Get-SquareRepositoryRoot
Push-Location $root
try {
    Assert-SquareDotNetSdk
    Assert-SquareNodeVersion
    $arguments = @('format','build/SquareOrchestrator.Core.slnx','--no-restore')
    if ($Verify) { $arguments += '--verify-no-changes' }
    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    & node build/ui-lint.mjs
    exit $LASTEXITCODE
} finally { Pop-Location }
