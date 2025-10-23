using System;
using System.Collections.Generic;
using System.Linq;
using Aog.Agio.Linux;
using Aog.Agio.Linux.Gpsd;
using Aog.Agio.Linux.Serial;
using Aog.Agio.Linux.SocketCan;
using Aog.Agio.Nmea;
using Aog.Agio.Serial;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aog.Agio.Linux.Tests;

public sealed class LinuxAgioBackendTests
{
    [Fact]
    public void ConfigureServices_RegistersAllHostedServices()
    {
        var backend = new LinuxAgioBackend();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection().Build());
        services.AddLogging();

        backend.ConfigureServices(services);

        using var provider = services.BuildServiceProvider();

        var hosted = provider.GetServices<IHostedService>().ToList();
        Assert.Contains(hosted, service => service is LinuxNmeaBackgroundService);
        Assert.Contains(hosted, service => service is GpsdBackgroundService);
        Assert.Contains(hosted, service => service is SocketCanBackgroundService);

        var socketCanBus = provider.GetRequiredService<SocketCanBusService>();
        Assert.NotNull(socketCanBus);
    }

    [Fact]
    public void ConfigureServices_BindsNmeaSerialPortScanOptions()
    {
        var backend = new LinuxAgioBackend();
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["AgioHost:Linux:Serial:Scan:ProbeDuration"] = "00:00:07",
                    ["AgioHost:Linux:Serial:Scan:ReadTimeout"] = "00:00:01",
                    ["AgioHost:Linux:Serial:Scan:BaudRates:0"] = "4800",
                    ["AgioHost:Linux:Serial:Scan:BaudRates:1"] = "230400",
                    ["AgioHost:Linux:Serial:Scan:MaxReadAttemptsPerPort"] = "10",
                })
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();

        backend.ConfigureServices(services);

        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<NmeaSerialPortScanOptions>>().Value;

        Assert.Equal(TimeSpan.FromSeconds(7), options.ProbeDuration);
        Assert.Equal(TimeSpan.FromSeconds(1), options.ReadTimeout);
        Assert.Equal(new[] { 4800, 230400 }, options.BaudRates);
        Assert.Equal(10, options.MaxReadAttemptsPerPort);
    }

    [Fact]
    public void ConfigureServices_ResolvesNmeaAutoScanner()
    {
        var backend = new LinuxAgioBackend();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection().Build());
        services.AddLogging();

        backend.ConfigureServices(services);

        using var provider = services.BuildServiceProvider();

        var scanner = provider.GetRequiredService<NmeaAutoScanner>();
        Assert.NotNull(scanner);
    }
}
