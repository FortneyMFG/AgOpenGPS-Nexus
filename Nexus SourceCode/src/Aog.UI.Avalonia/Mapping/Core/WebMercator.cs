using System;

namespace Aog.UI.Avalonia.Mapping.Core;

/// <summary>
/// Provides helpers for converting between geodetic coordinates and EPSG:3857 (Web Mercator) metres.
/// </summary>
public sealed class WebMercator
{
    private const double EarthRadius = 6378137.0;
    private const double OriginShift = Math.PI * EarthRadius;

    public Double3 ToLocal(GeoCoordinate geo)
    {
        var lat = ClampLatitude(geo.Latitude);
        var lon = geo.Longitude;
        var x = DegToRad(lon) * EarthRadius;
        var y = Math.Log(Math.Tan(Math.PI / 4 + DegToRad(lat) / 2)) * EarthRadius;
        return new Double3(x, y, geo.AltitudeMeters);
    }

    public GeoCoordinate ToGeodetic(Double3 mercator)
    {
        var lon = RadToDeg(mercator.X / EarthRadius);
        var lat = RadToDeg(2 * Math.Atan(Math.Exp(mercator.Y / EarthRadius)) - Math.PI / 2);
        return new GeoCoordinate(lat, lon, mercator.Z);
    }

    public (int X, int Y, int Zoom) ToTile(Double3 mercator, int zoom)
    {
        var resolution = Resolution(zoom);
        var pixelX = (mercator.X + OriginShift) / resolution;
        var pixelY = (OriginShift - mercator.Y) / resolution;
        var tileX = (int)Math.Floor(pixelX / 256.0);
        var tileY = (int)Math.Floor(pixelY / 256.0);
        return (tileX, tileY, zoom);
    }

    public Double3 TileToMeters(int x, int y, int zoom)
    {
        var resolution = Resolution(zoom);
        var mercX = (x * 256.0 * resolution) - OriginShift;
        var mercY = OriginShift - (y * 256.0 * resolution);
        return new Double3(mercX, mercY, 0);
    }

    public double MetersPerPixel(int zoom) => Resolution(zoom);

    private static double Resolution(int zoom) => (2 * Math.PI * EarthRadius) / (256 * Math.Pow(2, zoom));

    private static double ClampLatitude(double latitude) => Math.Min(85.05112878, Math.Max(-85.05112878, latitude));

    private static double DegToRad(double degrees) => degrees * Math.PI / 180.0;

    private static double RadToDeg(double radians) => radians * 180.0 / Math.PI;
}
