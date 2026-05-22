# Ferramentas para Tabela NCM do Brasil

[![CI](https://github.com/victormends/brazil-ncm-table-tools/actions/workflows/ci.yml/badge.svg)](https://github.com/victormends/brazil-ncm-table-tools/actions/workflows/ci.yml)
[![Licença: MIT](https://img.shields.io/badge/Licen%C3%A7a-MIT-yellow.svg)](LICENSE)
[![.NET 8](https://img.shields.io/badge/.NET-8-blueviolet.svg)](https://dotnet.microsoft.com/)
[![PowerShell 5.1+](https://img.shields.io/badge/PowerShell-5.1%2B-blue.svg)](https://learn.microsoft.com/powershell/)
[![Plataforma: Windows](https://img.shields.io/badge/Plataforma-Windows-lightgrey.svg)](https://www.microsoft.com/windows)

Ferramentas Windows-first em .NET e PowerShell para baixar a tabela NCM oficial do Siscomex/Classif e converter o JSON para um CSV amigável para ERP e busca local.

Documentação em inglês: [README.md](README.md).

## Por Que Este Projeto Existe

Sistemas ERP normalmente não deveriam consultar o Siscomex em tempo real para cada produto. Eles precisam de uma tabela mestra local de NCM que seja pesquisável, auditável, importável e comparável com segurança entre atualizações oficiais.

Este projeto padroniza esse fluxo: baixar o JSON oficial, validar o arquivo, converter para um contrato CSV determinístico e preservar contexto de hierarquia, datas e ato legal para rotinas de importação e revisão no ERP.

## O Que Este Projeto Faz

- Baixa o JSON oficial vigente da tabela NCM pelo Siscomex/Classif.
- Converte o JSON oficial para um CSV normalizado para uso local em ERP/busca.
- Preserva hierarquia, datas, ato legal e descrições contextualizadas.
- Marca somente NCMs de 8 dígitos como selecionáveis para cadastro de produto.

Este projeto não classifica produtos, não substitui revisão fiscal e não redistribui a base oficial completa.

## Fonte Oficial Dos Dados

O downloader usa o endpoint público do Siscomex/Classif:

```text
https://portalunico.siscomex.gov.br/classif/api/publico/nomenclatura/download/json
```

O Portal Único pode limitar acessos a esse endpoint. Não rode downloads automatizados repetidos.

## Aviso Importante

Este é um projeto não oficial. A classificação NCM tem consequências fiscais e legais. Use estas ferramentas para processar localmente a tabela oficial, mas valide a classificação dos produtos com profissionais qualificados da área fiscal, contábil ou aduaneira.

## Início Rápido

Requisitos:

- .NET SDK 8+
- Windows PowerShell 5.1+ para o script de download

Baixar o JSON oficial:

```powershell
.\scripts\download-ncm.ps1 -OutputPath .\data\Tabela_NCM_Vigente.json
```

Converter JSON para CSV:

```powershell
dotnet run --project .\src\NcmCsvConverter -- --input .\data\Tabela_NCM_Vigente.json --output .\out\ncm.csv
```

Build e testes:

```powershell
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
```

## Baixar o JSON Oficial

Uso básico:

```powershell
.\scripts\download-ncm.ps1 -OutputPath .\data\Tabela_NCM_Vigente.json
```

Sobrescrever arquivo existente:

```powershell
.\scripts\download-ncm.ps1 -OutputPath .\data\Tabela_NCM_Vigente.json -Force
```

Rodar contra fixture local sem chamar o Portal Único:

```powershell
.\scripts\download-ncm.ps1 -Url file:///C:/path/to/ncm-sample.json -OutputPath .\out\Tabela_NCM_Vigente.json -Force
```

O script grava primeiro em arquivo temporário, valida que `Nomenclaturas` existe e possui registros, e só então move para o caminho final.

## Converter JSON Para CSV

```powershell
dotnet run --project .\src\NcmCsvConverter -- --input .\data\Tabela_NCM_Vigente.json --output .\out\ncm.csv
```

Comportamento da CLI:

- Ausência de `--input` ou `--output`: exit `2`.
- Argumentos inválidos: exit `2`.
- Arquivo de entrada inexistente, JSON inválido ou tabela NCM inválida: exit `1`.
- Conversão com sucesso: exit `0`.
- Diretórios de saída inexistentes são criados automaticamente.

## Schema do CSV

O CSV público v1 usa cabeçalhos em inglês:

```text
code;description;contextual_description;parent_code;digit_level;selectable;start_date;end_date;has_scheduled_end_date;initial_legal_act;path;search_text
```

Colunas:

| Coluna | Significado |
|---|---|
| `code` | Código NCM normalizado apenas com dígitos. |
| `description` | Descrição oficial sem marcadores iniciais de hierarquia. |
| `contextual_description` | Descrição enriquecida com contexto do pai para linhas genéricas como `Outros` ou `Outras`. |
| `parent_code` | Maior prefixo menor existente na hierarquia oficial. |
| `digit_level` | Quantidade de dígitos em `code`. |
| `selectable` | `true` somente para NCMs finais de 8 dígitos. |
| `start_date` | `Data_Inicio` oficial, preservada como `dd/MM/yyyy`. |
| `end_date` | `Data_Fim` oficial, preservada como `dd/MM/yyyy`. |
| `has_scheduled_end_date` | `true` quando `end_date` existe e é diferente de `31/12/9999`. Isto não é uma flag de ativo. |
| `initial_legal_act` | Ato legal inicial formatado como `Tipo Numero/Ano`, por exemplo `Resolução 812/2025`. |
| `path` | Caminho da raiz da hierarquia até a linha. |
| `search_text` | Texto para busca construído com descrições e ancestrais, sem prefixar o código. |

## Contrato do CSV

- Delimitador: ponto e vírgula `;`.
- Encoding: UTF-8 com BOM, usado intencionalmente para compatibilidade com Excel e importações comuns em Windows/ERP.
- Cabeçalho: sempre emitido.
- Datas: preservadas como `dd/MM/yyyy` conforme a fonte oficial.
- Booleanos: `true` / `false` em minúsculo.
- Quebras de linha: CRLF em todas as plataformas.
- Escape: campos são colocados entre aspas quando necessário; aspas internas viram `""`.
- Ordem das colunas: fixa e testada.

## Uso em ERP

- Não consulte o Siscomex em tempo real para cada produto.
- Mantenha uma tabela mestra local de NCM e sincronize de forma controlada.
- Permita apenas linhas com `selectable = true` no cadastro de produtos.
- Mantenha a hierarquia completa para navegação, busca e contexto.
- Não delete NCMs antigos cegamente; produtos e notas históricas podem precisar de rastreabilidade.

## Fluxo Seguro De Atualização

1. Baixe o novo JSON oficial para uma área de staging.
2. Converta para CSV.
3. Importe para uma tabela temporária/staging.
4. Compare com o snapshot anterior.
5. Classifique linhas como novas, removidas, alteradas ou inalteradas.
6. Sinalize produtos que usam NCMs removidos ou alterados para revisão.
7. Nunca reclassifique produtos automaticamente.

Para sugestões, priorize candidatos com mesmo pai, prefixo mais próximo, similaridade textual, similaridade de caminho anterior, `selectable = true` e `start_date` recente como critério de desempate.

## Política De Dados

Este repositório não versiona nem redistribui o JSON oficial completo ou CSVs completos gerados. Os testes usam apenas fixtures sintéticas pequenas.

## Build e Testes

```powershell
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
```

Teste manual do downloader com fixture:

```powershell
powershell -ExecutionPolicy Bypass -File .\tests\scripts\test-download-ncm.ps1
```

## Checklist de Release Público

- `dotnet build --configuration Release` passa.
- `dotnet test --configuration Release` passa.
- O teste do downloader com fixture passa sem chamar o Portal Único.
- Nenhum JSON oficial completo, CSV gerado, executável, ZIP, configuração local ou arquivo operacional privado é commitado.
- README, NOTICE e SECURITY deixam claro que este é um projeto não oficial e não é consultoria de classificação fiscal.

## Solução de Problemas

Se o restore falhar porque nenhuma fonte NuGet está configurada:

```powershell
dotnet nuget list source
dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org
dotnet restore
```

## Roadmap

Fora do escopo do v1:

- Executável C# para download.
- Empacotamento PS2EXE.
- Script de fallback com MSBuild.
- Binários em GitHub Releases.
- Documentação HTML / GitHub Pages.
- Ferramenta de diff entre snapshots.
- Sugestões automáticas de substituição de NCM.
