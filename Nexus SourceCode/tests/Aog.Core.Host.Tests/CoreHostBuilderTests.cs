using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Jobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aog.Core.Host.Tests;

public sealed class CoreHostBuilderTests
{
    [Fact]
    public async Task Host_Starts_And_Stops_Cleanly()
    {
        using var host = Program.CreateHostBuilder(Array.Empty<string>())
            .ConfigureAppConfiguration((_, builder) =>
            {
                builder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["CoreHost:Health:IntervalSeconds"] = "1"
                });
            })
            .Build();

        using var startCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await host.StartAsync(startCts.Token);

        var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
        Assert.True(lifetime.ApplicationStarted.IsCancellationRequested);

        var orchestrator = host.Services.GetRequiredService<IJobLifecycleOrchestrator>();
        Assert.NotNull(orchestrator);

        using var stopCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await host.StopAsync(stopCts.Token);
    }

    [Fact]
    public void Default_Health_Options_Are_Valid()
    {
        using var host = Program.CreateHostBuilder(Array.Empty<string>()).Build();
        var options = host.Services.GetRequiredService<IOptions<CoreHealthOptions>>().Value;

        Assert.Equal(30, options.IntervalSeconds);
    }

    [Fact]
    public async Task Invalid_Health_Interval_Throws()
    {
        var builder = Program.CreateHostBuilder(Array.Empty<string>())
            .ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["CoreHost:Health:IntervalSeconds"] = "0"
                });
            });

        using var host = builder.Build();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var exception = await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAsync(cts.Token));
        Assert.Contains(nameof(CoreHealthOptions.IntervalSeconds), exception.Message);
    }

    [Fact]
    public async Task MissingAgioEndpointFailsValidation()
    {
        var builder = Program.CreateHostBuilder(Array.Empty<string>())
            .ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["CoreHost:Agio:Endpoint"] = string.Empty,
                });
            });

        using var host = builder.Build();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var exception = await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAsync(cts.Token));
        Assert.Contains("CoreHost:Agio:Endpoint", exception.Message);
    }

    [Fact]
    public async Task MissingCapabilitiesNodeIdFailsValidation()
    {
        var builder = Program.CreateHostBuilder(Array.Empty<string>())
            .ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["CoreHost:Capabilities:NodeId"] = " ",
                });
            });

        using var host = builder.Build();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var exception = await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAsync(cts.Token));
        Assert.Contains("CoreHost:Capabilities:NodeId", exception.Message);
    }
}
