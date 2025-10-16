using System.CommandLine;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Nexus.Plugin.Cli.Abstractions;
using Nexus.SamplePlugin.Cli;
using Xunit;

namespace Nexus.Cli.Host.Tests;

public sealed class SamplePluginCommandModuleTests
{
    [Fact]
    public void Configure_AddsSampleCommands()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new PluginCommandModuleDescriptor(
            "org.agopengps.nx.sample",
            "Sample Calibration",
            "0.1.0",
            "/plugins/sample",
            "/plugins/sample/plugin.json",
            "/plugins/sample/Nexus.SamplePlugin.Cli.dll"));

        using var provider = services.BuildServiceProvider();

        var root = new RootCommand();
        var context = new CommandModuleContext(root, provider, null);
        var module = new SampleCalibrationCommandModule();

        module.Configure(context);

        var sampleCommand = root.Children.Should().ContainSingle(c => c.Name == "sample").Subject;
        sampleCommand.Children.Should().Contain(c => c.Name == "calibrate");
        sampleCommand.Children.Should().Contain(c => c.Name == "sniff");
    }
}
