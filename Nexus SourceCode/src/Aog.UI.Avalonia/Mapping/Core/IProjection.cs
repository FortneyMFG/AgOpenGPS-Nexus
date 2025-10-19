namespace Aog.UI.Avalonia.Mapping.Core;

/// <summary>
/// Provides conversion services between geodetic coordinates and the scene's local metric space.
/// </summary>
public interface IProjection
{
    /// <summary>
    /// Converts the supplied geodetic coordinate into local metres.
    /// </summary>
    Double3 ToLocal(in GeoCoordinate geo);

    /// <summary>
    /// Converts the supplied local metric position into a geodetic coordinate.
    /// </summary>
    GeoCoordinate ToGeodetic(in Double3 local);
}
