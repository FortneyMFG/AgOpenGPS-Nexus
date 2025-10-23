using Aog.Agio.Windows;
using Aog.Agio.Serial;
using Aog.Agio.Nmea;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Aog.Agio.Windows.Tests;

public sealed class WindowsAgioBackendTests
{
    [Fact]
    public void ConfigureServices_ResolvesNmeaAutoScanner()
    {
        var backend = new WindowsAgioBackend();
        var services = new ServiceCollection();
        services.AddLogging();

        backend.ConfigureServices(services);

        using var provider = services.BuildServiceProvider();

        var scanner = provider.GetRequiredService<NmeaAutoScanner>();
        Assert.NotNull(scanner);
    }
}
