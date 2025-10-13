using System.IO;
using Aog.Core.Legacy;
using FluentAssertions;
using Xunit;

namespace Aog.Tools.LegacyConfigTranslator.Tests;

public sealed class LegacySettingsLoaderTests
{
    [Fact]
    public void Load_ParsesMachineSettingsFromXml()
    {
        var loader = new LegacySettingsLoader();
        var path = Path.Combine("Data", "sample-vehicle.xml");

        var settings = loader.Load(path);

        settings.WheelbaseMeters.Should().Be(3.45);
        settings.TrackWidthMeters.Should().Be(2.05);
        settings.ToolWidthMeters.Should().Be(9.2);
        settings.ToolLookAheadOnMeters.Should().Be(1.5);
        settings.ToolLookAheadOffMeters.Should().Be(0.75);
        settings.ToolTrailingHitchLengthMeters.Should().Be(-2.8);
        settings.TankTrailingHitchLengthMeters.Should().Be(4.5);
        settings.IsToolTrailing.Should().BeTrue();
        settings.IsHydraulicEnabled.Should().BeTrue();
        settings.HydraulicRaiseTimeSeconds.Should().Be(3);
        settings.HydraulicLowerTimeSeconds.Should().Be(4);
        settings.HydraulicLookAheadMeters.Should().Be(2.25);
        settings.SectionCount.Should().Be(6);
        settings.DefaultSectionWidthMeters.Should().Be(1.8);
        settings.MinimumCoveragePercent.Should().Be(92);
        settings.UsesSectionZones.Should().BeTrue();
        settings.SectionPositions.Should().HaveCount(17);
        settings.SectionPositions[0].Should().Be(-3.2m);
        settings.SectionPositions[5].Should().Be(4.8m);
    }

    [Fact]
    public void Translate_ProducesMachineProfileAndTuning()
    {
        var loader = new LegacySettingsLoader();
        var settings = loader.Load(Path.Combine("Data", "sample-vehicle.xml"));

        var translator = new V6MachineProfileTranslator();
        var profile = translator.Translate(settings);

        profile.Vehicle.WheelbaseMeters.Should().Be(3.45);
        profile.Implement.WidthMeters.Should().Be(9.2);
        profile.Sections.SectionCount.Should().Be(6);
        profile.Sections.UsesMultiSectionZones.Should().BeTrue();

        var tuning = Aog.Plugins.AutoSteer.AutoSteerLiteTuningCalculator.BuildProfile(profile);
        tuning.MinimumLookAheadMeters.Should().BeGreaterThan(0);
    }
}
