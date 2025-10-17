using System;
using System.Collections.Generic;
using System.Linq;
using Aog.UI.Avalonia.Models;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Aog.UI.Avalonia.Controls;

/// <summary>
/// Interactive map surface rendered with Skia. Supports mouse pan/zoom and renders
/// the current vehicle pose.
/// </summary>
public sealed class MapView : Control
{
    private readonly Rendering.MapViewport _viewport = new();
    private bool _isPanning;
    private bool _hasUserPanned;
    private IPointer? _panPointer;
    private Point _lastPointerPosition;

    /// <summary>
    /// Identifies the <see cref="VehiclePose"/> styled property.
    /// </summary>
    public static readonly StyledProperty<VehiclePose> VehiclePoseProperty =
        AvaloniaProperty.Register<MapView, VehiclePose>(
            nameof(VehiclePose),
            VehiclePose.Origin);

    /// <summary>
    /// Identifies the <see cref="GuidanceTracks"/> styled property.
    /// </summary>
    public static readonly StyledProperty<IReadOnlyList<GuidanceTrack>> GuidanceTracksProperty =
        AvaloniaProperty.Register<MapView, IReadOnlyList<GuidanceTrack>>(
            nameof(GuidanceTracks),
            Array.Empty<GuidanceTrack>());

    /// <summary>
    /// Identifies the <see cref="Layers"/> styled property.
    /// </summary>
    public static readonly StyledProperty<IReadOnlyList<MapLayer>> LayersProperty =
        AvaloniaProperty.Register<MapView, IReadOnlyList<MapLayer>>(
            nameof(Layers),
            Array.Empty<MapLayer>());

    /// <summary>
    /// Gets or sets the pose to render on the map.
    /// </summary>
    public VehiclePose VehiclePose
    {
        get => GetValue(VehiclePoseProperty);
        set => SetValue(VehiclePoseProperty, value);
    }

    /// <summary>Gets or sets the map layers visualised on the map.</summary>
    public IReadOnlyList<MapLayer> Layers
    {
        get => GetValue(LayersProperty);
        set => SetValue(LayersProperty, value);
    }

    /// <summary>Gets or sets the guidance tracks rendered on top of the map.</summary>
    public IReadOnlyList<GuidanceTrack> GuidanceTracks
    {
        get => GetValue(GuidanceTracksProperty);
        set => SetValue(GuidanceTracksProperty, value);
    }

    /// <summary>
    /// Creates a new <see cref="MapView"/> instance.
    /// </summary>
    public MapView()
    {
        ClipToBounds = true;
        Focusable = true;

        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;
        PointerCaptureLost += OnPointerCaptureLost;
        PointerWheelChanged += OnPointerWheelChanged;
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == VehiclePoseProperty)
        {
            if (!_hasUserPanned)
            {
                CenterOnPose(Bounds.Size);
            }

            InvalidateVisual();
        }
        else if (change.Property == BoundsProperty && !_hasUserPanned &&
                 change.NewValue is Rect rect && rect.Width > 0 && rect.Height > 0)
        {
            CenterOnPose(rect.Size);
            InvalidateVisual();
        }
        else if (change.Property == GuidanceTracksProperty || change.Property == LayersProperty)
        {
            InvalidateVisual();
        }
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var backgroundBrush = new ImmutableSolidColorBrush(Color.FromRgb(24, 31, 36));
        context.FillRectangle(backgroundBrush, new Rect(Bounds.Size));

