using System;
using System.Collections.Generic;
using Aog.Core.Paths;
using Xunit;

namespace Aog.Core.Tests.Paths;

public sealed class CurvePathPlannerTests
{
    [Fact]
    public void Offset_ShiftsPoints_ToRightOfTravel()
    {
        var path = new List<PlanarPoint>
        {
            new(0, 0),
            new(10, 0),
            new(10, 10)
        };

        var offset = CurvePathPlanner.Offset(path, 5);

        Assert.Equal(new PlanarPoint(0, -5), offset[0]);
        Assert.Equal(new PlanarPoint(15, -5), offset[1]);
        Assert.Equal(new PlanarPoint(15, 10), offset[2]);
    }

    [Fact]
    public void Offset_NegativeDistance_ShiftsLeft()
    {
        var path = new List<PlanarPoint>
        {
            new(0, 0),
            new(10, 0),
            new(20, 0)
        };

        var offset = CurvePathPlanner.Offset(path, -2);

        Assert.Equal(new PlanarPoint(0, 2), offset[0]);
        Assert.Equal(new PlanarPoint(10, 2), offset[1]);
        Assert.Equal(new PlanarPoint(20, 2), offset[2]);
    }
}
