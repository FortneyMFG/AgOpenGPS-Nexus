using Aog.Core.Legacy;
using Aog.Core.Machines;
using Aog.Plugins.AutoSteer;
using FluentAssertions;
using Xunit;

namespace Aog.Plugins.Tests.AutoSteer;

public sealed class AutoSteerLiteTuningCalculatorTests
{
    [Fact]
    public void BuildProfile_DefaultMachineMatchesLegacyBaselines()
    {
        var translator = new V6MachineProfileTranslator();
        var settings = new LegacyMachineSettings
        {
            WheelbaseMeters = 3.3,
            TrackWidthMeters = 1.9,
            AntennaHeightMeters = 3,
            AntennaOffsetMeters = 0,
            AntennaPivotMeters = 0.1,
            MaxSteerAngleDegrees = 30,
            ToolWidthMeters = 4.0,
            ToolOverlapMeters = 0,
            ToolOffsetMeters = 0,
            ToolLookAheadOnMeters = 1.0,
            ToolLookAheadOffMeters = 0.5,
            ToolTrailingHitchLengthMeters = -2.5,
            TankTrailingHitchLengthMeters = 3.0,
            IsToolTrailing = true,
            SectionCount = 3,
        };

        var profile = translator.Translate(settings);
        var tuning = AutoSteerLiteTuningCalculator.BuildProfile(profile);

        tuning.LookAheadHoldMultiplier.Should().BeApproximately(3.0, 0.2);
        tuning.SpeedMultiplier.Should().BeApproximately(1.5, 0.1);
        tuning.AcquireFactor.Should().BeApproximately(0.9, 0.05);
        tuning.MinimumLookAheadMeters.Should().BeGreaterThanOrEqualTo(2.0);
        tuning.HeadlandSlowdownMultiplier.Should().BeGreaterThan(0.6);
        tuning.ConstraintSlowdownMultiplier.Should().BeApproximately(0.4, 1e-6);
        tuning.ConstraintDistanceMarginMeters.Should().BeGreaterThan(0.5);
    }

    [Fact]
    public void BuildProfile_WiderImplementRaisesHoldMultiplier()
    {
        var machine = new MachineProfile(
            new VehicleDimensions(3.0, 2.0, 3.0, 0, 0.2, 30),
            new ImplementDimensions(10.0, 0.5, 0, 1.2, 0.6, -3.0, 4.0, true, false, false),
            new HydraulicLiftSettings(false, 0, 0, 0),
            new SectionConfiguration(1, new[] { 0.0 }, 10.0, 0, 100, false));

        var tuning = AutoSteerLiteTuningCalculator.BuildProfile(machine);

        tuning.LookAheadHoldMultiplier.Should().BeGreaterThan(3.5);
        tuning.MinimumLookAheadMeters.Should().BeGreaterThan(2.0);
        tuning.HeadlandSlowdownMultiplier.Should().BeGreaterThan(0.7);
        tuning.ConstraintDistanceMarginMeters.Should().BeGreaterThan(1.0);
    }
}
