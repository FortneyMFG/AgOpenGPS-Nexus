using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using Avalonia;
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
    private CameraRig? _camera;
    private Localizer? _localizer;
    private IDisposable? _boundsSubscription;
    private Double3? _pendingBoundsMin;
    private Double3? _pendingBoundsMax;
    private bool _needsFrame;
    private MappingWorkspaceViewModel? _workspace;

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

        if (MapViewport is not null)
        {
            _boundsSubscription = MapViewport.GetObservable(BoundsProperty)
                .Subscribe(_ => ApplyPendingFrame());
        }
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

        if (!ReferenceEquals(_workspace, workspace))
        {
            if (_workspace is not null)
            {
                _workspace.PropertyChanged -= WorkspaceOnPropertyChanged;
            }

            _workspace = workspace;
            _workspace.PropertyChanged += WorkspaceOnPropertyChanged;
        }

        var scene = new MapScene();
        var camera = new CameraRig();
        var localizer = new Localizer();
        MapViewport.Attach(scene, camera, localizer);

        _camera = camera;
        _localizer = localizer;
        scene.Add(new GridPassLayer
        {
            ZIndex = 0
        });

        var min = new Double3(double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity);
        var max = new Double3(double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity);

        void Include(Double3 point)
        {
            min = new Double3(
                Math.Min(min.X, point.X),
                Math.Min(min.Y, point.Y),
                Math.Min(min.Z, point.Z));
            max = new Double3(
                Math.Max(max.X, point.X),
                Math.Max(max.Y, point.Y),
                Math.Max(max.Z, point.Z));
        }

        foreach (var layer in workspace.Layers)
        {
            var featureZ = new List<VectorFeature>();
            foreach (var cell in layer.Cells)
            {
                var half = cell.SizeMeters / 2.0;
                var corners = new[]
                {
                    new Double3(cell.Center.X - half, cell.Center.Y - half, 0),
                    new Double3(cell.Center.X + half, cell.Center.Y - half, 0),
                    new Double3(cell.Center.X + half, cell.Center.Y + half, 0),
                    new Double3(cell.Center.X - half, cell.Center.Y + half, 0)
                };

                foreach (var corner in corners)
                {
                    Include(corner);
                }

                var fillColor = layer.Style.Evaluate(cell.Value);
                var outline = layer.Style.OutlineColor;
                featureZ.Add(new VectorFeature(
                    VectorFeatureType.Polygon,
                    corners,
                    fillColor: ToVector(fillColor, fillColor.A / 255f),
                    lineColor: outline.A > 0 ? ToVector(outline, outline.A / 255f) : null,
                    lineWidthMeters: 0.5f));
            }

            if (featureZ.Count == 0)
            {
                continue;
            }

            scene.Add(new VectorLayer(new InMemoryVectorSource(featureZ))
            {
                ZIndex = 100,
                Visible = layer.IsVisible
            });
        }

        foreach (var track in workspace.GuidanceTracks)
        {
            var points = track.Points.Select(p => new Double3(p.X, p.Y, 0)).ToArray();
            if (points.Length == 0)
            {
                continue;
            }

            foreach (var point in points)
            {
                Include(point);
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

        var posePoint = new Double3(workspace.VehiclePose.X, workspace.VehiclePose.Y, 0);
        Include(posePoint);

        if (!double.IsInfinity(min.X) && !double.IsInfinity(min.Y) &&
            !double.IsInfinity(max.X) && !double.IsInfinity(max.Y))
        {
            _pendingBoundsMin = min;
            _pendingBoundsMax = max;
            _needsFrame = true;
            ApplyPendingFrame();
        }

        _scene = scene;
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

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _boundsSubscription?.Dispose();
        _boundsSubscription = null;
        if (_workspace is not null)
        {
            _workspace.PropertyChanged -= WorkspaceOnPropertyChanged;
            _workspace = null;
        }
    }

    private void ApplyPendingFrame()
    {
        if (!_needsFrame || _camera is null || _localizer is null || MapViewport is null)
        {
            return;
        }

        if (_camera.ViewportPixels.X <= 1 || _camera.ViewportPixels.Y <= 1)
        {
            return;
        }

        if (_pendingBoundsMin is null || _pendingBoundsMax is null)
        {
            return;
        }

        _camera.FrameBounds(_pendingBoundsMin.Value, _pendingBoundsMax.Value, 12f);
        var center = _camera.Center;
        _localizer.ForceAnchor(new Double3(center.X, center.Y, 0));
        _needsFrame = false;
    }

    private void WorkspaceOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MappingWorkspaceViewModel.Layers) or
            nameof(MappingWorkspaceViewModel.GuidanceTracks))
        {
            RefreshScene();
        }
    }
}
