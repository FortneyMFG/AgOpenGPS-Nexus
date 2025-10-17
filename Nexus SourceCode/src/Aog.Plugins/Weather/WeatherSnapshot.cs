using System;

namespace Aog.Plugins.Weather;

/// <summary>
/// Canonical weather snapshot persisted with session records and broadcast to interested plugins.
/// </summary>
public sealed record WeatherSnapshot
{
    /// <summary>
    /// Gets or sets the timestamp when the snapshot was captured.
    /// </summary>
    public DateTimeOffset CapturedAt { get; init; }

    /// <summary>
    /// Gets or sets the identifier for the data provider that produced the snapshot.
    /// </summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the ambient air temperature in degrees Celsius.
    /// </summary>
    public double? TemperatureC { get; init; }

    /// <summary>
    /// Gets or sets the relative humidity percentage.
    /// </summary>
    public double? HumidityPct { get; init; }

    /// <summary>
    /// Gets or sets the sustained wind speed in kilometres per hour.
    /// </summary>
    public double? WindKph { get; init; }

    /// <summary>
    /// Gets or sets the wind direction in degrees, following meteorological convention.
    /// </summary>
    public double? WindDirectionDeg { get; init; }

    /// <summary>
    /// Gets or sets the peak wind gust measured in kilometres per hour.
    /// </summary>
    public double? WindGustKph { get; init; }

    /// <summary>
    /// Gets or sets the total rainfall measured in millimetres.
    /// </summary>
    public double? RainfallMm { get; init; }

    /// <summary>
    /// Gets or sets the barometric pressure measured in kilopascals.
    /// </summary>
    public double? PressureKpa { get; init; }

    /// <summary>
    /// Gets or sets the dew point temperature in degrees Celsius.
    /// </summary>
    public double? DewPointC { get; init; }

    /// <summary>
    /// Gets or sets the wet-bulb temperature in degrees Celsius.
    /// </summary>
    public double? WetBulbC { get; init; }

    /// <summary>
    /// Gets or sets the Delta T (dry bulb minus wet bulb) temperature in degrees Celsius.
    /// </summary>
    public double? DeltaTC { get; init; }

    /// <summary>
    /// Gets or sets the evapotranspiration amount in millimetres.
    /// </summary>
    public double? EvapotranspirationMm { get; init; }

    /// <summary>
    /// Gets or sets the solar irradiance in watts per square metre.
    /// </summary>
    public double? SolarIrradianceWm2 { get; init; }

    /// <summary>
    /// Gets or sets the ultraviolet index.
    /// </summary>
    public double? UvIndex { get; init; }

    /// <summary>
    /// Gets or sets the estimated cloud cover percentage.
    /// </summary>
    public double? CloudCoverPct { get; init; }

    /// <summary>
    /// Gets or sets the visibility distance in kilometres.
    /// </summary>
    public double? VisibilityKm { get; init; }

    /// <summary>
    /// Gets or sets the soil temperature in degrees Celsius.
    /// </summary>
    public double? SoilTempC { get; init; }

    /// <summary>
    /// Gets or sets the soil moisture percentage.
    /// </summary>
    public double? SoilMoisturePct { get; init; }

    /// <summary>
    /// Gets or sets the leaf wetness percentage.
    /// </summary>
    public double? LeafWetnessPct { get; init; }

    /// <summary>
    /// Creates a snapshot from the provided sample, optionally merging missing values from the previous snapshot.
    /// </summary>
    /// <param name="previous">The previous snapshot to merge with when <paramref name="mergePartial"/> is <see langword="true"/>.</param>
    /// <param name="sample">The latest weather sample to materialize as a snapshot.</param>
    /// <param name="mergePartial">True to copy missing values from <paramref name="previous"/>; otherwise false.</param>
    /// <returns>A normalized <see cref="WeatherSnapshot"/> produced from the supplied inputs.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="sample"/> is <see langword="null"/>.</exception>
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
    /// <param name="snapshot">The snapshot to evaluate for derived value backfilling.</param>
    /// <returns>A snapshot with derived values populated when enough input data was present.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="snapshot"/> is <see langword="null"/>.</exception>
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
    /// <param name="snapshot">The snapshot whose scalar values should be clamped.</param>
    /// <returns>A snapshot with normalized percentages, directions, and non-negative quantities.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="snapshot"/> is <see langword="null"/>.</exception>
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

    /// <summary>
    /// Clamps a nullable percentage to the range [0, 100] when a value is present.
    /// </summary>
    /// <param name="value">The percentage value to normalize.</param>
    /// <returns>The clamped percentage, or <see langword="null"/> when no value was provided.</returns>
    private static double? ClampPercent(double? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return Math.Clamp(value.Value, 0d, 100d);
    }

    /// <summary>
    /// Ensures nullable scalar quantities are non-negative by substituting zero for negative inputs.
    /// </summary>
    /// <param name="value">The quantity to evaluate.</param>
    /// <returns>The original value when positive, zero when negative, or <see langword="null"/> when absent.</returns>
    private static double? ClampNonNegative(double? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return value.Value < 0 ? 0 : value;
    }

    /// <summary>
    /// Normalizes a nullable direction in degrees to the [0, 360) interval.
    /// </summary>
    /// <param name="value">The direction in degrees to normalize.</param>
    /// <returns>The normalized direction, or <see langword="null"/> when no value was supplied.</returns>
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
