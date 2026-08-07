param(
    [ValidateSet('Cli','Daemon','Desktop')][string]$Component = 'Cli',
    [Parameter(ValueFromRemainingArguments)][string[]]$ForwardedArguments
)
. (Join-Path $PSScriptRoot 'common.ps1')
$root = Get-SquareRepositoryRoot
Push-Location $root
try {
    Assert-SquareDotNetSdk
    $project = switch ($Component) {
        'Cli' { 'src/Square.Cli/Square.Cli.csproj' }
        'Daemon' { 'src/Square.Daemon/Square.Daemon.csproj' }
        'Desktop' { 'src/Square.Desktop/Square.Desktop.csproj' }
    }
    & dotnet run --project $project -- @ForwardedArguments
    exit $LASTEXITCODE
} finally { Pop-Location }
