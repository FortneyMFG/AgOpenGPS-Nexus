using System;
using System.Collections.Generic;

namespace Aog.Plugins.AutoSteer;

/// <summary>
/// Provides reusable path geometry utilities shared by the autosteer controllers.
/// </summary>
internal static class AutoSteerPathGeometry
{
    public static (double X, double Y, double DirectionX, double DirectionY, int SegmentIndex, double SegmentProgress, double SegmentLength)
        FindClosestPoint(VehicleState state, IReadOnlyList<PathPoint> path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (path.Count < 2)
        {
            throw new InvalidOperationException("Path must contain at least two points.");
        }

        var bestDistanceSquared = double.MaxValue;
        (double X, double Y, double DirectionX, double DirectionY, int SegmentIndex, double SegmentProgress, double SegmentLength) best = default;
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

    public static (double X, double Y, double DirectionX, double DirectionY) ComputeLookAheadTarget(
        IReadOnlyList<PathPoint> path,
        (double X, double Y, double DirectionX, double DirectionY, int SegmentIndex, double SegmentProgress, double SegmentLength) closest,
        double lookAhead)
    {
        ArgumentNullException.ThrowIfNull(path);
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

    public static double ComputePurePursuitSteering(
        VehicleState state,
        double toTargetX,
        double toTargetY,
        double targetDistance)
    {
        return ComputePurePursuitTargetGeometry(state, toTargetX, toTargetY, targetDistance).SteeringAngleRadians;
    }

    public static PurePursuitTargetGeometry ComputePurePursuitTargetGeometry(
        VehicleState state,
        double toTargetX,
        double toTargetY,
        double targetDistance)
    {
        if (targetDistance < 1e-6)
        {
            return new PurePursuitTargetGeometry(state.HeadingRadians, 0, 0, 0);
        }

        var headingToTarget = Math.Atan2(toTargetY, toTargetX);
        var headingError = AutoSteerMath.NormalizeAngle(headingToTarget - state.HeadingRadians);
        var curvature = 2 * Math.Sin(headingError) / Math.Max(targetDistance, 1e-6);
        var steering = Math.Atan(curvature * state.WheelbaseMeters);
        return new PurePursuitTargetGeometry(headingToTarget, headingError, curvature, steering);
    }

    public static double ComputeCrossTrack(
        VehicleState state,
        (double X, double Y, double DirectionX, double DirectionY, int SegmentIndex, double SegmentProgress, double SegmentLength) closest)
    {
        var vectorToClosestX = state.X - closest.X;
        var vectorToClosestY = state.Y - closest.Y;
        return closest.DirectionX * vectorToClosestY - closest.DirectionY * vectorToClosestX;
    }
}

public readonly record struct PurePursuitTargetGeometry(
    double HeadingToTargetRadians,
    double HeadingErrorRadians,
    double CurvaturePerMeter,
    double SteeringAngleRadians);
