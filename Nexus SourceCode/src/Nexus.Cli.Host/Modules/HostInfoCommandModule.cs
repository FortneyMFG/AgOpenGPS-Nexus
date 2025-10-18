using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.Extensions.DependencyInjection;
using Nexus.Cli.Host.Host;
using Nexus.Cli.Host.Output;
using CliCommandContext = Nexus.Plugin.Cli.Abstractions.CommandContext;
using CliCommandHandler = Nexus.Plugin.Cli.Abstractions.ICommandHandler;
using Spectre.Console;

namespace Nexus.Cli.Host.Modules;

public sealed class HostInfoCommandModule : CliCommandHandler
{
    private readonly IHostInfoProvider _infoProvider;
    private readonly HostInfoPresenter _presenter;

    public HostInfoCommandModule(IHostInfoProvider infoProvider, HostInfoPresenter presenter)
    {
        _infoProvider = infoProvider ?? throw new ArgumentNullException(nameof(infoProvider));
        _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
    }

    public void Configure(CliCommandContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var hostCommand = new Command("host", "Built-in Nexus host commands.");
        var infoCommand = new Command("info", "Display details about the Nexus CLI host environment.");

        infoCommand.SetHandler((InvocationContext invocationContext) =>
        {
            var hostInfo = _infoProvider.Create();
            var console = context.Services.GetRequiredService<IAnsiConsole>();
            var format = invocationContext.GetOutputFormat();
            _presenter.Render(hostInfo, format, console);
        });

        hostCommand.AddCommand(infoCommand);
        context.RootCommand.AddCommand(hostCommand);
    }
}
