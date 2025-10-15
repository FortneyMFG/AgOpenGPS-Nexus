using System;
using System.Threading.Tasks;
using Aog.Core.Eventing;
using Aog.Core.Layers;
using Aog.Core.Paths;
using Aog.Plugins.Weather;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Aog.Plugins.Tests;

public sealed class WeatherOverlayFeedTests
{
    private static WeatherSnapshot CreateSnapshot(string source, double temperatureC) => new()
    {
        CapturedAt = new DateTimeOffset(2024, 4, 2, 9, 0, 0, TimeSpan.Zero),
        Source = source,
        TemperatureC = temperatureC,
        HumidityPct = 60,
        RainfallMm = 1.2,
        WindKph = 8
    };

    [Fact]
    public async Task PublishAsync_PublishesLayerWhenIntervalZero()
    {
        var bus = new InMemoryEventBus();
        var options = new WeatherOverlayOptions
        {
            PublishInterval = TimeSpan.Zero,
            LayerId = "weather.overlay.temperature",
            Kind = "weather.temperature",
            Units = "°C",
            Metric = WeatherOverlayMetric.Temperature
        };
        var time = new FakeTimeProvider(new DateTimeOffset(2024, 4, 2, 9, 0, 0, TimeSpan.Zero));
        var feed = new WeatherOverlayFeed(bus, options, time);

        AgronomicLayerDocument? published = null;
        using var subscription = bus.Subscribe<AgronomicLayerDocument>((layer, _) =>
        {
            published = layer;
            return ValueTask.CompletedTask;
        });

        var observation = new WeatherObservation(CreateSnapshot("sensor:wx", 22.5), new PlanarPoint(10, 5));
        await feed.PublishAsync(observation);

        published.Should().NotBeNull();
        published!.LayerId.Should().Be(options.LayerId);
        published.Kind.Should().Be(options.Kind);
        published.Units.Should().Be("°C");
        published.Cells.Should().HaveCount(1);
        published.Cells[0].Value.Should().Be(22.5);
        published.Provenance.Source.Should().Be(options.Source);
        published.Provenance.Transform.Should().Be(options.Transform);
        published.Provenance.Hash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task PublishAsync_RespectsPublishIntervalUntilFlush()
    {
        var bus = new InMemoryEventBus();
        var options = new WeatherOverlayOptions
        {
            PublishInterval = TimeSpan.FromMinutes(5),
            Metric = WeatherOverlayMetric.Temperature
        };
        var time = new FakeTimeProvider(new DateTimeOffset(2024, 4, 2, 9, 0, 0, TimeSpan.Zero));
        var feed = new WeatherOverlayFeed(bus, options, time);

        AgronomicLayerDocument? published = null;
        using var subscription = bus.Subscribe<AgronomicLayerDocument>((layer, _) =>
        {
            published = layer;
            return ValueTask.CompletedTask;
        });

        await feed.PublishAsync(new WeatherObservation(CreateSnapshot("sensor:wx", 20), new PlanarPoint(0, 0)));
        published.Should().NotBeNull();
        var firstHash = published!.Provenance.Hash;

        time.Advance(TimeSpan.FromMinutes(2));
        published = null;
        await feed.PublishAsync(new WeatherObservation(CreateSnapshot("sensor:wx", 21), new PlanarPoint(0, 0)));
        published.Should().BeNull();

        await feed.FlushAsync();
        published.Should().NotBeNull();
        published!.Provenance.Hash.Should().NotBe(firstHash);
        published.Cells[0].Value.Should().Be(21);
    }

    [Fact]
    public async Task CreateLayerSnapshot_ReturnsCurrentCells()
    {
        var bus = new InMemoryEventBus();
        var options = new WeatherOverlayOptions
        {
            PublishInterval = TimeSpan.FromMinutes(60),
            Metric = WeatherOverlayMetric.Humidity,
            Units = "%RH",
            Transform = "weather.humidity"
        };
        var time = new FakeTimeProvider(new DateTimeOffset(2024, 4, 2, 9, 0, 0, TimeSpan.Zero));
        var feed = new WeatherOverlayFeed(bus, options, time);

        await feed.PublishAsync(new WeatherObservation(CreateSnapshot("sensor:a", 18) with { HumidityPct = 70 }, new PlanarPoint(0, 0)));
        await feed.PublishAsync(new WeatherObservation(CreateSnapshot("sensor:b", 16) with { HumidityPct = 65 }, new PlanarPoint(10, 0)));

        var snapshot = feed.CreateLayerSnapshot();

        snapshot.Should().NotBeNull();
        snapshot!.Cells.Should().HaveCount(2);
        snapshot.Cells[0].Value.Should().Be(70);
        snapshot.Cells[1].Value.Should().Be(65);
    }
}
