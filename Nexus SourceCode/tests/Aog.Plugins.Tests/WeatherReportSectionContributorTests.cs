using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Reporting;
using Aog.Plugins.Weather;
using Xunit;

namespace Aog.Plugins.Tests.Weather;

public sealed class WeatherReportSectionContributorTests
{
    [Fact]
    public async Task PrepareAsync_ReturnsNotReady_WhenScopeIsNotSession()
    {
        var provider = new FakeWeatherSnapshotProvider(Array.Empty<WeatherSnapshot>());
        var contributor = new WeatherReportSummarySectionContributor(provider);
        var context = CreateGenerationContext(ReportScopeKind.Job, contributor.Descriptor.SectionId);

        var result = await contributor.PrepareAsync(context, CancellationToken.None);

        Assert.False(result.IsReady);
        Assert.Equal("Weather report sections require a session scope.", result.Reason);
        Assert.Equal("Job", result.Diagnostics["scopeKind"]);
    }

    [Fact]
    public async Task PrepareAsync_ReturnsNotReady_WhenSnapshotsMissing()
    {
        var provider = new FakeWeatherSnapshotProvider(Array.Empty<WeatherSnapshot>());
        var contributor = new WeatherReportSummarySectionContributor(provider);
        var context = CreateGenerationContext(ReportScopeKind.Session, contributor.Descriptor.SectionId);

        var result = await contributor.PrepareAsync(context, CancellationToken.None);

        Assert.False(result.IsReady);
        Assert.Equal("Weather data was not captured for this session.", result.Reason);
    }

    [Fact]
    public async Task RenderAsync_ReturnsMissingData_WhenSnapshotsMissing()
    {
        var provider = new FakeWeatherSnapshotProvider(Array.Empty<WeatherSnapshot>());
        var contributor = new WeatherReportSummarySectionContributor(provider);
        var generationContext = CreateGenerationContext(ReportScopeKind.Session, contributor.Descriptor.SectionId);
        var sectionContext = CreateSectionContext(generationContext);

        var result = await contributor.RenderAsync(sectionContext, CancellationToken.None);

        Assert.Equal(ReportSectionStatus.MissingData, result.Status);
        Assert.Equal("Weather data was not captured for this session.", result.Message);
    }

    [Fact]
    public async Task RenderAsync_ComputesAggregateMetrics()
    {
        var baseTime = new DateTimeOffset(2025, 3, 19, 8, 0, 0, TimeSpan.Zero);
        var snapshots = new[]
        {
            CreateSnapshot(baseTime.AddMinutes(10), "sensor:b", temperatureC: 19.2, humidityPct: 65, windKph: 18,
                windGustKph: 24, rainfallMm: 0.3, pressureKpa: 100.4, dewPointC: 12.0, wetBulbC: 14.0, deltaTC: 5.2,
                evapotranspirationMm: 0.05, solarIrradianceWm2: 340, uvIndex: 4.0, visibilityKm: 12.0, soilTempC: 16.0,
                soilMoisturePct: 31.0, leafWetnessPct: 40.0),
            CreateSnapshot(baseTime, "sensor:a", temperatureC: 20.5, humidityPct: 58, windKph: 15, windGustKph: 21,
                rainfallMm: 0.1, pressureKpa: 101.2, dewPointC: 11.5, wetBulbC: 13.2, deltaTC: 7.3,
                evapotranspirationMm: 0.02, solarIrradianceWm2: 320, uvIndex: 3.5, visibilityKm: 14.0, soilTempC: 15.0,
                soilMoisturePct: 30.0, leafWetnessPct: 38.0),
            CreateSnapshot(baseTime.AddMinutes(5), "sensor:a", temperatureC: 21.3, humidityPct: 55, windKph: 17,
                windGustKph: 22, rainfallMm: 0.0, pressureKpa: 101.0, dewPointC: 12.1, wetBulbC: 14.1, deltaTC: 7.2,
                evapotranspirationMm: 0.03, solarIrradianceWm2: 330, uvIndex: 3.8, visibilityKm: 13.0, soilTempC: 15.5,
                soilMoisturePct: 29.0, leafWetnessPct: 37.0)
        };

        var provider = new FakeWeatherSnapshotProvider(snapshots);
        var contributor = new WeatherReportSummarySectionContributor(provider);
        var generationContext = CreateGenerationContext(ReportScopeKind.Session, contributor.Descriptor.SectionId);
        await contributor.PrepareAsync(generationContext, CancellationToken.None);
        var sectionContext = CreateSectionContext(generationContext);

        var result = await contributor.RenderAsync(sectionContext, CancellationToken.None);

        Assert.Equal(ReportSectionStatus.Success, result.Status);
        var payload = Assert.IsType<WeatherSummaryReport>(result.Payload);
        Assert.Equal(3, payload.ObservationCount);
        Assert.Equal(baseTime, payload.FirstObservationAt);
        Assert.Equal(baseTime.AddMinutes(10), payload.LastObservationAt);
        Assert.Equal(new[] { "sensor:a", "sensor:b" }, payload.Sources);

        var temperature = Assert.Single(payload.Metrics.Where(m => m.MetricId == "weather.temperature.c"));
        Assert.Equal(19.2, temperature.Minimum!.Value, 1);
        Assert.Equal(21.3, temperature.Maximum!.Value, 1);
        Assert.Equal(20.33, Math.Round(temperature.Average!.Value, 2));
        Assert.Equal(19.2, temperature.Latest);

        var rainfall = Assert.Single(payload.Metrics.Where(m => m.MetricId == "weather.rainfall.mm"));
        Assert.Equal(0.0, rainfall.Minimum);
        Assert.Equal(0.3, rainfall.Maximum);
        Assert.Equal(0.13, Math.Round(rainfall.Average!.Value, 2));
        Assert.Equal(0.4, Math.Round(rainfall.Sum!.Value, 2));

        var pressure = Assert.Single(payload.Metrics.Where(m => m.MetricId == "weather.pressure.kpa"));
        Assert.Equal(100.4, pressure.Minimum);
        Assert.Equal(101.2, pressure.Maximum);
    }

