using System;
using System.Collections.Generic;
using System.Threading;
using Avalonia.OpenGL;

namespace Aog.Plugins.Mapping.Core;

/// <summary>
/// Maintains an ordered collection of map layers and orchestrates their render cadence.
/// </summary>
public sealed class MapScene : IDisposable
{
    private readonly List<IMapLayer> _layers = new();
    private readonly ReaderWriterLockSlim _layerLock = new();

    /// <summary>
    /// Gets a snapshot of the current layer set.
    /// </summary>
    public IReadOnlyList<IMapLayer> Layers
    {
        get
        {
            _layerLock.EnterReadLock();
            try
            {
                return _layers.ToArray();
            }
            finally
            {
                _layerLock.ExitReadLock();
            }
        }
    }

    /// <summary>
    /// Adds a new layer to the scene and re-sorts by Z-index.
    /// </summary>
    public void Add(IMapLayer layer)
    {
        if (layer is null)
        {
            throw new ArgumentNullException(nameof(layer));
        }

        _layerLock.EnterWriteLock();
        try
        {
            _layers.Add(layer);
            _layers.Sort(static (a, b) => a.ZIndex.CompareTo(b.ZIndex));
        }
        finally
        {
            _layerLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Removes a previously registered layer from the scene.
    /// </summary>
    public bool Remove(IMapLayer layer)
    {
        if (layer is null)
        {
            return false;
        }

        _layerLock.EnterWriteLock();
        try
        {
            return _layers.Remove(layer);
        }
        finally
        {
            _layerLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Invokes the render pipeline for each visible layer.
    /// </summary>
    public void Render(GlInterface gl, in FrameCtx ctx)
    {
        IMapLayer[] snapshot;
        _layerLock.EnterReadLock();
        try
        {
            if (_layers.Count == 0)
            {
                return;
            }

            snapshot = _layers.ToArray();
        }
        finally
        {
            _layerLock.ExitReadLock();
        }

        foreach (var layer in snapshot)
        {
            if (!layer.Visible)
            {
                continue;
            }

            layer.UpdateCpu(ctx.TimeSeconds);
            layer.UploadGpu(gl);
            layer.Draw(gl, ctx);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        IMapLayer[] snapshot;
        _layerLock.EnterWriteLock();
        try
        {
            snapshot = _layers.ToArray();
            _layers.Clear();
        }
        finally
        {
            _layerLock.ExitWriteLock();
        }

        foreach (var layer in snapshot)
        {
            layer.Dispose();
        }

        _layerLock.Dispose();
    }
}
