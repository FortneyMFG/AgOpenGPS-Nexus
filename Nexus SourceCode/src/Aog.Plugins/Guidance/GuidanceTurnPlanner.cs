using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Aog.Plugins.AutoSteer;

namespace Aog.Plugins.Guidance;

/// <summary>
/// Describes the tunable parameters for the turn planner.
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
/// Represents a computed turn plan connecting two parallel field passes.
/// </summary>
public sealed class GuidanceTurnPlan
{
    public GuidanceTurnPlan(
        IReadOnlyList<PathPoint> path,
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

        Path = new ReadOnlyCollection<PathPoint>(path is List<PathPoint> list ? list : new List<PathPoint>(path));
        LaneSpacingMeters = laneSpacingMeters;
        TurnRadiusMeters = turnRadiusMeters;
        TotalLengthMeters = totalLengthMeters;
        HeadingChangeRadians = headingChangeRadians;
    }

    public IReadOnlyList<PathPoint> Path { get; }

    public double LaneSpacingMeters { get; }

    public double TurnRadiusMeters { get; }

    public double TotalLengthMeters { get; }

    public double HeadingChangeRadians { get; }
}

/// <summary>
/// Generates smooth U-turn paths for the guidance lane planner.
/// </summary>
public sealed class GuidanceTurnPlanner
{
    private readonly TurnPlannerSettings _settings;

    public GuidanceTurnPlanner(TurnPlannerSettings? settings = null)
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

    public GuidanceTurnPlan Plan(IReadOnlyList<PathPoint> currentPass, IReadOnlyList<PathPoint> nextPass)
    {
        ArgumentNullException.ThrowIfNull(currentPass);
        ArgumentNullException.ThrowIfNull(nextPass);
        if (currentPass.Count < 2 || nextPass.Count < 2)
        {
            throw new ArgumentException("Passes must contain at least two points.");
        }

        var (startPoint, startHeading) = ExtractPose(currentPass, atEnd: true);
        var (targetPoint, targetHeading) = ExtractPose(nextPass, atEnd: false);

        var leftNormal = (-startHeading.Y, startHeading.X);
        var offset = (targetPoint.X - startPoint.X, targetPoint.Y - startPoint.Y);
        var targetLocalX = offset.X * startHeading.X + offset.Y * startHeading.Y;
        var targetLocalY = offset.X * leftNormal.Item1 + offset.Y * leftNormal.Item2;

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
        var radius = Math.Clamp(_settings.DesiredTurnRadiusMeters, _settings.MinimumTurnRadiusMeters, Math.Max(_settings.MinimumTurnRadiusMeters, radiusLimit));

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

        var targetHeadingLocal = Normalize(
            targetHeading.X * startHeading.X + targetHeading.Y * startHeading.Y,
            targetHeading.X * leftNormal.Item1 + targetHeading.Y * leftNormal.Item2);

        if (_settings.ExitExtensionMeters > 1e-6)
        {
            var exitEnd = (
                targetLocal.X + targetHeadingLocal.X * _settings.ExitExtensionMeters,
                targetLocal.Y + targetHeadingLocal.Y * _settings.ExitExtensionMeters);
            AddLinearSegment(localPoints, exitEnd, sampleSpacing);
        }

        var worldPoints = new List<PathPoint>(localPoints.Count);
        foreach (var local in localPoints)
        {
            var worldX = startPoint.X + local.X * startHeading.X + local.Y * leftNormal.Item1;
            var worldY = startPoint.Y + local.X * startHeading.Y + local.Y * leftNormal.Item2;
            AddWorldPoint(worldPoints, new PathPoint(worldX, worldY));
        }

        var totalLength = ComputeLength(worldPoints);
        var headingChange = AutoSteerMath.NormalizeAngle(Math.Atan2(targetHeading.Y, targetHeading.X) - Math.Atan2(startHeading.Y, startHeading.X));

        return new GuidanceTurnPlan(worldPoints, laneSpacing, radius, totalLength, headingChange);
    }

    private static (PathPoint Point, (double X, double Y) Heading) ExtractPose(IReadOnlyList<PathPoint> pass, bool atEnd)
    {
        var index = atEnd ? pass.Count - 1 : 0;
        var anchor = pass[index];
        var step = atEnd ? -1 : 1;
        var neighbourIndex = index + step;
        while (neighbourIndex >= 0 && neighbourIndex < pass.Count)
        {
            var neighbour = pass[neighbourIndex];
            var dx = anchor.X - neighbour.X;
            var dy = anchor.Y - neighbour.Y;
            var lengthSquared = dx * dx + dy * dy;
            if (lengthSquared > 1e-12)
            {
                var length = Math.Sqrt(lengthSquared);
                var heading = atEnd ? (dx / length, dy / length) : (-dx / length, -dy / length);
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
        var length = Math.Sqrt(dx * dx + dy * dy);
        if (length < 1e-9)
        {
            AddPoint(points, end);
            return;
        }

        var steps = Math.Max(1, (int)Math.Ceiling(length / spacing));
        for (var i = 1; i <= steps; i++)
        {
            var t = (double)i / steps;
            var x = start.X + dx * t;
            var y = start.Y + dy * t;
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
            var angle = startAngle + sweepAngle * i / steps;
            var x = center.X + radius * Math.Cos(angle);
            var y = center.Y + radius * Math.Sin(angle);
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
        if (dx * dx + dy * dy < 1e-10)
        {
            return;
        }

        points.Add(point);
    }

    private static void AddWorldPoint(List<PathPoint> points, PathPoint point)
    {
        if (points.Count > 0)
        {
            var last = points[^1];
            var dx = point.X - last.X;
            var dy = point.Y - last.Y;
            if (dx * dx + dy * dy < 1e-10)
            {
                return;
            }
        }

        points.Add(point);
    }

    private static double ComputeLength(IReadOnlyList<PathPoint> path)
    {
        var length = 0.0;
        for (var i = 1; i < path.Count; i++)
        {
            var dx = path[i].X - path[i - 1].X;
            var dy = path[i].Y - path[i - 1].Y;
            length += Math.Sqrt(dx * dx + dy * dy);
        }

        return length;
    }

    private static (double X, double Y) Normalize(double x, double y)
    {
        var magnitude = Math.Sqrt(x * x + y * y);
        if (magnitude < 1e-9)
        {
            return (1, 0);
        }

        return (x / magnitude, y / magnitude);
    }
}