    [Fact]
    public async Task RenderAsync_ProducesTimelineOrderedByTimestamp()
    {
        var baseTime = new DateTimeOffset(2025, 3, 19, 9, 0, 0, TimeSpan.Zero);
        var snapshots = new[]
        {
            CreateSnapshot(baseTime.AddMinutes(15), "sensor:z", temperatureC: 19.0, humidityPct: 70, windKph: 10,
                windDirectionDeg: 270, windGustKph: 15, rainfallMm: 0.2, pressureKpa: 100.2, dewPointC: 12.0,
                wetBulbC: 14.0, deltaTC: 5.0, evapotranspirationMm: 0.01, solarIrradianceWm2: 200, uvIndex: 2.0,
                cloudCoverPct: 60.0, visibilityKm: 10.0, soilTempC: 14.0, soilMoisturePct: 33.0, leafWetnessPct: 45.0),
            CreateSnapshot(baseTime, "sensor:a", temperatureC: 22.0, humidityPct: 50, windKph: 12, windDirectionDeg: 180,
                windGustKph: 18, rainfallMm: 0.1, pressureKpa: 101.0, dewPointC: 11.0, wetBulbC: 13.0, deltaTC: 9.0,
                evapotranspirationMm: 0.02, solarIrradianceWm2: 250, uvIndex: 3.0, cloudCoverPct: 40.0, visibilityKm: 16.0,
                soilTempC: 15.0, soilMoisturePct: 28.0, leafWetnessPct: 35.0)
        };

        var provider = new FakeWeatherSnapshotProvider(snapshots);
        var contributor = new WeatherReportTimelineSectionContributor(provider);
        var generationContext = CreateGenerationContext(ReportScopeKind.Session, contributor.Descriptor.SectionId);
        await contributor.PrepareAsync(generationContext, CancellationToken.None);
        var sectionContext = CreateSectionContext(generationContext);

        var result = await contributor.RenderAsync(sectionContext, CancellationToken.None);

        Assert.Equal(ReportSectionStatus.Success, result.Status);
        var payload = Assert.IsType<WeatherTimelineReport>(result.Payload);
        Assert.Equal(2, payload.Observations.Count);
        Assert.Equal(baseTime, payload.Observations[0].CapturedAt);
        Assert.Equal("sensor:a", payload.Observations[0].Source);
        Assert.Equal(180, payload.Observations[0].WindDirectionDeg);
        Assert.Equal(270, payload.Observations[1].WindDirectionDeg);
        Assert.Equal(45.0, payload.Observations[1].LeafWetnessPct);
        Assert.Equal(0.2, payload.Observations[1].RainfallMm);
    }

