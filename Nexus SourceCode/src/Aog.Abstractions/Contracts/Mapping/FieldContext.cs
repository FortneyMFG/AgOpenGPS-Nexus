namespace Aog.Abstractions.Mapping;

/// <summary>
/// Describes the currently active field frame that mapping plugins should render within.
/// </summary>
/// <param name="LongitudeDegrees">Field origin longitude in degrees (WGS84).</param>
/// <param name="LatitudeDegrees">Field origin latitude in degrees (WGS84).</param>
/// <param name="AltitudeMeters">Field origin altitude in metres.</param>
/// <param name="Name">Friendly name for the field, when known.</param>
public sealed record FieldContext(
    double LongitudeDegrees,
    double LatitudeDegrees,
    double AltitudeMeters,
    string? Name);
