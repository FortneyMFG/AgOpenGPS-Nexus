using System;
using System.Collections.Generic;
using System.Linq;
using Aog.UI.Avalonia.Models;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using SkiaSharp;
using SkiaSharp.Views.Avalonia;

namespace Aog.UI.Avalonia.Controls;

/// <summary>
/// Interactive map surface rendered with Skia. Supports mouse pan/zoom and renders
/// the current vehicle pose.
/// </summary>
public sealed class MapView : SKElement
{
    private readonly Rendering.MapViewport _viewport = new();
    private bool _isPanning;
    private bool _hasUserPanned;
    private Pointer? _panPointer;
    private Point _lastPointerPosition;

    /// <summary>
    /// Identifies the <see cref="VehiclePose"/> styled property.
    /// </summary>
    public static readonly StyledProperty<VehiclePose> VehiclePoseProperty =
        AvaloniaProperty.Register<MapView, VehiclePose>(
            nameof(VehiclePose),
            VehiclePose.Origin,
            notifying: static (sender, _) => sender.InvalidateVisual());

    /// <summary>
    /// Identifies the <see cref="CoverageCells"/> styled property.
    /// </summary>
    public static readonly StyledProperty<IReadOnlyList<CoverageCell>> CoverageCellsProperty =
        AvaloniaProperty.Register<MapView, IReadOnlyList<CoverageCell>>(
            nameof(CoverageCells),
            Array.Empty<CoverageCell>(),
            notifying: static (sender, _) => sender.InvalidateVisual());

    /// <summary>
    /// Identifies the <see cref="GuidanceTracks"/> styled property.
    /// </summary>
    public static readonly StyledProperty<IReadOnlyList<GuidanceTrack>> GuidanceTracksProperty =
        AvaloniaProperty.Register<MapView, IReadOnlyList<GuidanceTrack>>(
            nameof(GuidanceTracks),
            Array.Empty<GuidanceTrack>(),
            notifying: static (sender, _) => sender.InvalidateVisual());

    /// <summary>
    /// Gets or sets the pose to render on the map.
    /// </summary>
    public VehiclePose VehiclePose
    {
        get => GetValue(VehiclePoseProperty);
        set => SetValue(VehiclePoseProperty, value);
    }

