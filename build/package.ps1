param([ValidateSet('Debug','Release')][string]$Configuration = 'Release')
. (Join-Path $PSScriptRoot 'common.ps1')
$root = Get-SquareRepositoryRoot
Push-Location $root
try {
    & (Join-Path $PSScriptRoot 'build.ps1') -Configuration $Configuration -IncludeWindows
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    throw 'Release packaging is intentionally gated until SP13. This command currently validates the build and stops.'
} finally { Pop-Location }
