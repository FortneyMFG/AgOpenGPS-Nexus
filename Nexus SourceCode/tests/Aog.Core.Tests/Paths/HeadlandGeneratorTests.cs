using System;
using System.Collections.Generic;
using Aog.Core.Paths;
using Xunit;

namespace Aog.Core.Tests.Paths;

public sealed class HeadlandGeneratorTests
{
    [Fact]
    public void GenerateHeadlands_ProducesInsetSquares()
    {
        var boundary = new List<PlanarPoint>
        {
            new(0, 0),
            new(20, 0),
            new(20, 20),
            new(0, 20)
        };

        var rings = HeadlandGenerator.GenerateHeadlands(boundary, headlandWidth: 3, passes: 2);

        Assert.Equal(2, rings.Count);
        Assert.Equal(new PlanarPoint(3, 3), rings[0][0]);
        Assert.Equal(new PlanarPoint(17, 3), rings[0][1]);
        Assert.Equal(new PlanarPoint(17, 17), rings[0][2]);
        Assert.Equal(new PlanarPoint(3, 17), rings[0][3]);

        Assert.Equal(new PlanarPoint(6, 6), rings[1][0]);
        Assert.Equal(new PlanarPoint(14, 6), rings[1][1]);
        Assert.Equal(new PlanarPoint(14, 14), rings[1][2]);
        Assert.Equal(new PlanarPoint(6, 14), rings[1][3]);
    }

    [Fact]
    public void GenerateHeadlands_Throws_WhenPolygonCollapses()
    {
        var boundary = new List<PlanarPoint>
        {
            new(0, 0),
            new(5, 0),
            new(5, 5),
            new(0, 5)
        };

        Assert.Throws<InvalidOperationException>(() => HeadlandGenerator.GenerateHeadlands(boundary, headlandWidth: 3, passes: 2));
    }
}
