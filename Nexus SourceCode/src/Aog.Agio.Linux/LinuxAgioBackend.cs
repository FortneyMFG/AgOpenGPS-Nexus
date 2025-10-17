using Aog.Agio.Linux.Gpsd;
using Aog.Agio.Linux.Serial;
using Aog.Agio.Linux.SocketCan;
using Aog.Agio.Nmea;
using Aog.Agio.Serial;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aog.Agio.Linux;

/// <summary>
/// Linux-specific AGiO backend that scans serial devices and gpsd feeds for GNSS data.
/// </summary>
public sealed class LinuxAgioBackend : IAgioBackend
{
    /// <inheritdoc />
    public string Name => "Linux Serial, gpsd & SocketCAN";

    /// <inheritdoc />
    public void ConfigureServices(IServiceCollection services)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        services
            .AddOptions<NmeaSerialPortScanOptions>()
            .BindConfiguration("AgioHost:Linux:Serial:Scan");
        services
            .AddOptions<LinuxSerialPortEnumeratorOptions>()
            .BindConfiguration("AgioHost:Linux:Serial");

        services
            .AddOptions<GpsdClientOptions>()
            .BindConfiguration("AgioHost:Linux:Gpsd");

        services
            .AddOptions<SocketCanOptions>()
            .BindConfiguration("AgioHost:Linux:SocketCan");

        services.AddSingleton<NmeaSentenceParser>();
        services.AddSingleton<ISerialPortEnumerator, LinuxSerialPortEnumerator>();
        services.AddSingleton<ISerialPortSessionFactory, SerialPortSessionFactory>();
        services.AddSingleton<NmeaAutoScanner>();
        services.AddSingleton<IHostedService, LinuxNmeaBackgroundService>();

        services.AddSingleton<IGpsdConnectionFactory, UnixDomainSocketGpsdConnectionFactory>();
        services.AddSingleton<GpsdClient>();
        services.AddSingleton<IHostedService, GpsdBackgroundService>();

        services.AddSingleton<ICanNetworkInterfaceProvider, SocketCanNetworkInterfaceProvider>();
        services.AddSingleton<ISocketCanClientFactory, SocketCanClientFactory>();
        services.AddSingleton<SocketCanFrameChannel>();
        services.AddSingleton<ISocketCanFramePublisher>(sp => sp.GetRequiredService<SocketCanFrameChannel>());
        services.AddSingleton<ISocketCanFrameSource>(sp => sp.GetRequiredService<SocketCanFrameChannel>());
        services.AddSingleton<IHostedService, SocketCanBackgroundService>();
        services.AddSingleton<SocketCanBusService>();
    }
}
