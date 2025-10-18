using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Text.Json;
using Nexus.Cli.Host.Output;
using Nexus.Cli.Host.Plugins.Reflection;
using Spectre.Console;
using PluginCommandContext = Nexus.Plugin.Cli.Abstractions.CommandContext;
using PluginCommandHandler = Nexus.Plugin.Cli.Abstractions.ICommandHandler;

namespace Nexus.Cli.Host.Modules;

/// <summary>
/// Provides commands for interacting with plugin reflection endpoints.
/// </summary>
public sealed class PluginReflectionCommandModule : PluginCommandHandler
{
    private readonly IPluginReflectionClient _client;
    private readonly IAnsiConsole _console;

    public PluginReflectionCommandModule(IPluginReflectionClient client, IAnsiConsole console)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _console = console ?? throw new ArgumentNullException(nameof(console));
    }

    /// <inheritdoc />
    public void Configure(PluginCommandContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var pluginCommand = new Command("plugin", "Inspect and interact with Nexus plugins.");
        pluginCommand.AddCommand(CreateReflectCommand(context));

        context.RootCommand.AddCommand(pluginCommand);
    }

    private Command CreateReflectCommand(PluginCommandContext context)
    {
        var command = new Command("reflect", "Query a plugin reflection endpoint and list contributed verbs.");
        var endpointOption = new Option<Uri>("--endpoint", "The gRPC endpoint hosting the plugin reflection service.")
        {
            IsRequired = true,
        };

        command.AddOption(endpointOption);

        command.SetHandler(async (InvocationContext invocationContext) =>
        {
            var endpoint = invocationContext.ParseResult.GetValueForOption(endpointOption);
            var format = invocationContext.GetOutputFormat();
            var cancellationToken = invocationContext.GetCancellationTokenSafe();

            IReadOnlyList<PluginVerb> verbs;
            try
            {
                verbs = await _client.ListVerbsAsync(endpoint, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _console.MarkupLineInterpolated($"[red]Failed to query plugin reflection endpoint: {Markup.Escape(ex.Message)}[/]");
                invocationContext.ExitCode = 1;
                return;
            }

            if (format == OutputFormat.Human)
            {
                RenderHuman(verbs);
            }
            else
            {
                RenderStructured(format, verbs, invocationContext.Console);
            }

            invocationContext.ExitCode = 0;
        });

        return command;
    }

    private void RenderHuman(IReadOnlyList<PluginVerb> verbs)
    {
        if (verbs.Count == 0)
        {
            _console.MarkupLine("[yellow]No verbs were returned by the reflection service.[/]");
            return;
        }

        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("Verb");
        table.AddColumn("Description");
        table.AddColumn("Options");

        foreach (var verb in verbs)
        {
            var optionSummary = verb.Options.Count == 0
                ? "(none)"
                : string.Join("\n", verb.Options.Select(option =>
                    $"[bold]{option.Name}[/] ({option.Kind.ToString().ToLowerInvariant()}){(option.Required ? "*" : string.Empty)} - {option.Description}"));

            table.AddRow($"[green]{Markup.Escape(verb.Name)}[/]", Markup.Escape(verb.Description), optionSummary);
        }

        _console.Write(table);
    }

    private static void RenderStructured(OutputFormat format, IReadOnlyList<PluginVerb> verbs, IConsole console)
    {
        var payload = verbs.Select(verb => new
        {
            verb.Name,
            verb.Description,
            options = verb.Options.Select(option => new
            {
                option.Name,
                option.Description,
                option.Required,
                Kind = option.Kind.ToString(),
                option.DefaultValue,
            }),
        });

        var options = new JsonSerializerOptions
        {
            WriteIndented = format == OutputFormat.Json,
        };

        var json = JsonSerializer.Serialize(payload, options);
        console.Out.Write(json);
        console.Out.Write(Environment.NewLine);
    }
}