        DrawLayers(context);
        DrawGuidance(context);
        DrawAxes(context);
        DrawVehicle(context);
    }

    private void DrawLayers(DrawingContext context)
    {
        var layers = Layers;
        if (layers is null || layers.Count == 0)
        {
            return;
        }

        foreach (var layer in layers)
        {
            if (!layer.IsVisible || layer.Cells.Count == 0)
            {
                continue;
            }

            foreach (var cell in layer.Cells)
            {
                var half = cell.SizeMeters / 2.0;
                var topLeft = _viewport.WorldToScreen(new Point(cell.Center.X - half, cell.Center.Y + half));
                var bottomRight = _viewport.WorldToScreen(new Point(cell.Center.X + half, cell.Center.Y - half));

                var left = Math.Min(topLeft.X, bottomRight.X);
                var right = Math.Max(topLeft.X, bottomRight.X);
                var top = Math.Min(topLeft.Y, bottomRight.Y);
                var bottom = Math.Max(topLeft.Y, bottomRight.Y);

                var rect = new Rect(new Point(left, top), new Point(right, bottom));
                var fillColor = layer.Style.Evaluate(cell.Value);
                if (layer.Style.IsPlanned)
                {
                    fillColor = Color.FromArgb((byte)(fillColor.A * 0.55), fillColor.R, fillColor.G, fillColor.B);
                }

                var fillBrush = new ImmutableSolidColorBrush(fillColor);
                context.FillRectangle(fillBrush, rect);

                if (layer.Style.OutlineColor.A > 0)
                {
                    var outlinePen = new Pen(new ImmutableSolidColorBrush(layer.Style.OutlineColor), 1);
                    context.DrawRectangle(outlinePen, rect);
                }
            }
        }
    }

    private void DrawGuidance(DrawingContext context)
    {
        var tracks = GuidanceTracks;
        if (tracks is null || tracks.Count == 0)
        {
            return;
        }

        foreach (var track in tracks.Where(t => t.Points.Count >= 2))
        {
            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                var start = _viewport.WorldToScreen(track.Points[0]);
                ctx.BeginFigure(start, false);
                for (var index = 1; index < track.Points.Count; index++)
                {
                    var next = _viewport.WorldToScreen(track.Points[index]);
                    ctx.LineTo(next);
                }
                ctx.EndFigure(false);
            }

            var pen = new Pen(new ImmutableSolidColorBrush(track.Color), Math.Max(track.Thickness, 1))
            {
                LineCap = PenLineCap.Round,
                LineJoin = PenLineJoin.Round,
            };

            context.DrawGeometry(null, pen, geometry);
        }
    }

    private void DrawAxes(DrawingContext context)
    {
        var viewportBounds = Bounds;
        var topLeftWorld = _viewport.ScreenToWorld(new Point(0, 0));
        var bottomRightWorld = _viewport.ScreenToWorld(new Point(viewportBounds.Width, viewportBounds.Height));
        var worldLeft = Math.Min(topLeftWorld.X, bottomRightWorld.X);
        var worldRight = Math.Max(topLeftWorld.X, bottomRightWorld.X);
        var worldTop = Math.Max(topLeftWorld.Y, bottomRightWorld.Y);
        var worldBottom = Math.Min(topLeftWorld.Y, bottomRightWorld.Y);

        var axisPen = new Pen(new ImmutableSolidColorBrush(Color.FromRgb(64, 86, 96)), 2)
        {
            LineCap = PenLineCap.Round,
        };

        if (worldLeft <= 0 && worldRight >= 0)
        {
            var start = _viewport.WorldToScreen(new Point(0, worldBottom));
            var end = _viewport.WorldToScreen(new Point(0, worldTop));
            context.DrawLine(axisPen, start, end);
        }

        if (worldBottom <= 0 && worldTop >= 0)
        {
            var start = _viewport.WorldToScreen(new Point(worldLeft, 0));
            var end = _viewport.WorldToScreen(new Point(worldRight, 0));
            context.DrawLine(axisPen, start, end);
        }
    }

    private void DrawVehicle(DrawingContext context)
    {
        var pose = VehiclePose;
        var center = _viewport.WorldToScreen(new Point(pose.X, pose.Y));

        var radius = Math.Max(_viewport.Scale * 0.5, 6);
        var bodyBrush = new ImmutableSolidColorBrush(Color.FromRgb(17, 201, 141));
        var outlinePen = new Pen(new ImmutableSolidColorBrush(Color.FromRgb(10, 87, 67)), 2);
        var ellipseRect = new Rect(center.X - radius, center.Y - radius, radius * 2, radius * 2);
        context.DrawEllipse(bodyBrush, outlinePen, ellipseRect);

        var headingRadians = pose.HeadingDegrees * Math.PI / 180.0;
        var headingDir = new Vector(Math.Cos(headingRadians), -Math.Sin(headingRadians));
        var headingLength = Math.Max(radius * 2.2, 28);
        var tip = new Point(center.X + headingDir.X * headingLength, center.Y + headingDir.Y * headingLength);

        var headingPen = new Pen(new ImmutableSolidColorBrush(Colors.White), 3)
        {
            LineCap = PenLineCap.Round,
        };

        context.DrawLine(headingPen, center, tip);

        var backDir = -headingDir;
        var arrowAngle = Math.PI / 6; // 30 degrees.
        var arrowSize = Math.Max(headingLength * 0.35, 14);
        var leftHeadDir = Rotate(backDir, arrowAngle);
        var rightHeadDir = Rotate(backDir, -arrowAngle);
        var left = new Point(tip.X + leftHeadDir.X * arrowSize, tip.Y + leftHeadDir.Y * arrowSize);
        var right = new Point(tip.X + rightHeadDir.X * arrowSize, tip.Y + rightHeadDir.Y * arrowSize);
        context.DrawLine(headingPen, tip, left);
        context.DrawLine(headingPen, tip, right);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(this);
        if (point.Properties.IsLeftButtonPressed && !_isPanning)
        {
            _isPanning = true;
            _panPointer = e.Pointer;
            _lastPointerPosition = point.Position;
            e.Pointer.Capture(this);
            e.Handled = true;
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isPanning || _panPointer != e.Pointer)
        {
            return;
        }

        var position = e.GetPosition(this);
        var delta = position - _lastPointerPosition;
        _lastPointerPosition = position;
        _viewport.PanBy(delta);
        if (!delta.Equals(default(Vector)))
        {
            _hasUserPanned = true;
        }
        InvalidateVisual();
        e.Handled = true;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isPanning && _panPointer == e.Pointer)
        {
            e.Pointer.Capture(null);
            _isPanning = false;
            _panPointer = null;
            e.Handled = true;
        }
    }

    private void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (_isPanning && _panPointer == e.Pointer)
        {
            _isPanning = false;
            _panPointer = null;
        }
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        var zoomFactor = Math.Pow(1.1, e.Delta.Y);
        if (Math.Abs(zoomFactor - 1) < double.Epsilon)
        {
            return;
        }

        var cursor = e.GetPosition(this);
        _viewport.ZoomAt(cursor, zoomFactor);
        _hasUserPanned = true;
        InvalidateVisual();
        e.Handled = true;
    }

    private void CenterOnPose(Size viewportSize)
    {
        if (viewportSize.Width <= 0 || viewportSize.Height <= 0)
        {
            return;
        }

        _viewport.CenterOn(new Point(VehiclePose.X, VehiclePose.Y), viewportSize);
        InvalidateVisual();
    }

    private static Vector Rotate(Vector vector, double radians)
    {
        var cos = Math.Cos(radians);
        var sin = Math.Sin(radians);
        return new Vector(vector.X * cos - vector.Y * sin, vector.X * sin + vector.Y * cos);
    }
}
