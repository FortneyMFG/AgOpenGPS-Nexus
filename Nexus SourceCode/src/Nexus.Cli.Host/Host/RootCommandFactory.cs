using System.CommandLine;
using Nexus.Cli.Host.Modules;
using Nexus.Cli.Host.Output;

namespace Nexus.Cli.Host.Host;

public sealed class RootCommandFactory
{
    private readonly IServiceProvider _services;
    private readonly IEnumerable<ICommandModule> _modules;

    public RootCommandFactory(IServiceProvider services, IEnumerable<ICommandModule> modules)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _modules = modules ?? throw new ArgumentNullException(nameof(modules));
    }

    public RootCommand Create()
    {
        var rootCommand = new RootCommand("Nexus CLI host for Core and plugin automation.")
        {
            Name = "nx",
            TreatUnmatchedTokensAsErrors = true,
        };

        rootCommand.AddGlobalOption(OutputOptions.ModeOption);

        var context = new CommandModuleContext(rootCommand, _services, OutputOptions.ModeOption);

        foreach (var module in _modules)
        {
            module.Configure(context);
        }

        return rootCommand;
    }
}
