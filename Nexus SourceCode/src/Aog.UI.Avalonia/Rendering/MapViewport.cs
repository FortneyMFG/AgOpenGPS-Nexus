using Avalonia;

namespace Aog.UI.Avalonia.Rendering;

/// <summary>
/// Maintains the transform that maps between world and screen coordinates for the
/// <see cref="Controls.MapView"/>.
/// </summary>
public sealed class MapViewport
{
    private Vector _pan = default;

    /// <summary>
    /// Initializes a new instance of the <see cref="MapViewport"/> class.
    /// </summary>
    /// <param name="minScale">The minimum allowed zoom scale.</param>
    /// <param name="maxScale">The maximum allowed zoom scale.</param>
    public MapViewport(double minScale = 0.05, double maxScale = 50)
    {
        MinScale = minScale;
        MaxScale = maxScale;
        Scale = 1;
    }

    /// <summary>
    /// Gets the minimum allowed zoom scale.
    /// </summary>
    public double MinScale { get; }

    /// <summary>
    /// Gets the maximum allowed zoom scale.
    /// </summary>
    public double MaxScale { get; }

    /// <summary>
    /// Gets the current zoom scale.
    /// </summary>
    public double Scale { get; private set; }

    /// <summary>
    /// Pans the viewport by the supplied delta in screen space.
    /// </summary>
    public void PanBy(Vector screenDelta)
    {
        _pan += screenDelta;
    }

    /// <summary>
    /// Zooms the viewport by the provided factor, keeping the specified screen
    /// position stable.
    /// </summary>
    /// <param name="screenPoint">The cursor position in screen coordinates.</param>
    /// <param name="zoomFactor">The zoom factor to apply.</param>
    public void ZoomAt(Point screenPoint, double zoomFactor)
    {
        if (zoomFactor <= 0)
        {
            return;
        }

        var targetScale = Clamp(Scale * zoomFactor, MinScale, MaxScale);
        var worldPoint = ScreenToWorld(screenPoint);
        Scale = targetScale;
        var adjustedScreen = WorldToScreen(worldPoint);
        _pan += screenPoint - adjustedScreen;
    }

    /// <summary>
    /// Converts a point from world coordinates (meters) to screen space (pixels).
    /// </summary>
    public Point WorldToScreen(Point worldPoint)
    {
        return new Point(
            _pan.X + worldPoint.X * Scale,
            _pan.Y - worldPoint.Y * Scale);
    }

    /// <summary>
    /// Converts a point from screen space (pixels) to world coordinates (meters).
    /// </summary>
    public Point ScreenToWorld(Point screenPoint)
    {
        return new Point(
            (screenPoint.X - _pan.X) / Scale,
            (_pan.Y - screenPoint.Y) / Scale);
    }

    /// <summary>
    /// Recenters the viewport so that the provided world coordinate appears in the
    /// middle of the supplied viewport size.
    /// </summary>
    public void CenterOn(Point worldPoint, Size viewportSize)
    {
        _pan = new Vector(
            viewportSize.Width / 2 - worldPoint.X * Scale,
            viewportSize.Height / 2 + worldPoint.Y * Scale);
    }

    private static double Clamp(double value, double min, double max)
        => value < min ? min : (value > max ? max : value);
}
