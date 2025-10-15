using System;

namespace Aog.Plugins.Weather;

/// <summary>
/// Configuration for publishing weather overlays as agronomic layers.
/// </summary>
public sealed class WeatherOverlayOptions
{
    /// <summary>
    /// Identifier assigned to the published layer.
    /// </summary>
    public string LayerId { get; set; } = "weather.overlay.temperature";

    /// <summary>
    /// Logical kind used by consumers to route the overlay.
    /// </summary>
    public string Kind { get; set; } = "weather.temperature";

    /// <summary>
    /// Display units attached to layer values.
    /// </summary>
    public string Units { get; set; } = "°C";

    /// <summary>
    /// User or system attribution for layer creation.
    /// </summary>
    public string CreatedBy { get; set; } = "plugin:weather";

    /// <summary>
    /// Source metadata recorded in layer provenance.
    /// </summary>
    public string Source { get; set; } = "plugin:weather";

    /// <summary>
    /// Transform metadata recorded in layer provenance.
    /// </summary>
    public string Transform { get; set; } = "weather.temperature";

    /// <summary>
    /// Actor recorded in the provenance entry.
    /// </summary>
    public string Actor { get; set; } = "plugin:weather";

    /// <summary>
    /// Metric extracted from <see cref="WeatherSnapshot"/> values.
    /// </summary>
    public WeatherOverlayMetric Metric { get; set; } = WeatherOverlayMetric.Temperature;

    /// <summary>
    /// Square cell edge length in metres for generated overlay cells.
    /// </summary>
    public double CellSizeMeters { get; set; } = 25d;

    /// <summary>
    /// Minimum interval between automatic overlay publications.
    /// </summary>
    public TimeSpan PublishInterval { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Validates option values.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(LayerId))
        {
            throw new ArgumentOutOfRangeException(nameof(LayerId), "Layer identifier must be provided.");
        }

        if (string.IsNullOrWhiteSpace(Kind))
        {
            throw new ArgumentOutOfRangeException(nameof(Kind), "Layer kind must be provided.");
        }

        if (string.IsNullOrWhiteSpace(Units))
        {
            throw new ArgumentOutOfRangeException(nameof(Units), "Units must be provided.");
        }

        if (string.IsNullOrWhiteSpace(CreatedBy))
        {
            throw new ArgumentOutOfRangeException(nameof(CreatedBy), "CreatedBy must be provided.");
        }

        if (string.IsNullOrWhiteSpace(Source))
        {
            throw new ArgumentOutOfRangeException(nameof(Source), "Source must be provided.");
        }

        if (string.IsNullOrWhiteSpace(Transform))
        {
            throw new ArgumentOutOfRangeException(nameof(Transform), "Transform must be provided.");
        }

        if (string.IsNullOrWhiteSpace(Actor))
        {
            throw new ArgumentOutOfRangeException(nameof(Actor), "Actor must be provided.");
        }

        if (double.IsNaN(CellSizeMeters) || double.IsInfinity(CellSizeMeters) || CellSizeMeters <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(CellSizeMeters), CellSizeMeters, "Cell size must be a positive finite value.");
        }

        if (PublishInterval < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(PublishInterval), PublishInterval, "Publish interval cannot be negative.");
        }
    }
}

/// <summary>
/// Metrics that can be projected into a weather overlay.
/// </summary>
public enum WeatherOverlayMetric
{
    Temperature,
    Humidity,
    Rainfall,
    WindSpeed,
    WindGust,
    Pressure,
    SolarIrradiance,
    UvIndex,
    DewPoint,
    WetBulb,
    DeltaT,
    Evapotranspiration,
    Visibility,
    SoilTemperature,
    SoilMoisture,
    LeafWetness
}
