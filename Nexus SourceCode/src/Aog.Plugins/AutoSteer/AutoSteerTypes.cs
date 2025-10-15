using System;

namespace Aog.Plugins.AutoSteer;

/// <summary>
/// Represents the vehicle state used by autosteer controllers.
/// </summary>
public readonly struct VehicleState
{
    public VehicleState(
        double x,
        double y,
        double headingRadians,
        double speedMetersPerSecond,
        double wheelbaseMeters,
        ConstraintLookAheadContext? constraintContext = null)
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
        ConstraintContext = constraintContext;
    }

    public double X { get; }

    public double Y { get; }

    public double HeadingRadians { get; }

    public double SpeedMetersPerSecond { get; }

    public double WheelbaseMeters { get; }

    public ConstraintLookAheadContext? ConstraintContext { get; }

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
        return new VehicleState(newX, newY, newHeading, SpeedMetersPerSecond, WheelbaseMeters, ConstraintContext);
    }

    public VehicleState WithConstraintContext(ConstraintLookAheadContext? constraintContext) =>
        new(X, Y, HeadingRadians, SpeedMetersPerSecond, WheelbaseMeters, constraintContext);
}

/// <summary>
/// Path geometry helpers for the autosteer controllers.
/// </summary>
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

/// <summary>
/// Defines a 2D path point in metres.
/// </summary>
public readonly record struct PathPoint(double X, double Y);
