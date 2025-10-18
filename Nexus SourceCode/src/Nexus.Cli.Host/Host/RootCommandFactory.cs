using System.CommandLine;
using System.Threading;
using Nexus.Cli.Host.Modules;
using Nexus.Cli.Host.Output;
using Nexus.Cli.Host.Plugins;
using CliCommandContext = Nexus.Plugin.Cli.Abstractions.CommandContext;
using CliCommandHandler = Nexus.Plugin.Cli.Abstractions.ICommandHandler;

namespace Nexus.Cli.Host.Host;

public sealed class RootCommandFactory
{
    private readonly IServiceProvider _services;
    private readonly IEnumerable<CliCommandHandler> _modules;
    private readonly IPluginCommandModuleLoader _pluginModuleLoader;

    public RootCommandFactory(
        IServiceProvider services,
        IEnumerable<CliCommandHandler> modules,
        IPluginCommandModuleLoader pluginModuleLoader)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _modules = modules ?? throw new ArgumentNullException(nameof(modules));
        _pluginModuleLoader = pluginModuleLoader ?? throw new ArgumentNullException(nameof(pluginModuleLoader));
    }

    public RootCommand Create()
    {
        var rootCommand = new RootCommand("Nexus CLI host for Core and plugin automation.")
        {
            Name = "nx",
            TreatUnmatchedTokensAsErrors = true,
        };

        rootCommand.AddGlobalOption(OutputOptions.ModeOption);

        var context = new CliCommandContext(rootCommand, _services, OutputOptions.ModeOption);

        foreach (var module in _modules)
        {
            module.Configure(context);
        }

        var pluginModules = _pluginModuleLoader.LoadModulesAsync(CancellationToken.None)
            .GetAwaiter().GetResult();

        foreach (var module in pluginModules)
        {
            module.Configure(context);
        }

        return rootCommand;
    }
}
