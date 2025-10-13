using System;
using System.Collections.Generic;
using System.Globalization;
using Aog.Core.Machines;

namespace Aog.Core.Legacy;

/// <summary>
/// Translates legacy V6 settings into the Nexus machine profile model.
/// </summary>
public sealed class V6MachineProfileTranslator
{
    /// <summary>
    /// Converts the supplied legacy settings into a <see cref="MachineProfile"/>.
    /// </summary>
    public MachineProfile Translate(LegacyMachineSettings settings)
    {
        if (settings is null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        var vehicle = new VehicleDimensions(
            settings.WheelbaseMeters,
            settings.TrackWidthMeters,
            settings.AntennaHeightMeters,
            settings.AntennaOffsetMeters,
            settings.AntennaPivotMeters,
            settings.MaxSteerAngleDegrees);

        var implement = new ImplementDimensions(
            settings.ToolWidthMeters,
            Math.Max(0, settings.ToolOverlapMeters),
            settings.ToolOffsetMeters,
            settings.ToolLookAheadOnMeters,
            settings.ToolLookAheadOffMeters,
            settings.ToolTrailingHitchLengthMeters,
            settings.TankTrailingHitchLengthMeters,
            settings.IsToolTrailing,
            settings.IsToolRearFixed,
            settings.IsToolFront);

        var hydraulics = new HydraulicLiftSettings(
            settings.IsHydraulicEnabled,
            Math.Max(0, settings.HydraulicRaiseTimeSeconds),
            Math.Max(0, settings.HydraulicLowerTimeSeconds),
            Math.Max(0, settings.HydraulicLookAheadMeters));

        var sectionOffsets = ExtractSectionOffsets(settings.SectionCount, settings.SectionPositions);
        var sections = new SectionConfiguration(
            settings.SectionCount,
            sectionOffsets,
            settings.DefaultSectionWidthMeters,
            settings.SectionOffDelaySeconds,
            settings.MinimumCoveragePercent,
            settings.UsesSectionZones);

        return new MachineProfile(vehicle, implement, hydraulics, sections);
    }

    private static IReadOnlyList<double> ExtractSectionOffsets(int sectionCount, IReadOnlyList<decimal> positions)
    {
        if (sectionCount <= 0)
        {
            return Array.Empty<double>();
        }

        var offsets = new double[sectionCount];
        for (var i = 0; i < sectionCount; i++)
        {
            var value = i < positions.Count ? positions[i] : 0m;
            offsets[i] = Convert.ToDouble(value, CultureInfo.InvariantCulture);
        }

        return offsets;
    }
}

/// <summary>
/// Captures the subset of legacy settings required to translate machine profiles.
/// </summary>
public sealed class LegacyMachineSettings
{
    public double WheelbaseMeters { get; init; }

    public double TrackWidthMeters { get; init; }

    public double AntennaHeightMeters { get; init; }

    public double AntennaOffsetMeters { get; init; }

    public double AntennaPivotMeters { get; init; }

    public double MaxSteerAngleDegrees { get; init; }

    public double ToolWidthMeters { get; init; }

    public double ToolOverlapMeters { get; init; }

    public double ToolOffsetMeters { get; init; }

    public double ToolLookAheadOnMeters { get; init; }

    public double ToolLookAheadOffMeters { get; init; }

    public double ToolTrailingHitchLengthMeters { get; init; }

    public double TankTrailingHitchLengthMeters { get; init; }

    public bool IsToolTrailing { get; init; }

    public bool IsToolRearFixed { get; init; }

    public bool IsToolFront { get; init; }

    public bool IsHydraulicEnabled { get; init; }

    public double HydraulicRaiseTimeSeconds { get; init; }

    public double HydraulicLowerTimeSeconds { get; init; }

    public double HydraulicLookAheadMeters { get; init; }

    public int SectionCount { get; init; }

    public IReadOnlyList<decimal> SectionPositions { get; init; } = Array.Empty<decimal>();

    public double DefaultSectionWidthMeters { get; init; }

    public double SectionOffDelaySeconds { get; init; }

    public int MinimumCoveragePercent { get; init; }

    public bool UsesSectionZones { get; init; }
}
