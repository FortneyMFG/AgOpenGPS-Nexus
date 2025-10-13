using System;
using Aog.Core.Paths;
using Xunit;

namespace Aog.Core.Tests.Paths;

public sealed class ABLinePlannerTests
{
    [Fact]
    public void GetPassIndex_ReturnsZero_OnReferenceLine()
    {
        var planner = new ABLinePlanner(new PlanarPoint(0, 0), new PlanarPoint(100, 0), toolWidth: 9, overlap: 1);
        var pivot = new PlanarPoint(5, 0);

        var index = planner.GetPassIndex(pivot, toolOffset: 0, headingSameWay: true);

        Assert.Equal(0, index);
    }

    [Theory]
    [InlineData(8.5, 1)]
    [InlineData(-8.5, -1)]
    public void GetPassIndex_MapsOffsetsToExpectedPass(double lateralOffset, int expectedIndex)
    {
        var planner = new ABLinePlanner(new PlanarPoint(0, 0), new PlanarPoint(0, 100), toolWidth: 9, overlap: 1);
        var pivot = new PlanarPoint(lateralOffset, 50);

        var index = planner.GetPassIndex(pivot, toolOffset: 0, headingSameWay: true);

        Assert.Equal(expectedIndex, index);
    }

    [Fact]
    public void GetPassLine_AppliesToolOffset_WhenReversingDirection()
    {
        var planner = new ABLinePlanner(new PlanarPoint(0, 0), new PlanarPoint(0, 100), toolWidth: 6, overlap: 0.5, nudgeDistance: 0);

        // When driving back along the line the implement offset flips sign.
        var forwardLine = planner.GetPassLine(0, toolOffset: 0.2, headingSameWay: true);
        var reverseLine = planner.GetPassLine(0, toolOffset: 0.2, headingSameWay: false);

        Assert.NotEqual(forwardLine.Start.Easting, reverseLine.Start.Easting);
        Assert.True(forwardLine.Start.Easting < reverseLine.Start.Easting);
    }

    [Fact]
    public void GetSignedDistanceToPass_IsZero_OnLaneCenter()
    {
        var planner = new ABLinePlanner(new PlanarPoint(0, 0), new PlanarPoint(50, 0), toolWidth: 8, overlap: 0.5);
        var line = planner.GetPassLine(2, toolOffset: 0, headingSameWay: true);
        var midpoint = new PlanarPoint((line.Start.Easting + line.End.Easting) / 2.0, (line.Start.Northing + line.End.Northing) / 2.0);

        var distance = planner.GetSignedDistanceToPass(midpoint, 2, toolOffset: 0, headingSameWay: true);

        Assert.InRange(distance, -1e-6, 1e-6);
    }
}
