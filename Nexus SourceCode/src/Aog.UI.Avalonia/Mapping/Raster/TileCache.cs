using System;
using System.Collections.Generic;
using System.IO;

namespace Aog.UI.Avalonia.Mapping.Raster;

/// <summary>
/// Simple in-memory LRU cache for raster tiles. Disk persistence can be layered on later.
/// </summary>
public sealed class TileCache
{
    private readonly Dictionary<TileId, byte[]> _entries = new();
    private readonly LinkedList<TileId> _order = new();
    private readonly long _maxBytes;
    private long _currentBytes;

    public TileCache(long maxBytes = 128 * 1024 * 1024)
    {
        if (maxBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxBytes));
        }

        _maxBytes = maxBytes;
    }

    public bool TryGet(TileId id, out ReadOnlyMemory<byte> buffer)
    {
        if (_entries.TryGetValue(id, out var data))
        {
            Touch(id);
            buffer = data;
            return true;
        }

        buffer = default;
        return false;
    }

    public void Add(TileId id, Stream stream)
    {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        var bytes = ms.ToArray();
        Add(id, bytes);
    }

    public void Add(TileId id, byte[] bytes)
    {
        if (_entries.ContainsKey(id))
        {
            _currentBytes -= _entries[id].LongLength;
            _entries[id] = bytes;
            _currentBytes += bytes.LongLength;
            Touch(id);
            return;
        }

        _entries[id] = bytes;
        _order.AddFirst(id);
        _currentBytes += bytes.LongLength;
        Trim();
    }

    private void Touch(TileId id)
    {
        var node = _order.Find(id);
        if (node is null)
        {
            return;
        }

        _order.Remove(node);
        _order.AddFirst(node);
    }

    private void Trim()
    {
        while (_currentBytes > _maxBytes && _order.Last is not null)
        {
            var id = _order.Last.Value;
            _order.RemoveLast();
            if (_entries.Remove(id, out var data))
            {
                _currentBytes -= data.LongLength;
            }
        }
    }
}
