using System;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nexus.Cli.Host;
using Nexus.Cli.Host.Core.Endpoints;
using Nexus.Cli.Host.Core.Status;
using Spectre.Console;
using Spectre.Console.Testing;
using Xunit;

namespace Nexus.Cli.Host.Tests;

public class CoreStatusCommandModuleTests
{
    [Fact]
    public async Task CoreStatusCommand_WritesHumanOutput()
    {
        var console = new TestConsole();
        var endpoint = new CoreEndpoint(CoreTransportKind.Tcp, "https://127.0.0.1:5157", "test");
        var status = new CoreStatusResult(endpoint, CoreStatusState.Healthy, TimeSpan.FromMilliseconds(12), "Connected", DateTimeOffset.UtcNow);

        var host = await CreateHostAsync(console, status);
        try
        {
            var application = host.Services.GetRequiredService<NxApplication>();

            var exitCode = await application.InvokeAsync(new[] { "core", "status" }, CancellationToken.None);

            exitCode.Should().Be(0);
            var output = console.Output.ToString();
            output.Should().Contain("Nexus Core status");
            output.Should().Contain("Healthy");
            output.Should().Contain("Connected");
        }
        finally
        {
            await host.StopAsync();
            host.Dispose();
        }
    }

    [Fact]
    public async Task CoreStatusCommand_WritesJsonOutput()
    {
        var console = new TestConsole();
        var endpoint = new CoreEndpoint(CoreTransportKind.Tcp, "https://127.0.0.1:5157", "test");
        var status = new CoreStatusResult(endpoint, CoreStatusState.Healthy, TimeSpan.FromMilliseconds(20), "Connected", DateTimeOffset.UtcNow);

        var host = await CreateHostAsync(console, status);
        try
        {
            var application = host.Services.GetRequiredService<NxApplication>();

            var exitCode = await application.InvokeAsync(new[] { "core", "status", "--output", "json" }, CancellationToken.None);

            exitCode.Should().Be(0);
            var payload = console.Output.ToString().Trim();
            payload.Should().StartWith("{");

            using var document = JsonDocument.Parse(payload);
            document.RootElement.TryGetProperty("FinalState", out var finalState).Should().BeTrue();
            finalState.GetString().Should().Be("Healthy");
            document.RootElement.TryGetProperty("Attempts", out var attempts).Should().BeTrue();
            attempts.GetArrayLength().Should().Be(1);
        }
        finally
        {
            await host.StopAsync();
            host.Dispose();
        }
    }

    [Fact]
    public async Task CoreStatusCommand_SetsNonZeroExitCode_WhenUnavailable()
    {
        var console = new TestConsole();
        var endpoint = new CoreEndpoint(CoreTransportKind.Tcp, "https://127.0.0.1:5157", "test");
        var status = new CoreStatusResult(endpoint, CoreStatusState.Unavailable, null, "Failed", DateTimeOffset.UtcNow);

        var host = await CreateHostAsync(console, status);
        try
        {
            var application = host.Services.GetRequiredService<NxApplication>();

            var exitCode = await application.InvokeAsync(new[] { "core", "status" }, CancellationToken.None);

            exitCode.Should().Be(2);
            console.Output.ToString().Should().Contain("Failed");
        }
        finally
        {
            await host.StopAsync();
            host.Dispose();
        }
    }

    private static async Task<IHost> CreateHostAsync(TestConsole console, CoreStatusResult status)
    {
        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(Array.Empty<string>());
        builder.Services.AddNxCliHost();
        builder.Services.AddSingleton<IAnsiConsole>(_ => console);
        builder.Services.AddSingleton<ICoreEndpointResolver>(new StubEndpointResolver(status.Endpoint));
        builder.Services.AddSingleton<ICoreStatusProbe>(new StubCoreStatusProbe(status));

        var host = builder.Build();
        await host.StartAsync();
        return host;
    }

    private sealed class StubEndpointResolver : ICoreEndpointResolver
    {
        private readonly CoreEndpoint _endpoint;

        public StubEndpointResolver(CoreEndpoint endpoint)
        {
            _endpoint = endpoint;
        }

        public Task<CoreEndpointResolution> ResolveAsync(string? endpointOverride, CancellationToken cancellationToken)
        {
            var strategy = endpointOverride is null ? "test-default" : "test-override";
            return Task.FromResult(new CoreEndpointResolution(new[] { _endpoint }, strategy));
        }
    }

    private sealed class StubCoreStatusProbe : ICoreStatusProbe
    {
        private readonly CoreStatusResult _result;

        public StubCoreStatusProbe(CoreStatusResult result)
        {
            _result = result;
        }

        public Task<CoreStatusResult> CheckAsync(CoreEndpoint endpoint, CancellationToken cancellationToken)
        {
            return Task.FromResult(_result with { Endpoint = endpoint });
        }
    }
}
