using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Paths;

namespace Aog.Plugins.Weather;

/// <summary>
/// Bridges raw sensor readings into the <see cref="WeatherIngestPipeline"/> and tracks sensor locations.
/// </summary>
public sealed class WeatherSensorAdapter
{
    private readonly WeatherIngestPipeline _pipeline;
    private readonly Dictionary<string, PlanarPoint> _lastKnownLocations = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _gate = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="WeatherSensorAdapter"/> class.
    /// </summary>
    public WeatherSensorAdapter(WeatherIngestPipeline pipeline)
    {
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
    }

    /// <summary>
    /// Ingests a raw sensor reading and publishes a snapshot when the underlying pipeline emits.
    /// </summary>
    public async ValueTask<WeatherObservation?> IngestAsync(WeatherSensorReading reading, CancellationToken cancellationToken = default)
    {
        if (reading is null)
        {
            throw new ArgumentNullException(nameof(reading));
        }

        var sample = reading.ToWeatherSample();
        var location = UpdateLocation(sample.Source, reading.Location);

        var snapshot = await _pipeline.IngestAsync(sample, cancellationToken).ConfigureAwait(false);
        if (snapshot is null)
        {
            return null;
        }

        return new WeatherObservation(snapshot, location);
    }

    /// <summary>
    /// Forces publication of the most recent snapshot along with the best-known sensor location.
    /// </summary>
    public async ValueTask<WeatherObservation?> FlushAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await _pipeline.FlushAsync(cancellationToken).ConfigureAwait(false);
        if (snapshot is null)
        {
            return null;
        }

        var location = TryGetLocation(snapshot.Source);
        return new WeatherObservation(snapshot, location);
    }

    private PlanarPoint? UpdateLocation(string source, PlanarPoint? location)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return location;
        }

        if (location.HasValue)
        {
            lock (_gate)
            {
                _lastKnownLocations[source] = location.Value;
            }

            return location;
        }

        return TryGetLocation(source);
    }

    private PlanarPoint? TryGetLocation(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return null;
        }

        lock (_gate)
        {
            return _lastKnownLocations.TryGetValue(source, out var existing) ? existing : null;
        }
    }
}
