using System.Collections.Generic;
using System.Linq;
using Aog.Core.Jobs.Envelopes;
using Aog.Core.Legacy;
using Aog.Core.Paths;
using FluentAssertions;
using Xunit;

namespace Aog.Core.Tests.Legacy;

public sealed class LegacyMultiFieldEnvelopeTranslatorTests
{
    [Fact]
    public void Translate_MergesLegacyFields()
    {
        var translator = new LegacyMultiFieldEnvelopeTranslator();

        var origin = new GeographicCoordinate(41.0, -94.0);

        var fieldA = new LegacyFieldEnvelopeSource(
            "field:north",
            origin,
            new[]
            {
                CreateBoundary(new[]
                {
                    new PlanarPoint(0, 0),
                    new PlanarPoint(0, 100),
                    new PlanarPoint(100, 100),
                    new PlanarPoint(100, 0),
                })
            });

        var fieldB = new LegacyFieldEnvelopeSource(
            "field:south",
            origin,
            new[]
            {
                CreateBoundary(new[]
                {
                    new PlanarPoint(200, 0),
                    new PlanarPoint(200, 100),
                    new PlanarPoint(300, 100),
                    new PlanarPoint(300, 0),
                })
            });

        var result = translator.Translate(new[] { fieldA, fieldB });

        result.CrsEpsg.Should().Be(4326);
        result.AreaHectares.Should().BeApproximately(2.0, 0.05);

        result.Members.Should().HaveCount(2);
        result.Members.Should().OnlyContain(m => m.AreaHectares > 0.9 && m.AreaHectares < 1.1);

        var boundingBox = result.BoundingBox;
        boundingBox.MinLatitude.Should().BeApproximately(41.0, 1e-6);
        boundingBox.MaxLatitude.Should().BeApproximately(41.0008983, 1e-6);
        boundingBox.MinLongitude.Should().BeApproximately(-94.0, 1e-6);
        boundingBox.MaxLongitude.Should().BeApproximately(-93.9964291, 1e-6);

        result.Polygons.Should().HaveCount(2);
        result.Polygons.Should().OnlyContain(p => p.Exterior.Count >= 4);

        var centroid = result.Centroid;
        centroid.Latitude.Should().BeApproximately(41.0004491, 1e-6);
        centroid.Longitude.Should().BeApproximately(-93.9982146, 1e-6);
    }

    [Fact]
    public void Translate_PreservesHeadlandHoles()
    {
        var translator = new LegacyMultiFieldEnvelopeTranslator();
        var origin = new GeographicCoordinate(41.5, -93.75);

        var exterior = new[]
        {
            new PlanarPoint(0, 0),
            new PlanarPoint(0, 200),
            new PlanarPoint(200, 200),
            new PlanarPoint(200, 0),
        };

        var hole = new[]
        {
            new PlanarPoint(50, 50),
            new PlanarPoint(50, 150),
            new PlanarPoint(150, 150),
            new PlanarPoint(150, 50),
        };

        var boundary = CreateBoundary(exterior, new[] { hole });

        var field = new LegacyFieldEnvelopeSource("field:donut", origin, new[] { boundary });

        var result = translator.Translate(new[] { field });

        result.Polygons.Should().ContainSingle();
        var polygon = result.Polygons[0];
        polygon.Holes.Should().ContainSingle().Which.Should().HaveCount(5);
    }

    [Fact]
    public void Translate_ThrowsWhenBoundariesMissing()
    {
        var translator = new LegacyMultiFieldEnvelopeTranslator();
        var origin = new GeographicCoordinate(41.0, -94.0);

        var field = new LegacyFieldEnvelopeSource("field:empty", origin, new List<FieldBoundary>());

        var act = () => translator.Translate(new[] { field });

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*does not contain any boundaries*");
    }

    private static FieldBoundary CreateBoundary(IReadOnlyList<PlanarPoint> exterior, IReadOnlyList<IReadOnlyList<PlanarPoint>>? holes = null)
    {
        var exteriorVertices = CloseRing(exterior)
            .Select(point => new BoundaryVertex(point, 0))
            .ToList();

        IReadOnlyList<HeadlandRing> headlands = Array.Empty<HeadlandRing>();
        if (holes is not null)
        {
            headlands = holes
                .Select(h => new HeadlandRing(CloseRing(h).Select(point => new BoundaryVertex(point, 0)).ToList()))
                .ToList();
        }

        return new FieldBoundary(false, exteriorVertices, headlands);
    }

    private static IReadOnlyList<PlanarPoint> CloseRing(IReadOnlyList<PlanarPoint> vertices)
    {
        var points = new List<PlanarPoint>(vertices);
        if (points.Count == 0)
        {
            return points;
        }

        if (points[0] != points[^1])
        {
            points.Add(points[0]);
        }

        return points;
    }
}
