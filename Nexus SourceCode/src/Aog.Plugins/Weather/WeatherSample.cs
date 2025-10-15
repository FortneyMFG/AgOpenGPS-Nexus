using System;

namespace Aog.Plugins.Weather;

/// <summary>
/// Represents a raw weather reading ingested from a sensor, API, or manual entry.
/// </summary>
public sealed record WeatherSample
{
    /// <summary>
    /// Timestamp when the sample was captured (UTC).
    /// </summary>
    public DateTimeOffset CapturedAt { get; init; }

    /// <summary>
    /// Origin of the sample (sensor ID, API, manual entry, etc.).
    /// </summary>
    public string Source { get; init; } = string.Empty;

    public double? TemperatureC { get; init; }
    public double? HumidityPct { get; init; }
    public double? WindKph { get; init; }
    public double? WindDirectionDeg { get; init; }
    public double? WindGustKph { get; init; }
    public double? RainfallMm { get; init; }
    public double? PressureKpa { get; init; }
    public double? DewPointC { get; init; }
    public double? WetBulbC { get; init; }
    public double? DeltaTC { get; init; }
    public double? EvapotranspirationMm { get; init; }
    public double? SolarIrradianceWm2 { get; init; }
    public double? UvIndex { get; init; }
    public double? CloudCoverPct { get; init; }
    public double? VisibilityKm { get; init; }
    public double? SoilTempC { get; init; }
    public double? SoilMoisturePct { get; init; }
    public double? LeafWetnessPct { get; init; }

    /// <summary>
    /// When set, bypasses the minimum publish interval and emits the sample immediately.
    /// </summary>
    public bool ForcePublish { get; init; }

    /// <summary>
    /// Validates the sample values and throws when any value is out of range.
    /// </summary>
    public void Validate()
    {
        if (CapturedAt == default)
        {
            throw new ArgumentException("CapturedAt must be specified.", nameof(CapturedAt));
        }

        if (string.IsNullOrWhiteSpace(Source))
        {
            throw new ArgumentException("Source must be provided.", nameof(Source));
        }

        EnsureFinite(TemperatureC, nameof(TemperatureC));
        EnsureFinite(WetBulbC, nameof(WetBulbC));
        EnsureFinite(DewPointC, nameof(DewPointC));
        EnsureFinite(DeltaTC, nameof(DeltaTC));
        EnsureFinite(PressureKpa, nameof(PressureKpa));
        EnsureFinite(SolarIrradianceWm2, nameof(SolarIrradianceWm2));
        EnsureFinite(VisibilityKm, nameof(VisibilityKm));
        EnsureFinite(SoilTempC, nameof(SoilTempC));

        EnsurePercent(HumidityPct, nameof(HumidityPct));
        EnsurePercent(SoilMoisturePct, nameof(SoilMoisturePct));
        EnsurePercent(LeafWetnessPct, nameof(LeafWetnessPct));
        EnsurePercent(CloudCoverPct, nameof(CloudCoverPct));

        EnsureNonNegative(WindKph, nameof(WindKph));
        EnsureNonNegative(WindGustKph, nameof(WindGustKph));
        EnsureNonNegative(RainfallMm, nameof(RainfallMm));
        EnsureNonNegative(EvapotranspirationMm, nameof(EvapotranspirationMm));
        EnsureNonNegative(UvIndex, nameof(UvIndex));
        EnsureNonNegative(VisibilityKm, nameof(VisibilityKm));

        EnsureDirection(WindDirectionDeg, nameof(WindDirectionDeg));
    }

    private static void EnsureFinite(double? value, string name)
    {
        if (!value.HasValue)
        {
            return;
        }

        if (!double.IsFinite(value.Value))
        {
            throw new ArgumentOutOfRangeException(name, value, $"{name} must be a finite number.");
        }
    }

    private static void EnsurePercent(double? value, string name)
    {
        if (!value.HasValue)
        {
            return;
        }

        EnsureFinite(value, name);
        if (value.Value is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(name, value, $"{name} must be between 0 and 100.");
        }
    }

    private static void EnsureNonNegative(double? value, string name)
    {
        if (!value.HasValue)
        {
            return;
        }

        EnsureFinite(value, name);
        if (value.Value < 0)
        {
            throw new ArgumentOutOfRangeException(name, value, $"{name} cannot be negative.");
        }
    }

    private static void EnsureDirection(double? value, string name)
    {
        if (!value.HasValue)
        {
            return;
        }

        EnsureFinite(value, name);
        if (value.Value is < 0 or > 360)
        {
            throw new ArgumentOutOfRangeException(name, value, $"{name} must be between 0 and 360 degrees.");
        }
    }
}
