using System;
using Aog.Core.Paths;

namespace Aog.Plugins.Weather;

/// <summary>
/// Represents a raw weather observation emitted by a sensor or API prior to unit normalization.
/// </summary>
public sealed record WeatherSensorReading
{
    /// <summary>
    /// Timestamp when the observation was captured (UTC).
    /// </summary>
    public DateTimeOffset CapturedAt { get; init; }

    /// <summary>
    /// Identifier for the originating sensor or integration (e.g. "sensor:wx").
    /// </summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>
    /// Optional planar location associated with the sensor reading.
    /// </summary>
    public PlanarPoint? Location { get; init; }

    public double? Temperature { get; init; }
    public TemperatureUnit TemperatureUnit { get; init; } = TemperatureUnit.Celsius;

    public double? HumidityPct { get; init; }

    public double? WindSpeed { get; init; }
    public WindSpeedUnit WindSpeedUnit { get; init; } = WindSpeedUnit.KilometersPerHour;

    public double? WindDirectionDeg { get; init; }

    public double? WindGust { get; init; }
    public WindSpeedUnit WindGustUnit { get; init; } = WindSpeedUnit.KilometersPerHour;

    public double? Rainfall { get; init; }
    public RainfallUnit RainfallUnit { get; init; } = RainfallUnit.Millimeters;

    public double? Pressure { get; init; }
    public PressureUnit PressureUnit { get; init; } = PressureUnit.Kilopascals;

    public double? DewPoint { get; init; }
    public TemperatureUnit DewPointUnit { get; init; } = TemperatureUnit.Celsius;

    public double? WetBulb { get; init; }
    public TemperatureUnit WetBulbUnit { get; init; } = TemperatureUnit.Celsius;

    public double? DeltaT { get; init; }

    public double? Evapotranspiration { get; init; }
    public RainfallUnit EvapotranspirationUnit { get; init; } = RainfallUnit.Millimeters;

    public double? SolarIrradianceWm2 { get; init; }
    public double? UvIndex { get; init; }
    public double? CloudCoverPct { get; init; }

    public double? Visibility { get; init; }
    public DistanceUnit VisibilityUnit { get; init; } = DistanceUnit.Kilometers;

    public double? SoilTemp { get; init; }
    public TemperatureUnit SoilTempUnit { get; init; } = TemperatureUnit.Celsius;

    public double? SoilMoisturePct { get; init; }
    public double? LeafWetnessPct { get; init; }

    /// <summary>
    /// When set, bypasses the ingest pipeline interval checks and publishes immediately.
    /// </summary>
    public bool ForcePublish { get; init; }

    /// <summary>
    /// Converts the reading into a normalized <see cref="WeatherSample"/>.
    /// </summary>
    public WeatherSample ToWeatherSample()
    {
        return new WeatherSample
        {
            CapturedAt = CapturedAt,
            Source = Source ?? string.Empty,
            TemperatureC = ConvertTemperature(Temperature, TemperatureUnit),
            HumidityPct = HumidityPct,
            WindKph = ConvertWind(WindSpeed, WindSpeedUnit),
            WindDirectionDeg = WindDirectionDeg,
            WindGustKph = ConvertWind(WindGust, WindGustUnit),
            RainfallMm = ConvertRainfall(Rainfall, RainfallUnit),
            PressureKpa = ConvertPressure(Pressure, PressureUnit),
            DewPointC = ConvertTemperature(DewPoint, DewPointUnit),
            WetBulbC = ConvertTemperature(WetBulb, WetBulbUnit),
            DeltaTC = DeltaT,
            EvapotranspirationMm = ConvertRainfall(Evapotranspiration, EvapotranspirationUnit),
            SolarIrradianceWm2 = SolarIrradianceWm2,
            UvIndex = UvIndex,
            CloudCoverPct = CloudCoverPct,
            VisibilityKm = ConvertDistance(Visibility, VisibilityUnit),
            SoilTempC = ConvertTemperature(SoilTemp, SoilTempUnit),
            SoilMoisturePct = SoilMoisturePct,
            LeafWetnessPct = LeafWetnessPct,
            ForcePublish = ForcePublish
        };
    }

    private static double? ConvertTemperature(double? value, TemperatureUnit unit)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return unit switch
        {
            TemperatureUnit.Celsius => value,
            TemperatureUnit.Fahrenheit => (value - 32d) * 5d / 9d,
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unsupported temperature unit."),
        };
    }

    private static double? ConvertWind(double? value, WindSpeedUnit unit)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return unit switch
        {
            WindSpeedUnit.KilometersPerHour => value,
            WindSpeedUnit.MetersPerSecond => value * 3.6d,
            WindSpeedUnit.MilesPerHour => value * 1.609344d,
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unsupported wind speed unit."),
        };
    }

    private static double? ConvertRainfall(double? value, RainfallUnit unit)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return unit switch
        {
            RainfallUnit.Millimeters => value,
            RainfallUnit.Inches => value * 25.4d,
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unsupported rainfall unit."),
        };
    }

    private static double? ConvertPressure(double? value, PressureUnit unit)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return unit switch
        {
            PressureUnit.Kilopascals => value,
            PressureUnit.Hectopascals => value / 10d,
            PressureUnit.Millibars => value / 10d,
            PressureUnit.InchesOfMercury => value * 3.386389d,
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unsupported pressure unit."),
        };
    }

    private static double? ConvertDistance(double? value, DistanceUnit unit)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return unit switch
        {
            DistanceUnit.Kilometers => value,
            DistanceUnit.Meters => value / 1000d,
            DistanceUnit.Miles => value * 1.609344d,
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unsupported distance unit."),
        };
    }
}

/// <summary>
/// Supported temperature units for sensor observations.
/// </summary>
public enum TemperatureUnit
{
    Celsius,
    Fahrenheit
}

/// <summary>
/// Supported wind speed units for sensor observations.
/// </summary>
public enum WindSpeedUnit
{
    KilometersPerHour,
    MetersPerSecond,
    MilesPerHour
}

/// <summary>
/// Supported rainfall/precipitation units for sensor observations.
/// </summary>
public enum RainfallUnit
{
    Millimeters,
    Inches
}

/// <summary>
/// Supported pressure units for sensor observations.
/// </summary>
public enum PressureUnit
{
    Kilopascals,
    Hectopascals,
    Millibars,
    InchesOfMercury
}

/// <summary>
/// Supported distance units for visibility measurements.
/// </summary>
public enum DistanceUnit
{
    Kilometers,
    Meters,
    Miles
}
