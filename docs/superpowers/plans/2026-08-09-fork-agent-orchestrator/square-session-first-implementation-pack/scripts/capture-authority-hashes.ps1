[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$AuthorityDirectory,

    [string]$OutputPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = [System.IO.Path]::GetFullPath($AuthorityDirectory)
if (-not (Test-Path $root -PathType Container)) {
    throw "Authority directory not found: $root"
}
if (-not $OutputPath) {
    $OutputPath = Join-Path $root 'AUTHORITY_MANIFEST.sha256'
}

function Get-RelativePathCompat {
    param(
        [Parameter(Mandatory = $true)]
        [string]$BasePath,

        [Parameter(Mandatory = $true)]
        [string]$TargetPath
    )

    $base = [System.IO.Path]::GetFullPath($BasePath)
    if (-not $base.EndsWith([System.IO.Path]::DirectorySeparatorChar)) {
        $base = $base + [System.IO.Path]::DirectorySeparatorChar
    }
    $target = [System.IO.Path]::GetFullPath($TargetPath)
    $baseUri = New-Object System.Uri($base)
    $targetUri = New-Object System.Uri($target)
    [System.Uri]::UnescapeDataString($baseUri.MakeRelativeUri($targetUri).ToString())
}

$files = Get-ChildItem -LiteralPath $root -File -Recurse |
    Where-Object { $_.FullName -ne [System.IO.Path]::GetFullPath($OutputPath) } |
    Sort-Object FullName

$lines = foreach ($file in $files) {
    $hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    $relative = (Get-RelativePathCompat -BasePath $root -TargetPath $file.FullName).Replace('\\','/')
    "$hash  $relative"
}
$lines | Set-Content -LiteralPath $OutputPath -Encoding ascii
Write-Host "Wrote $($files.Count) authority hashes to $OutputPath"
