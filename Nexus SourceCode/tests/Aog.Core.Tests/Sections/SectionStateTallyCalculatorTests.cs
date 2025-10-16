using System;
using Aog.Core.Sections;
using FluentAssertions;
using Xunit;

namespace Aog.Core.Tests.Sections;

public sealed class SectionStateTallyCalculatorTests
{
    [Fact]
    public void Apply_ShouldAccumulateDurationsCountsAndAreas()
    {
        var calculator = new SectionStateTallyCalculator();
        var baseTimestamp = new DateTimeOffset(2024, 3, 1, 12, 0, 0, TimeSpan.Zero);

        calculator.Apply(
            "section:planter:01",
            new SectionStateSample(
                baseTimestamp,
                SectionCommandState.Off,
                SectionCommandState.Off,
                SectionCommandSource.Operator,
                ManualOverride: false,
                WorkOpportunity: false,
                WorkEvent: false));

        var second = calculator.Apply(
            "section:planter:01",
            new SectionStateSample(
                baseTimestamp + TimeSpan.FromSeconds(1),
                SectionCommandState.On,
                SectionCommandState.On,
                SectionCommandSource.Automation,
                ManualOverride: false,
                WorkOpportunity: true,
                WorkEvent: true,
                OpportunityAreaSqMeters: 2.0,
                AppliedAreaSqMeters: 1.5));

        var third = calculator.Apply(
            "section:planter:01",
            new SectionStateSample(
                baseTimestamp + TimeSpan.FromSeconds(3),
                SectionCommandState.On,
                SectionCommandState.On,
                SectionCommandSource.Automation,
                ManualOverride: false,
                WorkOpportunity: true,
                WorkEvent: false,
                OpportunityAreaSqMeters: 2.0,
                AppliedAreaSqMeters: 2.0));

        second.CommandCount.Should().Be(1);
        second.EventCount.Should().Be(1);

        third.CommandCount.Should().Be(1);
        third.EventCount.Should().Be(1);
        third.OpportunityDuration.Should().Be(TimeSpan.FromSeconds(2));
        third.ActiveDuration.Should().Be(TimeSpan.FromSeconds(2));
        third.AppliedAreaSqMeters.Should().BeApproximately(3.5, 1e-6);
        third.MissedAreaSqMeters.Should().BeApproximately(0.5, 1e-6);
    }

    [Fact]
    public void Apply_ShouldThrowWhenTimestampRegresses()
    {
        var calculator = new SectionStateTallyCalculator();
        var timestamp = DateTimeOffset.UtcNow;

        calculator.Apply(
            "section:alpha",
            new SectionStateSample(
                timestamp,
                SectionCommandState.Off,
                SectionCommandState.Off,
                SectionCommandSource.Operator,
                ManualOverride: false,
                WorkOpportunity: false,
                WorkEvent: false));

        var act = () => calculator.Apply(
            "section:alpha",
            new SectionStateSample(
                timestamp - TimeSpan.FromMilliseconds(1),
                SectionCommandState.On,
                SectionCommandState.On,
                SectionCommandSource.Automation,
                ManualOverride: false,
                WorkOpportunity: true,
                WorkEvent: true));

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*chronological order*");
    }

    [Fact]
    public void GetSnapshots_ShouldReturnLatestStatePerSection()
    {
        var calculator = new SectionStateTallyCalculator();
        var timestamp = DateTimeOffset.UtcNow;

        calculator.Apply(
            "section:a",
            new SectionStateSample(
                timestamp,
                SectionCommandState.Armed,
                SectionCommandState.Off,
                SectionCommandSource.Automation,
                ManualOverride: false,
                WorkOpportunity: true,
                WorkEvent: false));

        calculator.Apply(
            "section:b",
            new SectionStateSample(
                timestamp + TimeSpan.FromSeconds(1),
                SectionCommandState.On,
                SectionCommandState.On,
                SectionCommandSource.Automation,
                ManualOverride: true,
                WorkOpportunity: true,
                WorkEvent: true,
                AppliedAreaSqMeters: 1.2));

        var snapshots = calculator.GetSnapshots();

        snapshots.Should().HaveCount(2);
        snapshots.Should().ContainSingle(snapshot => snapshot.SectionId == "section:a" && snapshot.ManualOverride == false);
        snapshots.Should().ContainSingle(snapshot => snapshot.SectionId == "section:b" && snapshot.ManualOverride);
    }
}

