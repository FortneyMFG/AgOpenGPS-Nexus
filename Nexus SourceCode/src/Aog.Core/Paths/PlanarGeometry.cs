using System;
using System.Collections.Generic;

namespace Aog.Core.Paths;

/// <summary>
/// Lightweight planar geometry primitives used by path planners.
/// </summary>
public static class PlanarGeometry
{
    /// <summary>
    /// Small tolerance used to guard against degenerate floating-point results.
    /// </summary>
    public const double Epsilon = 1e-9;
}

/// <summary>
/// Represents a point in the local east/north plane (meters).
/// </summary>
public readonly record struct PlanarPoint(double Easting, double Northing)
{
    /// <summary>
    /// Translates the point by the specified vector.
    /// </summary>
    public PlanarPoint Translate(PlanarVector vector) => new(Easting + vector.X, Northing + vector.Y);

    public static PlanarVector operator -(PlanarPoint end, PlanarPoint start) =>
        new(end.Easting - start.Easting, end.Northing - start.Northing);

    public static PlanarPoint operator +(PlanarPoint point, PlanarVector vector) =>
        point.Translate(vector);

    public static PlanarPoint operator -(PlanarPoint point, PlanarVector vector) =>
        new(point.Easting - vector.X, point.Northing - vector.Y);
}

/// <summary>
/// Represents a 2D vector in meters.
/// </summary>
public readonly record struct PlanarVector(double X, double Y)
{
    /// <summary>
    /// Returns the length (magnitude) of the vector.
    /// </summary>
    public double Length => Math.Sqrt((X * X) + (Y * Y));

    /// <summary>
    /// Returns the squared length of the vector.
    /// </summary>
    public double LengthSquared => (X * X) + (Y * Y);

    /// <summary>
    /// Normalises the vector.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the vector has zero length.</exception>
    public PlanarVector Normalize()
    {
        var length = Length;
        if (length < PlanarGeometry.Epsilon)
        {
            throw new InvalidOperationException("Cannot normalise a zero-length vector.");
        }

        return new PlanarVector(X / length, Y / length);
    }

    /// <summary>
    /// Returns the right-hand perpendicular vector.
    /// </summary>
    public PlanarVector PerpendicularRight() => new(Y, -X);

    /// <summary>
    /// Returns the left-hand perpendicular vector.
    /// </summary>
    public PlanarVector PerpendicularLeft() => new(-Y, X);

    public static PlanarVector operator +(PlanarVector a, PlanarVector b) => new(a.X + b.X, a.Y + b.Y);

    public static PlanarVector operator -(PlanarVector a, PlanarVector b) => new(a.X - b.X, a.Y - b.Y);

    public static PlanarVector operator *(PlanarVector vector, double scalar) => new(vector.X * scalar, vector.Y * scalar);

    public static PlanarVector operator *(double scalar, PlanarVector vector) => vector * scalar;

    /// <summary>
    /// Dot product of two vectors.
    /// </summary>
    public static double Dot(PlanarVector a, PlanarVector b) => (a.X * b.X) + (a.Y * b.Y);

    /// <summary>
    /// 2D cross product (equivalent to the signed area of the parallelogram).
    /// </summary>
    public static double Cross(PlanarVector a, PlanarVector b) => (a.X * b.Y) - (a.Y * b.X);
}

/// <summary>
/// Represents a line segment in the planar frame.
/// </summary>
public readonly record struct PlanarLine(PlanarPoint Start, PlanarPoint End)
{
    /// <summary>
    /// Gets the direction vector from <see cref="Start"/> to <see cref="End"/>.
    /// </summary>
    public PlanarVector Direction => End - Start;

    /// <summary>
    /// Computes the signed distance from the line to a point. Positive distances are to the right of the line.
    /// </summary>
    public double SignedDistanceTo(PlanarPoint point)
    {
        var start = Start;
        var end = End;

        var dx = end.Easting - start.Easting;
        var dy = end.Northing - start.Northing;
        var numerator = (dy * point.Easting) - (dx * point.Northing) + (end.Easting * start.Northing) - (end.Northing * start.Easting);
        var denominator = Math.Sqrt((dx * dx) + (dy * dy));
        if (denominator < PlanarGeometry.Epsilon)
        {
            throw new InvalidOperationException("Cannot compute distance for a zero-length line segment.");
        }

        // Positive distances are on the right-hand side of the travel direction.
        return numerator / denominator;
    }

    /// <summary>
    /// Creates a new line extended in both directions by the specified distance.
    /// </summary>
    public PlanarLine Extend(double distance)
    {
        var direction = Direction;
        if (direction.LengthSquared < PlanarGeometry.Epsilon)
        {
            throw new InvalidOperationException("Cannot extend a zero-length line segment.");
        }

        var unit = direction.Normalize();
        var extension = unit * distance;
        return new PlanarLine(Start - extension, End + extension);
    }
}

/// <summary>
/// Utility helpers for geometric computations.
/// </summary>
public static class PlanarGeometryExtensions
{
    /// <summary>
    /// Computes the intersection point between two infinite lines.
    /// </summary>
    /// <returns><c>null</c> when the lines are parallel.</returns>
    public static PlanarPoint? IntersectLines(PlanarPoint originA, PlanarVector directionA, PlanarPoint originB, PlanarVector directionB)
    {
        var determinant = PlanarVector.Cross(directionA, directionB);
        if (Math.Abs(determinant) < PlanarGeometry.Epsilon)
        {
            return null;
        }

        var diff = originB - originA;
        var t = PlanarVector.Cross(diff, directionB) / determinant;
        return originA + (directionA * t);
    }

    /// <summary>
    /// Computes the signed polygon area (twice the actual area) to determine winding order.
    /// </summary>
    public static double ComputePolygonArea(IReadOnlyList<PlanarPoint> polygon)
    {
        if (polygon.Count < 3)
        {
            return 0;
        }

        double area2 = 0;
        for (var i = 0; i < polygon.Count; i++)
        {
            var current = polygon[i];
            var next = polygon[(i + 1) % polygon.Count];
            area2 += (current.Easting * next.Northing) - (next.Easting * current.Northing);
        }

        return area2 / 2.0;
    }
}
