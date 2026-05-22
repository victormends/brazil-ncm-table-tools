using System.Text;

namespace NcmCsvConverter.Tests;

public sealed class CsvContractTests
{
    [Fact]
    public void FixtureConversionMatchesExpectedCsvExactly()
    {
        var json = File.ReadAllText(NcmConverterTests.FixturePath("ncm-sample.json"));
        var expected = NormalizeCrLf(File.ReadAllText(NcmConverterTests.FixturePath("ncm-sample.expected.csv"))).TrimEnd('\r', '\n') + "\r\n";

        var csv = NcmConversionService.ConvertJsonToCsv(json);

        Assert.Equal(expected, csv);
    }

    [Fact]
    public void CsvAlwaysUsesCrLf()
    {
        var json = File.ReadAllText(NcmConverterTests.FixturePath("ncm-sample.json"));
        var csv = NcmConversionService.ConvertJsonToCsv(json);

        Assert.Contains("\r\n", csv);
        Assert.DoesNotContain("\n", csv.Replace("\r\n", string.Empty, StringComparison.Ordinal));
    }

    [Fact]
    public void WrittenCsvStartsWithUtf8Bom()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), "ncm-bom-test-" + Guid.NewGuid().ToString("N") + ".csv");

        try
        {
            CsvWriter.WriteFile(outputPath, CsvWriter.Header + "\r\n");
            var bytes = File.ReadAllBytes(outputPath);

            Assert.True(bytes.Length >= 3);
            Assert.Equal(0xEF, bytes[0]);
            Assert.Equal(0xBB, bytes[1]);
            Assert.Equal(0xBF, bytes[2]);
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public void OutputDirectoryIsCreatedAutomatically()
    {
        var root = Path.Combine(Path.GetTempPath(), "ncm-output-dir-test-" + Guid.NewGuid().ToString("N"));
        var outputPath = Path.Combine(root, "nested", "ncm.csv");

        try
        {
            NcmConversionService.ConvertFile(NcmConverterTests.FixturePath("ncm-sample.json"), outputPath);

            Assert.True(File.Exists(outputPath));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private static string NormalizeCrLf(string value)
    {
        return value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\n", "\r\n", StringComparison.Ordinal);
    }
}
