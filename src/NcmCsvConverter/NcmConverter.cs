using System.Text.Json;
using System.Text.RegularExpressions;

namespace NcmCsvConverter;

public static class NcmConversionService
{
    public static void ConvertFile(string inputPath, string outputPath)
    {
        if (!File.Exists(inputPath))
        {
            throw new FileNotFoundException("Input file was not found.", inputPath);
        }

        var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        var json = File.ReadAllText(inputPath);
        var csv = ConvertJsonToCsv(json);
        CsvWriter.WriteFile(outputPath, csv);
    }

    public static string ConvertJsonToCsv(string json)
    {
        var table = JsonSerializer.Deserialize<NcmTable>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Invalid JSON file.");

        if (table.Nomenclaturas.Count == 0)
        {
            throw new InvalidOperationException("Invalid NCM table: Nomenclaturas is empty or missing.");
        }

        return CsvWriter.ToCsv(Convert(table.Nomenclaturas));
    }

    public static IReadOnlyList<NcmRow> Convert(IEnumerable<NcmItem> items)
    {
        var sourceRows = items
            .Select(item => new SourceRow(item, NormalizeCode(item.Codigo), CleanDescription(item.Descricao)))
            .Where(row => row.Code.Length > 0)
            .OrderBy(row => row.Code, StringComparer.Ordinal)
            .ToList();

        var existingCodes = sourceRows.Select(row => row.Code).ToHashSet(StringComparer.Ordinal);
        var rowsByCode = new Dictionary<string, NcmRow>(StringComparer.Ordinal);
        var result = new List<NcmRow>(sourceRows.Count);

        foreach (var source in sourceRows)
        {
            var parentCode = FindParentCode(source.Code, existingCodes);
            rowsByCode.TryGetValue(parentCode, out var parent);

            var contextualDescription = BuildContextualDescription(source.Description, parent);
            var path = string.IsNullOrWhiteSpace(parent?.Path)
                ? contextualDescription
                : parent.Path + " > " + contextualDescription;
            var searchText = string.IsNullOrWhiteSpace(parent?.SearchText)
                ? contextualDescription
                : parent.SearchText + " " + contextualDescription;

            var row = new NcmRow(
                source.Code,
                source.Description,
                contextualDescription,
                parentCode,
                source.Code.Length,
                source.Code.Length == 8,
                source.Item.Data_Inicio,
                source.Item.Data_Fim,
                HasScheduledEndDate(source.Item.Data_Fim),
                BuildInitialLegalAct(source.Item),
                path,
                searchText);

            rowsByCode[source.Code] = row;
            result.Add(row);
        }

        return result;
    }

    public static string NormalizeCode(string officialCode)
    {
        return Regex.Replace(officialCode ?? string.Empty, "[^0-9]", string.Empty);
    }

    public static string FindParentCode(string normalizedCode, ISet<string> existingCodes)
    {
        for (var length = normalizedCode.Length - 1; length > 0; length--)
        {
            var prefix = normalizedCode[..length];
            if (existingCodes.Contains(prefix))
            {
                return prefix;
            }
        }

        return string.Empty;
    }

    public static string BuildInitialLegalAct(NcmItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Tipo_Ato_Ini) &&
            string.IsNullOrWhiteSpace(item.Numero_Ato_Ini) &&
            string.IsNullOrWhiteSpace(item.Ano_Ato_Ini))
        {
            return string.Empty;
        }

        var actType = item.Tipo_Ato_Ini?.Trim() ?? string.Empty;
        var number = item.Numero_Ato_Ini?.Trim() ?? string.Empty;
        var year = item.Ano_Ato_Ini?.Trim() ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(number) && !string.IsNullOrWhiteSpace(year))
        {
            return string.IsNullOrWhiteSpace(actType) ? $"{number}/{year}" : $"{actType} {number}/{year}";
        }

        return string.Join(" ", new[] { actType, number, year }.Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    private static bool HasScheduledEndDate(string endDate)
    {
        return !string.IsNullOrWhiteSpace(endDate) && endDate.Trim() != "31/12/9999";
    }

    private static string CleanDescription(string description)
    {
        return Regex.Replace(description ?? string.Empty, "^-+\\s*", string.Empty).Trim();
    }

    private static string BuildContextualDescription(string description, NcmRow? parent)
    {
        if (!IsGenericDescription(description) || parent is null)
        {
            return description;
        }

        return IsGenericDescription(parent.ContextualDescription)
            ? description
            : parent.ContextualDescription + " - " + description;
    }

    private static bool IsGenericDescription(string description)
    {
        var normalized = description.Trim().ToUpperInvariant();
        return normalized is "OUTROS" or "OUTRAS" or "DEMAIS";
    }

    private sealed record SourceRow(NcmItem Item, string Code, string Description);
}
