using System;
using System.Collections.Generic;

namespace Aog.Plugins.AutoSteer;

/// <summary>
/// Implements the AutoSteer-Lite controller that can switch between Pure Pursuit and Stanley steering at runtime.
/// </summary>
public sealed class AutoSteerLiteController
{
    private AutoSteerMode _mode = AutoSteerMode.PurePursuit;

    public AutoSteerLiteController(AutoSteerLiteSettings? settings = null)
    {
        Settings = (settings ?? new AutoSteerLiteSettings()).Clone();
        Settings.Validate();
    }

    public AutoSteerLiteSettings Settings { get; }

    public AutoSteerMode Mode
    {
        get => _mode;
        set
        {
            if (!Enum.IsDefined(typeof(AutoSteerMode), value))
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            _mode = value;
        }
    }

    public double ComputeSteeringAngle(VehicleState state, IReadOnlyList<PathPoint> path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (path.Count < 2)
        {
            throw new InvalidOperationException("AutoSteer requires at least two path points.");
        }

        var closest = FindClosestPoint(state, path);
        var target = ComputeLookAheadTarget(path, closest, Settings.LookAheadDistance);

        var toTargetX = target.X - state.X;
        var toTargetY = target.Y - state.Y;
        var distanceToTarget = Math.Sqrt(toTargetX * toTargetX + toTargetY * toTargetY);
        if (distanceToTarget < 1e-6)
        {
            return 0;
        }

        var steering = Mode switch
        {
            AutoSteerMode.PurePursuit => ComputePurePursuit(state, toTargetX, toTargetY, distanceToTarget),
            AutoSteerMode.Stanley => ComputeStanley(state, closest, target, toTargetX, toTargetY, distanceToTarget),
            _ => throw new InvalidOperationException($"Unsupported AutoSteer mode: {Mode}.")
        };

        return Math.Clamp(steering, -Settings.SteeringAngleLimitRadians, Settings.SteeringAngleLimitRadians);
    }

    private static (double X, double Y, double DirectionX, double DirectionY, int SegmentIndex, double SegmentProgress, double SegmentLength) FindClosestPoint(
        VehicleState state,
        IReadOnlyList<PathPoint> path)
    {
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

    private static double ComputePurePursuit(VehicleState state, double toTargetX, double toTargetY, double targetDistance)
    {
        var headingToTarget = Math.Atan2(toTargetY, toTargetX);
        var headingError = AutoSteerMath.NormalizeAngle(headingToTarget - state.HeadingRadians);
        var curvature = 2 * Math.Sin(headingError) / Math.Max(targetDistance, 1e-6);
        return Math.Atan(curvature * state.WheelbaseMeters);
    }

    private double ComputeStanley(
        VehicleState state,
        (double X, double Y, double DirectionX, double DirectionY, int SegmentIndex, double SegmentProgress, double SegmentLength) closest,
        (double X, double Y, double DirectionX, double DirectionY) target,
        double toTargetX,
        double toTargetY,
        double targetDistance)
    {
        var directionX = target.DirectionX;
        var directionY = target.DirectionY;
        if (Math.Abs(directionX) < 1e-9 && Math.Abs(directionY) < 1e-9)
        {
            directionX = closest.DirectionX;
            directionY = closest.DirectionY;
        }

        var pathHeading = Math.Atan2(directionY, directionX);
        var headingError = AutoSteerMath.NormalizeAngle(pathHeading - state.HeadingRadians);

        var vectorToClosestX = state.X - closest.X;
        var vectorToClosestY = state.Y - closest.Y;
        var crossTrack = directionX * vectorToClosestY - directionY * vectorToClosestX;

        var speed = Math.Max(state.SpeedMetersPerSecond, 0);
        var softening = Math.Max(Settings.StanleySoftening, 1e-3);
        var correction = Math.Atan(Settings.StanleyGain * crossTrack / (speed + softening));

        if (speed < 0.1)
        {
            var pp = ComputePurePursuit(state, toTargetX, toTargetY, targetDistance);
            return 0.5 * (headingError + correction) + 0.5 * pp;
        }

        return headingError + correction;
    }
}

public enum AutoSteerMode
{
    PurePursuit,
    Stanley
}

public sealed class AutoSteerLiteSettings
{
    public double LookAheadDistance { get; set; } = 5.0;
    public double StanleyGain { get; set; } = 2.2;
    public double StanleySoftening { get; set; } = 1.0;
    public double SteeringAngleLimitRadians { get; set; } = Math.PI / 180d * 35;

    internal AutoSteerLiteSettings Clone() => new()
    {
        LookAheadDistance = LookAheadDistance,
        StanleyGain = StanleyGain,
        StanleySoftening = StanleySoftening,
        SteeringAngleLimitRadians = SteeringAngleLimitRadians
    };

    internal void Validate()
    {
        if (LookAheadDistance <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(LookAheadDistance), LookAheadDistance, "Look-ahead distance must be positive.");
        }

        if (StanleyGain <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(StanleyGain), StanleyGain, "Stanley gain must be positive.");
        }

        if (StanleySoftening < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(StanleySoftening), StanleySoftening, "Stanley softening must be non-negative.");
        }

        if (SteeringAngleLimitRadians <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(SteeringAngleLimitRadians), SteeringAngleLimitRadians, "Steering limit must be positive.");
        }
    }
}

public readonly struct VehicleState
{
    public VehicleState(double x, double y, double headingRadians, double speedMetersPerSecond, double wheelbaseMeters)
    {
        if (wheelbaseMeters <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(wheelbaseMeters), wheelbaseMeters, "Wheelbase must be positive.");
        }

        X = x;
        Y = y;
        HeadingRadians = headingRadians;
        SpeedMetersPerSecond = speedMetersPerSecond;
        WheelbaseMeters = wheelbaseMeters;
    }

    public double X { get; }
    public double Y { get; }
    public double HeadingRadians { get; }
    public double SpeedMetersPerSecond { get; }
    public double WheelbaseMeters { get; }

    public VehicleState Advance(double steeringAngleRadians, double timeStepSeconds)
    {
        if (timeStepSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(timeStepSeconds), timeStepSeconds, "Time step must be positive.");
        }

        var turnRate = SpeedMetersPerSecond / WheelbaseMeters * Math.Tan(steeringAngleRadians);
        var headingDelta = turnRate * timeStepSeconds;
        var headingMid = AutoSteerMath.NormalizeAngle(HeadingRadians + 0.5 * headingDelta);
        var newX = X + SpeedMetersPerSecond * Math.Cos(headingMid) * timeStepSeconds;
        var newY = Y + SpeedMetersPerSecond * Math.Sin(headingMid) * timeStepSeconds;
        var newHeading = AutoSteerMath.NormalizeAngle(HeadingRadians + headingDelta);
        return new VehicleState(newX, newY, newHeading, SpeedMetersPerSecond, WheelbaseMeters);
    }
}

public static class AutoSteerMath
{
    public static double NormalizeAngle(double angle)
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

public readonly record struct PathPoint(double X, double Y);
