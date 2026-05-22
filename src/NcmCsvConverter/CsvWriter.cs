using System.Text;

namespace NcmCsvConverter;

public static class CsvWriter
{
    public const string Header = "code;description;contextual_description;parent_code;digit_level;selectable;start_date;end_date;has_scheduled_end_date;initial_legal_act;path;search_text";
    public static readonly Encoding Utf8WithBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

    public static string ToCsv(IEnumerable<NcmRow> rows)
    {
        var builder = new StringBuilder();
        builder.Append(Header).Append("\r\n");

        foreach (var row in rows)
        {
            builder.AppendJoin(';', new[]
            {
                Escape(row.Code),
                Escape(row.Description),
                Escape(row.ContextualDescription),
                Escape(row.ParentCode),
                row.DigitLevel.ToString(),
                row.Selectable ? "true" : "false",
                Escape(row.StartDate),
                Escape(row.EndDate),
                row.HasScheduledEndDate ? "true" : "false",
                Escape(row.InitialLegalAct),
                Escape(row.Path),
                Escape(row.SearchText)
            }).Append("\r\n");
        }

        return builder.ToString();
    }

    public static void WriteFile(string outputPath, string csv)
    {
        File.WriteAllText(outputPath, csv, Utf8WithBom);
    }

    public static string Escape(string? value)
    {
        value ??= string.Empty;

        if (!value.Contains(';') && !value.Contains('"') && !value.Contains('\r') && !value.Contains('\n'))
        {
            return value;
        }

        return '"' + value.Replace("\"", "\"\"") + '"';
    }
}
