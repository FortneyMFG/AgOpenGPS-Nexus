using Aog.Agio.Nmea;
using Aog.Agio.Serial;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aog.Agio.Linux;

/// <summary>
/// Linux-specific AGiO backend that scans serial ports and gpsd for GNSS data.
/// </summary>
public sealed class LinuxAgioBackend : IAgioBackend
{
    /// <inheritdoc />
    public string Name => "Linux Serial";

    /// <inheritdoc />
    public void ConfigureServices(IServiceCollection services)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        services.AddOptions<NmeaSerialPortScanOptions>();
        services.AddOptions<GpsdClientOptions>();

        services.AddSingleton<NmeaSentenceParser>();
        services.AddSingleton<ISerialPortEnumerator, LinuxSerialPortEnumerator>();
        services.AddSingleton<ISerialPortSessionFactory, SerialPortSessionFactory>();
        services.AddSingleton<NmeaAutoScanner>();
        services.AddSingleton<IHostedService, LinuxNmeaBackgroundService>();

        services.AddSingleton<IGpsdTransport, UnixDomainSocketGpsdTransport>();
        services.AddSingleton<GpsdClient>();
        services.AddSingleton<IHostedService, GpsdBackgroundService>();
    }
}
