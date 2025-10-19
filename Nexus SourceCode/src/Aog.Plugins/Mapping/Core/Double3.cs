using System;
using System.Numerics;

namespace Aog.Plugins.Mapping.Core;

/// <summary>
/// Represents a three-dimensional point or vector with double precision.
/// </summary>
public readonly record struct Double3(double X, double Y, double Z)
{
    public static Double3 Zero => new(0, 0, 0);

    public static Double3 operator +(Double3 a, Double3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    public static Double3 operator -(Double3 a, Double3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    public static Double3 operator *(Double3 a, double scalar) => new(a.X * scalar, a.Y * scalar, a.Z * scalar);

    public static Double3 operator /(Double3 a, double scalar) => new(a.X / scalar, a.Y / scalar, a.Z / scalar);

    public double Length() => Math.Sqrt((X * X) + (Y * Y) + (Z * Z));

    public Vector3 ToVector3() => new((float)X, (float)Y, (float)Z);
}
