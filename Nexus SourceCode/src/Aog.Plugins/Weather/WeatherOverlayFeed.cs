using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Eventing;
using Aog.Core.Layers;
using Aog.Core.Paths;

namespace Aog.Plugins.Weather;

/// <summary>
/// Publishes weather overlays based on the latest observations per sensor.
/// </summary>
public sealed class WeatherOverlayFeed
{
    private readonly IEventBus _eventBus;
    private readonly WeatherOverlayOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly Dictionary<string, WeatherObservation> _observations = new(StringComparer.OrdinalIgnoreCase);
    private DateTimeOffset _lastPublish = DateTimeOffset.MinValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="WeatherOverlayFeed"/> class.
    /// </summary>
    public WeatherOverlayFeed(IEventBus eventBus, WeatherOverlayOptions options, TimeProvider? timeProvider = null)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _options.Validate();
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Updates the overlay state with a new observation and publishes when criteria are met.
    /// </summary>
    public async ValueTask PublishAsync(WeatherObservation observation, CancellationToken cancellationToken = default)
    {
        if (observation is null)
        {
            throw new ArgumentNullException(nameof(observation));
        }

        var key = observation.Snapshot.Source;
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        var effectiveObservation = EnsureLocation(observation, key);
        if (effectiveObservation is null)
        {
            return;
        }

        _observations[key] = effectiveObservation;

        var now = _timeProvider.GetUtcNow();
        if (ShouldPublish(now))
        {
            await PublishLayerAsync(now, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Forces publication using the latest observations.
    /// </summary>
    public ValueTask FlushAsync(CancellationToken cancellationToken = default)
    {
        if (_observations.Count == 0)
        {
            return ValueTask.CompletedTask;
        }

        var timestamp = _timeProvider.GetUtcNow();
        if (_lastPublish != DateTimeOffset.MinValue && _options.PublishInterval > TimeSpan.Zero)
        {
            var earliestAllowed = _lastPublish + _options.PublishInterval;
            if (timestamp < earliestAllowed)
            {
                timestamp = earliestAllowed;
            }
        }

        return PublishLayerAsync(timestamp, cancellationToken);
    }

    /// <summary>
    /// Creates a snapshot of the current overlay without publishing it.
    /// </summary>
    public AgronomicLayerDocument? CreateLayerSnapshot()
    {
        if (_observations.Count == 0)
        {
            return null;
        }

        return CreateLayer(_timeProvider.GetUtcNow());
    }

    private WeatherObservation? EnsureLocation(WeatherObservation observation, string key)
    {
        if (observation.Location is not PlanarPoint location)
        {
            if (!_observations.TryGetValue(key, out var existing) || existing.Location is not PlanarPoint existingLocation)
            {
                return null;
            }

            return new WeatherObservation(observation.Snapshot, existingLocation);
        }

        return observation;
    }

    private bool ShouldPublish(DateTimeOffset now)
    {
        if (_observations.Count == 0)
        {
            return false;
        }

        if (_lastPublish == DateTimeOffset.MinValue)
        {
            return true;
        }

        if (_options.PublishInterval == TimeSpan.Zero)
        {
            return true;
        }

        return now - _lastPublish >= _options.PublishInterval;
    }

    private async ValueTask PublishLayerAsync(DateTimeOffset timestamp, CancellationToken cancellationToken)
    {
        var layer = CreateLayer(timestamp);
        if (layer is null)
        {
            return;
        }

        await _eventBus.PublishAsync(layer, cancellationToken).ConfigureAwait(false);
        _lastPublish = timestamp;
    }

    private AgronomicLayerDocument? CreateLayer(DateTimeOffset timestamp)
    {
        var cells = _observations.Values
            .Where(o => o.Location is PlanarPoint && SelectValue(o.Snapshot).HasValue)
            .Select(o => new AgronomicLayerCell(
                o.Location!.Value,
                _options.CellSizeMeters,
                SelectValue(o.Snapshot)!.Value))
            .OrderBy(cell => cell.Position.Northing)
            .ThenBy(cell => cell.Position.Easting)
            .ToList();

        if (cells.Count == 0)
        {
            return null;
        }

        var provenance = new LayerProvenance(
            _options.Source,
            _options.Transform,
            ComputeHash(cells),
            timestamp,
            _options.Actor);

        return new AgronomicLayerDocument(
            _options.LayerId,
            _options.Kind,
            _options.Units,
            timestamp,
            _options.CreatedBy,
            cells,
            provenance);
    }

    private double? SelectValue(WeatherSnapshot snapshot) => _options.Metric switch
    {
        WeatherOverlayMetric.Temperature => snapshot.TemperatureC,
        WeatherOverlayMetric.Humidity => snapshot.HumidityPct,
        WeatherOverlayMetric.Rainfall => snapshot.RainfallMm,
        WeatherOverlayMetric.WindSpeed => snapshot.WindKph,
        WeatherOverlayMetric.WindGust => snapshot.WindGustKph,
        WeatherOverlayMetric.Pressure => snapshot.PressureKpa,
        WeatherOverlayMetric.SolarIrradiance => snapshot.SolarIrradianceWm2,
        WeatherOverlayMetric.UvIndex => snapshot.UvIndex,
        WeatherOverlayMetric.DewPoint => snapshot.DewPointC,
        WeatherOverlayMetric.WetBulb => snapshot.WetBulbC,
        WeatherOverlayMetric.DeltaT => snapshot.DeltaTC,
        WeatherOverlayMetric.Evapotranspiration => snapshot.EvapotranspirationMm,
        WeatherOverlayMetric.Visibility => snapshot.VisibilityKm,
        WeatherOverlayMetric.SoilTemperature => snapshot.SoilTempC,
        WeatherOverlayMetric.SoilMoisture => snapshot.SoilMoisturePct,
        WeatherOverlayMetric.LeafWetness => snapshot.LeafWetnessPct,
        _ => throw new ArgumentOutOfRangeException(nameof(_options.Metric), _options.Metric, "Unsupported overlay metric."),
    };

    private static string ComputeHash(IReadOnlyList<AgronomicLayerCell> cells)
    {
        using var sha = SHA256.Create();
        var builder = new StringBuilder();
        foreach (var cell in cells)
        {
            builder.AppendFormat(
                CultureInfo.InvariantCulture,
                "{0:F3},{1:F3},{2:F3},{3:G17};",
                cell.Position.Easting,
                cell.Position.Northing,
                cell.CellSizeMeters,
                cell.Value);
        }

        var bytes = Encoding.UTF8.GetBytes(builder.ToString());
        var hash = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }
}
