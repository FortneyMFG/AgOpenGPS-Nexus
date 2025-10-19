using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Aog.Abstractions.Mapping;
using Aog.Plugins.Mapping.Core;
using Aog.Plugins.Mapping.HUD;
using Aog.Plugins.Mapping.Input;
using Aog.Plugins.Mapping.Rendering;
using Avalonia;
using Avalonia.Controls;

namespace Aog.Plugins.Mapping;

/// <summary>
/// Default implementation of the mapping plugin that wires the rendering stack to the host services.
/// </summary>
public sealed class MappingPlugin : IMappingPlugin
{
    public Control CreateMapView(IMapHostServices hostServices)
    {
        ArgumentNullException.ThrowIfNull(hostServices);

        var mapView = new MapView();
        var camera = new CameraRig();
        var localizer = new Localizer();
        var scene = new MapScene();
        mapView.Attach(scene, camera, localizer);

        var (layers, vehicleLayer) = BuildDefaultLayers();
        foreach (var layer in layers)
        {
            scene.Add(layer);
        }

        var disposables = new List<IDisposable>();
        var cts = new CancellationTokenSource();

        PoseSample latestPose = new(0, 0, 0, 0);
        vehicleLayer.UpdatePoseProvider(() => Volatile.Read(ref latestPose));

        var posePump = StartPosePump(hostServices, pose => Volatile.Write(ref latestPose, pose), cts.Token);
        disposables.Add(hostServices.FieldContext.Subscribe(_ => { }));

        EventHandler<VisualTreeAttachmentEventArgs>? onAttached = null;
        EventHandler<VisualTreeAttachmentEventArgs>? onDetached = null;

        onAttached = (_, _) =>
        {
            var bounds = mapView.Bounds;
            camera.Resize((float)Math.Max(bounds.Width, 1), (float)Math.Max(bounds.Height, 1));
        };

        onDetached = (_, _) =>
        {
            mapView.AttachedToVisualTree -= onAttached;
            mapView.DetachedFromVisualTree -= onDetached;

            cts.Cancel();
            foreach (var disposable in disposables)
            {
                disposable.Dispose();
            }

            cts.Dispose();
        };

        mapView.AttachedToVisualTree += onAttached;
        mapView.DetachedFromVisualTree += onDetached;

        _ = posePump.ContinueWith(t =>
        {
            _ = t.Exception;
        }, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);

        return mapView;
    }

    private static (IReadOnlyList<IMapLayer> Layers, VehiclePassLayer VehicleLayer) BuildDefaultLayers()
    {
        var vehicleLayer = new VehiclePassLayer(() => new PoseSample(0, 0, 0, 0))
        {
            ZIndex = 300,
        };

        var layers = new IMapLayer[]
        {
            new GridPassLayer { ZIndex = 0 },
            vehicleLayer,
            new DebugHudLayer { ZIndex = int.MaxValue },
        };

        return (layers, vehicleLayer);
    }

    private static Task StartPosePump(IMapHostServices hostServices, Action<PoseSample> onPose, CancellationToken cancellationToken)
    {
        return Task.Run(async () =>
        {
            try
            {
                await foreach (var sample in hostServices.PoseStream(cancellationToken).WithCancellation(cancellationToken))
                {
                    onPose(sample);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown.
            }
        }, cancellationToken);
    }
}
