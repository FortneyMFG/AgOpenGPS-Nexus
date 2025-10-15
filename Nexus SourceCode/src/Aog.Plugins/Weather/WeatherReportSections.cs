using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Reporting;

namespace Aog.Plugins.Weather;

/// <summary>
/// Provides access to weather snapshots scoped to a report.
/// </summary>
public interface IWeatherSnapshotProvider
{
    /// <summary>
    /// Retrieves weather snapshots applicable to the supplied report scope.
    /// Snapshots should be returned in chronological order; the contributor
    /// will normalise ordering to ensure deterministic output.
    /// </summary>
    /// <param name="scope">Report scope requesting weather data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Weather snapshots associated with the scope.</returns>
    ValueTask<IReadOnlyList<WeatherSnapshot>> GetSnapshotsAsync(
        ReportScope scope,
        CancellationToken cancellationToken);
}

/// <summary>
/// Base implementation shared by weather report section contributors.
/// Handles snapshot retrieval, caching between prepare/render, and
/// common validation.
/// </summary>
public abstract class WeatherReportSectionContributorBase : IReportSectionContributor
{
    private const string SessionScopeRequiredMessage = "Weather report sections require a session scope.";

    private readonly IWeatherSnapshotProvider _snapshotProvider;
    private readonly Dictionary<string, IReadOnlyList<WeatherSnapshot>> _preparedSnapshots = new(StringComparer.Ordinal);
    private readonly object _lock = new();

    /// <summary>
    /// Initialises a new instance of the <see cref="WeatherReportSectionContributorBase"/> class.
    /// </summary>
    /// <param name="snapshotProvider">Snapshot provider supplying weather data.</param>
    protected WeatherReportSectionContributorBase(IWeatherSnapshotProvider snapshotProvider)
    {
        _snapshotProvider = snapshotProvider ?? throw new ArgumentNullException(nameof(snapshotProvider));
    }

    /// <inheritdoc />
    public abstract ReportSectionDescriptor Descriptor { get; }

    /// <inheritdoc />
    public async ValueTask<ReportSectionPreparationResult> PrepareAsync(
        ReportGenerationContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        if (context.Scope.Kind != ReportScopeKind.Session)
        {
            return new ReportSectionPreparationResult(
                false,
                SessionScopeRequiredMessage,
                new Dictionary<string, string>
                {
                    ["scopeKind"] = context.Scope.Kind.ToString()
                });
        }

        var snapshots = await GetOrFetchSnapshotsAsync(context.Scope, cancellationToken).ConfigureAwait(false);
        if (snapshots.Count == 0)
        {
            return new ReportSectionPreparationResult(
                false,
                Descriptor.MissingDataMessage ?? "Weather data is unavailable for this session.");
        }

        return new ReportSectionPreparationResult(true);
    }

