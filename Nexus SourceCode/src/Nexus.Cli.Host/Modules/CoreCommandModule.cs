using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.Extensions.DependencyInjection;
using Nexus.Cli.Host.Core.Endpoints;
using Nexus.Cli.Host.Core.Status;
using Nexus.Cli.Host.Output;
using Nexus.Plugin.Cli.Abstractions;
using Spectre.Console;

namespace Nexus.Cli.Host.Modules;

public sealed class CoreCommandModule : ICommandModule
{
    private static readonly Option<string?> EndpointOption = CreateEndpointOption();

    public void Configure(CommandModuleContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var coreCommand = new Command("core", "Interact with a running Nexus Core instance.");
        var statusCommand = new Command("status", "Check the health of the Nexus Core host.");

        statusCommand.AddOption(EndpointOption);

        statusCommand.SetHandler(async (InvocationContext invocationContext) =>
        {
            var services = context.Services;
            var resolver = services.GetRequiredService<ICoreEndpointResolver>();
            var probe = services.GetRequiredService<ICoreStatusProbe>();
            var presenter = services.GetRequiredService<CoreStatusPresenter>();
            var console = services.GetRequiredService<IAnsiConsole>();

            var endpointOverride = invocationContext.ParseResult.GetValueForOption(EndpointOption);
            var outputFormat = invocationContext.GetOutputFormat();
            var cancellationToken = invocationContext.GetCancellationTokenSafe();

            var resolution = await resolver.ResolveAsync(endpointOverride, cancellationToken).ConfigureAwait(false);
            var attempts = new List<CoreStatusResult>();

            foreach (var endpoint in resolution.Candidates)
            {
                var result = await probe.CheckAsync(endpoint, cancellationToken).ConfigureAwait(false);
                attempts.Add(result);

                if (result.State == CoreStatusState.Healthy)
                {
                    break;
                }
            }

            if (attempts.Count == 0)
            {
                attempts.Add(new CoreStatusResult(
                    resolution.Candidates[0],
                    CoreStatusState.Unknown,
                    null,
                    "No endpoints were probed.",
                    DateTimeOffset.UtcNow));
            }

            var summary = new CoreStatusSummary(resolution, attempts);
            presenter.Render(summary, outputFormat, console);

            invocationContext.ExitCode = summary.FinalState == CoreStatusState.Healthy ? 0 : 2;
        });

        coreCommand.AddCommand(statusCommand);
        context.RootCommand.AddCommand(coreCommand);
    }

    private static Option<string?> CreateEndpointOption()
    {
        var option = new Option<string?>(new[] { "--endpoint", "-e" }, () => null, "Override the Nexus Core endpoint.")
        {
            ArgumentHelpName = "endpoint",
        };

        option.AddCompletions(
            "unix://~/.nexus/run/core.sock",
            "pipe://nexus-core",
            "https://127.0.0.1:5157");

        return option;
    }
}
