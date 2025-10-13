using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Aog.Agio.Safety;

namespace Aog.Tools.SafetyLog;

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length == 0 || ContainsHelp(args))
        {
            PrintUsage();
            return args.Length == 0 ? 1 : 0;
        }

        var command = args[0];
        if (!string.Equals(command, "export", StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine($"error: unknown command '{command}'.");
            PrintUsage();
            return 1;
        }

        try
        {
            var parameters = ParseExportArguments(args);
            var defaults = new SafetyLogOptions();

            var options = new SafetyLogOptions
            {
                Directory = parameters.LogDirectory,
                RetentionDays = parameters.RetentionDays ?? defaults.RetentionDays,
                MaxFiles = parameters.MaxFiles ?? defaults.MaxFiles,
            };

            if (string.IsNullOrWhiteSpace(options.Directory))
            {
                throw new ArgumentException("Log directory is required.");
            }

            var exporter = new FileSafetyLog(options);
            var archive = exporter.Export(parameters.OutputDirectory ?? Environment.CurrentDirectory);
            Console.WriteLine(archive);
            return 0;
        }
        catch (Exception ex)
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

    private static ExportParameters ParseExportArguments(IReadOnlyList<string> args)
    {
        string? logDir = null;
        string? outputDir = null;
        int? retentionDays = null;
        int? maxFiles = null;

        for (var i = 1; i < args.Count; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "--log-dir":
                    logDir = RequireNext(args, ref i, "--log-dir");
                    break;
                case "--output":
                    outputDir = RequireNext(args, ref i, "--output");
                    break;
                case "--retention-days":
                    retentionDays = ParseInt(RequireNext(args, ref i, "--retention-days"));
                    break;
                case "--max-files":
                    maxFiles = ParseInt(RequireNext(args, ref i, "--max-files"));
                    break;
                default:
                    if (string.IsNullOrWhiteSpace(logDir))
                    {
                        logDir = arg;
                    }
                    else if (string.IsNullOrWhiteSpace(outputDir))
                    {
                        outputDir = arg;
                    }
                    else
                    {
                        throw new ArgumentException($"Unexpected argument '{arg}'.");
                    }
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(logDir))
        {
            throw new ArgumentException("Log directory must be specified via --log-dir or positional argument.");
        }

        return new ExportParameters(logDir, outputDir, retentionDays, maxFiles);
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

    private static int ParseInt(string value)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
        {
            throw new ArgumentException($"Invalid integer value '{value}'.");
        }

        return result;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage: safetylog export [--log-dir <path>] [--output <path>] [--retention-days <days>] [--max-files <count>]");
        Console.WriteLine();
        Console.WriteLine("If positional arguments are supplied, the first is treated as the log directory and the second as the output directory.");
        Console.WriteLine("The command prints the absolute path of the generated archive on success.");
    }

    private sealed record ExportParameters(string LogDirectory, string? OutputDirectory, int? RetentionDays, int? MaxFiles);
}
