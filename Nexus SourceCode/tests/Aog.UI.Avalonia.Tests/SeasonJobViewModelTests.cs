using System;
using Aog.UI.Avalonia.ViewModels;
using FluentAssertions;
using Xunit;

namespace Aog.UI.Avalonia.Tests;

public sealed class SeasonJobViewModelTests
{
    [Fact]
    public void Constructor_WithMultipleFields_ComputesSummaries()
    {
        var fields = new[] { "North 40", "Driveway West", "Pasture Strip" };
        var job = new SeasonJobViewModel(
            id: "job:test",
            name: "Field operations",
            farmName: "South Farm",
            fieldNames: fields,
            operationType: "Application",
            status: "In progress",
            scheduledFor: DateTimeOffset.UtcNow,
            lastWorkedAt: null,
            notes: "",
            isActive: true);

        job.FieldNames.Should().BeEquivalentTo(fields);
        job.FieldCountDisplay.Should().Be("3 fields");
        job.FieldSummary.Should().Be("North 40, Driveway West + 1 more");
        job.Subtitle.Should().Be("South Farm · North 40 + 2 more");
        job.MatchesSearch("Driveway").Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithSingleField_HasExpectedDisplays()
    {
        var job = new SeasonJobViewModel(
            id: "job:single",
            name: "Scouting",
            farmName: "Prairie Ridge",
            fieldNames: new[] { "Ridge Block" },
            operationType: "Scouting",
            status: "Scheduled",
            scheduledFor: null,
            lastWorkedAt: null,
            notes: null,
            isActive: false);

        job.FieldCountDisplay.Should().Be("1 field");
        job.FieldSummary.Should().Be("Ridge Block");
        job.Subtitle.Should().Be("Prairie Ridge · Ridge Block");
        job.PrimaryFieldName.Should().Be("Ridge Block");
    }
}
