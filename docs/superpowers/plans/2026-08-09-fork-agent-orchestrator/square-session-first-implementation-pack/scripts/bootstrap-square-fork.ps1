[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [Parameter(Mandatory = $true)]
    [string]$Destination,

    [string]$OriginUrl,

    [string]$AuthorityPackPath,

    [string]$CreatedBy = $env:USERNAME,

    [string]$UpstreamUrl = 'https://github.com/Untrivial-ai/agent-orchestrator.git',

    [string]$UpstreamTag = 'v0.12.1',

    [string]$ExpectedCommitPrefix = '1df40e9'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Require-Command([string]$Name) {
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command '$Name' was not found on PATH."
    }
}

function Invoke-Git {
    param([Parameter(Mandatory = $true)][string[]]$Arguments)
    & git @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "git $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

function Get-GitText {
    param([Parameter(Mandatory = $true)][string[]]$Arguments)
    $value = (& git @Arguments 2>&1 | Out-String).Trim()
    if ($LASTEXITCODE -ne 0) {
        throw "git $($Arguments -join ' ') failed: $value"
    }
    return $value
}

function Write-Utf8NoBom {
    param([Parameter(Mandatory = $true)][string]$Path,[Parameter(Mandatory = $true)][string]$Content)
    $utf8 = [System.Text.UTF8Encoding]::new($false)
    [System.IO.File]::WriteAllText($Path, $Content, $utf8)
}

Require-Command git

$destinationFull = [System.IO.Path]::GetFullPath($Destination)
if (Test-Path $destinationFull) {
    $existing = @(Get-ChildItem -LiteralPath $destinationFull -Force -ErrorAction Stop)
    if ($existing.Count -gt 0) {
        throw "Destination already exists and is not empty: $destinationFull"
    }
} else {
    $parent = Split-Path -Parent $destinationFull
    if ($parent -and -not (Test-Path $parent)) {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
    }
}

if (-not $PSCmdlet.ShouldProcess($destinationFull, "Clone and pin Agent Orchestrator $UpstreamTag")) {
    return
}

# Clone the normal repository rather than a source archive or shallow checkout so
# future upstream merges retain ancestry and remote branch visibility.
Invoke-Git -Arguments @('clone', $UpstreamUrl, $destinationFull)
Invoke-Git -Arguments @('-C', $destinationFull, 'fetch', 'origin', '--tags', '--prune')

$tagCommit = Get-GitText -Arguments @('-C', $destinationFull, 'rev-parse', "$UpstreamTag^{commit}")
if (-not $tagCommit.StartsWith($ExpectedCommitPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Unexpected upstream commit. Expected prefix $ExpectedCommitPrefix, got $tagCommit."
}

Invoke-Git -Arguments @('-C', $destinationFull, 'switch', '--detach', $tagCommit)
$head = Get-GitText -Arguments @('-C', $destinationFull, 'rev-parse', 'HEAD')
$exactTag = Get-GitText -Arguments @('-C', $destinationFull, 'describe', '--tags', '--exact-match', 'HEAD')
if (-not $head.Equals($tagCommit, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "The checked-out HEAD ($head) does not match $UpstreamTag ($tagCommit)."
}
if ($exactTag -ne $UpstreamTag) {
    throw "Expected exact tag $UpstreamTag at HEAD; found '$exactTag'."
}

Invoke-Git -Arguments @('-C', $destinationFull, 'remote', 'rename', 'origin', 'upstream')
Invoke-Git -Arguments @('-C', $destinationFull, 'switch', '-c', 'square/main')

$baselineTag = "square-base-$UpstreamTag"
$tagExists = Get-GitText -Arguments @('-C', $destinationFull, 'tag', '--list', $baselineTag)
if ($tagExists) {
    $existingTarget = Get-GitText -Arguments @('-C', $destinationFull, 'rev-parse', "$baselineTag^{commit}")
    if (-not $existingTarget.Equals($head, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Downstream baseline tag $baselineTag already exists at $existingTarget, expected $head."
    }
} else {
    Invoke-Git -Arguments @(
        '-C', $destinationFull,
        'tag', '-a', $baselineTag,
        '-m', "Square downstream baseline from AO $UpstreamTag ($head)",
        $head
    )
}

if ($OriginUrl) {
    $remoteNames = @(Get-GitText -Arguments @('-C', $destinationFull, 'remote') -split "`r?`n")
    if ($remoteNames -contains 'origin') {
        throw "Remote 'origin' already exists; no remote was changed."
    }
    Invoke-Git -Arguments @('-C', $destinationFull, 'remote', 'add', 'origin', $OriginUrl)
}

$docsRoot = Join-Path $destinationFull 'docs/square'
@('authority', 'adr', 'evidence', 'receipts', 'upstream', 'gates', 'templates', 'plans') | ForEach-Object {
    New-Item -ItemType Directory -Path (Join-Path $docsRoot $_) -Force | Out-Null
}

# The pack contains a curated starter overlay. Never recursively copy the entire
# implementation pack into the repository authority directory.
if ($AuthorityPackPath) {
    $pack = [System.IO.Path]::GetFullPath($AuthorityPackPath)
    $overlaySource = Join-Path $pack 'starter-overlay/docs/square'
    if (-not (Test-Path $overlaySource -PathType Container)) {
        throw "Curated starter overlay was not found: $overlaySource"
    }
    Get-ChildItem -LiteralPath $overlaySource -Force | ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination $docsRoot -Recurse -Force
    }
}

$tree = Get-GitText -Arguments @('-C', $destinationFull, 'rev-parse', 'HEAD^{tree}')
$parentsLine = Get-GitText -Arguments @('-C', $destinationFull, 'show', '-s', '--format=%P', 'HEAD')
$parents = if ([string]::IsNullOrWhiteSpace($parentsLine)) { @() } else { @($parentsLine -split '\s+') }
$show = Get-GitText -Arguments @('-C', $destinationFull, 'show', '-s', '--format=%H%n%T%n%P%n%an%n%ae%n%aI%n%cn%n%ce%n%cI%n%s', 'HEAD')
$upstreamFetch = Get-GitText -Arguments @('-C', $destinationFull, 'remote', 'get-url', 'upstream')
$upstreamPush = Get-GitText -Arguments @('-C', $destinationFull, 'remote', 'get-url', '--push', 'upstream')
$originFetch = $null
$originPush = $null
$remotes = @(Get-GitText -Arguments @('-C', $destinationFull, 'remote') -split "`r?`n")
if ($remotes -contains 'origin') {
    $originFetch = Get-GitText -Arguments @('-C', $destinationFull, 'remote', 'get-url', 'origin')
    $originPush = Get-GitText -Arguments @('-C', $destinationFull, 'remote', 'get-url', '--push', 'origin')
}

$baseline = [ordered]@{
    schema_version = 'square.upstream-baseline/v1'
    upstream_repository = $upstreamFetch
    upstream_push_url = $upstreamPush
    upstream_tag = $UpstreamTag
    upstream_commit = $head
    upstream_tree = $tree
    upstream_parent_commits = $parents
    upstream_commit_show = $show
    square_branch = 'square/main'
    square_baseline_tag = $baselineTag
    origin_repository = $originFetch
    origin_push_url = $originPush
    created_utc = [DateTime]::UtcNow.ToString('o')
    created_by = $CreatedBy
    pushed = $false
    working_tree_clean_before_downstream_files = $true
}
$baselinePath = Join-Path $docsRoot 'upstream/initial-baseline.json'
Write-Utf8NoBom -Path $baselinePath -Content (($baseline | ConvertTo-Json -Depth 10) + "`n")

$stamp = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
$evidence = Join-Path $docsRoot "evidence/SA00-T01/$stamp"
New-Item -ItemType Directory -Path $evidence -Force | Out-Null
Write-Utf8NoBom -Path (Join-Path $evidence 'git-head.txt') -Content "$head`n"
Write-Utf8NoBom -Path (Join-Path $evidence 'git-exact-tag.txt') -Content "$exactTag`n"
Write-Utf8NoBom -Path (Join-Path $evidence 'git-show.txt') -Content "$show`n"
Write-Utf8NoBom -Path (Join-Path $evidence 'git-remotes.txt') -Content ((Get-GitText -Arguments @('-C', $destinationFull, 'remote', '-v')) + "`n")
Write-Utf8NoBom -Path (Join-Path $evidence 'git-status.txt') -Content ((Get-GitText -Arguments @('-C', $destinationFull, 'status', '--porcelain=v1', '--branch')) + "`n")

$status = @(& git -C $destinationFull status --porcelain=v1)
$summary = [ordered]@{
    schema_version = 'square.SA00-T01-summary/v1'
    status = 'PASS'
    upstream_commit = $head
    upstream_tree = $tree
    upstream_tag = $exactTag
    square_branch = 'square/main'
    square_baseline_tag = $baselineTag
    origin_configured = [bool]$originFetch
    pushed = $false
    intended_uncommitted_paths = @($status)
}
Write-Utf8NoBom -Path (Join-Path $evidence 'summary.json') -Content (($summary | ConvertTo-Json -Depth 10) + "`n")

Get-ChildItem -LiteralPath $evidence -File | Sort-Object Name | ForEach-Object {
    $hash = Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256
    "$($hash.Hash.ToLowerInvariant())  $($_.Name)"
} | Set-Content -LiteralPath (Join-Path $evidence 'manifest.sha256') -Encoding ascii

Write-Host "Square fork created: $destinationFull"
Write-Host "Pinned AO commit: $head"
Write-Host "Pinned AO tree: $tree"
Write-Host "Baseline tag: $baselineTag"
Write-Host "Evidence: $evidence"
Write-Host "Uncommitted downstream files: $($status.Count)"
Write-Host 'No commit and no remote push were performed.'
Write-Host 'Next: inspect the diff, create the SA00-T01 receipt/commit, then dispatch SA00-T02.'
