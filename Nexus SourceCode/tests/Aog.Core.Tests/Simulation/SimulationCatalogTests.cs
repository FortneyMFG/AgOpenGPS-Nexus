using System;
using Aog.Core.Simulation;
using FluentAssertions;
using Xunit;

namespace Aog.Core.Tests.Simulation;

public sealed class SimulationCatalogTests
{
    [Fact]
    public void Register_Throws_WhenProviderIdIsDuplicate()
    {
        var catalog = new SimulationCatalog();
        var descriptor = new SimulationProviderDescriptor(
            "sim.pose",
            outputs: new[] { "pose" });

        catalog.Register(descriptor);

        var action = () => catalog.Register(descriptor);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*sim.pose*");
    }

    [Fact]
    public void BuildGraph_OrdersProvidersByDependencies()
    {
        var clock = new SimulationProviderDescriptor(
            "sim.clock",
            outputs: new[] { "time" });
        var vehicle = new SimulationProviderDescriptor(
            "sim.vehicle",
            outputs: new[] { "pose" },
            inputs: new[] { "time" });
        var imu = new SimulationProviderDescriptor(
            "sim.imu",
            outputs: new[] { "imu" },
            inputs: new[] { "pose", "time" });

        var catalog = new SimulationCatalog();
        catalog.Register(vehicle);
        catalog.Register(clock);
        catalog.Register(imu);

        var graph = catalog.BuildGraph();
        graph.Providers.Should().ContainInOrder(clock, vehicle, imu);
    }

    [Fact]
    public void BuildGraph_Throws_WhenCycleDetected()
    {
        var providerA = new SimulationProviderDescriptor(
            "sim.a",
            outputs: new[] { "topic-a" },
            inputs: new[] { "topic-c" });
        var providerB = new SimulationProviderDescriptor(
            "sim.b",
            outputs: new[] { "topic-b" },
            inputs: new[] { "topic-a" });
        var providerC = new SimulationProviderDescriptor(
            "sim.c",
            outputs: new[] { "topic-c" },
            inputs: new[] { "topic-b" });

        var catalog = new SimulationCatalog();
        catalog.Register(providerA);
        catalog.Register(providerB);
        catalog.Register(providerC);

        var action = () => catalog.BuildGraph();

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cycle detected*");
    }

    [Fact]
    public void FormatSummary_ProvidesHumanReadableOutput()
    {
        var clock = new SimulationProviderDescriptor(
            "sim.clock",
            outputs: new[] { "time" });
        var catalog = new SimulationCatalog();
        catalog.Register(clock);

        var summary = catalog.BuildGraph().FormatSummary();

        summary.Should().Contain("Simulation Provider Graph:");
        summary.Should().Contain("sim.clock");
        summary.Should().Contain("outputs: [time]");
        summary.Should().Contain("inputs: []");
    }

    [Fact]
    public void BuildGraph_Throws_WhenInputTopicIsUnresolved()
    {
        var vehicle = new SimulationProviderDescriptor(
            "sim.vehicle",
            outputs: new[] { "pose" },
            inputs: new[] { "time" });

        var catalog = new SimulationCatalog();
        catalog.Register(vehicle);

        var action = () => catalog.BuildGraph();

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*sim.vehicle*")
            .WithMessage("*time*");
    }
}
