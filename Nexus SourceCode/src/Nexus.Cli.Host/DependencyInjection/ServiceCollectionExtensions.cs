using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nexus.Cli.Host.Core.Endpoints;
using Nexus.Cli.Host.Core.Status;
using Aog.Plugins;
using Nexus.Cli.Host.Host;
using Nexus.Cli.Host.Modules;
using Nexus.Cli.Host.Output;
using Nexus.Cli.Host.Plugins;
using Nexus.Cli.Host.Runtime;
using Nexus.Plugin.Cli.Abstractions;
using Spectre.Console;

namespace Nexus.Cli.Host;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNxCliHost(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IAnsiConsole>(_ => AnsiConsole.Create(new AnsiConsoleSettings()));
        services.TryAddSingleton<INexusEnvironment, NexusEnvironment>();
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<ICoreEndpointResolver, CoreEndpointResolver>();
        services.TryAddSingleton<ICoreStatusProbe, CoreStatusProbe>();
        services.TryAddSingleton<CoreStatusPresenter>();
        services.TryAddSingleton<IHostInfoProvider, HostInfoProvider>();
        services.TryAddSingleton<HostInfoPresenter>();
        services.TryAddSingleton<PluginManifestLoader>();
        services.TryAddSingleton<IPluginCommandModuleLoader, PluginCommandModuleLoader>();

        services.TryAddEnumerable(ServiceDescriptor.Singleton<ICommandModule, HostInfoCommandModule>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ICommandModule, CoreCommandModule>());

        services.AddSingleton<RootCommandFactory>();
        services.AddSingleton(provider => provider.GetRequiredService<RootCommandFactory>().Create());
        services.AddSingleton<NxApplication>();

        return services;
    }
}
