using System;
using System.Collections.Generic;
using Aog.Core.Safety;

namespace Aog.Plugins.AutoSteer;

/// <summary>
/// Implements the AutoSteer-Lite controller that can switch between Pure Pursuit and Stanley steering at runtime.
/// </summary>
public sealed class AutoSteerLiteController
{
    private readonly AutoSteerLiteTuningState? _tuningState;
    private VehicleState _previousState;
    private bool _hasPreviousState;
    private AutoSteerMode _mode = AutoSteerMode.PurePursuit;

    public AutoSteerLiteController(AutoSteerLiteSettings? settings = null, AutoSteerLiteTuningProfile? tuningProfile = null)
    {
        Settings = (settings ?? new AutoSteerLiteSettings()).Clone();
        Settings.Validate();

        if (Settings.EnableDynamicLookAhead)
        {
            var profile = tuningProfile ?? Settings.CreateTuningProfile();
            _tuningState = new AutoSteerLiteTuningState(profile);
        }
    }

    public AutoSteerLiteSettings Settings { get; }

    public double LastLookAheadDistance { get; private set; }

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

    public double ComputeSteeringAngle(
        VehicleState state,
        IReadOnlyList<PathPoint> path,
        ConstraintGateSnapshot? constraintGate = null)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (path.Count < 2)
        {
            throw new InvalidOperationException("AutoSteer requires at least two path points.");
        }

        if (constraintGate is not null && !constraintGate.AutosteerAllowed)
        {
            LastLookAheadDistance = 0;
            return 0;
        }

        var distanceTravelled = UpdateTravelledDistance(state);
        var closest = AutoSteerPathGeometry.FindClosestPoint(state, path);

        var crossTrack = AutoSteerPathGeometry.ComputeCrossTrack(state, closest);
        var lookAheadDistance = _tuningState?.Update(
            crossTrack,
            state.SpeedMetersPerSecond,
            distanceTravelled,
            state.ConstraintContext)
            ?? Settings.LookAheadDistance;

        LastLookAheadDistance = lookAheadDistance;
        var target = AutoSteerPathGeometry.ComputeLookAheadTarget(path, closest, lookAheadDistance);

        var toTargetX = target.X - state.X;
        var toTargetY = target.Y - state.Y;
        var distanceToTarget = Math.Sqrt(toTargetX * toTargetX + toTargetY * toTargetY);
        if (distanceToTarget < 1e-6)
        {
            return 0;
        }

        var steering = Mode switch
        {
            AutoSteerMode.PurePursuit => AutoSteerPathGeometry.ComputePurePursuitSteering(state, toTargetX, toTargetY, distanceToTarget),
            AutoSteerMode.Stanley => ComputeStanley(state, closest, target, toTargetX, toTargetY, distanceToTarget, crossTrack),
            _ => throw new InvalidOperationException($"Unsupported AutoSteer mode: {Mode}.")
        };