    /// <inheritdoc />
    public async ValueTask<ReportSectionResult> RenderAsync(
        ReportSectionContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        var scope = context.Generation.Scope;
        if (scope.Kind != ReportScopeKind.Session)
        {
            return new ReportSectionResult(
                Descriptor.SectionId,
                ReportSectionStatus.MissingData,
                SessionScopeRequiredMessage,
                diagnostics: new Dictionary<string, string>
                {
                    ["scopeKind"] = scope.Kind.ToString()
                });
        }

        var snapshots = await GetOrFetchSnapshotsAsync(scope, cancellationToken).ConfigureAwait(false);
        try
        {
            if (snapshots.Count == 0)
            {
                return new ReportSectionResult(
                    Descriptor.SectionId,
                    ReportSectionStatus.MissingData,
                    Descriptor.MissingDataMessage ?? "Weather data is unavailable for this session.");
            }

            return await RenderInternalAsync(context, snapshots, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ClearPreparedSnapshots(scope);
        }
    }

    /// <summary>
    /// Renders the section payload using the supplied snapshots.
    /// </summary>
    protected abstract ValueTask<ReportSectionResult> RenderInternalAsync(
        ReportSectionContext context,
        IReadOnlyList<WeatherSnapshot> snapshots,
        CancellationToken cancellationToken);

    private async ValueTask<IReadOnlyList<WeatherSnapshot>> GetOrFetchSnapshotsAsync(
        ReportScope scope,
        CancellationToken cancellationToken)
    {
        if (TryGetPreparedSnapshots(scope, out var prepared))
        {
            return prepared;
        }

        var fetched = await FetchSnapshotsAsync(scope, cancellationToken).ConfigureAwait(false);
        lock (_lock)
        {
            var key = BuildCacheKey(scope);
            if (!_preparedSnapshots.ContainsKey(key))
            {
                _preparedSnapshots[key] = fetched;
            }
        }

        return fetched;
    }

    private bool TryGetPreparedSnapshots(ReportScope scope, out IReadOnlyList<WeatherSnapshot> snapshots)
    {
        lock (_lock)
        {
            return _preparedSnapshots.TryGetValue(BuildCacheKey(scope), out snapshots!);
        }
    }

    private void ClearPreparedSnapshots(ReportScope scope)
    {
        lock (_lock)
        {
            _preparedSnapshots.Remove(BuildCacheKey(scope));
        }
    }

    private async ValueTask<IReadOnlyList<WeatherSnapshot>> FetchSnapshotsAsync(
        ReportScope scope,
        CancellationToken cancellationToken)
    {
        var snapshots = await _snapshotProvider.GetSnapshotsAsync(scope, cancellationToken).ConfigureAwait(false)
            ?? Array.Empty<WeatherSnapshot>();

        if (snapshots.Count == 0)
        {
            return Array.Empty<WeatherSnapshot>();
        }

        var normalised = new List<WeatherSnapshot>(snapshots.Count);
        foreach (var snapshot in snapshots)
        {
            if (snapshot is null)
            {
                continue;
            }

            normalised.Add(snapshot);
        }

        if (normalised.Count == 0)
        {
            return Array.Empty<WeatherSnapshot>();
        }

        normalised.Sort(CompareSnapshots);
        return new ReadOnlyCollection<WeatherSnapshot>(normalised);
    }

    private static string BuildCacheKey(ReportScope scope)
    {
        var key = scope.Kind + ":" + scope.Identifier;
        if (scope.Properties.Count == 0)
        {
            return key;
        }

        var segments = scope.Properties
            .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair => pair.Key + "=" + pair.Value);
        return key + "|" + string.Join("|", segments);
    }

