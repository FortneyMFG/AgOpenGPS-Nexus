using Aog.Agio.Nmea;
using Aog.Agio.Serial;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aog.Agio.Windows;

/// <summary>
/// Windows-specific AGiO backend that scans COM ports for NMEA streams.
/// </summary>
public sealed class WindowsAgioBackend : IAgioBackend
{
    /// <inheritdoc />
    public string Name => "Windows Serial";

    /// <inheritdoc />
    public void ConfigureServices(IServiceCollection services)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        services.AddOptions<NmeaSerialPortScanOptions>();

        services.AddSingleton<NmeaSentenceParser>();
        services.AddSingleton<ISerialPortEnumerator, WindowsSerialPortEnumerator>();
        services.AddSingleton<ISerialPortSessionFactory, SerialPortSessionFactory>();
        services.AddSingleton<NmeaAutoScanner>();
        services.AddSingleton<IHostedService, WindowsNmeaBackgroundService>();
    }
}
