using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;
using System.Text.Json;
using Nexus.Plugin.Cli.Abstractions;
using CliInvocationContext = System.CommandLine.Invocation.InvocationContext;

namespace Nexus.SamplePlugin.Cli;

/// <summary>
/// Provides sample <c>nx sample</c> verbs for calibration and sniff workflows.
/// </summary>
public sealed class SampleCalibrationCommandModule : ICommandModule
{
    /// <inheritdoc />
    public void Configure(CommandModuleContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var descriptor = context.Services.GetService(typeof(PluginCommandModuleDescriptor)) as PluginCommandModuleDescriptor;
        var pluginCommand = new Command("sample", descriptor?.PluginName ?? "Sample CLI plugin verbs")
        {
            TreatUnmatchedTokensAsErrors = true,
        };

        pluginCommand.AddCommand(CreateCalibrateCommand(context));
        pluginCommand.AddCommand(CreateSniffCommand());

        context.RootCommand.AddCommand(pluginCommand);
    }

    private static Command CreateCalibrateCommand(CommandModuleContext context)
    {
        var command = new Command("calibrate", "Calibrate the sample grain flow sensor using static weight checks.");

        var offsetOption = new Option<double>("--offset", () => 0d, "Baseline offset in kg/s gleaned from the static calibration block.")
        {
            ArgumentHelpName = "kg/s",
        };
        var gainOption = new Option<double>("--gain", () => 1d, "Gain multiplier applied to the live flow sensor stream.")
        {
            ArgumentHelpName = "ratio",
        };
        var dryRunOption = new Option<bool>("--dry-run", "Skip writing results; preview expected coefficients only.");

        command.AddOption(offsetOption);
        command.AddOption(gainOption);
        command.AddOption(dryRunOption);

        command.SetHandler(invocationContext =>
        {
            var offset = invocationContext.ParseResult.GetValueForOption(offsetOption);
            var gain = invocationContext.ParseResult.GetValueForOption(gainOption);
            var dryRun = invocationContext.ParseResult.GetValueForOption(dryRunOption);
            var outputMode = ResolveOutputMode(context, invocationContext);

            if (outputMode == "json" || outputMode == "ndjson")
            {
                var payload = new
                {
                    command = "calibrate",
                    dryRun,
                    coefficients = new
                    {
                        offset,
                        gain,
                    },
                    timestamp = DateTimeOffset.UtcNow,
                };

                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    WriteIndented = outputMode == "json",
                });

                invocationContext.Console.WriteLine(json);
            }
            else
            {
                invocationContext.Console.WriteLine($"Applying calibration: offset={offset:F3} kg/s, gain={gain:F3}.");
                if (dryRun)
                {
                    invocationContext.Console.WriteLine("Dry-run enabled, coefficients were not persisted.");
                }
                else
                {
                    invocationContext.Console.WriteLine("Calibration persisted to the plugin state cache.");
                }
            }
        });

        return command;
    }

    private static Command CreateSniffCommand()
    {
        var command = new Command("sniff", "Capture a diagnostic trace of flow samples for offline analysis.");

        var durationOption = new Option<int>("--duration", () => 15, "Capture duration in seconds.")
        {
            ArgumentHelpName = "seconds",
        };
        var outputOption = new Option<string>("--file", () => "samples.ndjson", "Destination file for captured samples.")
        {
            ArgumentHelpName = "path",
        };

        command.AddOption(durationOption);
        command.AddOption(outputOption);

        command.SetHandler(invocationContext =>
        {
            var duration = invocationContext.ParseResult.GetValueForOption(durationOption);
            var file = invocationContext.ParseResult.GetValueForOption(outputOption);

            invocationContext.Console.WriteLine(
                $"Sniffing raw samples for {duration} second(s). Output will be appended to '{file}'.");
            invocationContext.Console.WriteLine("Use the bundled Jupyter notebook to visualize captured flow curves.");
        });

        return command;
    }

    private static string ResolveOutputMode(CommandModuleContext context, CliInvocationContext invocationContext)
    {
        if (context.OutputOption is null)
        {
            return "human";
        }

        var outputOption = context.OutputOption;
        var parseResult = invocationContext.ParseResult;
        var method = typeof(ParseResult)
            .GetMethods()
            .FirstOrDefault(m => m.Name == nameof(ParseResult.GetValueForOption) && m.IsGenericMethod);

        if (method is null)
        {
            return "human";
        }

        try
        {
            var generic = method.MakeGenericMethod(outputOption.ValueType);
            var value = generic.Invoke(parseResult, new object?[] { outputOption });
            return value?.ToString()?.ToLowerInvariant() ?? "human";
        }
        catch
        {
            return "human";
        }
    }
}
