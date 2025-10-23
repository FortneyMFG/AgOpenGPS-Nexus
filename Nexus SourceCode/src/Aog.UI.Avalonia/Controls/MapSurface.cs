using System;
using System.Collections.Generic;
using System.Linq;
using Aog.Core.Machines.Axle;
using Aog.UI.Avalonia.Models;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Aog.UI.Avalonia.Controls;

/// <summary>
/// Lightweight renderer that projects map layers, guidance tracks, and the current vehicle pose.
/// </summary>
public sealed class MapSurface : Control
{
    /// <summary>
    /// Identifies the <see cref="MapLayers"/> styled property.
    /// </summary>
    public static readonly StyledProperty<IReadOnlyList<MapLayer>?> MapLayersProperty =
        AvaloniaProperty.Register<MapSurface, IReadOnlyList<MapLayer>?>(nameof(MapLayers));

    /// <summary>
    /// Identifies the <see cref="GuidanceTracks"/> styled property.
    /// </summary>
    public static readonly StyledProperty<IReadOnlyList<GuidanceTrack>?> GuidanceTracksProperty =
        AvaloniaProperty.Register<MapSurface, IReadOnlyList<GuidanceTrack>?>(nameof(GuidanceTracks));

    /// <summary>
    /// Identifies the <see cref="VehiclePose"/> styled property.
    /// </summary>
    public static readonly StyledProperty<VehiclePose> VehiclePoseProperty =
        AvaloniaProperty.Register<MapSurface, VehiclePose>(nameof(VehiclePose), VehiclePose.Origin);

    /// <summary>
    /// Identifies the <see cref="EquipmentProfile"/> styled property.
    /// </summary>
    public static readonly StyledProperty<AxleCentricProfile?> EquipmentProfileProperty =
        AvaloniaProperty.Register<MapSurface, AxleCentricProfile?>(nameof(EquipmentProfile));

    private static readonly SolidColorBrush VehicleBrush = new(Color.FromArgb(220, 41, 128, 185));
    private static readonly Pen VehicleOutlinePen = new(new SolidColorBrush(Color.FromArgb(255, 21, 67, 96)), 1.5);

    static MapSurface()
    {
        AffectsRender<MapSurface>(MapLayersProperty, GuidanceTracksProperty, VehiclePoseProperty, EquipmentProfileProperty);
    }

    public IReadOnlyList<MapLayer>? MapLayers
    {
        get => GetValue(MapLayersProperty);
        set => SetValue(MapLayersProperty, value);
    }

    public IReadOnlyList<GuidanceTrack>? GuidanceTracks
    {
        get => GetValue(GuidanceTracksProperty);
        set => SetValue(GuidanceTracksProperty, value);
    }

    public VehiclePose VehiclePose
    {
        get => GetValue(VehiclePoseProperty);
        set => SetValue(VehiclePoseProperty, value);
    }

