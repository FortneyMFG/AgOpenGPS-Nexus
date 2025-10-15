using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Aog.Tools.Qa.Checklist;
using Aog.Tools.Qa.Dashboard;
using Aog.Tools.Qa.Faults;
using Aog.Tools.Qa.Hil;
using Aog.Tools.Qa.Reports;

namespace Aog.Tools.Qa;

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length == 0 || ContainsHelp(args))
        {
            PrintUsage();
            return args.Length == 0 ? 1 : 0;
        }

        var command = args[0].ToLowerInvariant();
        var remaining = args.Skip(1).ToArray();

        try
        {
            return command switch
            {
                "checklist" => RunChecklist(remaining),
                "hil" => RunHil(remaining),
                "fault" => RunFault(remaining),
                "dashboard" => RunDashboard(remaining),
                "report" => RunReport(remaining),
                _ => UnknownCommand(command)
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            return 1;
        }
    }

    private static int RunChecklist(string[] args)
    {
        if (args.Length == 0 || ContainsHelp(args))
        {
            PrintChecklistUsage();
            return args.Length == 0 ? 1 : 0;
        }

        var subcommand = args[0].ToLowerInvariant();
        var (options, positionals) = ParseOptions(args.Skip(1));

        switch (subcommand)
        {
            case "template":
                var template = FieldSafetyChecklistTemplate.Create();
                var json = JsonSerializer.Serialize(template, Serialization.Options);
                var outputPath = RequireOption(options, positionals, "--output", allowPositional: true, required: false);
                if (string.IsNullOrWhiteSpace(outputPath))
                {
                    Console.WriteLine(json);
                }
                else
                {
                    WriteTextFile(outputPath, json);
                    Console.WriteLine($"Template written to {Path.GetFullPath(outputPath)}");
                }

                return 0;

            case "validate":
                var inputPath = RequireOption(options, positionals, "--input", allowPositional: true, required: true);
                using (var stream = File.OpenRead(inputPath))
                {
                    var checklist = JsonSerializer.Deserialize<FieldSafetyChecklist>(stream, Serialization.Options)
                        ?? throw new InvalidOperationException("Checklist file was empty.");
                    var result = FieldSafetyChecklistValidator.Validate(checklist);
                    if (!result.IsValid)
                    {
                        foreach (var error in result.Errors)
                        {
                            Console.Error.WriteLine(error);
                        }

                        return 1;
                    }
                }

                Console.WriteLine("Checklist is valid.");
                return 0;

            default:
                throw new InvalidOperationException($"Unknown checklist command '{subcommand}'.");
        }
    }

    private static int RunHil(string[] args)
    {
        if (args.Length == 0 || ContainsHelp(args))
        {
            PrintHilUsage();
            return args.Length == 0 ? 1 : 0;
        }

        var subcommand = args[0].ToLowerInvariant();
        var (options, positionals) = ParseOptions(args.Skip(1));

        switch (subcommand)
        {
            case "run":
                var configPath = RequireOption(options, positionals, "--config", allowPositional: true, required: true);
                var runner = new HilRigRunner();
                var result = runner.Run(configPath);
                var outputPath = RequireOption(options, positionals, "--output", allowPositional: false, required: false);
                var json = JsonSerializer.Serialize(result, Serialization.Options);

                if (string.IsNullOrWhiteSpace(outputPath))
                {
                    Console.WriteLine(json);
                }
                else
                {
                    WriteTextFile(outputPath, json);
                    Console.WriteLine($"Results written to {Path.GetFullPath(outputPath)}");
                }

                Console.WriteLine(result.Passed ? "All HIL assertions passed." : "One or more HIL assertions failed.");
                return result.Passed ? 0 : 2;

            default:
                throw new InvalidOperationException($"Unknown HIL command '{subcommand}'.");
        }
    }

    private static int RunFault(string[] args)
    {
        if (args.Length == 0 || ContainsHelp(args))
        {
            PrintFaultUsage();
            return args.Length == 0 ? 1 : 0;
        }

        var subcommand = args[0].ToLowerInvariant();
        var (options, positionals) = ParseOptions(args.Skip(1));

        switch (subcommand)
        {
            case "run":
                var scenarioPath = RequireOption(options, positionals, "--scenario", allowPositional: true, required: true);
                var runner = new FaultInjectionRunner();
                var schedule = runner.BuildSchedule(scenarioPath);
                var outputPath = RequireOption(options, positionals, "--output", allowPositional: false, required: false);
                var json = JsonSerializer.Serialize(schedule, Serialization.Options);

                if (string.IsNullOrWhiteSpace(outputPath))
                {
                    Console.WriteLine(json);
                }
                else
                {
                    WriteTextFile(outputPath, json);
                    Console.WriteLine($"Schedule written to {Path.GetFullPath(outputPath)}");
                }

                return 0;

            default:
                throw new InvalidOperationException($"Unknown fault command '{subcommand}'.");
        }
    }

    private static int RunDashboard(string[] args)
    {
        if (args.Length == 0 || ContainsHelp(args))
        {
            PrintDashboardUsage();
            return args.Length == 0 ? 1 : 0;
        }

        var subcommand = args[0].ToLowerInvariant();
        var (options, positionals) = ParseOptions(args.Skip(1));

        switch (subcommand)
        {
            case "aggregate":
                var inputDir = RequireOption(options, positionals, "--input", allowPositional: true, required: true);
                var outputPath = RequireOption(options, positionals, "--output", allowPositional: false, required: false);
                var aggregator = new QaDashboardAggregator();
                var dashboard = aggregator.Aggregate(inputDir);
                var json = JsonSerializer.Serialize(dashboard, Serialization.Options);

                if (string.IsNullOrWhiteSpace(outputPath))
                {
                    Console.WriteLine(json);
                }
                else
                {
                    WriteTextFile(outputPath, json);
                    Console.WriteLine($"Dashboard written to {Path.GetFullPath(outputPath)}");
                }

                return dashboard.AllPassed ? 0 : 2;

            case "harness":
                var specPath = RequireOption(options, positionals, "--spec", allowPositional: true, required: true);
                var observationPath = RequireOption(options, positionals, "--observations", allowPositional: false, required: true);
                var harnessOutput = RequireOption(options, positionals, "--output", allowPositional: false, required: false);
                var fileName = RequireOption(options, positionals, "--file", allowPositional: false, required: false);
                var scenarioOverride = RequireOption(options, positionals, "--scenario", allowPositional: false, required: false);

                var harness = new DashboardAutomationHarness();
                var harnessResult = harness.Run(new DashboardHarnessRequest
                {
                    SpecPath = specPath,
                    ObservationPath = observationPath,
                    OutputDirectory = harnessOutput,
                    OutputFileName = fileName,
                    ScenarioOverride = scenarioOverride
                });

                foreach (var finding in harnessResult.Findings)
                {
                    var prefix = finding.Severity switch
                    {
                        DashboardHarnessFindingSeverity.Error => "[ERROR]",
                        DashboardHarnessFindingSeverity.Warning => "[WARN]",
                        _ => "[INFO]"
                    };

                    Console.WriteLine($"{prefix} {finding.Message}");
                }

                Console.WriteLine($"Harness scenario '{harnessResult.MetricSet.Scenario}' produced {harnessResult.MetricSet.Metrics.Count} metrics.");
                if (!string.IsNullOrWhiteSpace(harnessOutput))
                {
                    Console.WriteLine($"Metrics emitted to {Path.GetFullPath(harnessOutput)}.");
                }

                return harnessResult.Passed ? 0 : 2;

            default:
                throw new InvalidOperationException($"Unknown dashboard command '{subcommand}'.");
        }
    }

    private static int RunReport(string[] args)
    {
        if (args.Length == 0 || ContainsHelp(args))
        {
            PrintReportUsage();
            return args.Length == 0 ? 1 : 0;
        }

        var subcommand = args[0].ToLowerInvariant();
        var (options, positionals) = ParseOptions(args.Skip(1));

        switch (subcommand)
        {
            case "generate":
                var metricsDir = RequireOption(options, positionals, "--metrics", allowPositional: true, required: true);
                var outputPath = RequireOption(options, positionals, "--output", allowPositional: false, required: true);
                var checklist = RequireOption(options, positionals, "--checklist", allowPositional: false, required: false);
                var faults = RequireOption(options, positionals, "--faults", allowPositional: false, required: false);

                var generator = new PostRunReportGenerator();
                var markdown = generator.Generate(new PostRunReportRequest
                {
                    MetricsDirectory = metricsDir,
                    ChecklistPath = checklist,
                    FaultSchedulePath = faults
                });

                WriteTextFile(outputPath, markdown);
                Console.WriteLine($"Report written to {Path.GetFullPath(outputPath)}");
                return 0;

            default:
                throw new InvalidOperationException($"Unknown report command '{subcommand}'.");
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

    private static (Dictionary<string, string>, List<string>) ParseOptions(IEnumerable<string> args)
    {
        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var positionals = new List<string>();
        var tokens = args.ToArray();

        for (var i = 0; i < tokens.Length; i++)
        {
            var token = tokens[i];
            if (token.StartsWith("--", StringComparison.Ordinal))
            {
                string key;
                string value;
                var equalsIndex = token.IndexOf('=');
                if (equalsIndex > 0)
                {
                    key = token[..equalsIndex];
                    value = token[(equalsIndex + 1)..];
                }
                else
                {
                    key = token;
                    if (i + 1 >= tokens.Length)
                    {
                        throw new ArgumentException($"Missing value for option '{token}'.");
                    }

                    value = tokens[++i];
                }

                options[key] = value;
            }
            else
            {
                positionals.Add(token);
            }
        }

        return (options, positionals);
    }

    private static string? RequireOption(Dictionary<string, string> options, List<string> positionals, string name, bool allowPositional, bool required)
    {
        if (options.TryGetValue(name, out var value))
        {
            return value;
        }

        if (allowPositional && positionals.Count > 0)
        {
            var positional = positionals[0];
            positionals.RemoveAt(0);
            return positional;
        }

        if (required)
        {
            throw new ArgumentException($"Option '{name}' is required.");
        }

        return null;
    }

    private static void WriteTextFile(string path, string contents)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, contents);
    }

    private static int UnknownCommand(string command)
    {
        throw new InvalidOperationException($"Unknown command '{command}'.");
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage: qa <command> [options]");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  checklist   Export or validate field safety checklists.");
        Console.WriteLine("  hil         Execute hardware-in-the-loop scenarios.");
        Console.WriteLine("  fault       Build fault injection schedules.");
        Console.WriteLine("  dashboard   Aggregate QA metrics into a dashboard JSON.");
        Console.WriteLine("  report      Generate post-run Markdown reports.");
    }

    private static void PrintChecklistUsage()
    {
        Console.WriteLine("Usage: qa checklist <template|validate> [options]");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  template [--output <file>]   Write the field safety checklist template to a file or stdout.");
        Console.WriteLine("  validate <file>              Validate a completed checklist file.");
    }

    private static void PrintHilUsage()
    {
        Console.WriteLine("Usage: qa hil run --config <file> [--output <file>]");
    }

    private static void PrintFaultUsage()
    {
        Console.WriteLine("Usage: qa fault run --scenario <file> [--output <file>]");
    }

    private static void PrintDashboardUsage()
    {
        Console.WriteLine("Usage: qa dashboard <aggregate|harness> [options]");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  aggregate --input <directory> [--output <file>]   Combine metric JSON files into a dashboard summary.");
        Console.WriteLine("  harness <spec> --observations <file> [--output <dir>] [--file <name>] [--scenario <name>]   ");
        Console.WriteLine("           Evaluate dashboard automation logs and emit QA metrics.");
    }

    private static void PrintReportUsage()
    {
        Console.WriteLine("Usage: qa report generate --metrics <directory> --output <file> [--checklist <file>] [--faults <file>]");
    }
}
