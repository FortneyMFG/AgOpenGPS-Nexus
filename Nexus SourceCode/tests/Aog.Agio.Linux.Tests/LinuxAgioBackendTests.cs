using System;
using System.Linq;
using Aog.Agio.Linux;
using Aog.Agio.Linux.Gpsd;
using Aog.Agio.Linux.Serial;
using Aog.Agio.Linux.SocketCan;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
        services.AddSingleton(TimeProvider.System);

        backend.ConfigureServices(services);

        using var provider = services.BuildServiceProvider();

        var hosted = provider.GetServices<IHostedService>().ToList();
        Assert.Contains(hosted, service => service is LinuxNmeaBackgroundService);
        Assert.Contains(hosted, service => service is GpsdBackgroundService);
        Assert.Contains(hosted, service => service is SocketCanBackgroundService);

        var socketCanBus = provider.GetRequiredService<SocketCanBusService>();
        Assert.NotNull(socketCanBus);
    }
}
