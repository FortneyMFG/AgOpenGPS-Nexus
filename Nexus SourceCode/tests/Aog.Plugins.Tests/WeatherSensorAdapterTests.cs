using System;
using System.Threading.Tasks;
using Aog.Core.Paths;
using Aog.Plugins.Weather;
using FluentAssertions;
using Xunit;

namespace Aog.Plugins.Tests;

public sealed class WeatherSensorAdapterTests
{
    [Fact]
    public async Task IngestAsync_NormalizesUnitsAndPublishesSnapshot()
    {
        var options = new WeatherIngestOptions { MinimumPublishInterval = TimeSpan.Zero };
        var pipeline = new WeatherIngestPipeline(options);
        var adapter = new WeatherSensorAdapter(pipeline);
        var reading = new WeatherSensorReading
        {
            CapturedAt = new DateTimeOffset(2024, 4, 2, 9, 0, 0, TimeSpan.Zero),
            Source = "sensor:wx",
            Location = new PlanarPoint(100, 200),
            Temperature = 50,
            TemperatureUnit = TemperatureUnit.Fahrenheit,
            HumidityPct = 72,
            WindSpeed = 12,
            WindSpeedUnit = WindSpeedUnit.MilesPerHour,
            WindDirectionDeg = 135,
            WindGust = 8,
            WindGustUnit = WindSpeedUnit.MetersPerSecond,
            Rainfall = 0.2,
            RainfallUnit = RainfallUnit.Inches,
            Pressure = 1002,
            PressureUnit = PressureUnit.Hectopascals,
            ForcePublish = true
        };

        var observation = await adapter.IngestAsync(reading);

        observation.Should().NotBeNull();
        observation!.Location.Should().Be(reading.Location);
        observation.Snapshot.TemperatureC.Should().BeApproximately(10d, 1e-6);
        observation.Snapshot.HumidityPct.Should().Be(72);
        observation.Snapshot.WindKph.Should().BeApproximately(19.312128d, 1e-6);
        observation.Snapshot.WindDirectionDeg.Should().Be(135);
        observation.Snapshot.WindGustKph.Should().BeApproximately(28.8d, 1e-6);
        observation.Snapshot.RainfallMm.Should().BeApproximately(5.08d, 1e-6);
        observation.Snapshot.PressureKpa.Should().BeApproximately(100.2d, 1e-6);
    }

    [Fact]
    public async Task IngestAsync_ReusesLastKnownLocationWhenMissing()
    {
        var options = new WeatherIngestOptions { MinimumPublishInterval = TimeSpan.Zero };
        var pipeline = new WeatherIngestPipeline(options);
        var adapter = new WeatherSensorAdapter(pipeline);
        var initial = new WeatherSensorReading
        {
            CapturedAt = new DateTimeOffset(2024, 4, 2, 10, 0, 0, TimeSpan.Zero),
            Source = "sensor:wx",
            Location = new PlanarPoint(10, 5),
            Temperature = 18,
            ForcePublish = true
        };

        await adapter.IngestAsync(initial);

        var update = new WeatherSensorReading
        {
            CapturedAt = initial.CapturedAt.AddMinutes(5),
            Source = initial.Source,
            Temperature = 19,
            ForcePublish = true
        };

        var observation = await adapter.IngestAsync(update);

        observation.Should().NotBeNull();
        observation!.Location.Should().Be(initial.Location);
    }

    [Fact]
    public async Task FlushAsync_EmitsLatestSnapshotWithLocation()
    {
        var options = new WeatherIngestOptions
        {
            MinimumPublishInterval = TimeSpan.FromMinutes(10),
            MergePartialSamples = true
        };
        var pipeline = new WeatherIngestPipeline(options);
        var adapter = new WeatherSensorAdapter(pipeline);
        var reading = new WeatherSensorReading
        {
            CapturedAt = new DateTimeOffset(2024, 4, 2, 12, 0, 0, TimeSpan.Zero),
            Source = "sensor:wx",
            Location = new PlanarPoint(25, 75),
            Temperature = 15
        };

        await adapter.IngestAsync(reading);

        var observation = await adapter.FlushAsync();

        observation.Should().NotBeNull();
        observation!.Location.Should().Be(reading.Location);
        observation.Snapshot.TemperatureC.Should().Be(15);
    }

    [Fact]
    public async Task IngestAsync_NullReadingThrows()
    {
        var adapter = new WeatherSensorAdapter(new WeatherIngestPipeline(new WeatherIngestOptions()));
        Func<Task> act = () => adapter.IngestAsync(null!).AsTask();

        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
