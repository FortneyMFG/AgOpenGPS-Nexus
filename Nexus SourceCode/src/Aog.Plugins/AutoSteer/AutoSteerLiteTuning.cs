using System;

namespace Aog.Plugins.AutoSteer;

/// <summary>
/// Describes the legacy-derived tuning profile used by <see cref="AutoSteerLiteController"/>
/// to compute dynamic look-ahead distances and startup ramp behaviour.
/// </summary>
public sealed class AutoSteerLiteTuningProfile
{
    /// <summary>
    /// Gets or sets the multiplier applied when the cross-track error is within the "hold" band.
    /// Mirrors the legacy <c>goalPointLookAheadHold</c> parameter (default 3.0).
    /// </summary>
    public double LookAheadHoldMultiplier { get; set; } = 3.0;

    /// <summary>
    /// Gets or sets the multiplier applied to vehicle speed before the hold/acquire blend is applied.
    /// Mirrors the legacy <c>goalPointLookAheadMult</c> parameter (default 1.5).
    /// </summary>
    public double SpeedMultiplier { get; set; } = 1.5;

    /// <summary>
    /// Gets or sets the factor applied to the hold multiplier while the controller is still
    /// acquiring the guidance line (default 0.9).
    /// </summary>
    public double AcquireFactor { get; set; } = 0.9;

    /// <summary>
    /// Gets or sets the minimum permitted look-ahead distance in metres (default 2.0).
    /// </summary>
    public double MinimumLookAheadMeters { get; set; } = 2.0;

    /// <summary>
    /// Gets or sets the cross-track error threshold (metres) for remaining fully in the
    /// hold regime (default 0.1 m).
    /// </summary>
    public double CrossTrackHoldThresholdMeters { get; set; } = 0.1;

    /// <summary>
    /// Gets or sets the cross-track error threshold (metres) for fully switching to the
    /// acquire regime (default 0.4 m). Values between the hold and acquire thresholds
    /// are linearly blended.
    /// </summary>
    public double CrossTrackAcquireThresholdMeters { get; set; } = 0.4;

    /// <summary>
    /// Gets or sets the exponential smoothing factor (0..1) used to filter cross-track
    /// error prior to computing the look-ahead multiplier (default 0.5).
    /// </summary>
    public double CrossTrackFilterGain { get; set; } = 0.5;

    /// <summary>
    /// Gets or sets the exponential smoothing factor (0..1) used to filter the raw
    /// look-ahead distance output (default 0.25).
    /// </summary>
    public double LookAheadFilterGain { get; set; } = 0.25;

    /// <summary>
    /// Gets or sets the distance (metres) travelled after enable before the controller
    /// transitions from the startup hold ramp to the steady-state blend (default 4 m).
    /// </summary>
    public double StartupHoldDistanceMeters { get; set; } = 4.0;

    /// <summary>
    /// Gets or sets the multiplier applied to the raw look-ahead distance while the startup
    /// hold ramp is active (default 0.65).
    /// </summary>
    public double StartupLookAheadMultiplier { get; set; } = 0.65;

    /// <summary>
    /// Gets or sets the multiplier applied when the vehicle is operating inside a headland
    /// constraint zone (default 0.75).
    /// </summary>
    public double HeadlandSlowdownMultiplier { get; set; } = 0.75;

    /// <summary>
    /// Gets or sets the multiplier applied when a blocking constraint (e.g. keep-out) is active
    /// but look-ahead computation still proceeds (default 0.5).
    /// </summary>
    public double ConstraintSlowdownMultiplier { get; set; } = 0.5;

    /// <summary>
    /// Gets or sets the clearance margin maintained when clamping look-ahead against a nearby
    /// constraint distance (default 1.0 m).
    /// </summary>
    public double ConstraintDistanceMarginMeters { get; set; } = 1.0;

    /// <summary>
    /// Returns a copy of the default profile derived from the V6 controller settings.
    /// </summary>
    public static AutoSteerLiteTuningProfile Default => new();

    internal void Validate()
    {
        if (LookAheadHoldMultiplier <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(LookAheadHoldMultiplier), LookAheadHoldMultiplier, "Hold multiplier must be positive.");
        }

        if (SpeedMultiplier <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(SpeedMultiplier), SpeedMultiplier, "Speed multiplier must be positive.");
        }

        if (AcquireFactor <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(AcquireFactor), AcquireFactor, "Acquire factor must be positive.");
        }

        if (MinimumLookAheadMeters <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MinimumLookAheadMeters), MinimumLookAheadMeters, "Minimum look-ahead must be positive.");
        }

        if (CrossTrackHoldThresholdMeters < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(CrossTrackHoldThresholdMeters), CrossTrackHoldThresholdMeters, "Hold threshold must be non-negative.");
        }

        if (CrossTrackAcquireThresholdMeters <= CrossTrackHoldThresholdMeters)
        {
            throw new ArgumentOutOfRangeException(nameof(CrossTrackAcquireThresholdMeters), CrossTrackAcquireThresholdMeters, "Acquire threshold must exceed hold threshold.");
        }

        if (CrossTrackFilterGain is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(CrossTrackFilterGain), CrossTrackFilterGain, "Cross-track filter gain must be in [0, 1].");
        }

        if (LookAheadFilterGain is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(LookAheadFilterGain), LookAheadFilterGain, "Look-ahead filter gain must be in [0, 1].");
        }

        if (StartupHoldDistanceMeters < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(StartupHoldDistanceMeters), StartupHoldDistanceMeters, "Startup hold distance must be non-negative.");
        }

        if (StartupLookAheadMultiplier <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(StartupLookAheadMultiplier), StartupLookAheadMultiplier, "Startup multiplier must be positive.");
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
}

