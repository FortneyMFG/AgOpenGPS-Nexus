using System;
using Aog.Core.Paths;

namespace Aog.Plugins.Weather;

/// <summary>
/// Couples a published <see cref="WeatherSnapshot"/> with the best-known sensor location.
/// </summary>
public sealed class WeatherObservation
{
    public WeatherObservation(WeatherSnapshot snapshot, PlanarPoint? location)
    {
        Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        Location = location;
    }

    /// <summary>
    /// Snapshot produced by the ingest pipeline.
    /// </summary>
    public WeatherSnapshot Snapshot { get; }

    /// <summary>
    /// Location associated with the originating sensor, if known.
    /// </summary>
    public PlanarPoint? Location { get; }
}
