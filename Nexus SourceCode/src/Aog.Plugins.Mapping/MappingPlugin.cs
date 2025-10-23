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

        var (defaultLayers, vehicleLayer) = BuildDefaultLayers();
        foreach (var binding in defaultLayers)
        {
            scene.Add(binding.Layer);
        }

        ApplyLayerMetadata(hostServices, defaultLayers);

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

    private static (IReadOnlyList<DefaultLayerBinding> Layers, VehiclePassLayer VehicleLayer) BuildDefaultLayers()
    {
        var vehicleLayer = new VehiclePassLayer(() => new PoseSample(0, 0, 0, 0))
        {
            ZIndex = 300,
        };

        var layers = new DefaultLayerBinding[]
        {
            new DefaultLayerBinding("grid", "core.grid", new GridPassLayer { ZIndex = 0 }),
            new DefaultLayerBinding("vehicle", "core.vehicle", vehicleLayer),
            new DefaultLayerBinding("hud", "core.hud", new DebugHudLayer { ZIndex = int.MaxValue }),
        };

        return (layers, vehicleLayer);
    }

    private static void ApplyLayerMetadata(IMapHostServices hostServices, IReadOnlyList<DefaultLayerBinding> bindings)
    {
        if (bindings.Count == 0)
        {
            return;
        }

        var descriptors = hostServices.LayerRegistry.GetLayers();
        for (var i = 0; i < descriptors.Count; i++)
        {
            var descriptor = descriptors[i];
            for (var j = 0; j < bindings.Count; j++)
            {
                var binding = bindings[j];
                if (!string.Equals(binding.LayerType, descriptor.LayerType, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                binding.Layer.ZIndex = descriptor.ZIndexHint;
                binding.Layer.Visible = descriptor.Visible;
                ApplyStyleProfile(binding.Layer, descriptor.StyleDefaults);
                binding.Bind(descriptor.Id);
            }
        }

        for (var j = 0; j < bindings.Count; j++)
        {
            var binding = bindings[j];
            var effectiveLayerId = binding.GetEffectiveLayerId();
            var profile = hostServices.StyleProfiles.GetAsync(effectiveLayerId).GetAwaiter().GetResult();
            ApplyStyleProfile(binding.Layer, profile);
        }
    }

    private static void ApplyStyleProfile(IMapLayer layer, LayerStyleProfile? profile)
    {
        if (profile is null)
        {
            return;
        }

        if (profile.ZIndex.HasValue)
        {
            layer.ZIndex = profile.ZIndex.Value;
        }

        if (profile.Visible.HasValue)
        {
            layer.Visible = profile.Visible.Value;
        }
    }

    private sealed class DefaultLayerBinding
    {
        private string? _layerId;

        public DefaultLayerBinding(string layerType, string fallbackLayerId, IMapLayer layer)
        {
            LayerType = layerType ?? throw new ArgumentNullException(nameof(layerType));
            FallbackLayerId = fallbackLayerId ?? throw new ArgumentNullException(nameof(fallbackLayerId));
            Layer = layer ?? throw new ArgumentNullException(nameof(layer));
        }

        public string LayerType { get; }

        public string FallbackLayerId { get; }

        public IMapLayer Layer { get; }

        public void Bind(string layerId)
        {
            _layerId = layerId;
        }

        public string GetEffectiveLayerId()
            => _layerId ?? FallbackLayerId;
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
