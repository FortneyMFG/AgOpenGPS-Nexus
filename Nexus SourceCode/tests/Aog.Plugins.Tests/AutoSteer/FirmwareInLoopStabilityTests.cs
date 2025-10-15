using System;
using System.Collections.Generic;
using System.Linq;
using Aog.Core.Guidance;
using Aog.Core.V1;
using Aog.Plugins.AutoSteer;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Xunit;

namespace Aog.Plugins.Tests.AutoSteer;

public sealed class FirmwareInLoopStabilityTests
{
    [Fact]
    public void StanleyController_DynamicLookAheadRemainsStable()
    {
        var settings = new AutoSteerLiteSettings
        {
            EnableDynamicLookAhead = true,
            LookAheadDistance = 6.0,
            MinimumLookAheadMeters = 2.0,
            GoalPointLookAheadHold = 3.0,
            GoalPointLookAheadMultiplier = 1.5,
            GoalPointAcquireFactor = 0.9,
            CrossTrackHoldThresholdMeters = 0.1,
            CrossTrackAcquireThresholdMeters = 0.4,
            StartupHoldDistanceMeters = 2.0,
            StartupLookAheadMultiplier = 0.65,
            CrossTrackFilterGain = 0.2,
            LookAheadFilterGain = 0.25,
            HeadlandSlowdownMultiplier = 0.7,
            ConstraintSlowdownMultiplier = 0.4,
            ConstraintDistanceMarginMeters = 0.5,
            StanleyGain = 2.2,
            StanleySoftening = 1.0
        };

        var controller = new AutoSteerLiteController(settings)
        {
            Mode = AutoSteerMode.Stanley
        };

        var path = BuildStraightPath(160, 2);
        var state = new VehicleState(
            x: 0,
            y: 1.5,
            headingRadians: AutoSteerMath.NormalizeAngle(7 * Math.PI / 180),
            speedMetersPerSecond: 5.0,
            wheelbaseMeters: 3.0);

        const double dt = 0.1;
        const int steps = 220;

        var crossTrackHistory = new List<double>(steps);
        var headingErrorHistory = new List<double>(steps);
        var lookAheadHistory = new List<double>(steps);
        var constraintDistanceHistory = new List<double?>(steps);
        var blockingFlags = new List<bool>(steps);

        for (var i = 0; i < steps; i++)
        {
            var context = DetermineConstraintContext(i);
            state = state.WithConstraintContext(context);

            var steering = controller.ComputeSteeringAngle(state, path);
            var preview = ComputePreview(state, path, controller.LastLookAheadDistance, steering);

            crossTrackHistory.Add(Math.Abs(preview.CrossTrack));
            headingErrorHistory.Add(Math.Abs(preview.HeadingError));
            lookAheadHistory.Add(controller.LastLookAheadDistance);
            constraintDistanceHistory.Add(context?.DistanceToConstraintMeters);
            blockingFlags.Add(context?.HasBlockingConstraint ?? false);

            state = state.Advance(steering, dt);
        }

        crossTrackHistory[^1].Should().BeLessThan(0.05);
        crossTrackHistory.Skip(40).Max().Should().BeLessThan(0.35);
        headingErrorHistory.Skip(40).Max().Should().BeLessThan(0.2);

        var minLookAhead = lookAheadHistory.Min();
        minLookAhead.Should().BeGreaterOrEqualTo(settings.MinimumLookAheadMeters - 1e-6);

        var baselineAverage = lookAheadHistory.Take(80).Average();
        var headlandAverage = lookAheadHistory.Skip(100).Take(40).Average();
        headlandAverage.Should().BeLessThan(baselineAverage);

        var blockingIndices = blockingFlags
            .Select((flag, index) => (flag, index))
            .Where(item => item.flag)
            .Select(item => item.index)
            .ToArray();

        blockingIndices.Should().NotBeEmpty();
        foreach (var index in blockingIndices)
        {
            var distance = constraintDistanceHistory[index];
            distance.Should().NotBeNull();
            lookAheadHistory[index].Should().BeLessOrEqualTo(distance!.Value - settings.ConstraintDistanceMarginMeters + 1e-6);
        }

        var finalContext = DetermineConstraintContext(steps - 1);
        var finalState = state.WithConstraintContext(finalContext);
        var finalSteering = controller.ComputeSteeringAngle(finalState, path);
        var finalPreview = ComputePreview(finalState, path, controller.LastLookAheadDistance, finalSteering);

        var previewModel = new GuidanceLanePreview(
            crossTrackErrorMeters: finalPreview.CrossTrack,
            headingErrorRadians: finalPreview.HeadingError,
            lookAheadDistanceMeters: controller.LastLookAheadDistance,
            targetPoint: new GuidanceLanePoint(finalPreview.TargetX, finalPreview.TargetY, finalPreview.TargetHeading),
            controllerOutput: finalPreview.ControllerOutput,
            controllerEnabled: true,
            targetCurvaturePerMeter: 0,
            constraint: finalContext is null
                ? null
                : new GuidanceLaneConstraintState(
                    new PoseZoneMask { InsideHeadland = finalContext.Value.InsideHeadland, InsideBoundary = true },
                    finalContext.Value.HasBlockingConstraint,
                    finalContext.Value.InsideHeadland,
                    finalContext.Value.DistanceToConstraintMeters));

        var laneMetadata = new GuidanceLaneMetadata("lane-fi", GuidanceLaneTemplate.Straight, "Firmware Loop Lane");
        var lanePass = new GuidanceLanePass(0, path.Select(p => new GuidanceLanePoint(p.X, p.Y, 0)).ToArray(), 0, 0);
        var lane = new GuidanceLane(
            laneMetadata,
            laneSpacingMeters: 3.0,
            implementWidthMeters: 4.5,
            overlapMeters: 0.2,
            nudgeMeters: 0,
            extensionLengthMeters: 160,
            baseHeadingRadians: 0,
            passes: new[] { lanePass },
            preview: previewModel);

        var publish = new GuidanceLanePublish(new Header
        {
            Sequence = 512,
            Timestamp = Timestamp.FromDateTime(DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)),
            Frame = "field",
            Source = "sim"
        }, lane);

