using Aog.UI.Avalonia.Rendering;
using Avalonia;
using FluentAssertions;
using FluentAssertions.Primitives;
using Xunit;

namespace Aog.UI.Avalonia.Tests.Rendering;

public class MapViewportTests
{
    [Fact]
    public void ScreenToWorld_IsInverseOfWorldToScreen()
    {
        var viewport = new MapViewport();
        viewport.PanBy(new Vector(120, -80));
        viewport.ZoomAt(new Point(200, 150), 1.25);

        var worldPoint = new Point(42, -18);
        var screenPoint = viewport.WorldToScreen(worldPoint);

        viewport.ScreenToWorld(screenPoint).Should().BeApproximately(worldPoint, 1e-6);
    }

    [Fact]
    public void ZoomAt_KeepsCursorLockedToWorldPoint()
    {
        var viewport = new MapViewport();
        var cursor = new Point(350, 260);
        var worldAtCursorBefore = viewport.ScreenToWorld(cursor);

        viewport.ZoomAt(cursor, 2.0);
        var worldAtCursorAfter = viewport.ScreenToWorld(cursor);

        worldAtCursorAfter.Should().BeApproximately(worldAtCursorBefore, 1e-6);
        viewport.Scale.Should().BeApproximately(2.0, 1e-6);
    }

    [Fact]
    public void PanBy_ShiftsWorldCoordinates()
    {
        var viewport = new MapViewport();
        var worldBefore = viewport.ScreenToWorld(new Point(100, 100));

        viewport.PanBy(new Vector(20, 40));
        var worldAfter = viewport.ScreenToWorld(new Point(100, 100));

        worldAfter.X.Should().BeLessThan(worldBefore.X);
        worldAfter.Y.Should().BeGreaterThan(worldBefore.Y);
    }

    [Fact]
    public void CenterOn_PlacesWorldPointAtViewportCenter()
    {
        var viewport = new MapViewport();
        var focusPoint = new Point(12, -6);
        var size = new Size(800, 600);

        viewport.CenterOn(focusPoint, size);

        var screenPoint = viewport.WorldToScreen(focusPoint);
        screenPoint.Should().BeApproximately(new Point(size.Width / 2, size.Height / 2), 1e-6);
    }
}

internal static class PointAssertionsExtensions
{
    public static AndConstraint<ObjectAssertions<Point>> BeApproximately(
        this ObjectAssertions<Point> assertions,
        Point expected,
        double precision)
    {
        assertions.Subject.X.Should().BeApproximately(expected.X, precision);
        assertions.Subject.Y.Should().BeApproximately(expected.Y, precision);

        return new AndConstraint<ObjectAssertions<Point>>(assertions);
    }
}
