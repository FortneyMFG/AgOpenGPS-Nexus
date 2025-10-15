using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Aog.Tools.LegacyJobMigrator;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || ContainsHelp(args))
        {
            PrintUsage();
            return args.Length == 0 ? 1 : 0;
        }

        if (!string.Equals(args[0], "migrate", StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine($"error: unknown command '{args[0]}'.");
            PrintUsage();
            return 1;
        }

        try
        {
            var cliOptions = ParseOptions(args);
            var migrator = new LegacyJobMigrator();
            var reports = new List<LegacyJobMigrationReport>();

            if (cliOptions.IsDirectory)
            {
                var jobFiles = Directory.EnumerateFiles(cliOptions.InputPath, "job.json", SearchOption.AllDirectories).ToList();
                if (jobFiles.Count == 0)
                {
                    throw new FileNotFoundException($"No job.json files found under '{cliOptions.InputPath}'.");
                }

                foreach (var job in jobFiles)
                {
                    var report = await migrator.MigrateAsync(new LegacyJobMigrationOptions
                    {
                        JobFilePath = job,
                        OutputFilePath = job,
                        SeasonId = cliOptions.SeasonId,
                        SessionId = cliOptions.SessionId,
                        SessionName = cliOptions.SessionName,
                        OperatorIds = cliOptions.OperatorIds,
                        Force = cliOptions.Force,
                    }).ConfigureAwait(false);

                    reports.Add(report);
                    PrintReport(report);
                }
            }
            else
            {
                var report = await migrator.MigrateAsync(new LegacyJobMigrationOptions
                {
                    JobFilePath = cliOptions.InputPath,
                    OutputFilePath = cliOptions.OutputPath ?? cliOptions.InputPath,
                    SeasonId = cliOptions.SeasonId,
                    SessionId = cliOptions.SessionId,
                    SessionName = cliOptions.SessionName,
                    OperatorIds = cliOptions.OperatorIds,
                    Force = cliOptions.Force,
                }).ConfigureAwait(false);

                reports.Add(report);
                PrintReport(report);
            }

            var updatedCount = reports.Count(r => r.Updated);
            var skippedCount = reports.Count(r => r.Skipped);
            if (reports.Count > 1)
            {
                Console.WriteLine();
                Console.WriteLine($"Processed {reports.Count} job(s): {updatedCount} updated, {skippedCount} skipped.");
            }

            return 0;
        }
        catch (Exception ex) when (ex is ArgumentException or FileNotFoundException or DirectoryNotFoundException or JsonException)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
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

    private static CliOptions ParseOptions(IReadOnlyList<string> args)
    {
        string? input = null;
        string? output = null;
        string? season = null;
        string? sessionId = null;
        string? sessionName = null;
        var operators = new List<string>();
        var force = false;

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
                case "--season":
                    season = RequireNext(args, ref i, arg);
                    break;
                case "--session-id":
                    sessionId = RequireNext(args, ref i, arg);
                    break;
                case "--session-name":
                    sessionName = RequireNext(args, ref i, arg);
                    break;
                case "--operator":
                    operators.Add(RequireNext(args, ref i, arg));
                    break;
                case "--force":
                    force = true;
                    break;
                default:
                    if (string.IsNullOrWhiteSpace(input))
                    {
                        input = arg;
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
            throw new ArgumentException("Input path is required.");
        }

        var fullInput = Path.GetFullPath(input);
        var isDirectory = Directory.Exists(fullInput);
        var isFile = File.Exists(fullInput);

        if (!isDirectory && !isFile)
        {
            throw new FileNotFoundException($"Input path not found: {input}");
        }

        if (isDirectory && !string.IsNullOrWhiteSpace(output))
        {
            throw new ArgumentException("--output cannot be used when migrating a directory.");
        }

        var distinctOperators = operators
            .Where(o => !string.IsNullOrWhiteSpace(o))
            .Select(o => o.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new CliOptions(fullInput, output, season, sessionId, sessionName, distinctOperators, force, isDirectory);
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
        Console.WriteLine("Usage: legacy-job-migrator migrate [options] <job.json | directory>");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -i, --input <path>        Path to the legacy job.json or a directory containing jobs.");
        Console.WriteLine("  -o, --output <file>       Destination file when migrating a single job (default: in place).");
        Console.WriteLine("      --season <id>         Assign or override context.seasonId.");
        Console.WriteLine("      --session-id <id>     Override the generated session identifier.");
        Console.WriteLine("      --session-name <name> Override the generated session name.");
        Console.WriteLine("      --operator <id>       Seed activeOperators on the generated session (repeatable).");
        Console.WriteLine("      --force               Overwrite existing session entries if present.");
        Console.WriteLine("  -h, --help                Display this usage information.");
    }

    private static void PrintReport(LegacyJobMigrationReport report)
    {
        var jobName = Path.GetFileName(Path.GetDirectoryName(report.InputPath) ?? report.InputPath);
        if (report.Skipped)
        {
            Console.WriteLine($"{jobName}: already contained sessions, skipped.");
            return;
        }

        var seasonSuffix = report.SeasonAssigned ? " (season assigned)" : string.Empty;
        Console.WriteLine($"{jobName}: added session {report.SessionId ?? "<unchanged>"}{seasonSuffix}.");
    }

    private sealed record CliOptions(
        string InputPath,
        string? OutputPath,
        string? SeasonId,
        string? SessionId,
        string? SessionName,
        IReadOnlyList<string> OperatorIds,
        bool Force,
        bool IsDirectory);
}
