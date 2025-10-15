using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Aog.Core.Paths;

/// <summary>
/// Tunable parameters for the headland turn planner.
/// </summary>
public sealed class TurnPlannerSettings
{
    public double LeadInDistanceMeters { get; init; } = 8.0;

    public double ExitExtensionMeters { get; init; } = 6.0;

    public double DesiredTurnRadiusMeters { get; init; } = 8.5;

    public double MinimumTurnRadiusMeters { get; init; } = 3.0;

    public double SampleSpacingMeters { get; init; } = 0.5;
}

/// <summary>
/// Represents a computed turn connecting two parallel field passes.
/// </summary>
public sealed class TurnPlan
{
    public TurnPlan(
        IReadOnlyList<PlanarPoint> path,
        double laneSpacingMeters,
        double turnRadiusMeters,
        double totalLengthMeters,
        double headingChangeRadians)
    {
        if (path is null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        if (path.Count < 2)
        {
            throw new ArgumentException("Turn plan must contain at least two points.", nameof(path));
        }

        if (laneSpacingMeters <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(laneSpacingMeters), laneSpacingMeters, "Lane spacing must be positive.");
        }

        if (turnRadiusMeters <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(turnRadiusMeters), turnRadiusMeters, "Turn radius must be positive.");
        }

        if (totalLengthMeters <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalLengthMeters), totalLengthMeters, "Turn length must be positive.");
        }

        Path = new ReadOnlyCollection<PlanarPoint>(path is List<PlanarPoint> list ? list : path.ToList());
        LaneSpacingMeters = laneSpacingMeters;
        TurnRadiusMeters = turnRadiusMeters;
        TotalLengthMeters = totalLengthMeters;
        HeadingChangeRadians = headingChangeRadians;
    }

    public IReadOnlyList<PlanarPoint> Path { get; }

    public double LaneSpacingMeters { get; }

    public double TurnRadiusMeters { get; }

    public double TotalLengthMeters { get; }

    public double HeadingChangeRadians { get; }
}

/// <summary>
/// Generates smooth U-turn paths between adjacent passes.
/// </summary>
public sealed class TurnPlanner
{
    private readonly TurnPlannerSettings _settings;

    public TurnPlanner(TurnPlannerSettings? settings = null)
    {
        _settings = settings ?? new TurnPlannerSettings();
        if (_settings.LeadInDistanceMeters <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(_settings.LeadInDistanceMeters), _settings.LeadInDistanceMeters, "Lead-in distance must be positive.");
        }

