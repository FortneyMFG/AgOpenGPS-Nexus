using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aog.Plugins.Mapping.Core;
using Avalonia.OpenGL;

namespace Aog.Plugins.Mapping.Raster;

/// <summary>
/// Raster layer stub that schedules tile fetches and prepares GPU uploads (rendering pipeline TBD).
/// </summary>
public sealed class RasterTileLayer : IMapLayer
{
    private readonly ITileSource _source;
    private readonly TileCache _cache;
    private readonly WebMercator _mercator = new();
    private readonly HashSet<TileId> _requestedTiles = new();
    private readonly Queue<TileId> _pendingUploads = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly int _minZoom;
    private readonly int _maxZoom;
    private bool _visible = true;

    public RasterTileLayer(ITileSource source, int minZoom = 0, int maxZoom = 19, float opacity = 1f, TileCache? cache = null)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _minZoom = minZoom;
        _maxZoom = maxZoom;
        Opacity = Math.Clamp(opacity, 0f, 1f);
        _cache = cache ?? new TileCache();
    }

    public float Opacity { get; set; }

    public bool Visible
    {
        get => _visible;
        set => _visible = value;
    }

    public int ZIndex { get; set; }

    public void Load(GlInterface gl)
    {
        // Shaders and texture atlas preparation will be added in a future iteration.
    }

    public void UpdateCpu(double now)
    {
        // Placeholder: Determine visible tiles based on camera MPP and queue fetches.
    }

    public void UploadGpu(GlInterface gl)
    {
        // Placeholder: Convert ready tiles from _cache into GL textures and bind to sampler units.
    }

    public void Draw(GlInterface gl, in FrameCtx ctx)
    {
        // Rendering implementation will upload quads per visible tile. Stub keeps pipeline no-op.
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }

    private async Task FetchTileAsync(TileId tileId, CancellationToken cancellationToken)
    {
        if (_cache.TryGet(tileId, out _))
        {
            return;
        }

        if (!_requestedTiles.Add(tileId))
        {
            return;
        }

        var stream = await _source.OpenTileAsync(tileId, cancellationToken).ConfigureAwait(false);
        if (stream is null)
        {
            _requestedTiles.Remove(tileId);
            return;
        }

        await using (stream.ConfigureAwait(false))
        {
            _cache.Add(tileId, stream);
        }

        lock (_pendingUploads)
        {
            _pendingUploads.Enqueue(tileId);
        }
    }
}
