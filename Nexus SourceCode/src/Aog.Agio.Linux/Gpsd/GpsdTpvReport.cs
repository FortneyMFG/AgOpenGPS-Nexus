using System.Globalization;
using System.Text.Json;

namespace Aog.Agio.Linux.Gpsd;

/// <summary>
/// Represents a subset of fields from a gpsd TPV report.
/// </summary>
public sealed record GpsdTpvReport(
    DateTimeOffset? Timestamp,
    double? LatitudeDegrees,
    double? LongitudeDegrees,
    double? AltitudeMeters,
    double? SpeedMetersPerSecond,
    double? TrackDegrees,
    int? Mode)
{
    /// <summary>
    /// Attempts to parse a TPV report from a raw gpsd JSON message.
    /// </summary>
    public static bool TryParse(string? json, out GpsdTpvReport? report)
    {
        report = null;
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (!root.TryGetProperty("class", out var classProperty) ||
                !string.Equals(classProperty.GetString(), "TPV", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            DateTimeOffset? timestamp = null;
            if (root.TryGetProperty("time", out var timeProperty) && timeProperty.ValueKind == JsonValueKind.String)
            {
                var text = timeProperty.GetString();
                if (!string.IsNullOrWhiteSpace(text) &&
                    DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
                {
                    timestamp = parsed;
                }
            }

            var latitude = TryGetDouble(root, "lat");
            var longitude = TryGetDouble(root, "lon");
            var altitude = TryGetDouble(root, "alt");
            var speed = TryGetDouble(root, "speed");
            var track = TryGetDouble(root, "track");
            var mode = TryGetInt(root, "mode");

            report = new GpsdTpvReport(timestamp, latitude, longitude, altitude, speed, track, mode);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static double? TryGetDouble(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var element))
        {
            return null;
        }

        return element.ValueKind switch
        {
            JsonValueKind.Number when element.TryGetDouble(out var value) => value,
            JsonValueKind.String when double.TryParse(element.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => null,
        };
    }

    private static int? TryGetInt(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var element))
        {
            return null;
        }

        return element.ValueKind switch
        {
            JsonValueKind.Number when element.TryGetInt32(out var value) => value,
            JsonValueKind.String when int.TryParse(element.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => null,
        };
    }
}