    [Fact]
    public async Task RenderAsync_ReturnsMissingData_ForTimelineWhenSnapshotsMissing()
    {
        var provider = new FakeWeatherSnapshotProvider(Array.Empty<WeatherSnapshot>());
        var contributor = new WeatherReportTimelineSectionContributor(provider);
        var generationContext = CreateGenerationContext(ReportScopeKind.Session, contributor.Descriptor.SectionId);
        var sectionContext = CreateSectionContext(generationContext);

        var result = await contributor.RenderAsync(sectionContext, CancellationToken.None);

        Assert.Equal(ReportSectionStatus.MissingData, result.Status);
        Assert.Equal("Weather observation history is unavailable for this session.", result.Message);
    }

    private static ReportGenerationContext CreateGenerationContext(ReportScopeKind scopeKind, string sectionId)
    {
        var template = new ReportTemplate(
            "template:weather",
            "Weather Template",
            "1.0.0",
            scopeKind,
            new[] { new ReportTemplateSection(sectionId) },
            new[] { new ReportTemplateOutput("pdf", "PDF Document") });
        var scope = new ReportScope(scopeKind, "scope-001");
        return new ReportGenerationContext(template, scope, new ReportGenerationOptions());
    }

    private static ReportSectionContext CreateSectionContext(ReportGenerationContext generationContext)
    {
        var section = generationContext.Template.Sections[0];
        return new ReportSectionContext(generationContext, section);
    }

    private static WeatherSnapshot CreateSnapshot(
        DateTimeOffset capturedAt,
        string source,
        double? temperatureC = null,
        double? humidityPct = null,
        double? windKph = null,
        double? windDirectionDeg = null,
        double? windGustKph = null,
        double? rainfallMm = null,
        double? pressureKpa = null,
        double? dewPointC = null,
        double? wetBulbC = null,
        double? deltaTC = null,
        double? evapotranspirationMm = null,
        double? solarIrradianceWm2 = null,
        double? uvIndex = null,
        double? cloudCoverPct = null,
        double? visibilityKm = null,
        double? soilTempC = null,
        double? soilMoisturePct = null,
        double? leafWetnessPct = null)
    {
        return new WeatherSnapshot
        {
            CapturedAt = capturedAt,
            Source = source,
            TemperatureC = temperatureC,
            HumidityPct = humidityPct,
            WindKph = windKph,
            WindDirectionDeg = windDirectionDeg,
            WindGustKph = windGustKph,
            RainfallMm = rainfallMm,
            PressureKpa = pressureKpa,
            DewPointC = dewPointC,
            WetBulbC = wetBulbC,
            DeltaTC = deltaTC,
            EvapotranspirationMm = evapotranspirationMm,
            SolarIrradianceWm2 = solarIrradianceWm2,
            UvIndex = uvIndex,
            CloudCoverPct = cloudCoverPct,
            VisibilityKm = visibilityKm,
            SoilTempC = soilTempC,
            SoilMoisturePct = soilMoisturePct,
            LeafWetnessPct = leafWetnessPct
        };
    }

    private sealed class FakeWeatherSnapshotProvider : IWeatherSnapshotProvider
    {
        private readonly Func<ReportScope, IReadOnlyList<WeatherSnapshot>> _selector;

        public FakeWeatherSnapshotProvider(IReadOnlyList<WeatherSnapshot> snapshots)
            : this(_ => snapshots)
        {
        }

        public FakeWeatherSnapshotProvider(Func<ReportScope, IReadOnlyList<WeatherSnapshot>> selector)
        {
            _selector = selector ?? throw new ArgumentNullException(nameof(selector));
        }

        public ValueTask<IReadOnlyList<WeatherSnapshot>> GetSnapshotsAsync(ReportScope scope, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var snapshots = _selector(scope) ?? Array.Empty<WeatherSnapshot>();
            return ValueTask.FromResult(snapshots);
        }
    }
}
