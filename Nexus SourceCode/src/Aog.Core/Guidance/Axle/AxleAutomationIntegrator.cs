using System;
using Aog.Core.Machines.Axle;

namespace Aog.Core.Guidance.Axle;

/// <summary>
/// Bridges the axle-centric kinematic profile to automation consumers.
/// </summary>
public sealed class AxleAutomationIntegrator
{
    private readonly IAutomationModeSink _sink;

    public AxleAutomationIntegrator(IAutomationModeSink sink)
    {
        _sink = sink ?? throw new ArgumentNullException(nameof(sink));
    }

    /// <summary>
    /// Applies the specified mode to the automation sink.
    /// </summary>
    public void ApplyMode(AxleCentricProfile profile, string mode)
    {
        if (profile is null)
        {
            throw new ArgumentNullException(nameof(profile));
        }

        if (string.IsNullOrWhiteSpace(mode))
        {
            throw new ArgumentException("Mode is required.", nameof(mode));
        }

        var modeProfile = profile.GetMode(mode);
        var turnRadius = modeProfile.GetMinimumTurnRadiusMeters();
        var slipLimit = modeProfile.SlipLimit;

        _sink.PublishModeLimits(new AutomationModeSnapshot(
            modeProfile.Id,
            modeProfile.CurvatureLimit,
            turnRadius,
            slipLimit,
            modeProfile.DriveDirectionPolicy,
            profile.DeterministicSeed,
            profile.ContentHash));
    }
}

/// <summary>
/// Observer that consumes computed automation mode limits.
/// </summary>
public interface IAutomationModeSink
{
    void PublishModeLimits(AutomationModeSnapshot snapshot);
}

/// <summary>
/// Snapshot containing the limits consumed by planners and controllers.
/// </summary>
public sealed record AutomationModeSnapshot(
    string ModeId,
    double CurvatureLimit,
    double MinimumTurnRadiusMeters,
    double SlipLimit,
    DriveDirectionPolicy DriveDirectionPolicy,
    int DeterministicSeed,
    string ProfileHash);
