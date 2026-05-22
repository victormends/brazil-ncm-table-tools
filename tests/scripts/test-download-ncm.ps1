param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
)

$ErrorActionPreference = 'Stop'

$fixture = Join-Path $RepoRoot 'tests\fixtures\ncm-sample.json'
$outDir = Join-Path $RepoRoot 'out\download-test'
$outFile = Join-Path $outDir 'Tabela_NCM_Vigente.json'

if (Test-Path -LiteralPath $outDir) {
    Remove-Item -LiteralPath $outDir -Recurse -Force
}

New-Item -ItemType Directory -Path $outDir | Out-Null

$uri = (New-Object System.Uri($fixture)).AbsoluteUri
& powershell -ExecutionPolicy Bypass -File (Join-Path $RepoRoot 'scripts\download-ncm.ps1') -Url $uri -OutputPath $outFile -Force

if ($LASTEXITCODE -ne 0) {
    throw "download-ncm.ps1 failed with exit code $LASTEXITCODE"
}

if (-not (Test-Path -LiteralPath $outFile)) {
    throw "Expected output file was not created: $outFile"
}

$json = Get-Content -LiteralPath $outFile -Raw -Encoding UTF8 | ConvertFrom-Json
if (-not $json.Nomenclaturas -or $json.Nomenclaturas.Count -eq 0) {
    throw 'Downloaded fixture is invalid: Nomenclaturas is empty or missing.'
}

"Downloader fixture test passed. Records=$($json.Nomenclaturas.Count)"
