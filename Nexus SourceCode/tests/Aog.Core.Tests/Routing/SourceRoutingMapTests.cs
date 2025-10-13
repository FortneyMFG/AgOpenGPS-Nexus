using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aog.Core.Eventing;
using Aog.Core.Routing;
using FluentAssertions;
using Xunit;

namespace Aog.Core.Tests.Routing;

public sealed class SourceRoutingMapTests
{
    [Fact]
    public async Task ApplyAsync_ReplacesRoutesAndPublishesChanges()
    {
        var bus = new InMemoryEventBus();
        var map = new SourceRoutingMap(bus);
        var changes = new List<StreamRouteChangedEvent>();

        bus.Subscribe<StreamRouteChangedEvent>(message =>
        {
            changes.Add(message);
            return ValueTask.CompletedTask;
        });

        var initial = new[]
        {
            new StreamRoute("pose", "gnss-hw", RouteSourceMode.Hardware),
            new StreamRoute("imu", "sim-imu", RouteSourceMode.Simulation)
        };

        await map.ApplyAsync(initial);

        map.Routes.Should().BeEquivalentTo(initial);
        changes.Should().BeEquivalentTo(new[]
        {
            new StreamRouteChangedEvent("pose", null, initial[0]),
            new StreamRouteChangedEvent("imu", null, initial[1])
        });

        changes.Clear();

        var updated = new[]
        {
            new StreamRoute("pose", "replay-log", RouteSourceMode.Replay)
        };

        await map.ApplyAsync(updated);

        map.Routes.Should().BeEquivalentTo(updated);
        changes.Should().BeEquivalentTo(new[]
        {
            new StreamRouteChangedEvent("pose", initial[0], updated[0]),
            new StreamRouteChangedEvent("imu", initial[1], null)
        });
    }

    [Fact]
    public async Task ApplyAsync_FromOptions_BuildsRoutesAndEmitsChanges()
    {
        var bus = new InMemoryEventBus();
        var map = new SourceRoutingMap(bus);
        var changes = new List<StreamRouteChangedEvent>();

        bus.Subscribe<StreamRouteChangedEvent>(message =>
        {
            changes.Add(message);
            return ValueTask.CompletedTask;
        });

        var options = new SourceRoutingOptions();
        options.Routes.Add(new StreamRouteOptions
        {
            Stream = "pose",
            Source = "gnss-hw",
            Mode = RouteSourceMode.Hardware
        });
        options.Routes.Add(new StreamRouteOptions
        {
            Stream = "imu",
            Source = "sim-imu",
            Mode = RouteSourceMode.Simulation
        });

        await map.ApplyAsync(options);

        var expected = new[]
        {
            new StreamRoute("pose", "gnss-hw", RouteSourceMode.Hardware),
            new StreamRoute("imu", "sim-imu", RouteSourceMode.Simulation)
        };

        map.Routes.Should().BeEquivalentTo(expected);
        changes.Should().BeEquivalentTo(new[]
        {
            new StreamRouteChangedEvent("pose", null, expected[0]),
            new StreamRouteChangedEvent("imu", null, expected[1])
        });
    }

    [Fact]
    public async Task ApplyAsync_FromOptions_ThrowsOnDuplicateStreams()
    {
        var map = new SourceRoutingMap(new InMemoryEventBus());
        var options = new SourceRoutingOptions();
        options.Routes.Add(new StreamRouteOptions { Stream = "pose", Source = "sim-a" });
        options.Routes.Add(new StreamRouteOptions { Stream = "pose", Source = "sim-b" });

        await Assert.ThrowsAsync<InvalidOperationException>(() => map.ApplyAsync(options));
    }

    [Fact]
    public async Task SetRouteAsync_UpdatesRouteAndAvoidsDuplicateEvents()
    {
        var bus = new InMemoryEventBus();
        var map = new SourceRoutingMap(bus);
        var changes = new List<StreamRouteChangedEvent>();

        bus.Subscribe<StreamRouteChangedEvent>(message =>
        {
            changes.Add(message);
            return ValueTask.CompletedTask;
        });

        var route = new StreamRoute("sections", "sim-switch", RouteSourceMode.Simulation);
        await map.SetRouteAsync(route);

        map.TryGetRoute("sections", out var stored).Should().BeTrue();
        stored.Should().Be(route);
        changes.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new StreamRouteChangedEvent("sections", null, route));

        changes.Clear();
        await map.SetRouteAsync(route);

        changes.Should().BeEmpty();

        var overrideRoute = new StreamRoute("sections", "hardware-switch", RouteSourceMode.Hardware);
        await map.SetRouteAsync(overrideRoute);

        changes.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new StreamRouteChangedEvent("sections", route, overrideRoute));
    }

    [Fact]
    public async Task RemoveRouteAsync_PublishesRemovalEvent()
    {
        var bus = new InMemoryEventBus();
        var map = new SourceRoutingMap(bus);
        var changes = new List<StreamRouteChangedEvent>();

        bus.Subscribe<StreamRouteChangedEvent>(message =>
        {
            changes.Add(message);
            return ValueTask.CompletedTask;
        });

        var route = new StreamRoute("steer", "sim-auto", RouteSourceMode.Simulation);
        await map.SetRouteAsync(route);
        changes.Clear();

        await map.RemoveRouteAsync("steer");

        map.TryGetRoute("steer", out _).Should().BeFalse();
        changes.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new StreamRouteChangedEvent("steer", route, null));
    }
}
