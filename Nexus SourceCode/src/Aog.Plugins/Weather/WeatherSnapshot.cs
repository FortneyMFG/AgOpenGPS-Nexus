using System;

namespace Aog.Plugins.Weather;

/// <summary>
/// Canonical weather snapshot persisted with session records and broadcast to interested plugins.
/// </summary>
public sealed record WeatherSnapshot
{
    public DateTimeOffset CapturedAt { get; init; }
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
    /// Creates a snapshot from the provided sample, optionally merging missing values from the previous snapshot.
    /// </summary>
    public static WeatherSnapshot Merge(WeatherSnapshot? previous, WeatherSample sample, bool mergePartial)
    {
        if (sample is null)
        {
            throw new ArgumentNullException(nameof(sample));
        }

        return new WeatherSnapshot
        {
            CapturedAt = sample.CapturedAt,
            Source = sample.Source,
            TemperatureC = sample.TemperatureC ?? (mergePartial ? previous?.TemperatureC : null),
            HumidityPct = sample.HumidityPct ?? (mergePartial ? previous?.HumidityPct : null),
            WindKph = sample.WindKph ?? (mergePartial ? previous?.WindKph : null),
            WindDirectionDeg = sample.WindDirectionDeg ?? (mergePartial ? previous?.WindDirectionDeg : null),
            WindGustKph = sample.WindGustKph ?? (mergePartial ? previous?.WindGustKph : null),
            RainfallMm = sample.RainfallMm ?? (mergePartial ? previous?.RainfallMm : null),
            PressureKpa = sample.PressureKpa ?? (mergePartial ? previous?.PressureKpa : null),
            DewPointC = sample.DewPointC ?? (mergePartial ? previous?.DewPointC : null),
            WetBulbC = sample.WetBulbC ?? (mergePartial ? previous?.WetBulbC : null),
            DeltaTC = sample.DeltaTC ?? (mergePartial ? previous?.DeltaTC : null),
            EvapotranspirationMm = sample.EvapotranspirationMm ?? (mergePartial ? previous?.EvapotranspirationMm : null),
            SolarIrradianceWm2 = sample.SolarIrradianceWm2 ?? (mergePartial ? previous?.SolarIrradianceWm2 : null),
            UvIndex = sample.UvIndex ?? (mergePartial ? previous?.UvIndex : null),
            CloudCoverPct = sample.CloudCoverPct ?? (mergePartial ? previous?.CloudCoverPct : null),
            VisibilityKm = sample.VisibilityKm ?? (mergePartial ? previous?.VisibilityKm : null),
            SoilTempC = sample.SoilTempC ?? (mergePartial ? previous?.SoilTempC : null),
            SoilMoisturePct = sample.SoilMoisturePct ?? (mergePartial ? previous?.SoilMoisturePct : null),
            LeafWetnessPct = sample.LeafWetnessPct ?? (mergePartial ? previous?.LeafWetnessPct : null)
        };
    }

    /// <summary>
    /// Computes derived values such as dew point, wet bulb, and delta T when they are missing.
    /// </summary>
    public static WeatherSnapshot NormalizeDerivedValues(WeatherSnapshot snapshot)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        var result = snapshot;
        if (!result.DewPointC.HasValue && result.TemperatureC.HasValue && result.HumidityPct.HasValue)
        {
            var dewPoint = WeatherComputation.TryComputeDewPoint(result.TemperatureC.Value, result.HumidityPct.Value);
            if (dewPoint.HasValue)
            {
                result = result with { DewPointC = dewPoint };
            }
        }

        if (!result.WetBulbC.HasValue && result.TemperatureC.HasValue && result.HumidityPct.HasValue)
        {
            var wetBulb = WeatherComputation.TryComputeWetBulb(result.TemperatureC.Value, result.HumidityPct.Value);
            if (wetBulb.HasValue)
            {
                result = result with { WetBulbC = wetBulb };
            }
        }

        if (!result.DeltaTC.HasValue && result.TemperatureC.HasValue && result.WetBulbC.HasValue)
        {
            result = result with { DeltaTC = result.TemperatureC.Value - result.WetBulbC.Value };
        }

        return result;
    }

    /// <summary>
    /// Normalizes value ranges to ensure consistency with session schema expectations.
    /// </summary>
    public static WeatherSnapshot Clamp(WeatherSnapshot snapshot)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        return snapshot with
        {
            HumidityPct = ClampPercent(snapshot.HumidityPct),
            SoilMoisturePct = ClampPercent(snapshot.SoilMoisturePct),
            LeafWetnessPct = ClampPercent(snapshot.LeafWetnessPct),
            CloudCoverPct = ClampPercent(snapshot.CloudCoverPct),
            WindKph = ClampNonNegative(snapshot.WindKph),
            WindGustKph = ClampNonNegative(snapshot.WindGustKph),
            RainfallMm = ClampNonNegative(snapshot.RainfallMm),
            EvapotranspirationMm = ClampNonNegative(snapshot.EvapotranspirationMm),
            UvIndex = ClampNonNegative(snapshot.UvIndex),
            VisibilityKm = ClampNonNegative(snapshot.VisibilityKm),
            WindDirectionDeg = NormalizeDirection(snapshot.WindDirectionDeg)
        };
    }

    private static double? ClampPercent(double? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return Math.Clamp(value.Value, 0d, 100d);
    }

    private static double? ClampNonNegative(double? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return value.Value < 0 ? 0 : value;
    }

    private static double? NormalizeDirection(double? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        var normalized = value.Value % 360d;
        if (normalized < 0)
        {
            normalized += 360d;
        }

        if (normalized == 360d)
        {
            normalized = 0d;
        }

        return normalized;
    }
}
