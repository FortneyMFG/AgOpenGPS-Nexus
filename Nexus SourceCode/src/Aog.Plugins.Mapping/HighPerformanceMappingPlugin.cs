using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aog.UI.Avalonia.Mapping.Core;
using Aog.UI.Avalonia.Mapping.Raster;
using Aog.UI.Avalonia.Mapping.Vector;
using Aog.UI.Avalonia.Models;
using Nexus.Sdk.UI.Avalonia;
using Newtonsoft.Json.Linq;

namespace Aog.Plugins.Mapping;

/// <summary>
/// High-performance mapping plugin providing satellite imagery, boundaries, and equipment visualization.
/// </summary>
public sealed class HighPerformanceMappingPlugin : IPluginEntryPoint
{
    private readonly IMapHost _mapHost;
    private readonly ILayerRegistry _layerRegistry;
    private MapScene? _scene;
    private Dictionary<string, RasterTileLayer> _rasterLayers = new();
    private Dictionary<string, VectorLayer> _vectorLayers = new();

    public HighPerformanceMappingPlugin(
        IMapHost mapHost,
        ILayerRegistry layerRegistry)
    {
        _mapHost = mapHost ?? throw new ArgumentNullException(nameof(mapHost));
        _layerRegistry = layerRegistry ?? throw new ArgumentNullException(nameof(layerRegistry));
    }

    public async Task InitializeAsync(JObject config)
    {
        // Initialize scene
        _scene = new MapScene();
        
        // Configure raster layers from settings
        if (config["raster"] is JObject rasterConfig)
        {
            foreach (var layerConfig in rasterConfig["layers"]?.ToObject<IList<RasterLayerConfig>>() ?? Array.Empty<RasterLayerConfig>())
            {
                var layer = CreateRasterLayer(layerConfig);
                if (layer != null)
                {
                    _rasterLayers[layerConfig.Name] = layer;
                    _scene.Add(layer);
                }
            }
        }

        // Add core vector layers
        _vectorLayers["boundary"] = new VectorLayer(
            new EnuFeatureSource(), // Will be populated by field service
            fill: new Styles.FillSymbolizer(0.15f, new(1f, 1f, 0f, 1f)),
            line: new Styles.LineSymbolizer(1.0f, 1f, new(1f, 1f, 0f, 1f))) 
        { 
            ZIndex = 200 
        };

        _vectorLayers["keepouts"] = new VectorLayer(
            new EnuFeatureSource(),
            fill: new Styles.FillSymbolizer(0.12f, new(1f, 0f, 0f, 1f)),
            line: new Styles.LineSymbolizer(1.2f, 1f, new(1f, 0f, 0f, 1f)))
        {
            ZIndex = 210
        };

        foreach (var layer in _vectorLayers.Values)
        {
            _scene.Add(layer);
        }

        // Add vehicle pass layer
        _scene.Add(new VehiclePassLayer() { ZIndex = 300 });

        // Register with map host
        _mapHost.ActiveSurface.AttachScene(_scene);
    }

    public Task ShutdownAsync()
    {
        if (_scene != null)
        {
            _mapHost.ActiveSurface.DetachScene(_scene);
            _scene = null;
        }

        return Task.CompletedTask;
    }

    private RasterTileLayer? CreateRasterLayer(RasterLayerConfig config)
    {
        ITileSource source = config.Type.ToLowerInvariant() switch
        {
            "xyz" => new XyzTileSource(
                config.Template,
                config.Subdomains ?? Array.Empty<string>(),
                config.ApiKey),
                
            "mbtiles" => new MbTilesSource(config.Path!),
            
            _ => throw new NotSupportedException($"Unsupported tile source type: {config.Type}")
        };

        var layer = new RasterTileLayer(
            source,
            config.MinZoom,
            config.MaxZoom,
            cacheSizeMB: 256)
        {
            ZIndex = config.ZIndex,
            Visible = true,
            Opacity = config.Opacity
        };

        return layer;
    }

    private class RasterLayerConfig
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public string Template { get; set; } = "";
        public string[]? Subdomains { get; set; }
        public string? ApiKey { get; set; }
        public string? Path { get; set; }
        public int MinZoom { get; set; }
        public int MaxZoom { get; set; } = 19;
        public float Opacity { get; set; } = 1.0f;
        public int ZIndex { get; set; } = 100;
    }
}