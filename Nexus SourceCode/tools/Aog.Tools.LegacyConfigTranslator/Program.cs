using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Aog.Core.Legacy;
using Aog.Plugins.AutoSteer;

namespace Aog.Tools.LegacyConfigTranslator;

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
        try
        {
            switch (command.ToLowerInvariant())
            {
                case "translate":
                    return await RunTranslateAsync(args).ConfigureAwait(false);
                case "soak":
                    return await RunSoakAsync(args).ConfigureAwait(false);
                default:
                    Console.Error.WriteLine($"error: unknown command '{command}'.");
                    PrintUsage();
                    return 1;
            }
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

    private static async Task<int> RunTranslateAsync(string[] args)
    {
        string? inputPath = null;
        string? outputPath = null;
        var indent = true;

        for (var i = 1; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "--input":
                case "-i":
                    inputPath = RequireNext(args, ref i, arg);
                    break;
                case "--output":
                case "-o":
                    outputPath = RequireNext(args, ref i, arg);
                    break;
                case "--no-indent":
                    indent = false;
                    break;
                default:
                    if (inputPath is null)
                    {
                        inputPath = arg;
                    }
                    else if (outputPath is null)
                    {
                        outputPath = arg;
                    }
                    else
                    {
                        throw new ArgumentException($"Unexpected argument '{arg}'.");
                    }
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(inputPath))
        {
            throw new ArgumentException("Input settings file is required.");
        }

        var loader = new LegacySettingsLoader();
        var settings = loader.Load(inputPath);

        var translator = new V6MachineProfileTranslator();
        var machine = translator.Translate(settings);
        var tuning = AutoSteerLiteTuningCalculator.BuildProfile(machine);

        var report = LegacyTranslationReport.Create(inputPath!, machine, tuning, settings.SectionCount);

        var serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = indent,
        };

        var json = JsonSerializer.Serialize(report, serializerOptions);

        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(outputPath!));
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(outputPath!, json).ConfigureAwait(false);
        }
        else
        {
            Console.WriteLine(json);
        }

        return 0;
    }

    private static async Task<int> RunSoakAsync(string[] args)
    {
        double durationSeconds = 60;
        double udpRate = 200;
        double serialRate = 200;
        string? outputPath = null;

        for (var i = 1; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "--seconds":
                case "-s":
                    durationSeconds = double.Parse(RequireNext(args, ref i, arg), CultureInfo.InvariantCulture);
                    break;
                case "--udp-rate":
                    udpRate = double.Parse(RequireNext(args, ref i, arg), CultureInfo.InvariantCulture);
                    break;
                case "--serial-rate":
                    serialRate = double.Parse(RequireNext(args, ref i, arg), CultureInfo.InvariantCulture);
                    break;
                case "--output":
                case "-o":
                    outputPath = RequireNext(args, ref i, arg);
                    break;
                default:
                    throw new ArgumentException($"Unexpected argument '{arg}'.");
            }
        }

        if (durationSeconds <= 0)
        {
            throw new ArgumentException("Duration must be positive.");
        }

        if (udpRate <= 0 || serialRate <= 0)
        {
            throw new ArgumentException("Rates must be positive.");
        }

        var runner = new LegacySoakRunner();
        var report = await runner.RunAsync(new LegacySoakOptions(durationSeconds, udpRate, serialRate)).ConfigureAwait(false);

        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });

        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(outputPath!));
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(outputPath!, json).ConfigureAwait(false);
        }

        Console.WriteLine("Legacy soak report");
        Console.WriteLine($"  UDP frames: {report.Udp.TotalFrames} @ {report.Udp.EffectiveRateHz:F1} Hz (target {report.Udp.TargetRatePerStreamHz:F1} Hz/stream)");
        Console.WriteLine($"  Serial frames: {report.Serial.FramesDecoded} decoded, {report.Serial.DecodeFailures} failures");

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            Console.WriteLine();
            Console.WriteLine(json);
        }

        return 0;
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
        Console.WriteLine("Usage: legacy-tool <command> [options]");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  translate   Convert a legacy V6 vehicle XML into a Nexus machine profile package.");
        Console.WriteLine("  soak        Run high-rate UDP and serial stress tests and emit a soak report.");
        Console.WriteLine();
        Console.WriteLine("translate options:");
        Console.WriteLine("  --input <file>    Path to the legacy XML settings file (positional arg also accepted).");
        Console.WriteLine("  --output <file>   Write the JSON report to the specified file (defaults to stdout).");
        Console.WriteLine("  --no-indent       Emit minified JSON without indentation.");
        Console.WriteLine();
        Console.WriteLine("soak options:");
        Console.WriteLine("  --seconds <n>     Duration of the stress run in seconds (default 60).");
        Console.WriteLine("  --udp-rate <hz>   Target UDP frames per stream per second (default 200).");
        Console.WriteLine("  --serial-rate <hz> Target encoded serial frames per second (default 200).");
        Console.WriteLine("  --output <file>   Write the soak report JSON to a file instead of stdout.");
    }
}
