using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Aog.Core.Machines;

/// <summary>
/// Describes the aggregated machine configuration used by the Nexus runtime.
/// </summary>
public sealed class MachineProfile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MachineProfile"/> class.
    /// </summary>
    public MachineProfile(
        VehicleDimensions vehicle,
        ImplementDimensions implement,
        HydraulicLiftSettings hydraulics,
        SectionConfiguration sections)
    {
        Vehicle = vehicle ?? throw new ArgumentNullException(nameof(vehicle));
        Implement = implement ?? throw new ArgumentNullException(nameof(implement));
        Hydraulics = hydraulics ?? throw new ArgumentNullException(nameof(hydraulics));
        Sections = sections ?? throw new ArgumentNullException(nameof(sections));
    }

    /// <summary>
    /// Gets the vehicle geometry and motion constraints.
    /// </summary>
    public VehicleDimensions Vehicle { get; }

    /// <summary>
    /// Gets the implement geometry and offsets.
    /// </summary>
    public ImplementDimensions Implement { get; }

    /// <summary>
    /// Gets the hydraulic lift timing and look-ahead configuration.
    /// </summary>
    public HydraulicLiftSettings Hydraulics { get; }

    /// <summary>
    /// Gets the section layout metadata.
    /// </summary>
    public SectionConfiguration Sections { get; }
}

/// <summary>
/// Captures the physical geometry of the tractor or towing vehicle.
/// </summary>
public sealed class VehicleDimensions
{
    public VehicleDimensions(
        double wheelbaseMeters,
        double trackWidthMeters,
        double antennaHeightMeters,
        double antennaOffsetMeters,
        double antennaPivotMeters,
        double maxSteerAngleDegrees)
    {
        if (wheelbaseMeters <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(wheelbaseMeters));
        }

        if (trackWidthMeters <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(trackWidthMeters));
        }

        WheelbaseMeters = wheelbaseMeters;
        TrackWidthMeters = trackWidthMeters;
        AntennaHeightMeters = antennaHeightMeters;
        AntennaOffsetMeters = antennaOffsetMeters;
        AntennaPivotMeters = antennaPivotMeters;
        MaxSteerAngleDegrees = maxSteerAngleDegrees;
    }

    public double WheelbaseMeters { get; }

    public double TrackWidthMeters { get; }

    public double AntennaHeightMeters { get; }

    public double AntennaOffsetMeters { get; }

    public double AntennaPivotMeters { get; }

    public double MaxSteerAngleDegrees { get; }
}

/// <summary>
/// Captures the implement geometry used by guidance and coverage algorithms.
/// </summary>
public sealed class ImplementDimensions
{
    public ImplementDimensions(
        double widthMeters,
        double overlapMeters,
        double lateralOffsetMeters,
        double lookAheadOnMeters,
        double lookAheadOffMeters,
        double trailingHitchLengthMeters,
        double tankTrailingHitchLengthMeters,
        bool isTrailing,
        bool isRearFixed,
        bool isFrontMounted)
    {
        if (widthMeters <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(widthMeters));
        }

        if (overlapMeters < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(overlapMeters));
        }

        WidthMeters = widthMeters;
        OverlapMeters = overlapMeters;
        LateralOffsetMeters = lateralOffsetMeters;
        LookAheadOnMeters = lookAheadOnMeters;
        LookAheadOffMeters = lookAheadOffMeters;
        TrailingHitchLengthMeters = trailingHitchLengthMeters;
        TankTrailingHitchLengthMeters = tankTrailingHitchLengthMeters;
        IsTrailing = isTrailing;
        IsRearFixed = isRearFixed;
        IsFrontMounted = isFrontMounted;
    }

    public double WidthMeters { get; }

    public double OverlapMeters { get; }

    public double LateralOffsetMeters { get; }

    public double LookAheadOnMeters { get; }

    public double LookAheadOffMeters { get; }

    public double TrailingHitchLengthMeters { get; }

    public double TankTrailingHitchLengthMeters { get; }

    public bool IsTrailing { get; }

    public bool IsRearFixed { get; }

    public bool IsFrontMounted { get; }
}

/// <summary>
/// Stores the timing used to trigger hydraulic lift events around headlands.
/// </summary>
public sealed class HydraulicLiftSettings
{
    public HydraulicLiftSettings(bool isEnabled, double raiseTimeSeconds, double lowerTimeSeconds, double lookAheadMeters)
    {
        if (raiseTimeSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(raiseTimeSeconds));
        }

        if (lowerTimeSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(lowerTimeSeconds));
        }

        if (lookAheadMeters < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(lookAheadMeters));
        }

        IsEnabled = isEnabled;
        RaiseTimeSeconds = raiseTimeSeconds;
        LowerTimeSeconds = lowerTimeSeconds;
        LookAheadMeters = lookAheadMeters;
    }

    public bool IsEnabled { get; }

    public double RaiseTimeSeconds { get; }

    public double LowerTimeSeconds { get; }

    public double LookAheadMeters { get; }
}

/// <summary>
/// Describes the boom section layout and derived thresholds used by rate control.
/// </summary>
public sealed class SectionConfiguration
{
    public SectionConfiguration(
        int sectionCount,
        IReadOnlyList<double> sectionOffsetsMeters,
        double defaultSectionWidthMeters,
        double offDelaySeconds,
        int minimumCoveragePercent,
        bool usesMultiSectionZones)
    {
        if (sectionCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sectionCount));
        }

        SectionCount = sectionCount;
        SectionOffsetsMeters = new ReadOnlyCollection<double>(sectionOffsetsMeters?.ToArray() ?? Array.Empty<double>());
        DefaultSectionWidthMeters = defaultSectionWidthMeters;
        OffDelaySeconds = offDelaySeconds;
        MinimumCoveragePercent = minimumCoveragePercent;
        UsesMultiSectionZones = usesMultiSectionZones;
    }

    public int SectionCount { get; }

    public IReadOnlyList<double> SectionOffsetsMeters { get; }

    public double DefaultSectionWidthMeters { get; }

    public double OffDelaySeconds { get; }

    public int MinimumCoveragePercent { get; }

    public bool UsesMultiSectionZones { get; }
}
