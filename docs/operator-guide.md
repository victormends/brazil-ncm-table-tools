# Operator Guide

This guide explains how to use Brazil NCM Table Tools in a controlled ERP/search workflow.

The repository generates a CSV contract from the official Siscomex/Classif NCM JSON. ERP staging, snapshot diffing, product review queues, and production promotion are downstream processes implemented outside this repository.

## Workflow Summary

```text
Siscomex/Classif public endpoint
  -> official JSON file in a local staging folder
  -> NcmCsvConverter
  -> generated CSV file
  -> downstream ERP staging table or search index
```

Use this as a controlled update process. Do not query Siscomex/Classif live for every product lookup inside an ERP.

## Download The Official JSON

The downloader script calls the public Siscomex/Classif endpoint:

```text
https://portalunico.siscomex.gov.br/classif/api/publico/nomenclatura/download/json
```

Basic usage:

```powershell
.\scripts\download-ncm.ps1 -OutputPath .\data\Tabela_NCM_Vigente.json
```

Overwrite an existing local file intentionally:

```powershell
.\scripts\download-ncm.ps1 -OutputPath .\data\Tabela_NCM_Vigente.json -Force
```

The script writes to a temporary file first, validates that `Nomenclaturas` exists and has records, then moves the file to the requested output path.

Portal Unico may rate-limit this endpoint. Run downloads on a deliberate schedule or on demand, not in tight automation loops.

## Convert JSON To CSV

Run the converter from source:

```powershell
dotnet run --project .\src\NcmCsvConverter -- --input .\data\Tabela_NCM_Vigente.json --output .\out\ncm.csv
```

The converter creates missing output directories and writes a UTF-8 BOM CSV with CRLF line endings.

Exit behavior:

| Condition | Exit code |
|---|---:|
| Success | `0` |
| Missing `--input` or `--output` | `2` |
| Unknown option or invalid argument | `2` |
| Missing input file, invalid JSON, or invalid NCM table | `1` |

## CSV Contract

The v1 CSV header is fixed and tested:

```text
code;description;contextual_description;parent_code;digit_level;selectable;start_date;end_date;has_scheduled_end_date;initial_legal_act;path;search_text
```

| Column | Meaning |
|---|---|
| `code` | NCM code normalized to digits only. |
| `description` | Official description cleaned from leading hierarchy markers. |
| `contextual_description` | Description enriched with parent context for generic rows such as `Outros`, `Outras`, or `Demais`. |
| `parent_code` | Largest existing shorter prefix in the official hierarchy. |
| `digit_level` | Number of digits in `code`. |
| `selectable` | `true` only for 8-digit product-level NCMs. |
| `start_date` | Official `Data_Inicio`, preserved as `dd/MM/yyyy`. |
| `end_date` | Official `Data_Fim`, preserved as `dd/MM/yyyy`. |
| `has_scheduled_end_date` | `true` when `end_date` is present and different from `31/12/9999`; this is not an active/inactive flag. |
| `initial_legal_act` | Initial legal act formatted from the official source fields. |
| `path` | Breadcrumb from the hierarchy root to the row. |
| `search_text` | Search-oriented text built from descriptions and ancestors, without prefixing the code. |

Treat `code` as text, not as a number. NCM codes can have leading zeroes.

## ERP Import Pattern

Recommended downstream ERP process:

1. Generate the CSV from the current official JSON.
2. Import the CSV into an ERP staging table.
3. Validate row count, header, delimiter, dates, and required columns.
4. Compare the staging table with the current production NCM master table.
5. Promote the new snapshot only after review checks pass.

Avoid direct imports into production tables without a staging step.

The local NCM master table should preserve the CSV contract. Keep the full hierarchy for browsing, search, review, and audit context.

## Search And Product Registration

Only allow product registration against rows where:

```text
selectable = true
```

In this project, `selectable` is true only for 8-digit codes. Shorter rows are hierarchy/context nodes and should remain available for browsing and search, not product registration.

Recommended search inputs:

- Exact or prefix matching on `code`.
- Text search on `description`.
- Text search on `contextual_description`.
- Text search on `search_text`.
- Hierarchy browsing using `parent_code` and `path`.

Do not build search only on the raw official description. Generic labels such as `Outros` or `Outras` need parent context to be useful to an operator.

## Safe Update Routine

This repository does not currently provide a snapshot diff tool. The routine below describes the recommended downstream ERP process after importing generated CSV snapshots.

1. Download the official JSON to a local staging path.
2. Convert the JSON to CSV.
3. Import the CSV into an ERP staging table.
4. Validate the staging import.
5. Diff the staging snapshot against the current NCM master snapshot.
6. Classify rows as new, removed, changed, or unchanged.
7. Promote the snapshot only after review checks pass.
8. Flag affected products for manual fiscal review.

Suggested diff categories:

| Category | Meaning | Recommended action |
|---|---|---|
| New | `code` exists in the new snapshot but not in the previous one. | Add to the local master table after validation. |
| Removed | `code` existed previously but is absent from the new snapshot. | Keep historical references; block new product selection if appropriate; review linked products. |
| Changed | `code` exists in both snapshots but relevant fields changed. | Review descriptions, dates, legal act, hierarchy, and affected products. |
| Unchanged | `code` exists in both snapshots with no relevant field changes. | No action beyond normal snapshot bookkeeping. |

Common fields for change detection:

- `description`
- `contextual_description`
- `parent_code`
- `digit_level`
- `selectable`
- `start_date`
- `end_date`
- `has_scheduled_end_date`
- `initial_legal_act`
- `path`
- `search_text`

## Removed Or Changed NCM Codes

Do not delete old NCM rows blindly from ERP history. Invoices, products, fiscal documents, and audit trails may need to preserve the NCM code that was valid or used at the time of the transaction.

When an NCM row is removed or materially changed in a downstream ERP process:

1. Identify products currently linked to the code.
2. Freeze automatic changes to those product classifications.
3. Present candidates for manual review only if your ERP implements suggestion logic.
4. Require a qualified reviewer to approve any product classification change.

Candidate suggestions, if implemented outside this project, should prefer:

- Same parent code.
- Closest prefix.
- Similar official text.
- Similar hierarchy path.
- `selectable = true`.
- Recent `start_date` as a tie-breaker, not as the only rule.

`has_scheduled_end_date` can support review queues when `end_date` is present and different from `31/12/9999`. Do not treat it as a complete active/inactive model by itself.

## Local Artifacts And Data Policy

Recommended local folders:

| Folder | Purpose | Commit? |
|---|---|---|
| `data/` | Local official JSON download target. | No, for full official files. |
| `out/` | Generated CSV output. | No, for full generated CSVs. |
| `tests/fixtures/` | Small synthetic fixtures used by tests. | Yes. |

Do not commit full official JSON files, generated full CSV files, local import logs, customer ERP exports, credentials, or release binaries.

## Verification

Build and test from the repository root:

```powershell
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
```

Run the downloader fixture test without calling Portal Unico:

```powershell
powershell -ExecutionPolicy Bypass -File .\tests\scripts\test-download-ncm.ps1
```

## What This Project Does Not Do

- It does not classify products.
- It does not replace fiscal, accounting, customs, or legal review.
- It does not redistribute the full official NCM dataset.
- It does not generate release binaries in the current v1 scope.
- It does not currently provide a snapshot diff tool.
- It does not provide automatic NCM replacement decisions.

Back to [README](../README.md).