    /// <summary>Gets or sets the coverage cells visualised on the map.</summary>
    public IReadOnlyList<CoverageCell> CoverageCells
    {
        get => GetValue(CoverageCellsProperty);
        set => SetValue(CoverageCellsProperty, value);
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
    protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == VehiclePoseProperty && !_hasUserPanned)
        {
            CenterOnPose(Bounds.Size);
        }
        else if (change.Property == BoundsProperty && !_hasUserPanned &&
                 change.NewValue is Rect rect && rect.Width > 0 && rect.Height > 0)
        {
            CenterOnPose(rect.Size);
        }
    }

    /// <inheritdoc />
    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        base.OnPaintSurface(e);

        var canvas = e.Surface.Canvas;
        canvas.Clear(new SKColor(24, 31, 36));

        canvas.Save();
        DrawCoverage(canvas);
        DrawGuidance(canvas);
        DrawAxes(canvas);
        DrawVehicle(canvas);
        canvas.Restore();
    }

    private void DrawCoverage(SKCanvas canvas)
    {
        var cells = CoverageCells;
        if (cells is null || cells.Count == 0)
        {
            return;
        }

        foreach (var cell in cells)
        {
            var half = cell.SizeMeters / 2.0;
            var topLeft = _viewport.WorldToScreen(new Point(cell.Center.X - half, cell.Center.Y + half));
            var bottomRight = _viewport.WorldToScreen(new Point(cell.Center.X + half, cell.Center.Y - half));

            var left = Math.Min(topLeft.X, bottomRight.X);
            var right = Math.Max(topLeft.X, bottomRight.X);
            var top = Math.Min(topLeft.Y, bottomRight.Y);
            var bottom = Math.Max(topLeft.Y, bottomRight.Y);

            var rect = new SKRect((float)left, (float)top, (float)right, (float)bottom);
            var coverage = (float)cell.ClampedCoverage;
            var fillColor = new SKColor(
                (byte)(40 + coverage * 60),
                (byte)(110 + coverage * 100),
                (byte)(60 + coverage * 80),
                (byte)(160 + coverage * 80));

            using var fillPaint = new SKPaint
            {
                Color = fillColor,
                IsStroke = false,
                IsAntialias = true,
            };

            using var outlinePaint = new SKPaint
            {
                Color = new SKColor(33, 46, 51, 180),
                StrokeWidth = 1,
                IsStroke = true,
                IsAntialias = true,
            };

            canvas.DrawRect(rect, fillPaint);
            canvas.DrawRect(rect, outlinePaint);
        }
    }

    private void DrawGuidance(SKCanvas canvas)
    {
        var tracks = GuidanceTracks;
        if (tracks is null || tracks.Count == 0)
        {
            return;
        }

        foreach (var track in tracks.Where(t => t.Points.Count >= 2))
        {
            using var paint = new SKPaint
            {
                Color = ToSkColor(track.Color),
                StrokeWidth = Math.Max((float)track.Thickness, 1f),
                IsStroke = true,
                IsAntialias = true,
                StrokeCap = SKStrokeCap.Round,
            };

            using var path = new SKPath();
            for (var index = 0; index < track.Points.Count; index++)
            {
                var point = track.Points[index];
                var screen = ToSkPoint(_viewport.WorldToScreen(point));
                if (index == 0)
                {
                    path.MoveTo(screen);
                }
                else
                {
                    path.LineTo(screen);
                }
            }

            canvas.DrawPath(path, paint);
        }
    }

    private void DrawAxes(SKCanvas canvas)
    {
        var viewportBounds = Bounds;
        var topLeftWorld = _viewport.ScreenToWorld(new Point(0, 0));
        var bottomRightWorld = _viewport.ScreenToWorld(new Point(viewportBounds.Width, viewportBounds.Height));
        var worldLeft = Math.Min(topLeftWorld.X, bottomRightWorld.X);
        var worldRight = Math.Max(topLeftWorld.X, bottomRightWorld.X);
        var worldTop = Math.Max(topLeftWorld.Y, bottomRightWorld.Y);
        var worldBottom = Math.Min(topLeftWorld.Y, bottomRightWorld.Y);

        using var axisPaint = new SKPaint
        {
            Color = new SKColor(64, 86, 96),
            StrokeWidth = 2,
            IsStroke = true,
            IsAntialias = true,
        };

        if (worldLeft <= 0 && worldRight >= 0)
        {
            var start = ToSkPoint(_viewport.WorldToScreen(new Point(0, worldBottom)));
            var end = ToSkPoint(_viewport.WorldToScreen(new Point(0, worldTop)));
            canvas.DrawLine(start, end, axisPaint);
        }

        if (worldBottom <= 0 && worldTop >= 0)
        {
            var start = ToSkPoint(_viewport.WorldToScreen(new Point(worldLeft, 0)));
            var end = ToSkPoint(_viewport.WorldToScreen(new Point(worldRight, 0)));
            canvas.DrawLine(start, end, axisPaint);
        }
    }

    private void DrawVehicle(SKCanvas canvas)
    {
        var pose = VehiclePose;
        var center = ToSkPoint(_viewport.WorldToScreen(new Point(pose.X, pose.Y)));

        float radius = Math.Max((float)(_viewport.Scale * 0.5), 6f);
        using var bodyPaint = new SKPaint
        {
            Color = new SKColor(17, 201, 141),
            IsStroke = false,
            IsAntialias = true,
        };

        using var outlinePaint = new SKPaint
        {
            Color = new SKColor(10, 87, 67),
            StrokeWidth = 2,
            IsStroke = true,
            IsAntialias = true,
        };

        canvas.DrawCircle(center, radius, bodyPaint);
        canvas.DrawCircle(center, radius, outlinePaint);

        float headingRadians = (float)(pose.HeadingDegrees * Math.PI / 180.0);
        var screenDir = new SKPoint((float)Math.Cos(headingRadians), (float)-Math.Sin(headingRadians));
        float headingLength = Math.Max(radius * 2.2f, 28f);
        var tip = new SKPoint(center.X + screenDir.X * headingLength, center.Y + screenDir.Y * headingLength);

        using var headingPaint = new SKPaint
        {
            Color = new SKColor(255, 255, 255),
            StrokeWidth = 3,
            IsStroke = true,
            IsAntialias = true,
            StrokeCap = SKStrokeCap.Round,
        };

        canvas.DrawLine(center, tip, headingPaint);

        var backDir = new SKPoint(-screenDir.X, -screenDir.Y);
        var arrowAngle = (float)(Math.PI / 6); // 30 degrees.
        float arrowSize = Math.Max(headingLength * 0.35f, 14f);
        var leftHeadDir = Rotate(backDir, arrowAngle);
        var rightHeadDir = Rotate(backDir, -arrowAngle);
        var left = new SKPoint(tip.X + leftHeadDir.X * arrowSize, tip.Y + leftHeadDir.Y * arrowSize);
        var right = new SKPoint(tip.X + rightHeadDir.X * arrowSize, tip.Y + rightHeadDir.Y * arrowSize);
        canvas.DrawLine(tip, left, headingPaint);
        canvas.DrawLine(tip, right, headingPaint);
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

    private static SKPoint ToSkPoint(Point point) => new((float)point.X, (float)point.Y);

    private static SKColor ToSkColor(Color color) => new(color.A, color.R, color.G, color.B);

    private static SKPoint Rotate(SKPoint vector, float radians)
    {
        var cos = MathF.Cos(radians);
        var sin = MathF.Sin(radians);
        return new SKPoint(vector.X * cos - vector.Y * sin, vector.X * sin + vector.Y * cos);
    }
}
