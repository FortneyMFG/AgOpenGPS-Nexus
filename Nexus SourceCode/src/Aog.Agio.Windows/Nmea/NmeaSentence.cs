namespace Aog.Agio.Windows.Nmea;

/// <summary>
/// Base type for supported NMEA sentences.
/// </summary>
public abstract record NmeaSentence(string TalkerId, string SentenceId)
{
    public string TalkerId { get; } = TalkerId ?? throw new ArgumentNullException(nameof(TalkerId));

    public string SentenceId { get; } = SentenceId ?? throw new ArgumentNullException(nameof(SentenceId));
}

/// <summary>
/// Fix quality reported by a GGA sentence.
/// </summary>
public enum NmeaFixQuality
{
    Invalid = 0,
    Gps = 1,
    DifferentialGps = 2,
    Pps = 3,
    RealTimeKinematic = 4,
    FloatRtk = 5,
    Estimated = 6,
    Manual = 7,
    Simulation = 8,
}

/// <summary>
/// RMC status flag.
/// </summary>
public enum NmeaFixStatus
{
    Void = 0,
    Active = 1,
}

/// <summary>
/// Parsed form of a GGA sentence.
/// </summary>
public sealed record NmeaGgaSentence(
    string TalkerId,
    TimeOnly? FixTime,
    double? LatitudeDegrees,
    double? LongitudeDegrees,
    NmeaFixQuality FixQuality,
    int? SatelliteCount,
    double? HorizontalDilution,
    double? AltitudeMeters,
    double? GeoidSeparationMeters)
    : NmeaSentence(TalkerId, "GGA");

/// <summary>
/// Parsed form of an RMC sentence.
/// </summary>
public sealed record NmeaRmcSentence(
    string TalkerId,
    NmeaFixStatus Status,
    DateTimeOffset? Timestamp,
    double? LatitudeDegrees,
    double? LongitudeDegrees,
    double? SpeedKnots,
    double? TrackTrueDegrees,
    double? MagneticVariationDegrees,
    char? Mode)
    : NmeaSentence(TalkerId, "RMC");

/// <summary>
/// Parsed form of a VTG sentence.
/// </summary>
public sealed record NmeaVtgSentence(
    string TalkerId,
    double? TrueCourseDegrees,
    double? MagneticCourseDegrees,
    double? SpeedKnots,
    double? SpeedKilometersPerHour,
    char? Mode)
    : NmeaSentence(TalkerId, "VTG");
