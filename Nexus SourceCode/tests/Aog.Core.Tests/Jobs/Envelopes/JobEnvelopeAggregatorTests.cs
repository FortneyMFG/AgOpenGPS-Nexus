using System;
using System.Collections.Generic;
using System.Linq;
using Aog.Core.Jobs.Envelopes;
using FluentAssertions;
using Xunit;

namespace Aog.Core.Tests.Jobs.Envelopes;

public sealed class JobEnvelopeAggregatorTests
{
    [Fact]
    public void Aggregate_MergesDisjointSquares()
    {
        var aggregator = new JobEnvelopeAggregator();

        var fieldA = new JobEnvelopeFieldContribution(
            "field:north",
            new[] { CreateRectangle(0, 0, 100, 100) });

        var fieldB = new JobEnvelopeFieldContribution(
            "field:south",
            new[] { CreateRectangle(120, 0, 220, 100) });

        var result = aggregator.Aggregate(3857, new[] { fieldA, fieldB });

        result.CrsEpsg.Should().Be(3857);
        result.AreaHectares.Should().BeApproximately(2.0, 1e-6);
        result.Polygons.Should().HaveCount(2);

        result.Members.Should().HaveCount(2);
        result.Members.Select(m => m.FieldId).Should().Equal("field:north", "field:south");
        result.Members.Should().OnlyContain(m => Math.Abs(m.AreaHectares - 1.0) < 1e-6);

        result.BoundingBox.Should().BeEquivalentTo(new JobEnvelopeBoundingBox(0, 0, 220, 100));
        result.Centroid.Longitude.Should().BeApproximately(110, 1e-6);
        result.Centroid.Latitude.Should().BeApproximately(50, 1e-6);
    }

    [Fact]
    public void Aggregate_PreservesHoles()
    {
        var aggregator = new JobEnvelopeAggregator();

        var exterior = new[]
        {
            new JobEnvelopeCoordinate(0, 0),
            new JobEnvelopeCoordinate(0, 200),
            new JobEnvelopeCoordinate(200, 200),
            new JobEnvelopeCoordinate(200, 0),
            new JobEnvelopeCoordinate(0, 0)
        };

        var hole = new[]
        {
            new JobEnvelopeCoordinate(50, 50),
            new JobEnvelopeCoordinate(50, 150),
            new JobEnvelopeCoordinate(150, 150),
            new JobEnvelopeCoordinate(150, 50),
            new JobEnvelopeCoordinate(50, 50)
        };

        var field = new JobEnvelopeFieldContribution(
            "field:donut",
            new[] { new JobEnvelopePolygon(exterior, new[] { hole }) });

        var result = aggregator.Aggregate(3857, new[] { field });

        result.Polygons.Should().ContainSingle();
        var polygon = result.Polygons[0];
        polygon.Holes.Should().ContainSingle().Which.Should().HaveCount(5);
        result.AreaHectares.Should().BeApproximately((40000 - 10000) / 10_000d, 1e-6);
        result.Members.Single().AreaHectares.Should().BeApproximately((40000 - 10000) / 10_000d, 1e-6);
    }

    [Fact]
    public void Aggregate_ThrowsWhenFieldsMissing()
    {
        var aggregator = new JobEnvelopeAggregator();

        var act = () => aggregator.Aggregate(4326, Array.Empty<JobEnvelopeFieldContribution>());

        act.Should().Throw<ArgumentException>().WithMessage("*At least one field contribution*");
    }

    private static JobEnvelopePolygon CreateRectangle(double minX, double minY, double maxX, double maxY)
    {
        var exterior = new[]
        {
            new JobEnvelopeCoordinate(minX, minY),
            new JobEnvelopeCoordinate(minX, maxY),
            new JobEnvelopeCoordinate(maxX, maxY),
            new JobEnvelopeCoordinate(maxX, minY),
            new JobEnvelopeCoordinate(minX, minY)
        };

        return new JobEnvelopePolygon(exterior);
    }
}

