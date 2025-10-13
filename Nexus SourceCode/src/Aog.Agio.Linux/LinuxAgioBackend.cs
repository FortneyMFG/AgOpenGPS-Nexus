using System;
using Microsoft.Extensions.DependencyInjection;

namespace Aog.Agio.Linux;

/// <summary>
/// Linux-specific AGiO backend that surfaces SocketCAN interfaces over gRPC.
/// </summary>
public sealed class LinuxAgioBackend : IAgioBackend
{
    /// <inheritdoc />
    public string Name => "Linux SocketCAN";

    /// <inheritdoc />
    public void ConfigureServices(IServiceCollection services)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        services
            .AddOptions<SocketCanOptions>()
            .BindConfiguration("AgioHost:Linux:SocketCan")
            .Validate(static options => !string.IsNullOrWhiteSpace(options.InterfaceName),
                "SocketCAN interface name must be specified.")
            .Validate(static options => options.PollInterval > TimeSpan.Zero,
                "SocketCAN poll interval must be greater than zero.")
            .ValidateOnStart();

        services.AddSingleton<SocketCanBusService>();
        services.AddHostedService<SocketCanListenerService>();
    }
}
