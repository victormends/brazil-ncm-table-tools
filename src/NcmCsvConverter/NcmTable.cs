using System.Text.Json.Serialization;

namespace NcmCsvConverter;

public sealed class NcmTable
{
    public string? Data_Ultima_Atualizacao_NCM { get; set; }
    public string? Ato { get; set; }
    public List<NcmItem> Nomenclaturas { get; set; } = [];
}

public sealed class NcmItem
{
    public string Codigo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Data_Inicio { get; set; } = string.Empty;
    public string Data_Fim { get; set; } = string.Empty;
    public string? Tipo_Ato_Ini { get; set; }
    public string? Numero_Ato_Ini { get; set; }
    public string? Ano_Ato_Ini { get; set; }
}

public sealed record NcmRow(
    string Code,
    string Description,
    string ContextualDescription,
    string ParentCode,
    int DigitLevel,
    bool Selectable,
    string StartDate,
    string EndDate,
    bool HasScheduledEndDate,
    string InitialLegalAct,
    string Path,
    string SearchText);
