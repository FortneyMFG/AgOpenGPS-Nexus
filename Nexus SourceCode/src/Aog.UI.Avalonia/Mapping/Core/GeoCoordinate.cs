namespace Aog.UI.Avalonia.Mapping.Core;

/// <summary>
/// Represents a geographic coordinate in latitude, longitude, and altitude (metres).
/// </summary>
public readonly record struct GeoCoordinate(double Latitude, double Longitude, double AltitudeMeters = 0);
