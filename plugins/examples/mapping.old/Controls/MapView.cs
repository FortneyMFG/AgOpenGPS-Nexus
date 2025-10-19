using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Aog.UI.Avalonia.Models;
using Aog.UI.Avalonia.Rendering;

namespace MappingPlugin.Controls;

public sealed class MapView : Control
{
    public static readonly StyledProperty<IReadOnlyList<MapLayer>?> LayersProperty =
        AvaloniaProperty.Register<MapView, IReadOnlyList<MapLayer>?>(nameof(Layers));

    public static readonly StyledProperty<VehiclePose> VehiclePoseProperty =
        AvaloniaProperty.Register<MapView, VehiclePose>(nameof(VehiclePose), VehiclePose.Origin);

    private readonly MapViewport _viewport = new();
    private bool _centerPending = true;
    private bool _isPanning;
    private Point _lastPointerPosition;

    public MapView()
    {
        ClipToBounds = true;
        Focusable = true;
    }

    public IReadOnlyList<MapLayer>? Layers
    {
        get => GetValue(LayersProperty);
        set => SetValue(LayersProperty, value);
    }

    public VehiclePose VehiclePose
    {
        get => GetValue(VehiclePoseProperty);
        set => SetValue(VehiclePoseProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == LayersProperty || change.Property == VehiclePoseProperty)
        {
            if (change.Property == VehiclePoseProperty)
            {
                _centerPending = true;
            }

            InvalidateVisual();
        }
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        _centerPending = true;
        return base.ArrangeOverride(finalSize);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var bounds = Bounds;
        if (_centerPending && bounds.Width > 0 && bounds.Height > 0)
        {
            var pose = VehiclePose;
            _viewport.CenterOn(new Point(pose.X, pose.Y), bounds.Size);
            _centerPending = false;
        }

        DrawBackground(context);
        DrawGrid(context);
        DrawLayers(context);
        DrawVehicle(context);
    }

    private void DrawBackground(DrawingContext context)
    {
        var brush = new SolidColorBrush(Color.Parse("#FF0F1722"));
        context.FillRectangle(brush, Bounds);
    }

    private void DrawGrid(DrawingContext context)
    {
        const double gridSpacing = 10;
        var pen = new Pen(new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), 1);

        var topLeftWorld = _viewport.ScreenToWorld(Bounds.TopLeft);
        var bottomRightWorld = _viewport.ScreenToWorld(Bounds.BottomRight);

        var minX = Math.Floor(Math.Min(topLeftWorld.X, bottomRightWorld.X) / gridSpacing) * gridSpacing;
        var maxX = Math.Ceiling(Math.Max(topLeftWorld.X, bottomRightWorld.X) / gridSpacing) * gridSpacing;
        var minY = Math.Floor(Math.Min(topLeftWorld.Y, bottomRightWorld.Y) / gridSpacing) * gridSpacing;
        var maxY = Math.Ceiling(Math.Max(topLeftWorld.Y, bottomRightWorld.Y) / gridSpacing) * gridSpacing;

        for (double x = minX; x <= maxX; x += gridSpacing)
        {
            var start = _viewport.WorldToScreen(new Point(x, minY));
            var end = _viewport.WorldToScreen(new Point(x, maxY));
            context.DrawLine(pen, start, end);
        }

        for (double y = minY; y <= maxY; y += gridSpacing)
        {
            var start = _viewport.WorldToScreen(new Point(minX, y));
            var end = _viewport.WorldToScreen(new Point(maxX, y));
            context.DrawLine(pen, start, end);
        }
    }

    private void DrawLayers(DrawingContext context)
    {
        if (Layers is null)
        {
            return;
        }

        foreach (var layer in Layers)
        {
            if (layer is null || !layer.IsVisible || layer.Cells.Count == 0)
            {
                continue;
            }

            var outlinePen = layer.Style.OutlineColor == Colors.Transparent
                ? null
                : new Pen(new SolidColorBrush(layer.Style.OutlineColor), 0.5);

            foreach (var cell in layer.Cells)
            {
                var half = cell.SizeMeters / 2.0;
                var bottomLeft = new Point(cell.Center.X - half, cell.Center.Y - half);
                var topRight = new Point(cell.Center.X + half, cell.Center.Y + half);

                var screenBottomLeft = _viewport.WorldToScreen(bottomLeft);
                var screenTopRight = _viewport.WorldToScreen(topRight);
                var rect = new Rect(screenBottomLeft, screenTopRight).Normalize();

                if (rect.Width <= 0 || rect.Height <= 0)
                {
                    continue;
                }

                var fill = new SolidColorBrush(layer.Style.Evaluate(cell.Value));
                context.FillRectangle(fill, rect);
                if (outlinePen is not null)
                {
                    context.DrawRectangle(outlinePen, rect);
                }
            }
        }
    }

    private void DrawVehicle(DrawingContext context)
    {
        var pose = VehiclePose;
        var center = _viewport.WorldToScreen(new Point(pose.X, pose.Y));

        var bodyBrush = new SolidColorBrush(Color.FromArgb(200, 29, 155, 240));
        context.DrawEllipse(bodyBrush, null, center, 12, 12);

        var headingRadians = pose.HeadingDegrees * Math.PI / 180.0;
        var arrowLength = 28;
        var arrowWidth = 12;

        var forward = new Vector(Math.Cos(headingRadians), Math.Sin(headingRadians));
        var right = new Vector(-forward.Y, forward.X);

        Point Transform(Vector offset) => center + new Vector(offset.X, -offset.Y);

        var tip = Transform(forward * arrowLength);
        var left = Transform(forward * (arrowLength * 0.2) + right * (arrowWidth / 2.0));
        var rightPoint = Transform(forward * (arrowLength * 0.2) - right * (arrowWidth / 2.0));

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(tip, true);
            ctx.LineTo(left);
            ctx.LineTo(center);
            ctx.LineTo(rightPoint);
            ctx.EndFigure(true);
        }

        var arrowBrush = new SolidColorBrush(Color.FromArgb(220, 255, 255, 255));
        context.DrawGeometry(arrowBrush, new Pen(new SolidColorBrush(Color.FromArgb(220, 0, 0, 0)), 1), geometry);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        var point = e.GetCurrentPoint(this);
        if (point.Properties.IsLeftButtonPressed)
        {
            _isPanning = true;
            _lastPointerPosition = e.GetPosition(this);
            e.Pointer.Capture(this);
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (!_isPanning)
        {
            return;
        }

        var position = e.GetPosition(this);
        var delta = position - _lastPointerPosition;
        _lastPointerPosition = position;
        _viewport.PanBy(delta);
        InvalidateVisual();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (_isPanning)
        {
            _isPanning = false;
            e.Pointer.Capture(null);
        }
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        var zoomFactor = Math.Pow(1.1, e.Delta.Y);
        _viewport.ZoomAt(e.GetPosition(this), zoomFactor);
        InvalidateVisual();
    }
}
