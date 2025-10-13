namespace Aog.Agio.Linux;

/// <summary>
/// Represents the subset of gpsd TPV fields consumed by Nexus.
/// </summary>
public sealed record GpsdTpvReport(
    string? Device,
    DateTimeOffset? Timestamp,
    double? LatitudeDegrees,
    double? LongitudeDegrees,
    double? AltitudeMeters,
    double? SpeedMetersPerSecond,
    double? TrackDegrees,
    int? Mode);