        return Math.Clamp(steering, -Settings.SteeringAngleLimitRadians, Settings.SteeringAngleLimitRadians);
    }

    private double ComputeStanley(
        VehicleState state,
        (double X, double Y, double DirectionX, double DirectionY, int SegmentIndex, double SegmentProgress, double SegmentLength) closest,
        (double X, double Y, double DirectionX, double DirectionY) target,
        double toTargetX,
        double toTargetY,
        double targetDistance,
        double crossTrack)
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

        var speed = Math.Max(state.SpeedMetersPerSecond, 0);
        var softening = Math.Max(Settings.StanleySoftening, 1e-3);
        var correction = Math.Atan(Settings.StanleyGain * crossTrack / (speed + softening));

        if (speed < 0.1)
        {
            var pp = AutoSteerPathGeometry.ComputePurePursuitSteering(state, toTargetX, toTargetY, targetDistance);
            return 0.5 * (headingError + correction) + 0.5 * pp;
        }

        return headingError + correction;
    }

    private double UpdateTravelledDistance(VehicleState state)
    {
        if (!_hasPreviousState)
        {
            _previousState = state;
            _hasPreviousState = true;
            return 0;
        }

        var dx = state.X - _previousState.X;
        var dy = state.Y - _previousState.Y;
        var distance = Math.Sqrt(dx * dx + dy * dy);
        _previousState = state;
        return distance;
    }

    public void Reset()
    {
        _tuningState?.Reset();
        _hasPreviousState = false;
        _previousState = default;
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

    public bool EnableDynamicLookAhead { get; set; } = true;
    public double MinimumLookAheadMeters { get; set; } = 2.0;
    public double GoalPointLookAheadHold { get; set; } = 3.0;
    public double GoalPointLookAheadMultiplier { get; set; } = 1.5;
    public double GoalPointAcquireFactor { get; set; } = 0.9;
    public double CrossTrackHoldThresholdMeters { get; set; } = 0.1;
    public double CrossTrackAcquireThresholdMeters { get; set; } = 0.4;
    public double StartupHoldDistanceMeters { get; set; } = 4.0;
    public double StartupLookAheadMultiplier { get; set; } = 0.65;
    public double CrossTrackFilterGain { get; set; } = 0.5;
    public double LookAheadFilterGain { get; set; } = 0.25;
    public double HeadlandSlowdownMultiplier { get; set; } = 0.75;
    public double ConstraintSlowdownMultiplier { get; set; } = 0.5;
    public double ConstraintDistanceMarginMeters { get; set; } = 1.0;

    internal AutoSteerLiteSettings Clone() => new()
    {
        LookAheadDistance = LookAheadDistance,
        StanleyGain = StanleyGain,
        StanleySoftening = StanleySoftening,
        SteeringAngleLimitRadians = SteeringAngleLimitRadians,
        EnableDynamicLookAhead = EnableDynamicLookAhead,
        MinimumLookAheadMeters = MinimumLookAheadMeters,
        GoalPointLookAheadHold = GoalPointLookAheadHold,
        GoalPointLookAheadMultiplier = GoalPointLookAheadMultiplier,
        GoalPointAcquireFactor = GoalPointAcquireFactor,
        CrossTrackHoldThresholdMeters = CrossTrackHoldThresholdMeters,
        CrossTrackAcquireThresholdMeters = CrossTrackAcquireThresholdMeters,
        StartupHoldDistanceMeters = StartupHoldDistanceMeters,
        StartupLookAheadMultiplier = StartupLookAheadMultiplier,
        CrossTrackFilterGain = CrossTrackFilterGain,
        LookAheadFilterGain = LookAheadFilterGain,
        HeadlandSlowdownMultiplier = HeadlandSlowdownMultiplier,
        ConstraintSlowdownMultiplier = ConstraintSlowdownMultiplier,
        ConstraintDistanceMarginMeters = ConstraintDistanceMarginMeters
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

        if (MinimumLookAheadMeters <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MinimumLookAheadMeters), MinimumLookAheadMeters, "Minimum look-ahead must be positive.");
        }

        if (GoalPointLookAheadHold <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(GoalPointLookAheadHold), GoalPointLookAheadHold, "Hold multiplier must be positive.");
        }

        if (GoalPointLookAheadMultiplier <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(GoalPointLookAheadMultiplier), GoalPointLookAheadMultiplier, "Speed multiplier must be positive.");
        }

        if (GoalPointAcquireFactor <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(GoalPointAcquireFactor), GoalPointAcquireFactor, "Acquire factor must be positive.");
        }

        if (CrossTrackHoldThresholdMeters < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(CrossTrackHoldThresholdMeters), CrossTrackHoldThresholdMeters, "Hold threshold must be non-negative.");
        }

        if (CrossTrackAcquireThresholdMeters <= CrossTrackHoldThresholdMeters)
        {
            throw new ArgumentOutOfRangeException(nameof(CrossTrackAcquireThresholdMeters), CrossTrackAcquireThresholdMeters, "Acquire threshold must exceed hold threshold.");
        }

        if (StartupHoldDistanceMeters < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(StartupHoldDistanceMeters), StartupHoldDistanceMeters, "Startup hold distance must be non-negative.");
        }

        if (StartupLookAheadMultiplier <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(StartupLookAheadMultiplier), StartupLookAheadMultiplier, "Startup multiplier must be positive.");
        }

        if (CrossTrackFilterGain is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(CrossTrackFilterGain), CrossTrackFilterGain, "Cross-track filter gain must be in [0, 1].");
        }

        if (LookAheadFilterGain is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(LookAheadFilterGain), LookAheadFilterGain, "Look-ahead filter gain must be in [0, 1].");
        }

        if (HeadlandSlowdownMultiplier <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(HeadlandSlowdownMultiplier), HeadlandSlowdownMultiplier, "Headland slowdown multiplier must be positive.");
        }

        if (ConstraintSlowdownMultiplier <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ConstraintSlowdownMultiplier), ConstraintSlowdownMultiplier, "Constraint slowdown multiplier must be positive.");
        }

        if (ConstraintDistanceMarginMeters < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ConstraintDistanceMarginMeters), ConstraintDistanceMarginMeters, "Constraint distance margin must be non-negative.");
        }
    }

    internal AutoSteerLiteTuningProfile CreateTuningProfile() => new()
    {
        LookAheadHoldMultiplier = GoalPointLookAheadHold,
        SpeedMultiplier = GoalPointLookAheadMultiplier,
        AcquireFactor = GoalPointAcquireFactor,
        MinimumLookAheadMeters = MinimumLookAheadMeters,
        CrossTrackHoldThresholdMeters = CrossTrackHoldThresholdMeters,
        CrossTrackAcquireThresholdMeters = CrossTrackAcquireThresholdMeters,
        StartupHoldDistanceMeters = StartupHoldDistanceMeters,
        StartupLookAheadMultiplier = StartupLookAheadMultiplier,
        CrossTrackFilterGain = CrossTrackFilterGain,
        LookAheadFilterGain = LookAheadFilterGain,
        HeadlandSlowdownMultiplier = HeadlandSlowdownMultiplier,
        ConstraintSlowdownMultiplier = ConstraintSlowdownMultiplier,
        ConstraintDistanceMarginMeters = ConstraintDistanceMarginMeters
    };
