using System;
using Aog.Core.Machines.Axle;

namespace Aog.Core.Guidance.Axle;

/// <summary>
/// Bridges the axle-centric kinematic profile to automation consumers.
/// </summary>
public sealed class AxleAutomationIntegrator
{
    private readonly IAutomationModeSink _sink;

    /// <summary>
    /// Initializes a new instance of the <see cref="AxleAutomationIntegrator"/> class.
    /// </summary>
    /// <param name="sink">Sink that receives computed automation mode limits.</param>
    public AxleAutomationIntegrator(IAutomationModeSink sink)
    {
        _sink = sink ?? throw new ArgumentNullException(nameof(sink));
    }

    /// <summary>
    /// Applies the specified mode to the automation sink.
    /// </summary>
    /// <param name="profile">Kinematic profile describing available automation modes.</param>
    /// <param name="mode">Identifier of the mode to publish.</param>
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
    /// <summary>
    /// Publishes an updated snapshot of automation mode limits.
    /// </summary>
    /// <param name="snapshot">Snapshot describing the currently selected mode.</param>
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
