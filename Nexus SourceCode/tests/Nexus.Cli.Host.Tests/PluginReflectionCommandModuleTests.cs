using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Nexus.Cli.Host.Modules;
using Nexus.Cli.Host.Output;
using Nexus.Cli.Host.Plugins.Reflection;
using Nexus.Plugin.Cli.Abstractions;
using Spectre.Console.Testing;
using Xunit;

namespace Nexus.Cli.Host.Tests;

public sealed class PluginReflectionCommandModuleTests
{
    [Fact]
    public async Task ReflectCommand_PrintsHumanTable()
    {
        var verbs = new List<PluginVerb>
        {
            new("calibrate", "Calibrate flow sensor", new List<PluginVerbOption>
            {
                new("offset", "Base offset", true, PluginVerbOptionKind.Double, "0"),
            }),
        };

        var client = new FakeReflectionClient(verbs);
        var console = new TestConsole();

        var services = new ServiceCollection();
        services.AddSingleton<IPluginReflectionClient>(client);
        services.AddSingleton<IAnsiConsole>(console);
        await using var provider = services.BuildServiceProvider();

        var root = new RootCommand();
        var context = new CommandContext(root, provider, OutputOptions.ModeOption);
        var module = new PluginReflectionCommandModule(client, console);
        module.Configure(context);

        var parser = new CommandLineBuilder(root).UseDefaults().Build();
        var result = await parser.InvokeAsync(new[] { "plugin", "reflect", "--endpoint", "http://localhost" });

        result.Should().Be(0);
        console.Output.Should().Contain("calibrate");
    }

    [Fact]
    public async Task ReflectCommand_PrintsJson()
    {
        var verbs = new List<PluginVerb>();
        var client = new FakeReflectionClient(verbs);
        var console = new TestConsole();

        var services = new ServiceCollection();
        services.AddSingleton<IPluginReflectionClient>(client);
        services.AddSingleton<IAnsiConsole>(console);
        await using var provider = services.BuildServiceProvider();

        var root = new RootCommand();
        root.AddGlobalOption(OutputOptions.ModeOption);
        var context = new CommandContext(root, provider, OutputOptions.ModeOption);
        var module = new PluginReflectionCommandModule(client, console);
        module.Configure(context);

        var parser = new CommandLineBuilder(root).UseDefaults().Build();
        var result = await parser.InvokeAsync(new[] { "plugin", "reflect", "--endpoint", "http://localhost", "--output", "json" });

        result.Should().Be(0);
        console.Output.Should().Contain("[]");
    }

    private sealed class FakeReflectionClient : IPluginReflectionClient
    {
        private readonly IReadOnlyList<PluginVerb> _verbs;

        public FakeReflectionClient(IReadOnlyList<PluginVerb> verbs)
        {
            _verbs = verbs;
        }

        public Task<IReadOnlyList<PluginVerb>> ListVerbsAsync(Uri endpoint, CancellationToken cancellationToken)
        {
            return Task.FromResult(_verbs);
        }
    }
}
