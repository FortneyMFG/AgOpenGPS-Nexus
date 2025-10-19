namespace Aog.Abstractions.Mapping;

/// <summary>
/// Represents a single pose observation emitted by the host.
/// </summary>
/// <param name="EastMeters">East offset in metres within the current field frame.</param>
/// <param name="NorthMeters">North offset in metres within the current field frame.</param>
/// <param name="HeadingRadians">Heading in radians, measured counter-clockwise from east.</param>
/// <param name="TimestampSeconds">Monotonic timestamp in seconds.</param>
public sealed record PoseSample(
    double EastMeters,
    double NorthMeters,
    double HeadingRadians,
    double TimestampSeconds);
