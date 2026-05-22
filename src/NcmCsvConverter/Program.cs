namespace NcmCsvConverter;

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Any(arg => arg is "--help" or "-h" or "/?"))
        {
            PrintHelp();
            return 0;
        }

        if (args.Any(arg => arg is "--version" or "-v"))
        {
            Console.WriteLine("ncm-csv-converter 0.1.0");
            return 0;
        }

        try
        {
            var options = CliOptions.Parse(args);
            if (options is null)
            {
                PrintHelp();
                return 2;
            }

            NcmConversionService.ConvertFile(options.InputPath, options.OutputPath);
            Console.WriteLine($"Converted NCM table to {options.OutputPath}");
            return 0;
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine($"Invalid arguments: {ex.Message}");
            return 2;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Conversion failed: {ex.Message}");
            return 1;
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Brazil NCM CSV Converter");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  ncm-csv-converter --input <Tabela_NCM_Vigente.json> --output <ncm.csv>");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --input, -i     Path to the official Siscomex/Classif NCM JSON file.");
        Console.WriteLine("  --output, -o    Path to the CSV file to write. Missing directories are created.");
        Console.WriteLine("  --help, -h      Show this help.");
        Console.WriteLine("  --version, -v   Show version.");
    }
}

internal sealed record CliOptions(string InputPath, string OutputPath)
{
    public static CliOptions? Parse(string[] args)
    {
        string? input = null;
        string? output = null;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "--input":
                case "-i":
                    input = ReadValue(args, ref i, arg);
                    break;
                case "--output":
                case "-o":
                    output = ReadValue(args, ref i, arg);
                    break;
                default:
                    throw new ArgumentException($"Unknown option '{arg}'.");
            }
        }

        if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(output))
        {
            return null;
        }

        return new CliOptions(input, output);
    }

    private static string ReadValue(string[] args, ref int index, string option)
    {
        if (index + 1 >= args.Length || args[index + 1].StartsWith("-", StringComparison.Ordinal))
        {
            throw new ArgumentException($"Option '{option}' requires a value.");
        }

        index++;
        return args[index];
    }
}
