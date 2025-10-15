using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Aog.Core.Paths;
using Aog.Plugins.AutoSteer;

namespace Aog.Plugins.Guidance;

/// <summary>
/// Represents a computed turn plan expressed in path points for the autosteer stack.
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

        Path = new ReadOnlyCollection<PathPoint>(path is List<PathPoint> list ? list : path.ToList());
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
/// Generates smooth U-turn paths for the guidance lane planner using the core turn planner library.
/// </summary>
public sealed class GuidanceTurnPlanner
{
    private readonly TurnPlanner _planner;

    public GuidanceTurnPlanner(TurnPlannerSettings? settings = null)
    {
        _planner = new TurnPlanner(settings);
    }

    public TurnPlannerSettings Settings => _planner.Settings;

    public GuidanceTurnPlan Plan(IReadOnlyList<PathPoint> currentPass, IReadOnlyList<PathPoint> nextPass)
    {
        ArgumentNullException.ThrowIfNull(currentPass);
        ArgumentNullException.ThrowIfNull(nextPass);

        var current = ConvertToPlanar(currentPass);
        var next = ConvertToPlanar(nextPass);

        var corePlan = _planner.Plan(current, next);
        var pathPoints = corePlan.Path.Select(point => new PathPoint(point.Easting, point.Northing)).ToList();

        return new GuidanceTurnPlan(
            pathPoints,
            corePlan.LaneSpacingMeters,
            corePlan.TurnRadiusMeters,
            corePlan.TotalLengthMeters,
            corePlan.HeadingChangeRadians);
    }

    private static IReadOnlyList<PlanarPoint> ConvertToPlanar(IReadOnlyList<PathPoint> pass)
    {
        var converted = new List<PlanarPoint>(pass.Count);
        foreach (var point in pass)
        {
            converted.Add(new PlanarPoint(point.X, point.Y));
        }

        return converted;
    }
}