    private static int CompareSnapshots(WeatherSnapshot left, WeatherSnapshot right)
    {
        var comparison = left.CapturedAt.CompareTo(right.CapturedAt);
        if (comparison != 0)
        {
            return comparison;
        }

        return string.Compare(left.Source, right.Source, StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Renders a weather summary section containing aggregate statistics and metadata.
/// </summary>
public sealed class WeatherReportSummarySectionContributor : WeatherReportSectionContributorBase
{
    private static readonly ReportSectionDescriptor SummaryDescriptor = new(
        sectionId: "weather.summary",
        displayName: "Weather Summary",
        capabilities: new[] { "weather" },
        requiredContext: new[] { "session" },
        missingDataMessage: "Weather data was not captured for this session.");

    /// <summary>
    /// Initialises a new instance of the <see cref="WeatherReportSummarySectionContributor"/> class.
    /// </summary>
    public WeatherReportSummarySectionContributor(IWeatherSnapshotProvider snapshotProvider)
        : base(snapshotProvider)
    {
    }

    /// <inheritdoc />
    public override ReportSectionDescriptor Descriptor => SummaryDescriptor;

    /// <inheritdoc />
    protected override ValueTask<ReportSectionResult> RenderInternalAsync(
        ReportSectionContext context,
        IReadOnlyList<WeatherSnapshot> snapshots,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var summary = WeatherSummaryReport.Create(snapshots);
        var result = new ReportSectionResult(
            Descriptor.SectionId,
            ReportSectionStatus.Success,
            payload: summary);
        return ValueTask.FromResult(result);
    }
}

/// <summary>
/// Renders a weather timeline section enumerating individual observations.
/// </summary>
public sealed class WeatherReportTimelineSectionContributor : WeatherReportSectionContributorBase
{
    private static readonly ReportSectionDescriptor TimelineDescriptor = new(
        sectionId: "weather.timeline",
        displayName: "Weather Observations",
        capabilities: new[] { "weather" },
        requiredContext: new[] { "session" },
        missingDataMessage: "Weather observation history is unavailable for this session.");

    /// <summary>
    /// Initialises a new instance of the <see cref="WeatherReportTimelineSectionContributor"/> class.
    /// </summary>
    public WeatherReportTimelineSectionContributor(IWeatherSnapshotProvider snapshotProvider)
        : base(snapshotProvider)
    {
    }

    /// <inheritdoc />
    public override ReportSectionDescriptor Descriptor => TimelineDescriptor;

    /// <inheritdoc />
    protected override ValueTask<ReportSectionResult> RenderInternalAsync(
        ReportSectionContext context,
        IReadOnlyList<WeatherSnapshot> snapshots,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var timeline = WeatherTimelineReport.Create(snapshots);
        var result = new ReportSectionResult(
            Descriptor.SectionId,
            ReportSectionStatus.Success,
            payload: timeline);
        return ValueTask.FromResult(result);
    }
}

/// <summary>
/// Represents the aggregated summary payload returned by the weather summary section.
/// </summary>
public sealed record WeatherSummaryReport
{
    private WeatherSummaryReport(
        DateTimeOffset? firstObservationAt,
        DateTimeOffset? lastObservationAt,
        int observationCount,
        IReadOnlyList<string> sources,
        IReadOnlyList<WeatherMetricSummary> metrics)
    {
        FirstObservationAt = firstObservationAt;
        LastObservationAt = lastObservationAt;
        ObservationCount = observationCount;
        Sources = sources;
        Metrics = metrics;
    }

    /// <summary>Timestamp of the earliest snapshot considered.</summary>
    public DateTimeOffset? FirstObservationAt { get; }

    /// <summary>Timestamp of the latest snapshot considered.</summary>
    public DateTimeOffset? LastObservationAt { get; }

    /// <summary>Total number of observations included in the summary.</summary>
    public int ObservationCount { get; }

    /// <summary>Unique sources that contributed snapshots.</summary>
    public IReadOnlyList<string> Sources { get; }

    /// <summary>Computed metric summaries for the session.</summary>
    public IReadOnlyList<WeatherMetricSummary> Metrics { get; }

    /// <summary>
    /// Creates a summary representation from the supplied snapshot collection.
    /// </summary>
    public static WeatherSummaryReport Create(IReadOnlyList<WeatherSnapshot> snapshots)
    {
        if (snapshots is null)
        {
            throw new ArgumentNullException(nameof(snapshots));
        }

        if (snapshots.Count == 0)
        {
            return new WeatherSummaryReport(null, null, 0, Array.Empty<string>(), Array.Empty<WeatherMetricSummary>());
        }

        var orderedSources = snapshots
            .Select(snapshot => snapshot.Source)
            .Where(source => !string.IsNullOrWhiteSpace(source))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(source => source, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var metrics = BuildMetricSummaries(snapshots);

        return new WeatherSummaryReport(
            snapshots[0].CapturedAt,
            snapshots[^1].CapturedAt,
            snapshots.Count,
            new ReadOnlyCollection<string>(orderedSources),
            new ReadOnlyCollection<WeatherMetricSummary>(metrics));
    }

    private static List<WeatherMetricSummary> BuildMetricSummaries(IReadOnlyList<WeatherSnapshot> snapshots)
    {
        var metrics = new List<WeatherMetricSummary>
        {
            Summarize("weather.temperature.c", "Temperature", "°C", snapshots, s => s.TemperatureC),
            Summarize("weather.humidity.pct", "Relative Humidity", "%", snapshots, s => s.HumidityPct),
            Summarize("weather.wind.speed.kph", "Wind Speed", "km/h", snapshots, s => s.WindKph),
            Summarize("weather.wind.gust.kph", "Wind Gust", "km/h", snapshots, s => s.WindGustKph),
            Summarize("weather.rainfall.mm", "Rainfall", "mm", snapshots, s => s.RainfallMm, includeSum: true),
            Summarize("weather.pressure.kpa", "Barometric Pressure", "kPa", snapshots, s => s.PressureKpa),
            Summarize("weather.dewPoint.c", "Dew Point", "°C", snapshots, s => s.DewPointC),
            Summarize("weather.wetBulb.c", "Wet Bulb", "°C", snapshots, s => s.WetBulbC),
            Summarize("weather.deltaT.c", "Delta T", "°C", snapshots, s => s.DeltaTC),
            Summarize("weather.evapotranspiration.mm", "Evapotranspiration", "mm", snapshots, s => s.EvapotranspirationMm, includeSum: true),
            Summarize("weather.solar.wm2", "Solar Irradiance", "W/m²", snapshots, s => s.SolarIrradianceWm2),
            Summarize("weather.uv.index", "UV Index", null, snapshots, s => s.UvIndex),
            Summarize("weather.visibility.km", "Visibility", "km", snapshots, s => s.VisibilityKm),
            Summarize("weather.soil.temperature.c", "Soil Temperature", "°C", snapshots, s => s.SoilTempC),
            Summarize("weather.soil.moisture.pct", "Soil Moisture", "%", snapshots, s => s.SoilMoisturePct),
            Summarize("weather.leaf.wetness.pct", "Leaf Wetness", "%", snapshots, s => s.LeafWetnessPct)
        };

        metrics.RemoveAll(metric => !metric.HasData);
        return metrics;
    }

    private static WeatherMetricSummary Summarize(
        string metricId,
        string displayName,
        string? unit,
        IReadOnlyList<WeatherSnapshot> snapshots,
        Func<WeatherSnapshot, double?> selector,
        bool includeSum = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(metricId);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentNullException.ThrowIfNull(snapshots);
        ArgumentNullException.ThrowIfNull(selector);

        var values = new List<double>(snapshots.Count);
        for (var i = 0; i < snapshots.Count; i++)
        {
            var value = selector(snapshots[i]);
            if (value.HasValue)
            {
                values.Add(value.Value);
            }
        }

        var latest = snapshots.Count > 0 ? selector(snapshots[^1]) : null;

        double? minimum = null;
        double? maximum = null;
        double? average = null;
        double? sum = null;

        if (values.Count > 0)
        {
            minimum = values.Min();
            maximum = values.Max();
            average = values.Average();
            if (includeSum)
            {
                sum = values.Sum();
            }
        }

        return new WeatherMetricSummary(
            metricId,
            displayName,
            unit,
            minimum,
            maximum,
            average,
            latest,
            sum);
    }
}

/// <summary>
/// Represents summary statistics for a specific weather metric.
/// </summary>
public sealed record WeatherMetricSummary
{
    /// <summary>
    /// Initialises a new instance of the <see cref="WeatherMetricSummary"/> class.
    /// </summary>
    public WeatherMetricSummary(
        string metricId,
        string displayName,
        string? unit,
        double? minimum,
        double? maximum,
        double? average,
        double? latest,
        double? sum)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(metricId);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        MetricId = metricId;
        DisplayName = displayName;
        Unit = unit;
        Minimum = minimum;
        Maximum = maximum;
        Average = average;
        Latest = latest;
        Sum = sum;
    }

    /// <summary>Stable identifier for the metric (for example, <c>weather.temperature.c</c>).</summary>
    public string MetricId { get; }

    /// <summary>Human readable metric name.</summary>
    public string DisplayName { get; }

    /// <summary>Optional unit associated with the metric.</summary>
    public string? Unit { get; }

    /// <summary>Minimum observed value.</summary>
    public double? Minimum { get; }

    /// <summary>Maximum observed value.</summary>
    public double? Maximum { get; }

    /// <summary>Average observed value.</summary>
    public double? Average { get; }

    /// <summary>Latest recorded value.</summary>
    public double? Latest { get; }

    /// <summary>Summed value across all observations (when applicable).</summary>
    public double? Sum { get; }

    /// <summary>Indicates whether the metric contains any observational data.</summary>
    public bool HasData => Minimum.HasValue || Maximum.HasValue || Average.HasValue || Latest.HasValue || Sum.HasValue;
}

/// <summary>
/// Represents the timeline payload containing individual weather observations.
/// </summary>
public sealed record WeatherTimelineReport
{
    private WeatherTimelineReport(IReadOnlyList<WeatherTimelineObservation> observations)
    {
        Observations = observations;
    }

    /// <summary>Ordered timeline of observations.</summary>
    public IReadOnlyList<WeatherTimelineObservation> Observations { get; }

    /// <summary>
    /// Creates a timeline from the supplied snapshot collection.
    /// </summary>
    public static WeatherTimelineReport Create(IReadOnlyList<WeatherSnapshot> snapshots)
    {
        if (snapshots is null)
        {
            throw new ArgumentNullException(nameof(snapshots));
        }

        if (snapshots.Count == 0)
        {
            return new WeatherTimelineReport(Array.Empty<WeatherTimelineObservation>());
        }

        var observations = new List<WeatherTimelineObservation>(snapshots.Count);
        foreach (var snapshot in snapshots)
        {
            observations.Add(new WeatherTimelineObservation(
                snapshot.CapturedAt,
                snapshot.Source,
                snapshot.TemperatureC,
                snapshot.HumidityPct,
                snapshot.WindKph,
                snapshot.WindDirectionDeg,
                snapshot.WindGustKph,
                snapshot.RainfallMm,
                snapshot.PressureKpa,
                snapshot.DewPointC,
                snapshot.WetBulbC,
                snapshot.DeltaTC,
                snapshot.EvapotranspirationMm,
                snapshot.SolarIrradianceWm2,
                snapshot.UvIndex,
                snapshot.CloudCoverPct,
                snapshot.VisibilityKm,
                snapshot.SoilTempC,
                snapshot.SoilMoisturePct,
                snapshot.LeafWetnessPct));
        }

        return new WeatherTimelineReport(new ReadOnlyCollection<WeatherTimelineObservation>(observations));
    }
}

/// <summary>
/// Represents a single observation published in the weather timeline section.
/// </summary>
public sealed record WeatherTimelineObservation
{
    /// <summary>
    /// Initialises a new instance of the <see cref="WeatherTimelineObservation"/> class.
    /// </summary>
    public WeatherTimelineObservation(
        DateTimeOffset capturedAt,
        string source,
        double? temperatureC,
        double? humidityPct,
        double? windKph,
        double? windDirectionDeg,
        double? windGustKph,
        double? rainfallMm,
        double? pressureKpa,
        double? dewPointC,
        double? wetBulbC,
        double? deltaTC,
        double? evapotranspirationMm,
        double? solarIrradianceWm2,
        double? uvIndex,
        double? cloudCoverPct,
        double? visibilityKm,
        double? soilTempC,
        double? soilMoisturePct,
        double? leafWetnessPct)
    {
        CapturedAt = capturedAt;
        Source = source ?? string.Empty;
        TemperatureC = temperatureC;
        HumidityPct = humidityPct;
        WindKph = windKph;
        WindDirectionDeg = windDirectionDeg;
        WindGustKph = windGustKph;
        RainfallMm = rainfallMm;
        PressureKpa = pressureKpa;
        DewPointC = dewPointC;
        WetBulbC = wetBulbC;
        DeltaTC = deltaTC;
        EvapotranspirationMm = evapotranspirationMm;
        SolarIrradianceWm2 = solarIrradianceWm2;
        UvIndex = uvIndex;
        CloudCoverPct = cloudCoverPct;
        VisibilityKm = visibilityKm;
        SoilTempC = soilTempC;
        SoilMoisturePct = soilMoisturePct;
        LeafWetnessPct = leafWetnessPct;
    }

    /// <summary>Observation timestamp.</summary>
    public DateTimeOffset CapturedAt { get; }

    /// <summary>Data source identifier.</summary>
    public string Source { get; }

    /// <summary>Ambient temperature in degrees Celsius.</summary>
    public double? TemperatureC { get; }

    /// <summary>Relative humidity percentage.</summary>
    public double? HumidityPct { get; }

    /// <summary>Wind speed in kilometres per hour.</summary>
    public double? WindKph { get; }

    /// <summary>Wind direction in degrees (0-360).</summary>
    public double? WindDirectionDeg { get; }

    /// <summary>Wind gust speed in kilometres per hour.</summary>
    public double? WindGustKph { get; }

    /// <summary>Rainfall measured in millimetres.</summary>
    public double? RainfallMm { get; }

    /// <summary>Atmospheric pressure in kilopascals.</summary>
    public double? PressureKpa { get; }

    /// <summary>Dew point temperature in degrees Celsius.</summary>
    public double? DewPointC { get; }

    /// <summary>Wet bulb temperature in degrees Celsius.</summary>
    public double? WetBulbC { get; }

    /// <summary>Delta T value in degrees Celsius.</summary>
    public double? DeltaTC { get; }

    /// <summary>Evapotranspiration in millimetres.</summary>
    public double? EvapotranspirationMm { get; }

    /// <summary>Solar irradiance in watts per square metre.</summary>
    public double? SolarIrradianceWm2 { get; }

    /// <summary>UV index reading.</summary>
    public double? UvIndex { get; }

    /// <summary>Cloud cover percentage.</summary>
    public double? CloudCoverPct { get; }

    /// <summary>Visibility distance in kilometres.</summary>
    public double? VisibilityKm { get; }

    /// <summary>Soil temperature in degrees Celsius.</summary>
    public double? SoilTempC { get; }

    /// <summary>Soil moisture percentage.</summary>
    public double? SoilMoisturePct { get; }

    /// <summary>Leaf wetness percentage.</summary>
    public double? LeafWetnessPct { get; }
}
