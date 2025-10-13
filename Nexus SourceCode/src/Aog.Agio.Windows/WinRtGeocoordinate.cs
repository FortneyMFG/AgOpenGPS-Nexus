namespace Aog.Agio.Windows;

/// <summary>
/// Snapshot of coordinate data reported by the Windows Runtime geolocator.
/// </summary>
/// <param name="Latitude">Latitude in decimal degrees (WGS84).</param>
/// <param name="Longitude">Longitude in decimal degrees (WGS84).</param>
/// <param name="Altitude">Altitude above mean sea level in meters.</param>
/// <param name="Accuracy">Estimated horizontal accuracy in meters.</param>
/// <param name="HeadingDeg">Course over ground in degrees clockwise from true north.</param>
/// <param name="SpeedMps">Ground speed in meters per second.</param>
public readonly record struct WinRtGeocoordinate(
    double Latitude,
    double Longitude,
    double? Altitude,
    double? Accuracy,
    double? HeadingDeg,
    double? SpeedMps);