        if (_settings.ExitExtensionMeters < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(_settings.ExitExtensionMeters), _settings.ExitExtensionMeters, "Exit extension must be non-negative.");
        }

        if (_settings.DesiredTurnRadiusMeters <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(_settings.DesiredTurnRadiusMeters), _settings.DesiredTurnRadiusMeters, "Desired turn radius must be positive.");
        }

        if (_settings.MinimumTurnRadiusMeters <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(_settings.MinimumTurnRadiusMeters), _settings.MinimumTurnRadiusMeters, "Minimum turn radius must be positive.");
        }

        if (_settings.SampleSpacingMeters <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(_settings.SampleSpacingMeters), _settings.SampleSpacingMeters, "Sample spacing must be positive.");
        }
    }

    public TurnPlannerSettings Settings => _settings;

    public TurnPlan Plan(IReadOnlyList<PlanarPoint> currentPass, IReadOnlyList<PlanarPoint> nextPass)
    {
        ArgumentNullException.ThrowIfNull(currentPass);
        ArgumentNullException.ThrowIfNull(nextPass);

        if (currentPass.Count < 2 || nextPass.Count < 2)
        {
            throw new ArgumentException("Passes must contain at least two points.");
        }

        var (startPoint, startHeading) = ExtractPose(currentPass, atEnd: true);
        var (targetPoint, targetHeading) = ExtractPose(nextPass, atEnd: false);

        var leftNormal = startHeading.PerpendicularLeft();
        var offset = targetPoint - startPoint;
        var targetLocalX = PlanarVector.Dot(offset, startHeading);
        var targetLocalY = PlanarVector.Dot(offset, leftNormal);

        var laneSpacing = Math.Abs(targetLocalY);
        if (laneSpacing < 1e-6)
        {
            throw new InvalidOperationException("Unable to determine lane spacing for the provided passes.");
        }

        var turnSign = Math.Sign(targetLocalY);
        if (turnSign == 0)
        {
            turnSign = 1;
        }

        var radiusLimit = laneSpacing / 2;
        var radius = Math.Clamp(
            _settings.DesiredTurnRadiusMeters,
            _settings.MinimumTurnRadiusMeters,
            Math.Max(_settings.MinimumTurnRadiusMeters, radiusLimit));

        var sampleSpacing = _settings.SampleSpacingMeters;
        var leadIn = Math.Max(_settings.LeadInDistanceMeters, radius);

        var localPoints = new List<(double X, double Y)> { (0, 0) };
        AddLinearSegment(localPoints, (leadIn, 0), sampleSpacing);

        var arc1Center = (leadIn, turnSign * radius);
        AddArcSegment(localPoints, arc1Center, radius, turnSign * Math.PI / 2, sampleSpacing);

        var straightTargetY = turnSign * (laneSpacing - radius);
        var straightEnd = (leadIn - turnSign * radius, straightTargetY);
        AddLinearSegment(localPoints, straightEnd, sampleSpacing);

        var arc2Center = (straightEnd.Item1 - turnSign * radius, straightEnd.Item2);
        AddArcSegment(localPoints, arc2Center, radius, turnSign * Math.PI / 2, sampleSpacing);

        var targetLocal = (targetLocalX, turnSign * laneSpacing);
        AddLinearSegment(localPoints, targetLocal, sampleSpacing);

        if (_settings.ExitExtensionMeters > 1e-6)
        {
            var targetHeadingLocal = Normalize(
                PlanarVector.Dot(targetHeading, startHeading),
                PlanarVector.Dot(targetHeading, leftNormal));

            var exitEnd = (
                targetLocal.X + targetHeadingLocal.X * _settings.ExitExtensionMeters,
                targetLocal.Y + targetHeadingLocal.Y * _settings.ExitExtensionMeters);
            AddLinearSegment(localPoints, exitEnd, sampleSpacing);
        }

        var worldPoints = new List<PlanarPoint>(localPoints.Count);
        foreach (var local in localPoints)
        {
            var world = new PlanarPoint(
                startPoint.Easting + (local.X * startHeading.X) + (local.Y * leftNormal.X),
                startPoint.Northing + (local.X * startHeading.Y) + (local.Y * leftNormal.Y));
            AddWorldPoint(worldPoints, world);
        }

        var totalLength = ComputeLength(worldPoints);
        var headingChange = NormalizeAngle(Math.Atan2(targetHeading.Y, targetHeading.X) - Math.Atan2(startHeading.Y, startHeading.X));

        return new TurnPlan(worldPoints, laneSpacing, radius, totalLength, headingChange);
    }

    private static (PlanarPoint Point, PlanarVector Heading) ExtractPose(IReadOnlyList<PlanarPoint> pass, bool atEnd)
    {
        var index = atEnd ? pass.Count - 1 : 0;
        var anchor = pass[index];
        var step = atEnd ? -1 : 1;
        var neighbourIndex = index + step;
        while (neighbourIndex >= 0 && neighbourIndex < pass.Count)
        {
            var neighbour = pass[neighbourIndex];
            var direction = anchor - neighbour;
            if (direction.LengthSquared > PlanarGeometry.Epsilon)
            {
                var heading = direction.Normalize();
                if (!atEnd)
                {
                    heading = new PlanarVector(-heading.X, -heading.Y);
                }

                return (anchor, heading);
            }

            neighbourIndex += step;
        }

        throw new InvalidOperationException("Unable to determine heading for the provided pass.");
    }

    private static void AddLinearSegment(List<(double X, double Y)> points, (double X, double Y) end, double spacing)
    {
        var start = points[^1];
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        var length = Math.Sqrt((dx * dx) + (dy * dy));
        if (length < 1e-9)
        {
            AddPoint(points, end);
            return;
        }

        var steps = Math.Max(1, (int)Math.Ceiling(length / spacing));
        for (var i = 1; i <= steps; i++)
        {
            var t = (double)i / steps;
            var x = start.X + (dx * t);
            var y = start.Y + (dy * t);
            AddPoint(points, (x, y));
        }
    }

    private static void AddArcSegment(List<(double X, double Y)> points, (double X, double Y) center, double radius, double sweepAngle, double spacing)
    {
        if (radius <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(radius), radius, "Radius must be positive.");
        }

        var start = points[^1];
        var startAngle = Math.Atan2(start.Y - center.Y, start.X - center.X);
        var arcLength = Math.Abs(sweepAngle) * radius;
        var steps = Math.Max(1, (int)Math.Ceiling(arcLength / spacing));
        for (var i = 1; i <= steps; i++)
        {
            var angle = startAngle + (sweepAngle * i / steps);
            var x = center.X + (radius * Math.Cos(angle));
            var y = center.Y + (radius * Math.Sin(angle));
            AddPoint(points, (x, y));
        }
    }

    private static void AddPoint(List<(double X, double Y)> points, (double X, double Y) point)
    {
        if (points.Count == 0)
        {
            points.Add(point);
            return;
        }

        var last = points[^1];
        var dx = point.X - last.X;
        var dy = point.Y - last.Y;
        if ((dx * dx) + (dy * dy) < 1e-10)
        {
            return;
        }

        points.Add(point);
    }

    private static void AddWorldPoint(List<PlanarPoint> points, PlanarPoint point)
    {
        if (points.Count > 0)
        {
            var last = points[^1];
            var diff = point - last;
            if (diff.LengthSquared < 1e-10)
            {
                return;
            }
        }

        points.Add(point);
    }

    private static double ComputeLength(IReadOnlyList<PlanarPoint> path)
    {
        var length = 0.0;
        for (var i = 1; i < path.Count; i++)
        {
            var diff = path[i] - path[i - 1];
            length += diff.Length;
        }

        return length;
    }

    private static (double X, double Y) Normalize(double x, double y)
    {
        var magnitude = Math.Sqrt((x * x) + (y * y));
        if (magnitude < 1e-9)
        {
            return (1, 0);
        }

        return (x / magnitude, y / magnitude);
    }

    private static double NormalizeAngle(double angle)
    {
        while (angle > Math.PI)
        {
            angle -= 2 * Math.PI;
        }

        while (angle < -Math.PI)
        {
            angle += 2 * Math.PI;
        }

        return angle;
    }
}
