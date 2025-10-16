using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nexus.Cli.Host;
using Spectre.Console;
using Spectre.Console.Testing;
using Xunit;

namespace Nexus.Cli.Host.Tests;

public class HostInfoCommandModuleTests
{
    [Fact]
    public async Task HostInfoCommand_WritesHumanReadableOutput()
    {
        var console = new TestConsole();
        await using var host = await CreateHostAsync(console);
        var application = host.Services.GetRequiredService<NxApplication>();

        var exitCode = await application.InvokeAsync(new[] { "host", "info" }, CancellationToken.None);

        exitCode.Should().Be(0);

        var output = console.Output.ToString();
        output.Should().Contain("Nexus CLI host diagnostics");
        output.Should().Contain("Runtime");
        output.Should().Contain("Environment");
    }

    [Fact]
    public async Task HostInfoCommand_SupportsJsonOutput()
    {
        var console = new TestConsole();
        await using var host = await CreateHostAsync(console);
        var application = host.Services.GetRequiredService<NxApplication>();

        var exitCode = await application.InvokeAsync(new[] { "host", "info", "--output", "json" }, CancellationToken.None);

        exitCode.Should().Be(0);

        var payload = console.Output.ToString().Trim();
        payload.Should().StartWith("{");

        using var document = JsonDocument.Parse(payload);
        document.RootElement.TryGetProperty("Version", out var version).Should().BeTrue();
        version.GetString().Should().NotBeNullOrWhiteSpace();
        document.RootElement.TryGetProperty("Environment", out var environment).Should().BeTrue();
        environment.TryGetProperty("UserDirectory", out _).Should().BeTrue();
    }

    [Fact]
    public async Task HostInfoCommand_RejectsInvalidOutputFormat()
    {
        var console = new TestConsole();
        await using var host = await CreateHostAsync(console);
        var application = host.Services.GetRequiredService<NxApplication>();

        var exitCode = await application.InvokeAsync(new[] { "host", "info", "--output", "yaml" }, CancellationToken.None);

        exitCode.Should().NotBe(0);

        console.Error.ToString().Should().Contain("Invalid output format");
    }

    private static async Task<IHost> CreateHostAsync(TestConsole console)
    {
        var builder = Host.CreateApplicationBuilder(Array.Empty<string>());
        builder.Services.AddNxCliHost();
        builder.Services.AddSingleton<IAnsiConsole>(console);

        var host = builder.Build();
        await host.StartAsync();
        return host;
    }
}
