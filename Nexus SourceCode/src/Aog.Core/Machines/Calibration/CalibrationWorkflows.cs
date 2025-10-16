using System;
using System.Collections.Generic;
using System.Linq;
using Aog.Core.Machines.Axle;

namespace Aog.Core.Machines.Calibration;

/// <summary>
/// Provides calibration workflows that close the loop on axle-centric rigs.
/// </summary>
public static class CalibrationWorkflows
{
    /// <summary>
    /// Runs the Ackermann wizard and returns an aggregate residual.
    /// </summary>
    public static AckermannCalibrationResult RunAckermannWizard(IEnumerable<AckermannCalibrationSample> samples, double wheelbaseMeters, double trackWidthMeters)
    {
        if (samples is null)
        {
            throw new ArgumentNullException(nameof(samples));
        }

        if (wheelbaseMeters <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(wheelbaseMeters));
        }

        if (trackWidthMeters <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(trackWidthMeters));
        }

        var residuals = new List<double>();
        foreach (var sample in samples)
        {
            var expectedInner = Math.Atan(wheelbaseMeters / (sample.TurnRadiusMeters - trackWidthMeters / 2.0)) * (180.0 / Math.PI);
            var residual = sample.InnerWheelAngleDegrees - expectedInner;
            residuals.Add(residual);
        }

        if (residuals.Count == 0)
        {
            throw new InvalidOperationException("Ackermann wizard requires at least one sample.");
        }

        var rms = Math.Sqrt(residuals.Sum(r => r * r) / residuals.Count);
        var pass = rms <= 0.2;
        return new AckermannCalibrationResult(rms, pass);
    }

    /// <summary>
    /// Calculates the hitch zero offset from yaw samples.
    /// </summary>
    public static HitchZeroingResult CalculateHitchZero(IEnumerable<double> yawSamplesDegrees, double toleranceDegrees)
    {
        if (yawSamplesDegrees is null)
        {
            throw new ArgumentNullException(nameof(yawSamplesDegrees));
        }

        if (toleranceDegrees < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(toleranceDegrees));
        }

        var samples = yawSamplesDegrees.ToList();
        if (samples.Count == 0)
        {
            throw new InvalidOperationException("Hitch zeroing requires samples.");
        }

        var mean = samples.Average();
        var spread = samples.Max() - samples.Min();
        var pass = Math.Abs(mean) <= toleranceDegrees && spread <= toleranceDegrees * 2;
        return new HitchZeroingResult(mean, spread, pass);
    }

    /// <summary>
    /// Evaluates slip behaviour for sanity against the advertised limits.
    /// </summary>
    public static SlipSanityResult EvaluateSlip(IEnumerable<SlipSample> samples, AxleModeProfile mode)
    {
        if (samples is null)
        {
            throw new ArgumentNullException(nameof(samples));
        }

        if (mode is null)
        {
            throw new ArgumentNullException(nameof(mode));
        }

        var list = samples.ToList();
        if (list.Count == 0)
        {
            throw new InvalidOperationException("Slip evaluation requires samples.");
        }

        var averageSlip = list.Average(s => ComputeSlip(s));
        var derate = averageSlip <= mode.SlipLimit ? 0 : Math.Min(1.0, (averageSlip - mode.SlipLimit) / mode.SlipLimit);
        return new SlipSanityResult(averageSlip, derate);
    }

    /// <summary>
    /// Verifies transport lock state transitions.
    /// </summary>
    public static TransportLockResult ValidateTransportLock(IEnumerable<bool> commandedState, IEnumerable<bool> observedState)
    {
        if (commandedState is null)
        {
            throw new ArgumentNullException(nameof(commandedState));
        }

        if (observedState is null)
        {
            throw new ArgumentNullException(nameof(observedState));
        }

        var commanded = commandedState.ToList();
        var observed = observedState.ToList();
        if (commanded.Count != observed.Count)
        {
            throw new ArgumentException("Commanded and observed sequences must have the same length.");
        }

        var mismatches = 0;
        for (var i = 0; i < commanded.Count; i++)
        {
            if (commanded[i] != observed[i])
            {
                mismatches++;
            }
        }

        return new TransportLockResult(commanded.Count - mismatches, mismatches);
    }

    private static double ComputeSlip(SlipSample sample)
    {
        var denominator = Math.Max(0.01, Math.Abs(sample.LongitudinalVelocityMetersPerSecond));
        return Math.Abs(sample.LateralVelocityMetersPerSecond) / denominator;
    }
}

/// <summary>
/// Sample captured by the Ackermann wizard.
/// </summary>
public sealed record AckermannCalibrationSample(double InnerWheelAngleDegrees, double TurnRadiusMeters);

/// <summary>
/// Result produced by the Ackermann wizard.
/// </summary>
public sealed record AckermannCalibrationResult(double RmsResidualDegrees, bool Pass);

/// <summary>
/// Result of hitch zeroing workflow.
/// </summary>
public sealed record HitchZeroingResult(double MeanOffsetDegrees, double SpreadDegrees, bool Pass);

/// <summary>
/// Slip sample captured during validation runs.
/// </summary>
public sealed record SlipSample(double LateralVelocityMetersPerSecond, double LongitudinalVelocityMetersPerSecond);

/// <summary>
/// Slip sanity check outcome.
/// </summary>
public sealed record SlipSanityResult(double AverageSlip, double Derate);

/// <summary>
/// Transport lock validation outcome.
/// </summary>
public sealed record TransportLockResult(int MatchedSamples, int MismatchSamples);
