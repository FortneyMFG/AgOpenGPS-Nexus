using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Aog.Tools.LegacyDataMigrator;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || ContainsHelp(args))
        {
            PrintUsage();
            return args.Length == 0 ? 1 : 0;
        }

        var command = args[0];
        if (!string.Equals(command, "migrate", StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine($"error: unknown command '{command}'.");
            PrintUsage();
            return 1;
        }

        try
        {
            var (options, verbose) = ParseOptions(args);
            var migrator = new LegacyDataMigrator();
            var report = await migrator.MigrateAsync(options).ConfigureAwait(false);

            PrintReport(report, verbose);
            return 0;
        }
        catch (Exception ex) when (ex is ArgumentException or DirectoryNotFoundException or FileNotFoundException)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"error: unexpected failure: {ex.Message}");
            return 1;
        }
    }

    private static bool ContainsHelp(IReadOnlyList<string> args)
    {
        foreach (var arg in args)
        {
            if (string.Equals(arg, "-h", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(arg, "--help", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(arg, "help", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static (LegacyMigrationOptions Options, bool Verbose) ParseOptions(IReadOnlyList<string> args)
    {
        string? input = null;
        string? output = null;
        string logsDirectory = "logs";
        string fieldHistoryFile = "field-history.csv";
        bool verbose = false;

        for (var i = 1; i < args.Count; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "--input":
                case "-i":
                    input = RequireNext(args, ref i, arg);
                    break;
                case "--output":
                case "-o":
                    output = RequireNext(args, ref i, arg);
                    break;
                case "--logs":
                    logsDirectory = RequireNext(args, ref i, arg);
                    break;
                case "--history":
                    fieldHistoryFile = RequireNext(args, ref i, arg);
                    break;
                case "--verbose":
                case "-v":
                    verbose = true;
                    break;
                default:
                    if (string.IsNullOrWhiteSpace(input))
                    {
                        input = arg;
                    }
                    else if (string.IsNullOrWhiteSpace(output))
                    {
                        output = arg;
                    }
                    else
                    {
                        throw new ArgumentException($"Unexpected argument '{arg}'.");
                    }

                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException("Input directory is required.");
        }

        if (string.IsNullOrWhiteSpace(output))
        {
            throw new ArgumentException("Output directory is required.");
        }

        var options = new LegacyMigrationOptions
        {
            InputDirectory = input!,
            OutputDirectory = output!,
            LogsDirectoryName = logsDirectory,
            FieldHistoryFileName = fieldHistoryFile,
        };

        return (options, verbose);
    }

    private static string RequireNext(IReadOnlyList<string> args, ref int index, string name)
    {
        index++;
        if (index >= args.Count)
        {
            throw new ArgumentException($"Missing value for {name}.");
        }

        return args[index];
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage: legacy-data-migrator migrate [options] <input> <output>");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -i, --input <path>       Legacy root directory containing logs and field history.");
        Console.WriteLine("  -o, --output <path>      Destination directory for Nexus-compatible assets.");
        Console.WriteLine("      --logs <name>        Name or relative path of the legacy logs directory (default: logs).");
        Console.WriteLine("      --history <file>     Field history CSV relative to the input directory (default: field-history.csv).");
        Console.WriteLine("  -v, --verbose            Emit detailed summary for migrated assets.");
        Console.WriteLine("  -h, --help               Display this usage information.");
    }

    private static void PrintReport(LegacyMigrationReport report, bool verbose)
    {
        Console.WriteLine($"Migrated data written to: {report.OutputDirectory}");
        Console.WriteLine($"  Pose samples: {report.PoseCount}");
        Console.WriteLine($"  IMU samples: {report.ImuCount}");
        Console.WriteLine($"  CAN frames: {report.CanCount}");
        Console.WriteLine($"  IO events: {report.SectionCount}");
        Console.WriteLine($"  Plugin events: {report.PluginCount}");
        Console.WriteLine($"  Weather snapshots: {report.WeatherCount}");

        if (report.FieldHistoryFields > 0 || verbose)
        {
            Console.WriteLine($"  Field history fields: {report.FieldHistoryFields}");
            Console.WriteLine($"  Field history entries: {report.FieldHistoryEntries}");
        }

        if (verbose && report.SkippedFiles.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Skipped legacy files:");
            foreach (var file in report.SkippedFiles)
            {
                Console.WriteLine($"  - {file}");
            }
        }
    }
}
