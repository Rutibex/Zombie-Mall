param(
    [Parameter(Mandatory = $true)]
    [string]$PublishDir
)
$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if (-not [System.IO.Path]::IsPathRooted($PublishDir)) {
    $PublishDir = Join-Path $repoRoot $PublishDir
}
if (!(Test-Path -LiteralPath $PublishDir -PathType Container)) {
    throw "Publish directory does not exist: $PublishDir"
}
$PublishDir = (Resolve-Path -LiteralPath $PublishDir).Path
$exePath = Join-Path $PublishDir 'ZombieMall.exe'
if (!(Test-Path -LiteralPath $exePath)) { throw "Missing ZombieMall.exe at $exePath" }

$assetDir = Join-Path $PublishDir 'Assets'
$campaignDir = Join-Path $PublishDir 'Campaigns'
$styleDir = Join-Path $PublishDir 'StylePacks'
foreach ($folder in @($assetDir, $campaignDir, $styleDir)) {
    if (!(Test-Path -LiteralPath $folder -PathType Container)) { throw "Missing published folder: $folder" }
}
foreach ($asset in @('music_title.wav', 'music_explore.wav', 'music_combat.wav', 'environment33.json')) {
    if (!(Test-Path (Join-Path $assetDir $asset))) { throw "Missing required asset: $asset" }
}

$expectedCampaigns = @(Get-ChildItem -LiteralPath (Join-Path $repoRoot 'content/campaigns') -File -Filter '*.json' | Where-Object { !$_.Name.StartsWith('.') } | Select-Object -ExpandProperty Name)
if ($expectedCampaigns.Count -lt 30) { throw 'Missing source campaign definitions' }
foreach ($campaign in $expectedCampaigns) {
    if (!(Test-Path (Join-Path $campaignDir $campaign))) { throw "Missing campaign: $campaign" }
}

# Only ship runtime content, not debug symbols and generated documentation.
Get-ChildItem -LiteralPath $PublishDir -File | Where-Object { $_.Extension -in @('.pdb', '.xml') } | Remove-Item -Force
Get-ChildItem -LiteralPath $campaignDir -File | Where-Object { $_.Name.StartsWith('.') -or $_.Name -match '\.json\.[^.]+$' } | Remove-Item -Force

foreach ($name in @('LICENSE', 'NOTICE.md', 'CREDITS.md')) {
    Copy-Item -LiteralPath (Join-Path $repoRoot $name) -Destination (Join-Path $PublishDir $name) -Force
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'README-WINDOWS.md') -Destination (Join-Path $PublishDir 'README.md') -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'START-HERE.txt') -Destination (Join-Path $PublishDir 'START-HERE.txt') -Force
Copy-Item -LiteralPath (Join-Path $repoRoot 'licenses') -Destination (Join-Path $PublishDir 'licenses') -Recurse -Force
$docsDir = Join-Path $PublishDir 'docs'
New-Item -ItemType Directory -Force -Path $docsDir | Out-Null
foreach ($name in @('PLAYING.md', 'TROUBLESHOOTING.md')) {
    Copy-Item -LiteralPath (Join-Path $repoRoot "docs/$name") -Destination (Join-Path $docsDir $name) -Force
}
Write-Host "Prepared clean Zombie Mall distribution: $PublishDir"
Write-Host "Campaign definitions: $($expectedCampaigns.Count)"
