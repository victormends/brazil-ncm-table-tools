param(
    [string]$OutputPath = (Join-Path (Get-Location) 'Tabela_NCM_Vigente.json'),
    [string]$Url = 'https://portalunico.siscomex.gov.br/classif/api/publico/nomenclatura/download/json',
    [switch]$Force,
    [switch]$Interactive,
    [switch]$PauseOnExit
)

$ErrorActionPreference = 'Stop'

function Confirm-Continue([string]$Message) {
    if (-not $Interactive) {
        return $true
    }

    $answer = Read-Host $Message
    return $answer -in @('Y', 'y', 'YES', 'yes', 'Yes', 'S', 's', 'SIM', 'sim', 'Sim')
}

function Pause-IfNeeded {
    if ($PauseOnExit) {
        [void](Read-Host 'Press Enter to close')
    }
}

$outputDir = Split-Path -Parent $OutputPath
if ([string]::IsNullOrWhiteSpace($outputDir)) {
    $outputDir = Get-Location
}

if (-not (Test-Path -LiteralPath $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir | Out-Null
}

$tempPath = Join-Path $outputDir 'Tabela_NCM_Vigente.tmp.json'

try {
    Write-Host 'Download current NCM table - Siscomex/Classif'
    Write-Host 'WARNING: Portal Unico may rate-limit this endpoint. Do not run repeated automated downloads.' -ForegroundColor Yellow
    Write-Host "URL: $Url"
    Write-Host "Output: $OutputPath"

    if ((Test-Path -LiteralPath $OutputPath) -and -not $Force) {
        if (-not (Confirm-Continue 'Output already exists. Type Y to overwrite:')) {
            Write-Host 'Operation canceled. Existing file was kept.'
            Pause-IfNeeded
            exit 0
        }
    }

    if (Test-Path -LiteralPath $tempPath) {
        Remove-Item -LiteralPath $tempPath -Force
    }

    Invoke-WebRequest -Uri $Url -OutFile $tempPath -UseBasicParsing
    $json = Get-Content -LiteralPath $tempPath -Raw -Encoding UTF8 | ConvertFrom-Json

    if (-not $json.Nomenclaturas -or $json.Nomenclaturas.Count -eq 0) {
        throw 'Downloaded file does not look like a valid NCM table: Nomenclaturas is empty or missing.'
    }

    Move-Item -LiteralPath $tempPath -Destination $OutputPath -Force

    Write-Host 'Download completed successfully.' -ForegroundColor Green
    Write-Host "Records: $($json.Nomenclaturas.Count)"
    Pause-IfNeeded
    exit 0
}
catch {
    if (Test-Path -LiteralPath $tempPath) {
        Remove-Item -LiteralPath $tempPath -Force -ErrorAction SilentlyContinue
    }

    Write-Error $_.Exception.Message
    Pause-IfNeeded
    exit 1
}
