using System;
using System.Collections.Generic;
using System.Linq;
using Aog.Core.Paths;
using FluentAssertions;
using Xunit;

namespace Aog.Core.Tests.Paths;

public sealed class TurnPlannerTests
{
    [Fact]
    public void Plan_ComputesDeterministicTurnMetrics()
    {
        var currentPass = BuildPass(0, 60, 0);
        var nextPass = BuildPass(60, 0, 10);

        var planner = new TurnPlanner(new TurnPlannerSettings
        {
            LeadInDistanceMeters = 8,
            ExitExtensionMeters = 6,
            DesiredTurnRadiusMeters = 8,
            MinimumTurnRadiusMeters = 3,
            SampleSpacingMeters = 0.5
        });

        var plan = planner.Plan(currentPass, nextPass);

        plan.LaneSpacingMeters.Should().BeApproximately(10.0, 1e-6);
        plan.TurnRadiusMeters.Should().BeApproximately(5.0, 1e-6);
        plan.TotalLengthMeters.Should().BeApproximately(41.70165578477376, 1e-6);
        plan.Path.Count.Should().Be(85);
        plan.Path[0].Easting.Should().BeApproximately(60.0, 1e-6);
        plan.Path[0].Northing.Should().BeApproximately(0.0, 1e-6);
        plan.Path[^2].Easting.Should().BeApproximately(60.0, 1e-6);
        plan.Path[^2].Northing.Should().BeApproximately(10.0, 1e-6);
        plan.Path[^1].Easting.Should().BeApproximately(54.0, 1e-6);
        plan.Path[^1].Northing.Should().BeApproximately(10.0, 1e-6);
        Math.Abs(Math.Abs(plan.HeadingChangeRadians) - Math.PI).Should().BeLessThan(1e-6);
    }

    [Theory]
    [InlineData(10.0, 8.0, 3.0, 5.0)]
    [InlineData(6.0, 8.0, 3.0, 3.0)]
    [InlineData(12.0, 8.0, 3.0, 6.0)]
    public void Plan_ClampsTurnRadiusBasedOnLaneSpacing(double laneSpacing, double desiredRadius, double minimumRadius, double expectedRadius)
    {
        var currentPass = BuildPass(0, 60, 0);
        var nextPass = BuildPass(60, 0, laneSpacing);

        var planner = new TurnPlanner(new TurnPlannerSettings
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
        plan.Path.First().Easting.Should().BeApproximately(currentPass[^1].Easting, 1e-6);
        plan.Path[^1].Northing.Should().BeApproximately(laneSpacing, 1e-6);
    }

    [Fact]
    public void Plan_ThrowsWhenPassesDegenerate()
    {
        var planner = new TurnPlanner();
        var invalidPass = new List<PlanarPoint> { new(0, 0), new(0, 0) };
        var validPass = BuildPass(0, 10, 5);

        Assert.Throws<InvalidOperationException>(() => planner.Plan(invalidPass, validPass));
        Assert.Throws<InvalidOperationException>(() => planner.Plan(validPass, invalidPass));
    }

    private static IReadOnlyList<PlanarPoint> BuildPass(double startX, double endX, double y)
    {
        var points = new List<PlanarPoint>();
        var step = startX < endX ? 1.0 : -1.0;
        for (double x = startX; step > 0 ? x <= endX : x >= endX; x += step)
        {
            points.Add(new PlanarPoint(x, y));
        }

        if (Math.Abs(points[^1].Easting - endX) > 1e-6)
        {
            points.Add(new PlanarPoint(endX, y));
        }

        return points;
    }
}
