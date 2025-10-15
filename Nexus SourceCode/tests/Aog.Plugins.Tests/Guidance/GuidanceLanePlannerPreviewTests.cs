using System;
using System.Collections.Generic;
using System.Linq;
using Aog.Core.Guidance;
using Aog.Plugins.AutoSteer;
using Aog.Plugins.Guidance;
using FluentAssertions;
using Xunit;

namespace Aog.Plugins.Tests.Guidance;

public sealed class GuidanceLanePlannerPreviewTests
{
    [Fact]
    public void BuildTurnPreview_ProducesPreviewWithConstraintContext()
    {
        var currentPass = BuildPass(0, 60, 0);
        var nextPass = BuildPass(60, 0, 10);

        var planner = new GuidanceLanePlanner(
            new GuidanceTurnPlanner(new TurnPlannerSettings
            {
                LeadInDistanceMeters = 8,
                ExitExtensionMeters = 4,
                DesiredTurnRadiusMeters = 7.5,
                MinimumTurnRadiusMeters = 3,
                SampleSpacingMeters = 0.5
            }),
            new PurePursuitController(6.0));

        var vehicle = new VehicleState(
            x: 55,
            y: -0.8,
            headingRadians: AutoSteerMath.NormalizeAngle(5 * Math.PI / 180),
            speedMetersPerSecond: 4.0,
            wheelbaseMeters: 3.0);

        var constraint = new ConstraintLookAheadContext(
            HasBlockingConstraint: false,
            InsideHeadland: true,
            DistanceToConstraintMeters: 12.0);

        var (plan, preview) = planner.BuildTurnPreview(vehicle, currentPass, nextPass, constraint);

        plan.Path.Count.Should().BeGreaterThan(10);
        preview.ControllerEnabled.Should().BeTrue();
        preview.LookAheadDistanceMeters.Should().BeApproximately(6.0, 1e-6);
        preview.Constraint.Should().NotBeNull();
        preview.Constraint!.InsideHeadland.Should().BeTrue();
        preview.Constraint.ZoneMask.Should().NotBeNull();
        preview.Constraint.ZoneMask!.InsideBoundary.Should().BeTrue();

        var targetPoint = new PathPoint(preview.TargetPoint.EastingMeters, preview.TargetPoint.NorthingMeters);
        plan.Path.Min(p => Distance(p, targetPoint)).Should().BeLessThan(0.2);
        Math.Abs(preview.ControllerOutput).Should().BeLessThan(Math.PI / 2);
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

    private static double Distance(PathPoint a, PathPoint b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}
