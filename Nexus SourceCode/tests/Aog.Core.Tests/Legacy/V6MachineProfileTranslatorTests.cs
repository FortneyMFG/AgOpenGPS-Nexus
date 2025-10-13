using Aog.Core.Legacy;
using FluentAssertions;
using Xunit;

namespace Aog.Core.Tests.Legacy;

public sealed class V6MachineProfileTranslatorTests
{
    [Fact]
    public void Translate_MapsGeometryAndHydraulics()
    {
        var translator = new V6MachineProfileTranslator();
        var settings = new LegacyMachineSettings
        {
            WheelbaseMeters = 3.3,
            TrackWidthMeters = 1.9,
            AntennaHeightMeters = 3.0,
            AntennaOffsetMeters = 0.0,
            AntennaPivotMeters = 0.1,
            MaxSteerAngleDegrees = 30,
            ToolWidthMeters = 4.0,
            ToolOverlapMeters = 0.2,
            ToolOffsetMeters = 0.1,
            ToolLookAheadOnMeters = 1.0,
            ToolLookAheadOffMeters = 0.5,
            ToolTrailingHitchLengthMeters = -2.5,
            TankTrailingHitchLengthMeters = 3.0,
            IsToolTrailing = true,
            IsToolRearFixed = false,
            IsToolFront = false,
            IsHydraulicEnabled = true,
            HydraulicRaiseTimeSeconds = 3,
            HydraulicLowerTimeSeconds = 4,
            HydraulicLookAheadMeters = 2,
            SectionCount = 3,
            SectionPositions = new[] { -2m, -1m, 1m },
            DefaultSectionWidthMeters = 2.0,
            SectionOffDelaySeconds = 0.5,
            MinimumCoveragePercent = 95,
            UsesSectionZones = true,
        };

        var profile = translator.Translate(settings);

        profile.Vehicle.WheelbaseMeters.Should().Be(3.3);
        profile.Vehicle.TrackWidthMeters.Should().Be(1.9);
        profile.Vehicle.MaxSteerAngleDegrees.Should().Be(30);

        profile.Implement.WidthMeters.Should().Be(4.0);
        profile.Implement.OverlapMeters.Should().Be(0.2);
        profile.Implement.IsTrailing.Should().BeTrue();

        profile.Hydraulics.IsEnabled.Should().BeTrue();
        profile.Hydraulics.RaiseTimeSeconds.Should().Be(3);
        profile.Hydraulics.LookAheadMeters.Should().Be(2);

        profile.Sections.SectionCount.Should().Be(3);
        profile.Sections.SectionOffsetsMeters.Should().BeEquivalentTo(new[] { -2.0, -1.0, 1.0 });
        profile.Sections.UsesMultiSectionZones.Should().BeTrue();
    }

    [Fact]
    public void Translate_TruncatesSectionPositions()
    {
        var translator = new V6MachineProfileTranslator();
        var settings = new LegacyMachineSettings
        {
            WheelbaseMeters = 3,
            TrackWidthMeters = 2,
            ToolWidthMeters = 5,
            SectionCount = 2,
            SectionPositions = new[] { -1m, 0m, 1m },
        };

        var profile = translator.Translate(settings);

        profile.Sections.SectionOffsetsMeters.Should().BeEquivalentTo(new[] { -1.0, 0.0 });
    }
}
