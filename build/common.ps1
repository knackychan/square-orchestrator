Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-SquareRepositoryRoot {
    return (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
}

function Assert-SquareTool {
    param([Parameter(Mandatory)][string]$Name)
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required tool '$Name' was not found on PATH."
    }
}

function Assert-SquareNodeVersion {
    Assert-SquareTool 'node'
    $root = Get-SquareRepositoryRoot
    $expected = (Get-Content (Join-Path $root '.nvmrc') -Raw).Trim()
    $actual = (& node --version).Trim().TrimStart('v')
    if ($actual -ne $expected) {
        throw "Square requires Node.js $expected; current version is $actual."
    }
}

function Assert-SquareDotNetSdk {
    Assert-SquareTool 'dotnet'
    $root = Get-SquareRepositoryRoot
    $expected = (Get-Content (Join-Path $root 'global.json') -Raw | ConvertFrom-Json).sdk.version
    $actual = (& dotnet --version).Trim()
    if ($actual -ne $expected) {
        throw "Square requires .NET SDK $expected; current SDK is $actual. global.json disables roll-forward."
    }
}

function Invoke-SquarePnpm {
    param([Parameter(ValueFromRemainingArguments)][string[]]$Arguments)
    Assert-SquareNodeVersion
    Assert-SquareTool 'corepack'
    & corepack pnpm @Arguments
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
