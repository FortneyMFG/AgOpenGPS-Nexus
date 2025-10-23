using System;

namespace Aog.UI.Avalonia.Mapping.Core;

/// <summary>
/// Converts between WGS84 geodetic coordinates and a local East-North-Up frame anchored at a field origin.
/// </summary>
public sealed class EnuProjection : IProjection
{
    private const double SemiMajor = 6378137.0;
    private const double Flattening = 1.0 / 298.257223563;
    private const double SemiMinor = SemiMajor * (1 - Flattening);
    private const double EccentricitySquared = (SemiMajor * SemiMajor - SemiMinor * SemiMinor) / (SemiMajor * SemiMajor);

    private readonly double _originLatRad;
    private readonly double _originLonRad;
    private readonly double _sinLat0;
    private readonly double _cosLat0;
    private readonly double _sinLon0;
    private readonly double _cosLon0;
    private readonly double _headingRad;
    private readonly double _cosHeading;
    private readonly double _sinHeading;
    private readonly Double3 _originEcef;

    public EnuProjection(GeoCoordinate origin, double headingDegrees = 0)
    {
        _originLatRad = DegToRad(origin.Latitude);
        _originLonRad = DegToRad(origin.Longitude);
        _sinLat0 = Math.Sin(_originLatRad);
        _cosLat0 = Math.Cos(_originLatRad);
        _sinLon0 = Math.Sin(_originLonRad);
        _cosLon0 = Math.Cos(_originLonRad);

        _headingRad = DegToRad(headingDegrees);
        _cosHeading = Math.Cos(_headingRad);
        _sinHeading = Math.Sin(_headingRad);

        _originEcef = ToEcef(origin);
    }

    /// <inheritdoc />
    public Double3 ToLocal(in GeoCoordinate geo)
    {
        var pointEcef = ToEcef(geo);
        var dx = pointEcef.X - _originEcef.X;
        var dy = pointEcef.Y - _originEcef.Y;
        var dz = pointEcef.Z - _originEcef.Z;

        // ENU basis
        var east = (-_sinLon0 * dx) + (_cosLon0 * dy);
        var north = (-_sinLat0 * _cosLon0 * dx) + (-_sinLat0 * _sinLon0 * dy) + (_cosLat0 * dz);
        var up = (_cosLat0 * _cosLon0 * dx) + (_cosLat0 * _sinLon0 * dy) + (_sinLat0 * dz);

        // Rotate by heading to align with field axes.
        var rotatedX = (_cosHeading * east) + (_sinHeading * north);
        var rotatedY = (-_sinHeading * east) + (_cosHeading * north);

        return new Double3(rotatedX, rotatedY, up);
    }

    /// <inheritdoc />
    public GeoCoordinate ToGeodetic(in Double3 local)
    {
        // Undo heading rotation.
        var east = (_cosHeading * local.X) - (_sinHeading * local.Y);
        var north = (_sinHeading * local.X) + (_cosHeading * local.Y);
        var up = local.Z;

        var dx = (-_sinLon0 * east) + (-_sinLat0 * _cosLon0 * north) + (_cosLat0 * _cosLon0 * up);
        var dy = (_cosLon0 * east) + (-_sinLat0 * _sinLon0 * north) + (_cosLat0 * _sinLon0 * up);
        var dz = (_cosLat0 * north) + (_sinLat0 * up);

        var x = _originEcef.X + dx;
        var y = _originEcef.Y + dy;
        var z = _originEcef.Z + dz;

        return FromEcef(x, y, z);
    }

    private static Double3 ToEcef(GeoCoordinate geo)
    {
        var lat = DegToRad(geo.Latitude);
        var lon = DegToRad(geo.Longitude);
        var sinLat = Math.Sin(lat);
        var cosLat = Math.Cos(lat);
        var sinLon = Math.Sin(lon);
        var cosLon = Math.Cos(lon);
        var n = SemiMajor / Math.Sqrt(1 - (EccentricitySquared * sinLat * sinLat));
        var alt = geo.AltitudeMeters;

        var x = (n + alt) * cosLat * cosLon;
        var y = (n + alt) * cosLat * sinLon;
        var z = ((n * (1 - EccentricitySquared)) + alt) * sinLat;
        return new Double3(x, y, z);
    }

    private static GeoCoordinate FromEcef(double x, double y, double z)
    {
        var lon = Math.Atan2(y, x);
        var p = Math.Sqrt((x * x) + (y * y));
        var lat = Math.Atan2(z, p * (1 - EccentricitySquared));

        for (var i = 0; i < 5; i++)
        {
            var sinLat = Math.Sin(lat);
            var n = SemiMajor / Math.Sqrt(1 - (EccentricitySquared * sinLat * sinLat));
            lat = Math.Atan2(z + (EccentricitySquared * n * sinLat), p);
        }

        var sinLatFinal = Math.Sin(lat);
        var nFinal = SemiMajor / Math.Sqrt(1 - (EccentricitySquared * sinLatFinal * sinLatFinal));
        var alt = (p / Math.Cos(lat)) - nFinal;

        return new GeoCoordinate(RadToDeg(lat), RadToDeg(lon), alt);
    }

    private static double DegToRad(double value) => value * Math.PI / 180.0;

    private static double RadToDeg(double value) => value * 180.0 / Math.PI;
}
