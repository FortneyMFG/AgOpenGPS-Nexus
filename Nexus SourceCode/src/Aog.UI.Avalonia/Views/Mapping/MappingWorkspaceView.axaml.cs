using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Aog.UI.Avalonia.Mapping.Core;
using Aog.UI.Avalonia.Mapping.Input;
using Aog.UI.Avalonia.Mapping.Rendering;
using Aog.UI.Avalonia.Mapping.Vector;
using Aog.UI.Avalonia.Models;
using Aog.UI.Avalonia.ViewModels;

namespace Aog.UI.Avalonia.Views.Mapping;

public partial class MappingWorkspaceView : UserControl
{
    private const double ForwardStepMeters = 0.75;
    private const double TurnStepDegrees = 4.5;

    private MapScene? _scene;
    private VehiclePassLayer? _vehicleLayer;

    public MappingWorkspaceView()
    {
        InitializeComponent();
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
        AttachedToVisualTree += (_, _) =>
        {
            Focus();
            RefreshScene();
        };
        PointerPressed += (_, _) => Focus();
        DataContextChanged += (_, _) => RefreshScene();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void RefreshScene()
    {
        if (MapViewport is null)
        {
            return;
        }

        if (DataContext is not MappingWorkspaceViewModel workspace)
        {
            return;
        }

        var scene = new MapScene();
        var camera = new CameraRig();
        var localizer = new Localizer();
        MapViewport.Attach(scene, camera, localizer);

        scene.Add(new GridPassLayer
        {
            ZIndex = 0
        });

        foreach (var layer in workspace.Layers)
        {
            var featureZ = new List<VectorFeature>();
            foreach (var cell in layer.Cells)
            {
                featureZ.Add(CreateCellFeature(layer, cell));
            }

            if (featureZ.Count == 0)
            {
                continue;
            }

            scene.Add(new VectorLayer(new InMemoryVectorSource(featureZ))
            {
                ZIndex = 100
            });
        }

        foreach (var track in workspace.GuidanceTracks)
        {
            var points = track.Points.Select(p => new Double3(p.X, p.Y, 0)).ToArray();
            if (points.Length == 0)
            {
                continue;
            }

            var feature = new VectorFeature(
                VectorFeatureType.Polyline,
                points,
                lineColor: ToVector(track.Color, track.Color.A / 255f),
                lineWidthMeters: (float)Math.Max(0.4, track.Thickness * 0.1));

            scene.Add(new VectorLayer(new InMemoryVectorSource(new[] { feature }))
            {
                ZIndex = 240
            });
        }

        _vehicleLayer = new VehiclePassLayer(() => workspace.VehiclePose)
        {
            ZIndex = 300
        };
        scene.Add(_vehicleLayer);

        _scene = scene;
    }

    private static VectorFeature CreateCellFeature(MapLayer layer, MapLayerCell cell)
    {
        var half = cell.SizeMeters / 2.0;
        var corners = new[]
        {
            new Double3(cell.Center.X - half, cell.Center.Y - half, 0),
            new Double3(cell.Center.X + half, cell.Center.Y - half, 0),
            new Double3(cell.Center.X + half, cell.Center.Y + half, 0),
            new Double3(cell.Center.X - half, cell.Center.Y + half, 0)
        };

        var fillColor = layer.Style.Evaluate(cell.Value);
        var outlineColor = layer.Style.OutlineColor;

        return new VectorFeature(
            VectorFeatureType.Polygon,
            corners,
            fillColor: ToVector(fillColor, fillColor.A / 255f),
            lineColor: outlineColor.A > 0 ? ToVector(outlineColor, outlineColor.A / 255f) : null,
            lineWidthMeters: 0.5f);
    }

    private static Vector4 ToVector(Color color, float alphaOverride)
    {
        return new Vector4(
            color.R / 255f,
            color.G / 255f,
            color.B / 255f,
            alphaOverride);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MappingWorkspaceViewModel workspace)
        {
            return;
        }

        var boost = e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? 2.0 : 1.0;
        switch (e.Key)
        {
            case Key.W:
                workspace.ApplyMovement(ForwardStepMeters * boost, 0);
                e.Handled = true;
                break;
            case Key.S:
                workspace.ApplyMovement(-ForwardStepMeters * boost, 0);
                e.Handled = true;
                break;
            case Key.A:
                workspace.ApplyMovement(0, -TurnStepDegrees * boost);
                e.Handled = true;
                break;
            case Key.D:
                workspace.ApplyMovement(0, TurnStepDegrees * boost);
                e.Handled = true;
                break;
            case Key.R:
                workspace.ResetPose();
                e.Handled = true;
                break;
        }
    }
}