/// <summary>
/// Tracks the mutable state used when applying <see cref="AutoSteerLiteTuningProfile"/> logic.
/// </summary>
public sealed class AutoSteerLiteTuningState
{
    private readonly AutoSteerLiteTuningProfile _profile;
    private double _smoothedCrossTrack;
    private double _filteredLookAhead = double.NaN;
    private double _startupDistanceRemaining;

    public AutoSteerLiteTuningState(AutoSteerLiteTuningProfile profile)
    {
        _profile = profile ?? throw new ArgumentNullException(nameof(profile));
        _profile.Validate();
        _startupDistanceRemaining = _profile.StartupHoldDistanceMeters;
    }

    /// <summary>
    /// Gets a value indicating whether the startup hold ramp is currently active.
    /// </summary>
    public bool IsInStartup => _startupDistanceRemaining > 0;

    /// <summary>
    /// Resets all internal state to the startup condition.
    /// </summary>
    public void Reset()
    {
        _smoothedCrossTrack = 0;
        _filteredLookAhead = double.NaN;
        _startupDistanceRemaining = _profile.StartupHoldDistanceMeters;
    }

    /// <summary>
    /// Updates the tuning state and returns the filtered look-ahead distance for the next control cycle.
    /// </summary>
    /// <param name="crossTrackErrorMeters">Signed cross-track error (metres) at the controller pivot axle.</param>
    /// <param name="speedMetersPerSecond">Current vehicle speed (m/s).</param>
    /// <param name="distanceTravelledMeters">Distance travelled since the previous update (metres).</param>
    public double Update(
        double crossTrackErrorMeters,
        double speedMetersPerSecond,
        double distanceTravelledMeters,
        ConstraintLookAheadContext? constraintContext = null)
    {
        if (distanceTravelledMeters < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(distanceTravelledMeters), distanceTravelledMeters, "Distance travelled cannot be negative.");
        }

        if (distanceTravelledMeters > 0 && _startupDistanceRemaining > 0)
        {
            _startupDistanceRemaining = Math.Max(0, _startupDistanceRemaining - distanceTravelledMeters);
        }

        var filter = Math.Clamp(_profile.CrossTrackFilterGain, 0, 1);
        _smoothedCrossTrack = filter * _smoothedCrossTrack + (1 - filter) * crossTrackErrorMeters;

        var absoluteCrossTrack = Math.Abs(_smoothedCrossTrack);

        var holdMultiplier = _profile.LookAheadHoldMultiplier;
        var acquireMultiplier = holdMultiplier * _profile.AcquireFactor;

        double blendedMultiplier;
        if (absoluteCrossTrack <= _profile.CrossTrackHoldThresholdMeters)
        {
            blendedMultiplier = holdMultiplier;
        }
        else if (absoluteCrossTrack >= _profile.CrossTrackAcquireThresholdMeters)
        {
            blendedMultiplier = acquireMultiplier;
        }
        else
        {
            var span = _profile.CrossTrackAcquireThresholdMeters - _profile.CrossTrackHoldThresholdMeters;
            var t = (absoluteCrossTrack - _profile.CrossTrackHoldThresholdMeters) / span;
            blendedMultiplier = holdMultiplier + (acquireMultiplier - holdMultiplier) * t;
        }

        var baseDistance = Math.Max(speedMetersPerSecond, 0) * 0.05 * _profile.SpeedMultiplier;
        var rawLookAhead = baseDistance * blendedMultiplier + blendedMultiplier;

        if (IsInStartup)
        {
            rawLookAhead *= _profile.StartupLookAheadMultiplier;
        }

        rawLookAhead = ApplyConstraintModifiers(rawLookAhead, constraintContext);

        var lookAheadFilter = Math.Clamp(_profile.LookAheadFilterGain, 0, 1);
        if (double.IsNaN(_filteredLookAhead))
        {
            _filteredLookAhead = rawLookAhead;
        }
        else
        {
            _filteredLookAhead = lookAheadFilter * _filteredLookAhead + (1 - lookAheadFilter) * rawLookAhead;
        }

        return _filteredLookAhead;
    }

    private double ApplyConstraintModifiers(double rawLookAhead, ConstraintLookAheadContext? constraintContext)
    {
        var lookAhead = Math.Max(rawLookAhead, _profile.MinimumLookAheadMeters);
        if (constraintContext is null)
        {
            return lookAhead;
        }

        var context = constraintContext.Value;

        if (context.DistanceToConstraintMeters is { } distance)
        {
            var clearance = Math.Max(0, distance - _profile.ConstraintDistanceMarginMeters);
            lookAhead = Math.Min(lookAhead, Math.Max(_profile.MinimumLookAheadMeters, clearance));
        }

        if (context.InsideHeadland)
        {
            lookAhead = Math.Max(_profile.MinimumLookAheadMeters, lookAhead * _profile.HeadlandSlowdownMultiplier);
        }

        if (context.HasBlockingConstraint)
        {
            lookAhead = Math.Max(_profile.MinimumLookAheadMeters, lookAhead * _profile.ConstraintSlowdownMultiplier);
        }

        return lookAhead;
    }
}

/// <summary>
/// Describes the constraint context used to bias dynamic look-ahead calculations.
/// </summary>
public readonly record struct ConstraintLookAheadContext(
    bool HasBlockingConstraint,
    bool InsideHeadland,
    double? DistanceToConstraintMeters);

