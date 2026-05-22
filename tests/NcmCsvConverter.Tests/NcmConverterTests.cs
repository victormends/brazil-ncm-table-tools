using System.Text;

namespace NcmCsvConverter.Tests;

public sealed class NcmConverterTests
{
    [Fact]
    public void NormalizeCodeRemovesPunctuation()
    {
        Assert.Equal("01012100", NcmConversionService.NormalizeCode("0101.21.00"));
    }

    [Fact]
    public void ParentUsesLargestExistingPrefix()
    {
        var codes = new HashSet<string> { "01", "0101", "01012", "010121", "0101210", "01012100" };

        Assert.Equal("0101210", NcmConversionService.FindParentCode("01012100", codes));
    }

    [Fact]
    public void ConvertsFixtureBusinessRules()
    {
        var rows = NcmConversionService.Convert(ReadFixtureTable().Nomenclaturas);
        var byCode = rows.ToDictionary(row => row.Code);

        Assert.False(byCode["0101210"].Selectable);
        Assert.True(byCode["01012100"].Selectable);
        Assert.Equal("0101210", byCode["01012100"].ParentCode);
        Assert.Equal("De raca pura - Outras", byCode["01012100"].ContextualDescription);
        Assert.True(byCode["01012100"].HasScheduledEndDate);
        Assert.False(byCode["01012900"].HasScheduledEndDate);
        Assert.Equal("Resolucao 5/2026", byCode["01012100"].InitialLegalAct);
    }

    private static NcmTable ReadFixtureTable()
    {
        var json = File.ReadAllText(FixturePath("ncm-sample.json"));
        return System.Text.Json.JsonSerializer.Deserialize<NcmTable>(json, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Fixture did not parse.");
    }

    public static string FixturePath(string fileName)
    {
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "fixtures", fileName));
    }
}
