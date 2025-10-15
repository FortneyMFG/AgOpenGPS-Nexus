using System;
using System.Collections.Generic;
using System.Linq;
using Aog.Plugins.AutoSteer;
using Aog.Plugins.Guidance;
using FluentAssertions;
using Xunit;

namespace Aog.Plugins.Tests.AutoSteer;

public sealed class AutoSteerClosedLoopTurnTests
{
    [Fact]
    public void PurePursuitController_TracksTurnPlanThroughLaneChange()
    {
        var currentPass = BuildPass(0, 60, 0);
        var nextPass = BuildPass(60, 0, 10);

        var turnPlanner = new GuidanceTurnPlanner(new TurnPlannerSettings
        {
            LeadInDistanceMeters = 8,
            ExitExtensionMeters = 4,
            DesiredTurnRadiusMeters = 8,
            MinimumTurnRadiusMeters = 3,
            SampleSpacingMeters = 0.5
        });

        var turnPlan = turnPlanner.Plan(currentPass, nextPass);
        var path = ComposePath(currentPass, turnPlan.Path, nextPass);

        var controller = new AutoSteerLiteController(new AutoSteerLiteSettings
        {
            EnableDynamicLookAhead = false,
            LookAheadDistance = 6.0,
            SteeringAngleLimitRadians = Math.PI / 180d * 35
        })
        {
            Mode = AutoSteerMode.PurePursuit
        };

        var state = new VehicleState(
            x: -2,
            y: -1.2,
            headingRadians: AutoSteerMath.NormalizeAngle(3 * Math.PI / 180),
            speedMetersPerSecond: 5.0,
            wheelbaseMeters: 3.0);

        const double dt = 0.1;
        const int steps = 420;

        var crossTrackHistory = new List<double>(steps);
        var headingHistory = new List<double>(steps);

        for (var i = 0; i < steps; i++)
        {
            var steering = controller.ComputeSteeringAngle(state, path);
            var closest = AutoSteerPathGeometry.FindClosestPoint(state, path);
            var crossTrack = AutoSteerPathGeometry.ComputeCrossTrack(state, closest);
            crossTrackHistory.Add(Math.Abs(crossTrack));

            var targetHeading = Math.Atan2(closest.DirectionY, closest.DirectionX);
            var headingError = AutoSteerMath.NormalizeAngle(targetHeading - state.HeadingRadians);
            headingHistory.Add(Math.Abs(headingError));

            state = state.Advance(steering, dt);
        }

        crossTrackHistory.Max().Should().BeLessThan(0.9);
        crossTrackHistory.Skip(80).Max().Should().BeLessThan(0.45);
        headingHistory.Skip(80).Max().Should().BeLessThan(0.35);

        state.Y.Should().BeApproximately(10, 0.3);
        AutoSteerMath.NormalizeAngle(state.HeadingRadians - Math.PI).Should().BeApproximately(0, 0.1);
        state.X.Should().BeGreaterThan(0);
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

    private static IReadOnlyList<PathPoint> ComposePath(
        IReadOnlyList<PathPoint> entry,
        IReadOnlyList<PathPoint> turn,
        IReadOnlyList<PathPoint> exit)
    {
        var combined = new List<PathPoint>(entry.Count + turn.Count + exit.Count);
        combined.AddRange(entry);
        combined.AddRange(turn.Skip(1));
        combined.AddRange(exit.Skip(1));
        return combined;
    }
}