        var proto = publish.ToProto();
        proto.Preview.Should().NotBeNull();
        proto.Preview.LookaheadDistanceM.Should().BeApproximately(controller.LastLookAheadDistance, 1e-6);
        proto.Preview.CrossTrackErrorM.Should().BeApproximately(finalPreview.CrossTrack, 1e-6);
        proto.Preview.ControllerOutput.Should().BeApproximately(finalPreview.ControllerOutput, 1e-6);
        proto.Preview.ControllerEnabled.Should().BeTrue();
        proto.Preview.Target.Point.EastingM.Should().BeApproximately(finalPreview.TargetX, 1e-6);
    }

    private static IReadOnlyList<PathPoint> BuildStraightPath(double lengthMetres, double spacingMetres)
    {
        var points = new List<PathPoint>();
        for (double x = 0; x <= lengthMetres; x += spacingMetres)
        {
            points.Add(new PathPoint(x, 0));
        }

        if (!points.Any() || points[^1].X < lengthMetres)
        {
            points.Add(new PathPoint(lengthMetres, 0));
        }

        return points;
    }

    private static ConstraintLookAheadContext? DetermineConstraintContext(int step)
    {
        if (step < 100)
        {
            return null;
        }

        if (step < 150)
        {
            return new ConstraintLookAheadContext(
                HasBlockingConstraint: false,
                InsideHeadland: true,
                DistanceToConstraintMeters: 9.0);
        }

        if (step < 190)
        {
            return new ConstraintLookAheadContext(
                HasBlockingConstraint: true,
                InsideHeadland: true,
                DistanceToConstraintMeters: 4.0);
        }

        return null;
    }

    private static PreviewMetrics ComputePreview(
        VehicleState state,
        IReadOnlyList<PathPoint> path,
        double lookAhead,
        double controllerOutput)
    {
        var closest = FindClosestPoint(state, path);
        var crossTrack = ComputeCrossTrack(state, closest);
        var target = ComputeLookAheadTarget(path, closest, lookAhead);
        var pathHeading = Math.Atan2(target.DirectionY, target.DirectionX);
        var headingError = AutoSteerMath.NormalizeAngle(pathHeading - state.HeadingRadians);

        return new PreviewMetrics(crossTrack, headingError, target.X, target.Y, pathHeading, controllerOutput);
    }

    private static (double X, double Y, double DirectionX, double DirectionY, int SegmentIndex, double SegmentProgress, double SegmentLength) FindClosestPoint(
        VehicleState state,
        IReadOnlyList<PathPoint> path)
    {
        var bestDistanceSquared = double.MaxValue;
        var best = default((double X, double Y, double DirectionX, double DirectionY, int SegmentIndex, double SegmentProgress, double SegmentLength));
        var found = false;

        for (var i = 0; i < path.Count - 1; i++)
        {
            var start = path[i];
            var end = path[i + 1];
            var segmentX = end.X - start.X;
            var segmentY = end.Y - start.Y;
            var lengthSquared = segmentX * segmentX + segmentY * segmentY;
            if (lengthSquared < 1e-9)
            {
                continue;
            }

            var toVehicleX = state.X - start.X;
            var toVehicleY = state.Y - start.Y;
            var projection = Math.Clamp((toVehicleX * segmentX + toVehicleY * segmentY) / lengthSquared, 0, 1);
            var closestX = start.X + segmentX * projection;
            var closestY = start.Y + segmentY * projection;
            var dx = state.X - closestX;
            var dy = state.Y - closestY;
            var distanceSquared = dx * dx + dy * dy;

            if (distanceSquared < bestDistanceSquared)
            {
                var length = Math.Sqrt(lengthSquared);
                best = (closestX, closestY, segmentX / length, segmentY / length, i, projection, length);
                bestDistanceSquared = distanceSquared;
                found = true;
            }
        }

        if (!found)
        {
            var last = path[^1];
            var index = Math.Max(path.Count - 2, 0);
            return (last.X, last.Y, 1, 0, index, 1, 0);
        }

        return best;
    }

    private static double ComputeCrossTrack(
        VehicleState state,
        (double X, double Y, double DirectionX, double DirectionY, int SegmentIndex, double SegmentProgress, double SegmentLength) closest)
    {
        var vectorToClosestX = state.X - closest.X;
        var vectorToClosestY = state.Y - closest.Y;
        return closest.DirectionX * vectorToClosestY - closest.DirectionY * vectorToClosestX;
    }

    private static (double X, double Y, double DirectionX, double DirectionY) ComputeLookAheadTarget(
        IReadOnlyList<PathPoint> path,
        (double X, double Y, double DirectionX, double DirectionY, int SegmentIndex, double SegmentProgress, double SegmentLength) closest,
        double lookAhead)
    {
        if (lookAhead <= 0)
        {
            throw new InvalidOperationException("Look-ahead distance must be greater than zero.");
        }

        var remaining = lookAhead;
        var segmentIndex = closest.SegmentIndex;
        var progress = closest.SegmentProgress;
        var currentX = closest.X;
        var currentY = closest.Y;
        var directionX = closest.DirectionX;
        var directionY = closest.DirectionY;
        var remainingOnSegment = closest.SegmentLength * (1 - progress);

        while (remaining > remainingOnSegment && segmentIndex < path.Count - 2)
        {
            remaining -= remainingOnSegment;
            segmentIndex++;
            var start = path[segmentIndex];
            var end = path[segmentIndex + 1];
            var segmentX = end.X - start.X;
            var segmentY = end.Y - start.Y;
            var length = Math.Sqrt(segmentX * segmentX + segmentY * segmentY);
            if (length < 1e-9)
            {
                currentX = start.X;
                currentY = start.Y;
                remainingOnSegment = 0;
                continue;
            }

            directionX = segmentX / length;
            directionY = segmentY / length;
            currentX = start.X;
            currentY = start.Y;
            remainingOnSegment = length;
        }

        if (remainingOnSegment < 1e-9 || remaining > remainingOnSegment)
        {
            var last = path[^1];
            var penultimate = path[Math.Max(path.Count - 2, 0)];
            var segmentX = last.X - penultimate.X;
            var segmentY = last.Y - penultimate.Y;
            var length = Math.Sqrt(segmentX * segmentX + segmentY * segmentY);
            if (length < 1e-9)
            {
                directionX = 1;
                directionY = 0;
            }
            else
            {
                directionX = segmentX / length;
                directionY = segmentY / length;
            }

            return (last.X, last.Y, directionX, directionY);
        }

        var targetX = currentX + directionX * remaining;
        var targetY = currentY + directionY * remaining;
        return (targetX, targetY, directionX, directionY);
    }

    private readonly record struct PreviewMetrics(
        double CrossTrack,
        double HeadingError,
        double TargetX,
        double TargetY,
        double TargetHeading,
        double ControllerOutput);
}
