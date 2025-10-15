using System;
using System.Collections.Generic;
using System.Linq;
using Aog.Plugins.AutoSteer;
using Aog.Plugins.Guidance;
using FluentAssertions;
using Xunit;

namespace Aog.Plugins.Tests.Guidance;

public sealed class GuidanceTurnPlannerTests
{
    [Fact]
    public void Plan_GeneratesContinuousTurnBetweenPasses()
    {
        var currentPass = BuildPass(0, 60, 0);
        var nextPass = BuildPass(60, 0, 10);

        var planner = new GuidanceTurnPlanner(new TurnPlannerSettings
        {
            LeadInDistanceMeters = 8,
            ExitExtensionMeters = 6,
            DesiredTurnRadiusMeters = 8,
            MinimumTurnRadiusMeters = 3,
            SampleSpacingMeters = 0.5
        });

        var plan = planner.Plan(currentPass, nextPass);

        plan.LaneSpacingMeters.Should().BeApproximately(10, 1e-6);
        plan.TurnRadiusMeters.Should().BeGreaterThan(0);
        plan.TotalLengthMeters.Should().BeGreaterThan(plan.LaneSpacingMeters);
        Math.Abs(Math.Abs(plan.HeadingChangeRadians) - Math.PI).Should().BeLessThan(1e-3);

        var startPoint = currentPass[^1];
        plan.Path[0].X.Should().BeApproximately(startPoint.X, 1e-6);
        plan.Path[0].Y.Should().BeApproximately(startPoint.Y, 1e-6);

        var targetPoint = nextPass[0];
        plan.Path.Should().Contain(point => Distance(point, targetPoint) < 0.05);

        var spacing = ComputeSpacing(plan.Path);
        spacing.Should().NotContain(distance => distance < 1e-6);

        plan.Path.Min(p => p.Y).Should().BeLessThan(0.1);
        plan.Path.Max(p => p.Y).Should().BeGreaterThan(9.9);
    }

    private static IReadOnlyList<PathPoint> BuildPass(double startX, double endX, double y)
    {
        var points = new List<PathPoint>();
        var step = startX < endX ? 1.0 : -1.0;
        for (double x = startX; step > 0 ? x <= endX : x >= endX; x += step)
        {
            points.Add(new PathPoint(x, y));
        }

        if (Math.Abs(points[^1].X - endX) > 1e-6)
        {
            points.Add(new PathPoint(endX, y));
        }

        return points;
    }

    private static IEnumerable<double> ComputeSpacing(IReadOnlyList<PathPoint> path)
    {
        for (var i = 1; i < path.Count; i++)
        {
            yield return Distance(path[i], path[i - 1]);
        }
    }

    private static double Distance(PathPoint a, PathPoint b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}
