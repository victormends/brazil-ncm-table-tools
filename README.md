# Brazil NCM Table Tools

[![CI](https://github.com/victormends/brazil-ncm-table-tools/actions/workflows/ci.yml/badge.svg)](https://github.com/victormends/brazil-ncm-table-tools/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET 8](https://img.shields.io/badge/.NET-8-blueviolet.svg)](https://dotnet.microsoft.com/)
[![PowerShell 5.1+](https://img.shields.io/badge/PowerShell-5.1%2B-blue.svg)](https://learn.microsoft.com/powershell/)
[![Platform: Windows](https://img.shields.io/badge/Platform-Windows-lightgrey.svg)](https://www.microsoft.com/windows)

Windows-first .NET and PowerShell tools to download Brazil's official NCM table from Siscomex/Classif and convert it into an ERP/search-friendly CSV.

Portuguese documentation: [README.pt-BR.md](README.pt-BR.md).

Operator documentation:

- [Operator guide](docs/operator-guide.md)

## Why This Exists

ERP systems usually should not query Siscomex live for every product lookup. They need a local, auditable NCM master table that can be searched, imported, and compared safely between official updates.

This project standardizes that workflow: download the official JSON, validate it, convert it to a deterministic CSV contract, and keep enough hierarchy/date/legal-act context for ERP import and review routines.

## What This Project Does

- Downloads the current official NCM JSON table from Siscomex/Classif.
- Converts the official JSON into a normalized CSV for local ERP/search usage.
- Preserves hierarchy, dates, legal act metadata, and contextual descriptions.
- Marks only 8-digit NCM codes as selectable for product registration.

This project does not classify products, replace fiscal review, or redistribute the full official dataset.

## Official Data Source

The downloader uses the public Siscomex/Classif endpoint:

```text
https://portalunico.siscomex.gov.br/classif/api/publico/nomenclatura/download/json
```

Portal Único may rate-limit this endpoint. Do not run repeated automated downloads.

## Important Disclaimer

This is an unofficial project. NCM classification has fiscal and legal consequences. Use these tools to process the official table locally, but validate product classification with qualified tax, accounting, or customs professionals.

## Quick Start

Requirements:

- .NET SDK 8+
- Windows PowerShell 5.1+ for the downloader script

Download the official JSON:

```powershell
.\scripts\download-ncm.ps1 -OutputPath .\data\Tabela_NCM_Vigente.json
```

Convert JSON to CSV:

```powershell
dotnet run --project .\src\NcmCsvConverter -- --input .\data\Tabela_NCM_Vigente.json --output .\out\ncm.csv
```

Build and test:

```powershell
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
```

## Download the Official JSON

Basic usage:

```powershell
.\scripts\download-ncm.ps1 -OutputPath .\data\Tabela_NCM_Vigente.json
```

Overwrite an existing file:

```powershell
.\scripts\download-ncm.ps1 -OutputPath .\data\Tabela_NCM_Vigente.json -Force
```

Run against a local fixture without calling Portal Único:

```powershell
.\scripts\download-ncm.ps1 -Url file:///C:/path/to/ncm-sample.json -OutputPath .\out\Tabela_NCM_Vigente.json -Force
```

The script writes to a temporary file first, validates that `Nomenclaturas` exists and has records, then moves the file to the final path.

## Convert JSON to CSV

```powershell
dotnet run --project .\src\NcmCsvConverter -- --input .\data\Tabela_NCM_Vigente.json --output .\out\ncm.csv
```

CLI behavior:

- Missing `--input` or `--output`: exit `2`.
- Invalid arguments: exit `2`.
- Missing input file, invalid JSON, or invalid NCM table: exit `1`.
- Successful conversion: exit `0`.
- Missing output directories are created automatically.

## CSV Schema

The public v1 CSV uses English headers:

```text
code;description;contextual_description;parent_code;digit_level;selectable;start_date;end_date;has_scheduled_end_date;initial_legal_act;path;search_text
```

Columns:

| Column | Meaning |
|---|---|
| `code` | NCM code normalized to digits only. |
| `description` | Official description cleaned from leading hierarchy markers. |
| `contextual_description` | Description enriched with parent context for generic rows such as `Outros` or `Outras`. |
| `parent_code` | Largest existing shorter prefix in the official hierarchy. |
| `digit_level` | Number of digits in `code`. |
| `selectable` | `true` only for 8-digit product-level NCMs. |
| `start_date` | Official `Data_Inicio`, preserved as `dd/MM/yyyy`. |
| `end_date` | Official `Data_Fim`, preserved as `dd/MM/yyyy`. |
| `has_scheduled_end_date` | `true` when `end_date` is present and different from `31/12/9999`. This is not an `active` flag. |
| `initial_legal_act` | Initial legal act formatted as `Tipo Numero/Ano`, for example `Resolucao 812/2025`. |
| `path` | Breadcrumb from hierarchy root to the row. |
| `search_text` | Search-oriented text built from descriptions and ancestors, without prefixing the code. |

## CSV Contract

- Delimiter: semicolon `;`.
- Encoding: UTF-8 with BOM, intentionally used for compatibility with Excel and common Windows/ERP imports.
- Header: always emitted.
- Dates: preserved as `dd/MM/yyyy` from the official source.
- Booleans: lowercase `true` / `false`.
- Line endings: CRLF on all platforms.
- Escaping: fields are quoted when needed; quotes are escaped as `""`.
- Column order: fixed and tested.

## ERP Usage Notes

- Do not query Siscomex live for every product.
- Keep a local NCM master table and sync it in a controlled process.
- Only allow rows with `selectable = true` for product registration.
- Keep the full hierarchy for browsing, search, and context.
- Do not delete old NCMs blindly; historical products and invoices may need auditability.

See the [operator guide](docs/operator-guide.md) for staging, search, and manual-review guidance.

## Safe Update Workflow

1. Download the new official JSON to a staging location.
2. Convert it to CSV.
3. Import it into a staging table.
4. Diff against the previous snapshot.
5. Classify rows as new, removed, changed, or unchanged.
6. Flag products using removed or changed NCMs for review.
7. Never auto-reclassify products.

For suggestions, prefer candidates with the same parent, closest prefix, similar text, previous path similarity, `selectable = true`, and recent `start_date` as a tie-breaker.

See the [operator guide](docs/operator-guide.md) for a fuller downstream snapshot/diff routine. This repository does not currently provide a snapshot diff tool.

## Data Policy

This repository does not commit or redistribute the full official JSON or generated full CSV files. Tests use small synthetic fixtures only.

## Build and Test

```powershell
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
```

Manual downloader fixture test:

```powershell
powershell -ExecutionPolicy Bypass -File .\tests\scripts\test-download-ncm.ps1
```

## Public Release Checklist

- `dotnet build --configuration Release` passes.
- `dotnet test --configuration Release` passes.
- Downloader fixture test passes without calling Portal Único.
- No official full JSON, generated CSV, executable, ZIP, local config, or private operational file is committed.
- README, NOTICE, and SECURITY explain that this is unofficial tooling and not fiscal classification advice.

## Troubleshooting

If package restore fails because no NuGet source is configured:

```powershell
dotnet nuget list source
dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org
dotnet restore
```

## Roadmap

Out of scope for v1:

- C# downloader executable.
- PS2EXE packaging.
- MSBuild fallback script.
- GitHub release binaries.
- HTML documentation / GitHub Pages.
- Snapshot diff tool.
- Automatic NCM replacement suggestions.
