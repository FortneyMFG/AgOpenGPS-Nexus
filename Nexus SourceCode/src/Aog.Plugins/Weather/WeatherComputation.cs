using System;

namespace Aog.Plugins.Weather;

/// <summary>
/// Utility routines for deriving additional weather metrics from primary measurements.
/// </summary>
public static class WeatherComputation
{
    /// <summary>
    /// Computes dew point in Celsius using the Magnus formula.
    /// </summary>
    public static double? TryComputeDewPoint(double temperatureC, double humidityPct)
    {
        if (!double.IsFinite(temperatureC) || !double.IsFinite(humidityPct))
        {
            return null;
        }

        if (humidityPct <= 0 || humidityPct > 100)
        {
            return null;
        }

        const double a = 17.27;
        const double b = 237.7;
        var alpha = (a * temperatureC / (b + temperatureC)) + Math.Log(humidityPct / 100d);
        var dewPoint = (b * alpha) / (a - alpha);
        return double.IsFinite(dewPoint) ? dewPoint : null;
    }

    /// <summary>
    /// Computes wet bulb temperature in Celsius using the Stull approximation.
    /// </summary>
    public static double? TryComputeWetBulb(double temperatureC, double humidityPct)
    {
        if (!double.IsFinite(temperatureC) || !double.IsFinite(humidityPct))
        {
            return null;
        }

        if (humidityPct <= 0 || humidityPct > 100)
        {
            return null;
        }

        var rh = humidityPct / 100d;
        var wetBulb = temperatureC * Math.Atan(0.151977 * Math.Sqrt(rh + 8.313659))
            + Math.Atan(temperatureC + rh)
            - Math.Atan(rh - 1.676331)
            + 0.00391838 * Math.Pow(rh, 1.5) * Math.Atan(0.023101 * rh)
            - 4.686035;

        return double.IsFinite(wetBulb) ? wetBulb : null;
    }
}
