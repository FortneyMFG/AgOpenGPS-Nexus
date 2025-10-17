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

    /// <summary>
    /// Ambient air temperature measurement supplied by the sensor.
    /// </summary>
    public double? Temperature { get; init; }

    /// <summary>
    /// Unit associated with <see cref="Temperature"/>.
    /// </summary>
    public TemperatureUnit TemperatureUnit { get; init; } = TemperatureUnit.Celsius;

    /// <summary>
    /// Relative humidity percentage (0-100) reported by the sensor.
    /// </summary>
    public double? HumidityPct { get; init; }

    /// <summary>
    /// Sustained wind speed reading provided by the sensor.
    /// </summary>
    public double? WindSpeed { get; init; }

    /// <summary>
    /// Unit associated with <see cref="WindSpeed"/>.
    /// </summary>
    public WindSpeedUnit WindSpeedUnit { get; init; } = WindSpeedUnit.KilometersPerHour;

    /// <summary>
    /// Wind direction in degrees where 0 represents north.
    /// </summary>
    public double? WindDirectionDeg { get; init; }

    /// <summary>
    /// Peak wind gust speed measured during the sampling window.
    /// </summary>
    public double? WindGust { get; init; }

    /// <summary>
    /// Unit associated with <see cref="WindGust"/>.
    /// </summary>
    public WindSpeedUnit WindGustUnit { get; init; } = WindSpeedUnit.KilometersPerHour;

    /// <summary>
    /// Total precipitation accumulated during the sampling window.
    /// </summary>
    public double? Rainfall { get; init; }

    /// <summary>
    /// Unit associated with <see cref="Rainfall"/>.
    /// </summary>
    public RainfallUnit RainfallUnit { get; init; } = RainfallUnit.Millimeters;

    /// <summary>
    /// Atmospheric pressure reading provided by the sensor.
    /// </summary>
    public double? Pressure { get; init; }

    /// <summary>
    /// Unit associated with <see cref="Pressure"/>.
    /// </summary>
    public PressureUnit PressureUnit { get; init; } = PressureUnit.Kilopascals;

    /// <summary>
    /// Dew point temperature derived by the sensor or upstream service.
    /// </summary>
    public double? DewPoint { get; init; }

    /// <summary>
    /// Unit associated with <see cref="DewPoint"/>.
    /// </summary>
    public TemperatureUnit DewPointUnit { get; init; } = TemperatureUnit.Celsius;

    /// <summary>
    /// Wet bulb temperature reported by the sensor.
    /// </summary>
    public double? WetBulb { get; init; }

    /// <summary>
    /// Unit associated with <see cref="WetBulb"/>.
    /// </summary>
    public TemperatureUnit WetBulbUnit { get; init; } = TemperatureUnit.Celsius;

    /// <summary>
    /// Delta-T value reported by the sensor, typically dew point minus wet bulb.
    /// </summary>
    public double? DeltaT { get; init; }

    /// <summary>
    /// Evapotranspiration estimate provided by the sensor or upstream service.
    /// </summary>
    public double? Evapotranspiration { get; init; }

    /// <summary>
    /// Unit associated with <see cref="Evapotranspiration"/>.
    /// </summary>
    public RainfallUnit EvapotranspirationUnit { get; init; } = RainfallUnit.Millimeters;

    /// <summary>
    /// Solar irradiance measured in watts per square meter.
    /// </summary>
    public double? SolarIrradianceWm2 { get; init; }

    /// <summary>
    /// UV index reported by the sensor.
    /// </summary>
    public double? UvIndex { get; init; }

    /// <summary>
    /// Estimated cloud cover percentage (0-100).
    /// </summary>
    public double? CloudCoverPct { get; init; }

    /// <summary>
    /// Horizontal visibility distance reported by the sensor.
    /// </summary>
    public double? Visibility { get; init; }

    /// <summary>
    /// Unit associated with <see cref="Visibility"/>.
    /// </summary>
    public DistanceUnit VisibilityUnit { get; init; } = DistanceUnit.Kilometers;

    /// <summary>
    /// Soil temperature measurement supplied by the sensor.
    /// </summary>
    public double? SoilTemp { get; init; }

    /// <summary>
    /// Unit associated with <see cref="SoilTemp"/>.
    /// </summary>
    public TemperatureUnit SoilTempUnit { get; init; } = TemperatureUnit.Celsius;

    /// <summary>
    /// Volumetric soil moisture percentage (0-100).
    /// </summary>
    public double? SoilMoisturePct { get; init; }

    /// <summary>
    /// Leaf wetness percentage reported by the sensor.
    /// </summary>
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
    /// <summary>
    /// Temperature expressed in degrees Celsius.
    /// </summary>
    Celsius,

    /// <summary>
    /// Temperature expressed in degrees Fahrenheit.
    /// </summary>
    Fahrenheit
}

/// <summary>
/// Supported wind speed units for sensor observations.
/// </summary>
public enum WindSpeedUnit
{
    /// <summary>
    /// Wind speed in kilometers per hour.
    /// </summary>
    KilometersPerHour,

    /// <summary>
    /// Wind speed in meters per second.
    /// </summary>
    MetersPerSecond,

    /// <summary>
    /// Wind speed in miles per hour.
    /// </summary>
    MilesPerHour
}

/// <summary>
/// Supported rainfall/precipitation units for sensor observations.
/// </summary>
public enum RainfallUnit
{
    /// <summary>
    /// Rainfall depth in millimeters.
    /// </summary>
    Millimeters,

    /// <summary>
    /// Rainfall depth in inches.
    /// </summary>
    Inches
}

/// <summary>
/// Supported pressure units for sensor observations.
/// </summary>
public enum PressureUnit
{
    /// <summary>
    /// Pressure expressed in kilopascals.
    /// </summary>
    Kilopascals,

    /// <summary>
    /// Pressure expressed in hectopascals.
    /// </summary>
    Hectopascals,

    /// <summary>
    /// Pressure expressed in millibars.
    /// </summary>
    Millibars,

    /// <summary>
    /// Pressure expressed in inches of mercury.
    /// </summary>
    InchesOfMercury
}

/// <summary>
/// Supported distance units for visibility measurements.
/// </summary>
public enum DistanceUnit
{
    /// <summary>
    /// Distance expressed in kilometers.
    /// </summary>
    Kilometers,

    /// <summary>
    /// Distance expressed in meters.
    /// </summary>
    Meters,

    /// <summary>
    /// Distance expressed in miles.
    /// </summary>
    Miles
}
