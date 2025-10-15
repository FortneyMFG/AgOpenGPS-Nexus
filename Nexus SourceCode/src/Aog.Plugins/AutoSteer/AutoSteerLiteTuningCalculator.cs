using System;
using Aog.Core.Machines;

namespace Aog.Plugins.AutoSteer;

/// <summary>
/// Derives guidance tuning parameters from a machine profile using heuristics that mirror V6 behaviour.
/// </summary>
public static class AutoSteerLiteTuningCalculator
{
    /// <summary>
    /// Builds an <see cref="AutoSteerLiteTuningProfile"/> tailored to the supplied machine profile.
    /// </summary>
    public static AutoSteerLiteTuningProfile BuildProfile(MachineProfile machine)
    {
        if (machine is null)
        {
            throw new ArgumentNullException(nameof(machine));
        }

        var vehicle = machine.Vehicle;
        var implement = machine.Implement;

        var holdMultiplier = Math.Clamp(2.0 + vehicle.WheelbaseMeters * 0.3, 2.0, 4.8);
        var speedMultiplier = Math.Clamp(1.1 + implement.WidthMeters / 10.0, 1.2, 2.2);
        var acquireFactor = Math.Clamp(0.8 + implement.WidthMeters / 40.0, 0.85, 0.95);
        var minimumLookAhead = Math.Max(2.0, vehicle.WheelbaseMeters * 0.45);
        var holdThreshold = Math.Clamp(implement.WidthMeters * 0.02, 0.05, 0.15);
        var acquireThreshold = Math.Clamp(holdThreshold + Math.Clamp(implement.WidthMeters * 0.075, 0.25, 0.5), holdThreshold + 0.1, 0.6);
        var startupDistance = Math.Clamp(vehicle.WheelbaseMeters * 1.2, 3.0, 6.0);
        var startupMultiplier = Math.Clamp(0.5 + implement.WidthMeters / 20.0, 0.6, 0.8);
        var headlandSlowdown = Math.Clamp(0.6 + implement.WidthMeters / 40.0, 0.6, 0.85);
        var constraintSlowdown = 0.4;
        var constraintMargin = Math.Clamp(implement.WidthMeters * 0.12, 0.5, 2.5);

        var profile = new AutoSteerLiteTuningProfile
        {
            LookAheadHoldMultiplier = holdMultiplier,
            SpeedMultiplier = speedMultiplier,
            AcquireFactor = acquireFactor,
            MinimumLookAheadMeters = minimumLookAhead,
            CrossTrackHoldThresholdMeters = holdThreshold,
            CrossTrackAcquireThresholdMeters = acquireThreshold,
            CrossTrackFilterGain = 0.5,
            LookAheadFilterGain = 0.25,
            StartupHoldDistanceMeters = startupDistance,
            StartupLookAheadMultiplier = startupMultiplier,
            HeadlandSlowdownMultiplier = headlandSlowdown,
            ConstraintSlowdownMultiplier = constraintSlowdown,
            ConstraintDistanceMarginMeters = constraintMargin,
        };

        profile.Validate();
        return profile;
    }
}
