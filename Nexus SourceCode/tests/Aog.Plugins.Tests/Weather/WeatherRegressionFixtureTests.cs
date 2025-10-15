using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aog.Plugins.Weather;
using FluentAssertions;
using Xunit;

namespace Aog.Plugins.Tests.Weather;

public sealed class WeatherRegressionFixtureTests
{
    private static readonly string FixturePath = Path.Combine("Weather", "Data", "WeatherRegressionFixture.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    [Fact]
    public void WeatherPipeline_MatchesRegressionFixture()
    {
        var fullPath = Path.Combine(AppContext.BaseDirectory, FixturePath);
        File.Exists(fullPath).Should().BeTrue($"Fixture '{FixturePath}' should be copied to the test output directory.");

        var json = File.ReadAllText(fullPath);
        var document = JsonSerializer.Deserialize<FixtureDocument>(json, JsonOptions);
        document.Should().NotBeNull();
        document!.Scenarios.Should().NotBeNull();
        document.Scenarios.Should().NotBeEmpty();

        foreach (var scenario in document.Scenarios)
        {
            ValidateScenario(scenario);
        }
    }

    private static void ValidateScenario(FixtureScenario scenario)
    {
        var pipeline = new WeatherIngestPipeline(scenario.Options.ToOptions());

        foreach (var operation in scenario.Operations)
        {
            WeatherSnapshot? actual;
            if (operation.Ingest is not null)
            {
                actual = pipeline.IngestAsync(operation.Ingest.ToSample()).AsTask().GetAwaiter().GetResult();
            }
            else if (operation.Flush)
            {
                actual = pipeline.FlushAsync().AsTask().GetAwaiter().GetResult();
            }
            else
            {
                throw new InvalidOperationException($"Scenario '{scenario.Name}' contains an operation without an ingest or flush action.");
            }

            if (operation.ExpectPublished is null)
            {
                actual.Should().BeNull(scenario.Name);
            }
            else
            {
                actual.Should().NotBeNull(scenario.Name);
                AssertSnapshot(operation.ExpectPublished, actual!, scenario.Name);
            }
        }

        var latest = pipeline.LatestSnapshot;
        scenario.Expected.LatestSnapshot.Should().NotBeNull();
        latest.Should().NotBeNull($"Scenario '{scenario.Name}' expected a latest snapshot.");
        AssertSnapshot(scenario.Expected.LatestSnapshot!, latest!, scenario.Name);
    }

    private static void AssertSnapshot(WeatherSnapshotModel expected, WeatherSnapshot actual, string scenarioName)
    {
        actual.CapturedAt.Should().Be(expected.CapturedAt, scenarioName);
        actual.Source.Should().Be(expected.Source, scenarioName);
        AssertClose(actual.TemperatureC, expected.TemperatureC, scenarioName);
        AssertClose(actual.HumidityPct, expected.HumidityPct, scenarioName);
        AssertClose(actual.WindKph, expected.WindKph, scenarioName);
        AssertClose(actual.WindDirectionDeg, expected.WindDirectionDeg, scenarioName);
        AssertClose(actual.WindGustKph, expected.WindGustKph, scenarioName);
        AssertClose(actual.RainfallMm, expected.RainfallMm, scenarioName);
        AssertClose(actual.PressureKpa, expected.PressureKpa, scenarioName);
        AssertClose(actual.DewPointC, expected.DewPointC, scenarioName);
        AssertClose(actual.WetBulbC, expected.WetBulbC, scenarioName);
        AssertClose(actual.DeltaTC, expected.DeltaTC, scenarioName);
        AssertClose(actual.EvapotranspirationMm, expected.EvapotranspirationMm, scenarioName);
        AssertClose(actual.SolarIrradianceWm2, expected.SolarIrradianceWm2, scenarioName);
        AssertClose(actual.UvIndex, expected.UvIndex, scenarioName);
        AssertClose(actual.CloudCoverPct, expected.CloudCoverPct, scenarioName);
        AssertClose(actual.VisibilityKm, expected.VisibilityKm, scenarioName);
        AssertClose(actual.SoilTempC, expected.SoilTempC, scenarioName);
        AssertClose(actual.SoilMoisturePct, expected.SoilMoisturePct, scenarioName);
        AssertClose(actual.LeafWetnessPct, expected.LeafWetnessPct, scenarioName);
    }

    private static void AssertClose(double? actual, double? expected, string scenarioName)
    {
        if (expected is null)
        {
            actual.Should().BeNull(scenarioName);
        }
        else
        {
            actual.Should().NotBeNull(scenarioName);
            actual!.Value.Should().BeApproximately(expected.Value, 1e-6, scenarioName);
        }
    }

    private sealed record FixtureDocument(FixtureScenario[] Scenarios);

    private sealed record FixtureScenario(
        string Name,
        PipelineOptionsModel Options,
        OperationModel[] Operations,
        ExpectedModel Expected);

    private sealed record PipelineOptionsModel(
        string? MinimumPublishInterval,
        bool? MergePartialSamples,
        double? ChangeEpsilon)
    {
        public WeatherIngestOptions ToOptions()
        {
            var options = new WeatherIngestOptions();
            if (!string.IsNullOrWhiteSpace(MinimumPublishInterval))
            {
                options.MinimumPublishInterval = TimeSpan.Parse(MinimumPublishInterval!, CultureInfo.InvariantCulture);
            }

            if (MergePartialSamples.HasValue)
            {
                options.MergePartialSamples = MergePartialSamples.Value;
            }

            if (ChangeEpsilon.HasValue)
            {
                options.ChangeEpsilon = ChangeEpsilon.Value;
            }

            return options;
        }
    }

    private sealed record OperationModel(
        WeatherSampleModel? Ingest,
        bool Flush,
        WeatherSnapshotModel? ExpectPublished);

    private sealed record ExpectedModel(WeatherSnapshotModel? LatestSnapshot);

    private sealed record WeatherSampleModel(
        DateTimeOffset CapturedAt,
        string Source,
        double? TemperatureC,
        double? HumidityPct,
        double? WindKph,
        double? WindDirectionDeg,
        double? WindGustKph,
        double? RainfallMm,
        double? PressureKpa,
        double? DewPointC,
        double? WetBulbC,
        double? DeltaTC,
        double? EvapotranspirationMm,
        double? SolarIrradianceWm2,
        double? UvIndex,
        double? CloudCoverPct,
        double? VisibilityKm,
        double? SoilTempC,
        double? SoilMoisturePct,
        double? LeafWetnessPct,
        bool? ForcePublish)
    {
        public WeatherSample ToSample()
        {
            return new WeatherSample
            {
                CapturedAt = CapturedAt,
                Source = Source,
                TemperatureC = TemperatureC,
                HumidityPct = HumidityPct,
                WindKph = WindKph,
                WindDirectionDeg = WindDirectionDeg,
                WindGustKph = WindGustKph,
                RainfallMm = RainfallMm,
                PressureKpa = PressureKpa,
                DewPointC = DewPointC,
                WetBulbC = WetBulbC,
                DeltaTC = DeltaTC,
                EvapotranspirationMm = EvapotranspirationMm,
                SolarIrradianceWm2 = SolarIrradianceWm2,
                UvIndex = UvIndex,
                CloudCoverPct = CloudCoverPct,
                VisibilityKm = VisibilityKm,
                SoilTempC = SoilTempC,
                SoilMoisturePct = SoilMoisturePct,
                LeafWetnessPct = LeafWetnessPct,
                ForcePublish = ForcePublish ?? false
            };
        }
    }

    private sealed record WeatherSnapshotModel(
        DateTimeOffset CapturedAt,
        string Source,
        double? TemperatureC,
        double? HumidityPct,
        double? WindKph,
        double? WindDirectionDeg,
        double? WindGustKph,
        double? RainfallMm,
        double? PressureKpa,
        double? DewPointC,
        double? WetBulbC,
        double? DeltaTC,
        double? EvapotranspirationMm,
        double? SolarIrradianceWm2,
        double? UvIndex,
        double? CloudCoverPct,
        double? VisibilityKm,
        double? SoilTempC,
        double? SoilMoisturePct,
        double? LeafWetnessPct);
}