    public AxleCentricProfile? EquipmentProfile
    {
        get => GetValue(EquipmentProfileProperty);
        set => SetValue(EquipmentProfileProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var bounds = CalculateWorldBounds();
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var margin = 32.0;
        var scaleX = (Bounds.Width - (margin * 2)) / bounds.Width;
        var scaleY = (Bounds.Height - (margin * 2)) / bounds.Height;
        var scale = Math.Max(Math.Min(scaleX, scaleY), 0.1);
        var offsetX = margin - bounds.X * scale;
        var offsetY = Bounds.Height - margin + bounds.Y * scale;

        void DrawLayers()
        {
            if (MapLayers is null)
            {
                return;
            }

            foreach (var layer in MapLayers.Where(l => l is not null && l.IsVisible))
            {
                foreach (var cell in layer.Cells)
                {
                    var half = cell.SizeMeters / 2.0;
                    var worldRect = new Rect(
                        cell.Center.X - half,
                        cell.Center.Y - half,
                        cell.SizeMeters,
                        cell.SizeMeters);

                    var screenRect = new Rect(
                        offsetX + worldRect.X * scale,
                        offsetY - (worldRect.Y + worldRect.Height) * scale,
                        worldRect.Width * scale,
                        worldRect.Height * scale);

                    var fill = new SolidColorBrush(layer.Style.Evaluate(cell.Value));
                    context.FillRectangle(fill, screenRect);

                    if (layer.Style.OutlineColor.A > 0)
                    {
                        var outlinePen = new Pen(new SolidColorBrush(layer.Style.OutlineColor), 1);
                        context.DrawRectangle(outlinePen, screenRect, 0);
                    }
                }
            }
        }

        void DrawGuidance()
        {
            if (GuidanceTracks is null)
            {
                return;
            }

            foreach (var track in GuidanceTracks)
            {
                if (track.Points.Count < 2)
                {
                    continue;
                }

                var geometry = new StreamGeometry();
                using (var geometryContext = geometry.Open())
                {
                    for (var i = 0; i < track.Points.Count; i++)
                    {
                        var world = track.Points[i];
                        var screen = new Point(
                            offsetX + world.X * scale,
                            offsetY - world.Y * scale);

                        if (i == 0)
                        {
                            geometryContext.BeginFigure(screen, false);
                        }
                        else
                        {
                            geometryContext.LineTo(screen);
                        }
                    }
                }

                var pen = new Pen(new SolidColorBrush(track.Color), Math.Max(track.Thickness, 1));
                context.DrawGeometry(null, pen, geometry);
            }
        }

        void DrawVehicle()
        {
            var pose = VehiclePose;
            var headingRadians = MathExtensions.ToRadians(pose.HeadingDegrees);
            var length = 4.8; // metres
            var width = 2.4;

            var corners = new[]
            {
                new Point(length / 2, 0),
                new Point(-length / 2, width / 2),
                new Point(-length / 2, -width / 2),
            };

            Point Transform(Point bodyPoint)
            {
                var cos = Math.Cos(headingRadians);
                var sin = Math.Sin(headingRadians);
                var world = new Point(
                    pose.X + (bodyPoint.X * cos - bodyPoint.Y * sin),
                    pose.Y + (bodyPoint.X * sin + bodyPoint.Y * cos));
                return new Point(
                    offsetX + world.X * scale,
                    offsetY - world.Y * scale);
            }

            var screenPoints = corners.Select(Transform).ToArray();
            var geometry = new StreamGeometry();
            using (var gc = geometry.Open())
            {
                gc.BeginFigure(screenPoints[0], true);
                gc.LineTo(screenPoints[1]);
                gc.LineTo(screenPoints[2]);
                gc.EndFigure(true);
            }

            context.DrawGeometry(VehicleBrush, VehicleOutlinePen, geometry);

            if (EquipmentProfile is { } profile)
            {
                DrawAxleMarkers(context, profile, pose, headingRadians, scale, offsetX, offsetY);
            }
        }

        DrawLayers();
        DrawGuidance();
        DrawVehicle();
    }

    private Rect CalculateWorldBounds()
    {
        var minX = double.PositiveInfinity;
        var maxX = double.NegativeInfinity;
        var minY = double.PositiveInfinity;
        var maxY = double.NegativeInfinity;

        void Include(double x, double y)
        {
            if (double.IsNaN(x) || double.IsNaN(y))
            {
                return;
            }

            minX = Math.Min(minX, x);
            maxX = Math.Max(maxX, x);
            minY = Math.Min(minY, y);
            maxY = Math.Max(maxY, y);
        }

        if (MapLayers is not null)
        {
            foreach (var cell in MapLayers.SelectMany(layer => layer.Cells))
            {
                var half = cell.SizeMeters / 2.0;
                Include(cell.Center.X - half, cell.Center.Y - half);
                Include(cell.Center.X + half, cell.Center.Y + half);
            }
        }

        if (GuidanceTracks is not null)
        {
            foreach (var point in GuidanceTracks.SelectMany(track => track.Points))
            {
                Include(point.X, point.Y);
            }
        }

        Include(VehiclePose.X, VehiclePose.Y);

        if (double.IsInfinity(minX) || double.IsInfinity(minY) ||
            double.IsInfinity(maxX) || double.IsInfinity(maxY))
        {
            return new Rect(-50, -50, 100, 100);
        }

        var width = Math.Max(maxX - minX, 10);
        var height = Math.Max(maxY - minY, 10);
        return new Rect(minX, minY, width, height);
    }

    private static void DrawAxleMarkers(
        DrawingContext context,
        AxleCentricProfile profile,
        VehiclePose pose,
        double headingRadians,
        double scale,
        double offsetX,
        double offsetY)
    {
        var axleBrush = new SolidColorBrush(Color.FromArgb(200, 46, 204, 113));
        var axlePen = new Pen(new SolidColorBrush(Color.FromArgb(255, 32, 106, 61)), 1);
        var positions = EstimateAxlePositions(profile);

        foreach (var (role, longitudinalOffset) in positions)
        {
            var cos = Math.Cos(headingRadians);
            var sin = Math.Sin(headingRadians);
            var world = new Point(
                pose.X + longitudinalOffset * cos,
                pose.Y + longitudinalOffset * sin);
            var screen = new Rect(
                offsetX + (world.X - 0.6) * scale,
                offsetY - (world.Y + 0.6) * scale,
                1.2 * scale,
                1.2 * scale);

            var fillColor = role switch
            {
                AxleNodeRole.Steer => Color.FromArgb(200, 41, 128, 185),
                AxleNodeRole.Drive => Color.FromArgb(200, 46, 204, 113),
                AxleNodeRole.Implement => Color.FromArgb(200, 241, 196, 15),
                _ => Color.FromArgb(200, 149, 165, 166)
            };

            var fillBrush = new SolidColorBrush(fillColor);
            context.FillRectangle(fillBrush, screen);
            context.DrawRectangle(axlePen, screen);
        }
    }

    private static IReadOnlyList<(AxleNodeRole Role, double OffsetMeters)> EstimateAxlePositions(AxleCentricProfile profile)
    {
        if (profile.Axles.Count == 0)
        {
            return Array.Empty<(AxleNodeRole, double)>();
        }

        // Build a simple chain along the longitudinal axis. Lacking explicit geometry,
        // we assume a consistent spacing between joints.
        const double spacing = 2.5; // metres
        var root = profile.Axles.FirstOrDefault(a => a.Role == AxleNodeRole.Steer)
                   ?? profile.Axles.First();

        var offsets = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            [root.Id] = 2.5,
        };

        foreach (var joint in profile.Joints)
        {
            if (offsets.TryGetValue(joint.Parent, out var parentOffset))
            {
                offsets[joint.Child] = parentOffset - spacing;
            }
        }

        return profile.Axles
            .Where(axle => offsets.TryGetValue(axle.Id, out _))
            .Select(axle => (axle.Role, offsets[axle.Id]))
            .ToArray();
    }

    private static class MathExtensions
    {
        public static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
    }
}
