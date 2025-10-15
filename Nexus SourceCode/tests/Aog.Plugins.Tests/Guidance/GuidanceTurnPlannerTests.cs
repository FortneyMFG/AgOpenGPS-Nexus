using System;
using System.Collections.Generic;
using System.Linq;
using Aog.Core.Paths;
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
        plan.TurnRadiusMeters.Should().BeApproximately(5.0, 1e-6);
        plan.TotalLengthMeters.Should().BeApproximately(41.70165578477376, 1e-6);
        plan.Path.Count.Should().Be(85);
        Math.Abs(Math.Abs(plan.HeadingChangeRadians) - Math.PI).Should().BeLessThan(1e-6);

        var startPoint = currentPass[^1];
        plan.Path[0].X.Should().BeApproximately(startPoint.X, 1e-6);
        plan.Path[0].Y.Should().BeApproximately(startPoint.Y, 1e-6);

        var targetPoint = nextPass[0];
        plan.Path.Should().Contain(point => Distance(point, targetPoint) < 0.05);

        var spacing = ComputeSpacing(plan.Path);
        spacing.Should().NotContain(distance => distance < 1e-6);

        plan.Path.Min(p => p.Y).Should().BeLessThan(0.1);
        plan.Path.Max(p => p.Y).Should().BeGreaterThan(9.9);

        plan.Path[^1].X.Should().BeApproximately(54.0, 1e-6);
        plan.Path[^1].Y.Should().BeApproximately(10.0, 1e-6);
        plan.Path[^2].X.Should().BeApproximately(60.0, 1e-6);
        plan.Path[^2].Y.Should().BeApproximately(10.0, 1e-6);
    }

    [Theory]
    [InlineData(10.0, 8.0, 3.0, 5.0)]
    [InlineData(6.0, 8.0, 3.0, 3.0)]
    [InlineData(12.0, 8.0, 3.0, 6.0)]
    public void Plan_ClampsTurnRadiusToLaneSpacing(double laneSpacing, double desiredRadius, double minimumRadius, double expectedRadius)
    {
        var currentPass = BuildPass(0, 60, 0);
        var nextPass = BuildPass(60, 0, laneSpacing);

        var planner = new GuidanceTurnPlanner(new TurnPlannerSettings
        {
            LeadInDistanceMeters = 8,
            ExitExtensionMeters = 0,
            DesiredTurnRadiusMeters = desiredRadius,
            MinimumTurnRadiusMeters = minimumRadius,
            SampleSpacingMeters = 0.5
        });

        var plan = planner.Plan(currentPass, nextPass);

        plan.LaneSpacingMeters.Should().BeApproximately(laneSpacing, 1e-6);
        plan.TurnRadiusMeters.Should().BeApproximately(expectedRadius, 1e-6);
        plan.Path[0].X.Should().BeApproximately(currentPass[^1].X, 1e-6);
        plan.Path[^1].Y.Should().BeApproximately(laneSpacing, 1e-6);
    }

    [Fact]
    public void Plan_GeneratesRightHandTurnWhenPassesOffsetNegative()
    {
        var currentPass = BuildPass(0, 60, 0);
        var nextPass = BuildPass(60, 0, -10);

        var planner = new GuidanceTurnPlanner(new TurnPlannerSettings
        {
            LeadInDistanceMeters = 8,
            ExitExtensionMeters = 4,
            DesiredTurnRadiusMeters = 7.5,
            MinimumTurnRadiusMeters = 3,
            SampleSpacingMeters = 0.5
        });

        var plan = planner.Plan(currentPass, nextPass);

        plan.LaneSpacingMeters.Should().BeApproximately(10, 1e-6);
        plan.Path.Min(p => p.Y).Should().BeLessThan(-9.9);
        plan.Path.Max(p => p.Y).Should().BeLessThan(0.1);
        plan.Path[^1].Y.Should().BeApproximately(-10, 1e-6);
        plan.Path[^2].Y.Should().BeApproximately(-10, 1e-6);
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
