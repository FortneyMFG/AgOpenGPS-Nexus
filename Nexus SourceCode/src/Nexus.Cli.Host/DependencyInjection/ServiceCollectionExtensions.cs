using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nexus.Cli.Host.Host;
using Nexus.Cli.Host.Modules;
using Nexus.Cli.Host.Output;
using Nexus.Cli.Host.Runtime;
using Spectre.Console;

namespace Nexus.Cli.Host;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNxCliHost(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IAnsiConsole>(_ => AnsiConsole.Create(new AnsiConsoleSettings()));
        services.TryAddSingleton<INexusEnvironment, NexusEnvironment>();
        services.TryAddSingleton<IHostInfoProvider, HostInfoProvider>();
        services.TryAddSingleton<HostInfoPresenter>();

        services.TryAddEnumerable(ServiceDescriptor.Singleton<ICommandModule, HostInfoCommandModule>());

        services.AddSingleton<RootCommandFactory>();
        services.AddSingleton(provider => provider.GetRequiredService<RootCommandFactory>().Create());
        services.AddSingleton<NxApplication>();

        return services;
    }
}
