[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$results = New-Object System.Collections.Generic.List[object]

function Add-Result {
    param(
        [Parameter(Mandatory = $true)][string] $Check,
        [Parameter(Mandatory = $true)][string] $Expected,
        [Parameter(Mandatory = $true)][string] $Actual,
        [Parameter(Mandatory = $true)][bool] $Passed
    )

    $results.Add([pscustomobject]@{
        Check = $Check
        Expected = $Expected
        Actual = $Actual
        Status = if ($Passed) { "PASS" } else { "FAIL" }
    })
}

function Get-NativeVersion {
    param(
        [Parameter(Mandatory = $true)][string] $CommandName,
        [Parameter(Mandatory = $true)][string[]] $Arguments
    )

    $command = Get-Command $CommandName -ErrorAction SilentlyContinue
    if ($null -eq $command) { return $null }
    $output = @(& $command.Source @Arguments 2>&1)
    if ($LASTEXITCODE -ne 0) { return "command failed: exit $LASTEXITCODE" }
    return (($output | ForEach-Object { ([string] $_).Trim() }) -join " | ").Trim()
}

$isWindows = $env:OS -eq "Windows_NT"
Add-Result "Operating system" "Windows" ([Environment]::OSVersion.VersionString) $isWindows

$isX64 = [Environment]::Is64BitOperatingSystem -and [Environment]::Is64BitProcess
Add-Result "PowerShell architecture" "x64 process on x64 Windows" `
    ("OS64={0}; Process64={1}" -f [Environment]::Is64BitOperatingSystem, [Environment]::Is64BitProcess) $isX64

$elevated = $false
if ($isWindows) {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object -TypeName Security.Principal.WindowsPrincipal -ArgumentList $identity
    $elevated = $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}
$elevationText = if ($elevated) { "administrator" } else { "normal user" }
Add-Result "Elevation" "normal non-administrator user" $elevationText (-not $elevated)

$psVersion = $PSVersionTable.PSVersion.ToString()
Add-Result "PowerShell" ">= 5.1" $psVersion ($PSVersionTable.PSVersion -ge [Version]"5.1")

$dotnet = Get-NativeVersion "dotnet" @("--version")
$dotnetText = if ($null -eq $dotnet) { "not found" } else { $dotnet }
Add-Result ".NET SDK" "10.0.302" $dotnetText ($dotnet -eq "10.0.302")

$node = Get-NativeVersion "node" @("--version")
$nodeText = if ($null -eq $node) { "not found" } else { $node }
Add-Result "Node.js" "v24.19.0" $nodeText ($node -eq "v24.19.0")

$pnpm = Get-NativeVersion "corepack" @("pnpm", "--version")
$pnpmText = if ($null -eq $pnpm) { "not found" } else { $pnpm }
Add-Result "pnpm" "11.20.0" $pnpmText ($pnpm -eq "11.20.0")

$code = Get-NativeVersion "code.cmd" @("--version")
if ($null -eq $code) { $code = Get-NativeVersion "code" @("--version") }
$codeText = if ($null -eq $code) { "not found" } else { $code }
Add-Result "VS Code command" "available, x64" $codeText `
    ($null -ne $code -and $code -notlike "command failed:*" -and $code -match "x64")

$results | Format-Table -AutoSize
$failures = @($results | Where-Object { $_.Status -eq "FAIL" })
if ($failures.Count -gt 0) {
    Write-Error "$($failures.Count) acceptance prerequisite(s) failed. Correct them before running the full SP00 proof commands." -ErrorAction Continue
    exit 2
}

Write-Host "All declared SP00 Windows proof prerequisites passed."
exit 0
