param(
    [Parameter(Mandatory = $true)]
    [string]$Target,

    [Parameter(Mandatory = $true)]
    [string]$SourceName
)

$ErrorActionPreference = "Stop"
$targetFull = [System.IO.Path]::GetFullPath($Target)

$codexHome = if ($env:CODEX_HOME) { $env:CODEX_HOME } else { Join-Path $env:USERPROFILE ".codex" }
$generatedRoot = Join-Path $codexHome "generated_images"
$latest = Get-ChildItem -LiteralPath $generatedRoot -Recurse -File -Filter "*.png" |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if (-not $latest) {
    throw "No generated PNG found under $generatedRoot"
}

$sourceDir = Join-Path "tmp/imagegen/character_sources" ([System.IO.Path]::GetFileNameWithoutExtension($targetFull))
New-Item -ItemType Directory -Force -Path $sourceDir | Out-Null
$workspaceSource = Join-Path $sourceDir $SourceName
Copy-Item -LiteralPath $latest.FullName -Destination $workspaceSource -Force

$targetDir = Split-Path -Parent $targetFull
New-Item -ItemType Directory -Force -Path $targetDir | Out-Null

$helper = Join-Path $codexHome "skills/.system/imagegen/scripts/remove_chroma_key.py"
python $helper `
    --input $workspaceSource `
    --out $targetFull `
    --auto-key border `
    --soft-matte `
    --transparent-threshold 12 `
    --opaque-threshold 220 `
    --despill `
    --force
if ($LASTEXITCODE -ne 0) {
    throw "Chroma-key removal failed with exit code $LASTEXITCODE"
}

python "tmp/imagegen/Clean-CharacterPortrait.py" $targetFull --strength 0.22
if ($LASTEXITCODE -ne 0) {
    throw "Portrait cleanup failed with exit code $LASTEXITCODE"
}

Write-Output ("Processed latest generated image: {0}" -f $latest.FullName)
Write-Output ("Target: {0}" -f $targetFull)
