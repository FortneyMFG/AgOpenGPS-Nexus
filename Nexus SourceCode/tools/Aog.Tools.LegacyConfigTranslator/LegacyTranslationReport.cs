using System;
using System.Collections.Generic;
using Aog.Core.Machines;
using Aog.Core.Legacy;
using Aog.Plugins.AutoSteer;

namespace Aog.Tools.LegacyConfigTranslator;

/// <summary>
/// Represents the JSON payload emitted by the translation CLI.
/// </summary>
public sealed record LegacyTranslationReport(
    string Source,
    DateTimeOffset GeneratedAtUtc,
    MachineProfileDto Machine,
    AutoSteerLiteTuningProfileDto AutoSteer,
    SectionLayoutDto Sections)
{
    public static LegacyTranslationReport Create(
        string sourcePath,
        MachineProfile machine,
        AutoSteerLiteTuningProfile tuning,
        int legacySectionCount)
    {
        var machineDto = MachineProfileDto.From(machine);
        var tuningDto = AutoSteerLiteTuningProfileDto.From(tuning);
        var sections = SectionLayoutDto.From(machine.Sections, legacySectionCount);

        return new LegacyTranslationReport(
            Source: sourcePath,
            GeneratedAtUtc: DateTimeOffset.UtcNow,
            Machine: machineDto,
            AutoSteer: tuningDto,
            Sections: sections);
    }
}

public sealed record MachineProfileDto(
    VehicleDimensionsDto Vehicle,
    ImplementDimensionsDto Implement,
    HydraulicLiftSettingsDto Hydraulics,
    SectionConfigurationDto Sections)
{
    public static MachineProfileDto From(MachineProfile profile)
    {
        return new MachineProfileDto(
            VehicleDimensionsDto.From(profile.Vehicle),
            ImplementDimensionsDto.From(profile.Implement),
            HydraulicLiftSettingsDto.From(profile.Hydraulics),
            SectionConfigurationDto.From(profile.Sections));
    }
}

public sealed record VehicleDimensionsDto(
    double WheelbaseMeters,
    double TrackWidthMeters,
    double AntennaHeightMeters,
    double AntennaOffsetMeters,
    double AntennaPivotMeters,
    double MaxSteerAngleDegrees)
{
    public static VehicleDimensionsDto From(VehicleDimensions dimensions)
    {
        return new VehicleDimensionsDto(
            dimensions.WheelbaseMeters,
            dimensions.TrackWidthMeters,
            dimensions.AntennaHeightMeters,
            dimensions.AntennaOffsetMeters,
            dimensions.AntennaPivotMeters,
            dimensions.MaxSteerAngleDegrees);
    }
}

public sealed record ImplementDimensionsDto(
    double WidthMeters,
    double OverlapMeters,
    double LateralOffsetMeters,
    double LookAheadOnMeters,
    double LookAheadOffMeters,
    double TrailingHitchLengthMeters,
    double TankTrailingHitchLengthMeters,
    bool IsTrailing,
    bool IsRearFixed,
    bool IsFrontMounted)
{
    public static ImplementDimensionsDto From(ImplementDimensions implement)
    {
        return new ImplementDimensionsDto(
            implement.WidthMeters,
            implement.OverlapMeters,
            implement.LateralOffsetMeters,
            implement.LookAheadOnMeters,
            implement.LookAheadOffMeters,
            implement.TrailingHitchLengthMeters,
            implement.TankTrailingHitchLengthMeters,
            implement.IsTrailing,
            implement.IsRearFixed,
            implement.IsFrontMounted);
    }
}

public sealed record HydraulicLiftSettingsDto(
    bool IsEnabled,
    double RaiseTimeSeconds,
    double LowerTimeSeconds,
    double LookAheadMeters)
{
    public static HydraulicLiftSettingsDto From(HydraulicLiftSettings hydraulics)
    {
        return new HydraulicLiftSettingsDto(
            hydraulics.IsEnabled,
            hydraulics.RaiseTimeSeconds,
            hydraulics.LowerTimeSeconds,
            hydraulics.LookAheadMeters);
    }
}

public sealed record SectionConfigurationDto(
    int SectionCount,
    IReadOnlyList<double> SectionOffsetsMeters,
    double DefaultSectionWidthMeters,
    double OffDelaySeconds,
    int MinimumCoveragePercent,
    bool UsesMultiSectionZones)
{
    public static SectionConfigurationDto From(SectionConfiguration sections)
    {
        return new SectionConfigurationDto(
            sections.SectionCount,
            sections.SectionOffsetsMeters,
            sections.DefaultSectionWidthMeters,
            sections.OffDelaySeconds,
            sections.MinimumCoveragePercent,
            sections.UsesMultiSectionZones);
    }
}

public sealed record SectionLayoutDto(
    int LegacySectionCount,
    int NexusSectionCount,
    bool UsesZones,
    int MinimumCoveragePercent,
    double DefaultWidthMeters)
{
    public static SectionLayoutDto From(SectionConfiguration configuration, int legacySectionCount)
    {
        return new SectionLayoutDto(
            LegacySectionCount: legacySectionCount,
            NexusSectionCount: configuration.SectionCount,
            UsesZones: configuration.UsesMultiSectionZones,
            MinimumCoveragePercent: configuration.MinimumCoveragePercent,
            DefaultWidthMeters: configuration.DefaultSectionWidthMeters);
    }
}

public sealed record AutoSteerLiteTuningProfileDto(
    double LookAheadHoldMultiplier,
    double SpeedMultiplier,
    double AcquireFactor,
    double MinimumLookAheadMeters,
    double CrossTrackHoldThresholdMeters,
    double CrossTrackAcquireThresholdMeters,
    double CrossTrackFilterGain,
    double LookAheadFilterGain,
    double StartupHoldDistanceMeters,
    double StartupLookAheadMultiplier)
{
    public static AutoSteerLiteTuningProfileDto From(AutoSteerLiteTuningProfile profile)
    {
        return new AutoSteerLiteTuningProfileDto(
            profile.LookAheadHoldMultiplier,
            profile.SpeedMultiplier,
            profile.AcquireFactor,
            profile.MinimumLookAheadMeters,
            profile.CrossTrackHoldThresholdMeters,
            profile.CrossTrackAcquireThresholdMeters,
            profile.CrossTrackFilterGain,
            profile.LookAheadFilterGain,
            profile.StartupHoldDistanceMeters,
            profile.StartupLookAheadMultiplier);
    }
}
