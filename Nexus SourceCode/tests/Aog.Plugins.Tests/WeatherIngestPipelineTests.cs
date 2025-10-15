using System;
using System.Threading.Tasks;
using Aog.Plugins.Weather;
using FluentAssertions;
using Xunit;

namespace Aog.Plugins.Tests;

public sealed class WeatherIngestPipelineTests
{
    private static WeatherIngestPipeline CreatePipeline(Action<WeatherIngestOptions>? configure = null)
    {
        var options = new WeatherIngestOptions
        {
            MinimumPublishInterval = TimeSpan.Zero,
            MergePartialSamples = true,
            ChangeEpsilon = 1e-3
        };
        configure?.Invoke(options);
        return new WeatherIngestPipeline(options);
    }

    private static WeatherSample CreateBaseSample(DateTimeOffset capturedAt) => new()
    {
        CapturedAt = capturedAt,
        Source = "sensor:wx",
        TemperatureC = 12.5,
        HumidityPct = 55,
        WindKph = 8.4,
        WindDirectionDeg = 220,
        WindGustKph = 14.2,
        RainfallMm = 0.3,
        PressureKpa = 99.4,
        SoilTempC = 10.2,
        SoilMoisturePct = 34,
        LeafWetnessPct = 16
    };

    [Fact]
    public async Task IngestAsync_FirstSamplePublishesSnapshot()
    {
        var timestamp = new DateTimeOffset(2024, 4, 1, 10, 0, 0, TimeSpan.Zero);
        var pipeline = CreatePipeline();

        var sample = CreateBaseSample(timestamp);

        var published = await pipeline.IngestAsync(sample);

        published.Should().NotBeNull();
        published!.CapturedAt.Should().Be(timestamp);
        published.Source.Should().Be("sensor:wx");
        published.TemperatureC.Should().Be(12.5);
        published.HumidityPct.Should().Be(55);
        pipeline.LatestSnapshot.Should().BeEquivalentTo(published);
    }

    [Fact]
    public async Task IngestAsync_MergesPartialSamplesWhenEnabled()
    {
        var pipeline = CreatePipeline();
        var first = CreateBaseSample(new DateTimeOffset(2024, 4, 1, 10, 0, 0, TimeSpan.Zero));
        var second = new WeatherSample
        {
            CapturedAt = new DateTimeOffset(2024, 4, 1, 10, 5, 0, TimeSpan.Zero),
            Source = "sensor:wx",
            RainfallMm = 1.2
        };

        await pipeline.IngestAsync(first);
        var published = await pipeline.IngestAsync(second);

        published.Should().NotBeNull();
        published!.RainfallMm.Should().Be(1.2);
        published.TemperatureC.Should().Be(first.TemperatureC);
        published.HumidityPct.Should().Be(first.HumidityPct);
    }

    [Fact]
    public async Task IngestAsync_ComputesDerivedMetricsWhenMissing()
    {
        var pipeline = CreatePipeline();
        var sample = new WeatherSample
        {
            CapturedAt = new DateTimeOffset(2024, 4, 1, 9, 30, 0, TimeSpan.Zero),
            Source = "sensor:wx",
            TemperatureC = 24,
            HumidityPct = 60
        };

        var published = await pipeline.IngestAsync(sample);

        published.Should().NotBeNull();
        var expectedDewPoint = WeatherComputation.TryComputeDewPoint(24, 60)!.Value;
        var expectedWetBulb = WeatherComputation.TryComputeWetBulb(24, 60)!.Value;
        published!.DewPointC.Should().BeApproximately(expectedDewPoint, 1e-6);
        published.WetBulbC.Should().BeApproximately(expectedWetBulb, 1e-6);
        published.DeltaTC.Should().BeApproximately(sample.TemperatureC!.Value - expectedWetBulb, 1e-6);
    }

    [Fact]
    public async Task IngestAsync_DefersWhenIntervalNotReachedUntilFlush()
    {
        var baseTime = new DateTimeOffset(2024, 4, 1, 8, 0, 0, TimeSpan.Zero);
        var pipeline = CreatePipeline(options => options.MinimumPublishInterval = TimeSpan.FromMinutes(5));

        var first = CreateBaseSample(baseTime);
        var second = CreateBaseSample(baseTime.AddMinutes(2)) with { RainfallMm = 0.8 };

        var initial = await pipeline.IngestAsync(first);
        initial.Should().NotBeNull();

        var deferred = await pipeline.IngestAsync(second);
        deferred.Should().BeNull();

        var flushed = await pipeline.FlushAsync();
        flushed.Should().NotBeNull();
        flushed!.CapturedAt.Should().BeGreaterOrEqualTo(baseTime.AddMinutes(5));
        flushed.RainfallMm.Should().Be(0.8);
    }

    [Fact]
    public async Task IngestAsync_IgnoresStaleSamples()
    {
        var pipeline = CreatePipeline();
        var first = CreateBaseSample(new DateTimeOffset(2024, 4, 1, 10, 0, 0, TimeSpan.Zero));
        var stale = CreateBaseSample(new DateTimeOffset(2024, 4, 1, 9, 45, 0, TimeSpan.Zero));

        var published = await pipeline.IngestAsync(first);
        published.Should().NotBeNull();

        var result = await pipeline.IngestAsync(stale);

        result.Should().BeNull();
        pipeline.LatestSnapshot.Should().BeEquivalentTo(published);
    }

    [Fact]
    public async Task IngestAsync_ForcePublishOverridesInterval()
    {
        var baseTime = new DateTimeOffset(2024, 4, 1, 7, 0, 0, TimeSpan.Zero);
        var pipeline = CreatePipeline(options => options.MinimumPublishInterval = TimeSpan.FromMinutes(15));

        var first = CreateBaseSample(baseTime);
        var forced = CreateBaseSample(baseTime.AddMinutes(3)) with { ForcePublish = true, TemperatureC = 18 };

        await pipeline.IngestAsync(first);
        var published = await pipeline.IngestAsync(forced);

        published.Should().NotBeNull();
        published!.CapturedAt.Should().Be(forced.CapturedAt);
        published.TemperatureC.Should().Be(18);
    }

    [Fact]
    public async Task IngestAsync_InvalidSampleThrows()
    {
        var pipeline = CreatePipeline();
        var sample = new WeatherSample
        {
            CapturedAt = new DateTimeOffset(2024, 4, 1, 6, 0, 0, TimeSpan.Zero),
            Source = "sensor:wx",
            HumidityPct = 150
        };

        var action = () => pipeline.IngestAsync(sample).AsTask();

        await action.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }
}
