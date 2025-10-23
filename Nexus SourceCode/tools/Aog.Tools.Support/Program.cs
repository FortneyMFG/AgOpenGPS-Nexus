using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Aog.Tools.Support;

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length == 0 || IsHelp(args[0]))
        {
            PrintUsage();
            return args.Length == 0 ? 1 : 0;
        }

        if (args.Length < 2 ||
            !string.Equals(args[0], "feedback", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(args[1], "aggregate", StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine("error: expected 'feedback aggregate'.");
            PrintUsage();
            return 1;
        }

        try
        {
            var (input, output, windowDays) = ParseAggregateArguments(args);
            var report = new FieldFeedbackAggregator().Aggregate(input, windowDays, now: null);

            var outputPath = output ?? Path.Combine(Environment.CurrentDirectory, "feedback-dashboard.json");
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var stream = File.Create(outputPath);
            var serializerOptions = new JsonSerializerOptions(FieldFeedbackAggregator.SerializerOptions)
            {
                WriteIndented = true,
            };
            JsonSerializer.Serialize(stream, report, serializerOptions);
            Console.WriteLine(outputPath);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            return 1;
        }
    }

    private static bool IsHelp(string value)
    {
        return string.Equals(value, "-h", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, "--help", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, "help", StringComparison.OrdinalIgnoreCase);
    }

    private static (string Input, string? Output, int? WindowDays) ParseAggregateArguments(IReadOnlyList<string> args)
    {
        string? input = null;
        string? output = null;
        int? windowDays = null;

        for (var i = 2; i < args.Count; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "--input":
                    input = RequireNext(args, ref i, "--input");
                    break;
                case "--output":
                    output = RequireNext(args, ref i, "--output");
                    break;
                case "--window-days":
                    windowDays = ParseInt(RequireNext(args, ref i, "--window-days"));
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
            throw new ArgumentException("Input directory must be specified via --input or positional argument.");
        }

        return (input, output, windowDays);
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
        if (!int.TryParse(value, out var result))
        {
            throw new ArgumentException($"Invalid integer value '{value}'.");
        }

        return result;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage: support-tool feedback aggregate [--input <dir>] [--output <file>] [--window-days <days>]");
        Console.WriteLine();
        Console.WriteLine("Aggregates anonymized field feedback telemetry JSON/JSONL files into dashboard-ready summaries.");
        Console.WriteLine("If positional arguments are supplied, the first is treated as the input directory and the second as the output file.");
    }
}
