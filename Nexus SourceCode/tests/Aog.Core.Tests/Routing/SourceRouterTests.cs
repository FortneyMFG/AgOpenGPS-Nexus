using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aog.Core.Eventing;
using Aog.Core.Routing;
using FluentAssertions;
using Xunit;

namespace Aog.Core.Tests.Routing;

public class SourceRouterTests
{
    private static (SourceRouter Router, InMemoryEventBus Bus, List<TopicRouteChanged> Events) CreateRouter(RoutingOptions? options = null)
    {
        var bus = new InMemoryEventBus();
        var events = new List<TopicRouteChanged>();
        bus.Subscribe<TopicRouteChanged>(message =>
        {
            events.Add(message);
            return ValueTask.CompletedTask;
        });

        return (new SourceRouter(bus, options), bus, events);
    }

    [Fact]
    public async Task RegisterSource_SelectsHighestPriority()
    {
        var (router, _, events) = CreateRouter();

        var firstRoute = await router.RegisterSourceAsync("pose", "sim", SourceKind.Simulation);
        firstRoute.Should().NotBeNull();
        firstRoute!.SourceId.Should().Be("sim");

        var secondRoute = await router.RegisterSourceAsync("pose", "hw", SourceKind.Hardware);
        secondRoute.Should().NotBeNull();
        secondRoute!.SourceId.Should().Be("hw");

        events.Select(e => e.Current?.SourceId).Should().Equal("sim", "hw");
    }

    [Fact]
    public async Task PreferredSourceFromOptionsWinsWhenAvailable()
    {
        var options = new RoutingOptions();
        options.PreferredSources["pose"] = "sim";

        var (router, _, events) = CreateRouter(options);

        var route = await router.RegisterSourceAsync("pose", "hardware", SourceKind.Hardware);
        route.Should().NotBeNull();
        route!.SourceId.Should().Be("hardware");

        route = await router.RegisterSourceAsync("pose", "sim", SourceKind.Simulation);
        route.Should().NotBeNull();
        route!.SourceId.Should().Be("sim");

        router.GetCurrentRoute("pose")!.SourceId.Should().Be("sim");
        events.Select(e => e.Current?.SourceId).Should().Equal("hardware", "sim");
    }

    [Fact]
    public async Task RuntimeOverrideChangesActiveRoute()
    {
        var (router, _, events) = CreateRouter();

        await router.RegisterSourceAsync("pose", "hardware", SourceKind.Hardware);
        await router.RegisterSourceAsync("pose", "sim", SourceKind.Simulation);

        var change = await router.SetPreferredSourceAsync("pose", "sim");
        change.Should().NotBeNull();
        change!.SourceId.Should().Be("sim");

        events.Last().Current!.SourceId.Should().Be("sim");
    }

    [Fact]
    public async Task RemovingActiveSourceFallsBackToNextBest()
    {
        var (router, _, events) = CreateRouter();

        await router.RegisterSourceAsync("pose", "sim", SourceKind.Simulation);
        await router.RegisterSourceAsync("pose", "hardware", SourceKind.Hardware);

        var route = await router.UnregisterSourceAsync("pose", "hardware");
        route.Should().NotBeNull();
        route!.SourceId.Should().Be("sim");

        events.Last().Current!.SourceId.Should().Be("sim");
        events.Last().Previous!.SourceId.Should().Be("hardware");
    }
}
