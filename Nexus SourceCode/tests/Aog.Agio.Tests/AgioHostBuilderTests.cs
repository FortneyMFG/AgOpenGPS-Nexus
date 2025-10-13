using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aog.Agio.Tests;

public sealed class AgioHostBuilderTests
{
    [Fact]
    public async Task Host_Starts_With_Default_Simulation_Backend()
    {
        using var host = Program.CreateHostBuilder(Array.Empty<string>())
            .Build();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await host.StartAsync(cts.Token);

        var registration = host.Services.GetRequiredService<AgioBackendRegistration>();
        Assert.Equal("Simulation", registration.BackendName);
        Assert.Equal("Aog.Agio.Sim.SimAgioBackend", registration.BackendType.FullName);

        var backend = host.Services.GetRequiredService<IAgioBackend>();
        Assert.Equal("Simulation", backend.Name);

        await host.StopAsync(cts.Token);
    }

    [Fact]
    public async Task Host_Uses_Custom_Backend_From_Configuration()
    {
        var builder = Program.CreateHostBuilder(Array.Empty<string>())
            .ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["AgioHost:Backend:Assembly"] = typeof(TestBackend).Assembly.GetName().Name!,
                    ["AgioHost:Backend:Type"] = typeof(TestBackend).FullName!,
                });
            });

        using var host = builder.Build();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await host.StartAsync(cts.Token);

        var registration = host.Services.GetRequiredService<AgioBackendRegistration>();
        Assert.Equal("Test Backend", registration.BackendName);
        Assert.Equal(typeof(TestBackend), registration.BackendType);

        var marker = host.Services.GetRequiredService<TestMarkerService>();
        Assert.NotNull(marker);

        await host.StopAsync(cts.Token);
    }

    [Fact]
    public void Host_Build_Fails_When_Backend_Cannot_Be_Loaded()
    {
        var builder = Program.CreateHostBuilder(Array.Empty<string>())
            .ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["AgioHost:Backend:Assembly"] = "NonExistent.Assembly",
                    ["AgioHost:Backend:Type"] = "Missing.Type",
                });
            });

        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());
        Assert.Contains("Failed to load AGiO backend assembly", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class TestBackend : IAgioBackend
    {
        public string Name => "Test Backend";

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<TestMarkerService>();
        }
    }

    public sealed class TestMarkerService;
}
