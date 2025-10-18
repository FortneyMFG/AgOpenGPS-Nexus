using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Aog.Tools.PluginCompliance;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || string.Equals(args[0], "help", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(args[0], "-h", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(args[0], "--help", StringComparison.OrdinalIgnoreCase))
        {
            PrintUsage();
            return args.Length == 0 ? 1 : 0;
        }

        string repoRoot;
        try
        {
            repoRoot = LocateRepositoryRoot();
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            return 1;
        }

        var command = args[0].ToLowerInvariant();
        var options = args.Skip(1).ToArray();

        try
        {
            return command switch
            {
                "lint" => await RunLintAsync(repoRoot, options).ConfigureAwait(false),
                "capabilities" => await RunCapabilitiesAsync(repoRoot, options).ConfigureAwait(false),
                _ => UnknownCommand(command)
            };
        }
        catch (Exception ex) when (ex is ArgumentException or DirectoryNotFoundException or InvalidDataException)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            return 1;
        }
    }

    private static int UnknownCommand(string command)
    {
        Console.Error.WriteLine($"error: unknown command '{command}'.");
        PrintUsage();
        return 1;
    }

    private static async Task<int> RunLintAsync(string repoRoot, string[] args)
    {
        var parameters = new List<string>(args);
        var manifestRoot = ResolveOptionPath(parameters, repoRoot, "--manifests", Path.Combine(repoRoot, "docs", "plugins", "manifests"));
        var baselineRoot = ResolveOptionPath(parameters, repoRoot, "--baselines", Path.Combine(repoRoot, "Nexus SourceCode", "tests", "Aog.Plugins.Tests", "Compatibility", "Baselines"));

        if (parameters.Count > 0)
        {
            throw new ArgumentException($"Unexpected argument(s): {string.Join(' ', parameters)}");
        }

        var checker = new PluginManifestComplianceChecker();
        var result = await checker.LintAsync(manifestRoot, baselineRoot).ConfigureAwait(false);

        if (result.Diagnostics.Count == 0)
        {
            Console.WriteLine("All plugin manifests passed compliance checks.");
            return 0;
        }

        foreach (var diagnostic in result.Diagnostics)
        {
            var displayPath = FormatRelativePath(repoRoot, diagnostic.ManifestPath);
            Console.Error.WriteLine($"[{diagnostic.Severity}] {displayPath}: {diagnostic.Message}");
        }

        return result.IsSuccess ? 0 : 1;
    }

    private static async Task<int> RunCapabilitiesAsync(string repoRoot, string[] args)
    {
        var parameters = new List<string>(args);
        var manifestRoot = ResolveOptionPath(parameters, repoRoot, "--manifests", Path.Combine(repoRoot, "docs", "plugins", "manifests"));
        var pluginFilter = ExtractOptionValue(parameters, "--plugin");
        var format = ExtractOptionValue(parameters, "--format") ?? "json";
        var outputPath = ExtractOptionValue(parameters, "--output");

        if (parameters.Count > 0)
        {
            throw new ArgumentException($"Unexpected argument(s): {string.Join(' ', parameters)}");
        }

        var generator = new CapabilityReportGenerator();
        var entries = await generator.GenerateAsync(manifestRoot, pluginFilter).ConfigureAwait(false);

        if (entries.Count == 0)
        {
            if (!string.IsNullOrWhiteSpace(pluginFilter))
            {
                Console.Error.WriteLine($"No manifests matched plugin filter '{pluginFilter}'.");
            }
            else
            {
                Console.Error.WriteLine("No plugin manifests were found.");
            }

            return 1;
        }

        switch (format.ToLowerInvariant())
        {
            case "json":
                await EmitJsonAsync(entries, outputPath).ConfigureAwait(false);
                break;
            case "text":
                EmitText(entries, repoRoot, outputPath);
                break;
            default:
                throw new ArgumentException($"Unknown capabilities output format '{format}'.");
        }

        return 0;
    }

    private static async Task EmitJsonAsync(IReadOnlyList<PluginCapabilityReport> entries, string? outputPath)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

        await using var destination = ResolveOutputStream(outputPath);
        await JsonSerializer.SerializeAsync(destination, entries, options).ConfigureAwait(false);
        await destination.FlushAsync().ConfigureAwait(false);
    }

    private static void EmitText(IReadOnlyList<PluginCapabilityReport> entries, string repoRoot, string? outputPath)
    {
        var builder = new StringBuilder();
        foreach (var entry in entries)
        {
            builder.AppendLine($"{entry.Name} ({entry.Version}) [{entry.Id}]");
            builder.AppendLine($"  Minimum runtime: {entry.MinimumRuntimeVersion}");
            builder.AppendLine($"  Manifest: {entry.Manifest}");
            foreach (var capability in entry.Capabilities)
            {
                builder.AppendLine($"  - {capability.Capability} — {capability.Mode} (timeout: {capability.TimeoutSeconds}s, recovery: {capability.RecoveryStrategy})");
            }

            builder.AppendLine();
        }

        var text = builder.ToString().TrimEnd();

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            Console.WriteLine(text);
        }
        else
        {
            var resolved = Path.GetFullPath(outputPath, repoRoot);
            var directory = Path.GetDirectoryName(resolved);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(resolved, text + Environment.NewLine);
        }
    }

    private static Stream ResolveOutputStream(string? outputPath)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            return Console.OpenStandardOutput();
        }

        var fullPath = Path.GetFullPath(outputPath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
    }

    private static string ResolveOptionPath(ICollection<string> parameters, string repoRoot, string optionName, string defaultPath)
    {
        var value = ExtractOptionValue(parameters, optionName);
        var path = value ?? defaultPath;
        if (!Path.IsPathRooted(path))
        {
            path = Path.GetFullPath(path, repoRoot);
        }

        return path;
    }

    private static string? ExtractOptionValue(ICollection<string> parameters, string optionName)
    {
        var list = parameters.ToList();
        for (var i = 0; i < list.Count; i++)
        {
            var argument = list[i];
            if (string.Equals(argument, optionName, StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= list.Count)
                {
                    throw new ArgumentException($"Option '{optionName}' is missing a value.");
                }

                var value = list[i + 1];
                parameters.Remove(argument);
                parameters.Remove(value);
                return value;
            }

            if (argument.StartsWith(optionName + "=", StringComparison.OrdinalIgnoreCase))
            {
                parameters.Remove(argument);
                return argument[(optionName.Length + 1)..];
            }
        }

        return null;
    }

    private static string FormatRelativePath(string repoRoot, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "<unknown>";
        }

        var fullPath = Path.GetFullPath(path);
        var relative = Path.GetRelativePath(repoRoot, fullPath);
        return relative.Replace(Path.DirectorySeparatorChar, '/');
    }

    private static string LocateRepositoryRoot()
    {
        var path = AppContext.BaseDirectory;
        for (var i = 0; i < 10; i++)
        {
            if (File.Exists(Path.Combine(path, "tasks.md")))
            {
                return path;
            }

            var parent = Directory.GetParent(path);
            if (parent is null)
            {
                break;
            }

            path = parent.FullName;
        }

        throw new InvalidOperationException("Failed to locate repository root (tasks.md not found).");
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Nexus plugin manifest governance utility");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  nexus-plugin <command> [options]");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  lint                Validate manifests against recorded baselines.");
        Console.WriteLine("  capabilities        Emit capability and lease data from manifests.");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --manifests <path>  Override manifest root (default: docs/plugins/manifests).");
        Console.WriteLine("  --baselines <path>  Override baseline root when linting.");
        Console.WriteLine("  --plugin <id|name>  Filter capabilities report to a specific plugin.");
        Console.WriteLine("  --format <type>     Capabilities output format (json, text). Default: json.");
        Console.WriteLine("  --output <path>     Write capabilities output to file instead of stdout.");
    }
}
