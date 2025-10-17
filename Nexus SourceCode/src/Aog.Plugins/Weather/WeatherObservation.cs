using System;
using Aog.Core.Paths;

namespace Aog.Plugins.Weather;

/// <summary>
/// Couples a published <see cref="WeatherSnapshot"/> with the best-known sensor location.
/// </summary>
public sealed class WeatherObservation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WeatherObservation"/> class.
    /// </summary>
    /// <param name="snapshot">The snapshot produced by the ingest pipeline.</param>
    /// <param name="location">The location associated with the originating sensor, if known.</param>
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
