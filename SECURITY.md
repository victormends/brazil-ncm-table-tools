# Security and Data Policy

This repository is public-safe tooling only. It must not contain generated official datasets, private ERP exports, customer files, credentials, local configuration, or executable release artifacts.

## Do Not Commit

- Full official Siscomex/Classif JSON downloads
- Generated full CSV exports
- ERP imports or exports
- Customer, supplier, product, invoice, or accounting files
- Local configuration files with operational paths
- Executables, ZIP packages, or build output
- Logs containing local paths, credentials, or operational data
- Certificates, private keys, PFX/P12 files, or PEM files

## Test Data

Tests use small synthetic fixtures under `tests/fixtures/`. They are intentionally minimal and are not copies of the full official NCM dataset.

## Official Source

The downloader uses the public Siscomex/Classif endpoint through Portal Único:

```text
https://portalunico.siscomex.gov.br/classif/api/publico/nomenclatura/download/json
```

Portal Único may rate-limit repeated accesses. CI and repository tests must not call the official endpoint.

## Fiscal Disclaimer

NCM classification has fiscal and legal consequences. This project provides tooling to download, normalize, and search the official table. It does not classify products and does not replace review by qualified tax, accounting, or customs professionals.

## Política de Segurança e Dados

Este repositório deve conter apenas ferramentas publicáveis. Ele não deve conter bases oficiais completas geradas, exportações privadas de ERP, arquivos de clientes, credenciais, configuração local ou artefatos executáveis de release.

Os testes usam apenas fixtures sintéticas pequenas em `tests/fixtures/`. CI e testes do repositório não devem chamar o endpoint oficial do Portal Único.
