using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Aog.Bridge.Host.Tests;

public sealed class BridgeHostBuilderTests
{
    [Fact]
    public async Task Host_Starts_And_Stops_Cleanly()
    {
        using var host = Program.CreateHostBuilder(Array.Empty<string>())
            .ConfigureAppConfiguration((_, builder) =>
            {
                builder.AddInMemoryCollection(new Dictionary<string, string?>());
            })
            .Build();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await host.StartAsync(cts.Token);

        var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
        Assert.True(lifetime.ApplicationStarted.IsCancellationRequested);

        await host.StopAsync(cts.Token);
    }

    [Fact]
    public void Default_Options_Are_Valid()
    {
        using var host = Program.CreateHostBuilder(Array.Empty<string>()).Build();
        var options = host.Services.GetRequiredService<IOptions<BridgeHostOptions>>().Value;

        Assert.Equal("bridge", options.NodeId);
        Assert.Equal("0.1.0", options.FirmwareVersion);
        Assert.Equal("0.0.0.0", options.Grpc.BindAddress);
        Assert.Equal(5600, options.Grpc.Port);
        Assert.Equal("239.10.6.1", options.Udp.MulticastAddress);
    }

    [Fact]
    public async Task Invalid_Grpc_Port_Fails_Validation()
    {
        var builder = Program.CreateHostBuilder(Array.Empty<string>())
            .ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["BridgeHost:Grpc:Port"] = "70000"
                });
            });

        using var host = builder.Build();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var exception = await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAsync(cts.Token));
        Assert.Contains("BridgeHost:Grpc:Port", exception.Message);
    }

    [Fact]
    public void Gateway_Is_Registered()
    {
        using var host = Program.CreateHostBuilder(Array.Empty<string>()).Build();

        var gateway = host.Services.GetRequiredService<IAogLinkGateway>();
        Assert.IsType<AogLinkGateway>(gateway);

        var hostedServices = host.Services.GetRequiredService<IEnumerable<IHostedService>>();
        Assert.Contains(hostedServices, service => service is BridgeHostedService);
    }
}
