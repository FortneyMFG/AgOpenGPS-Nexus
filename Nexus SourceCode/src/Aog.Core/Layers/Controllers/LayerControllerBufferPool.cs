using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using Aog.Core.Paths;

namespace Aog.Core.Layers.Controllers;

/// <summary>
/// Provides pooled buffers used by layer controllers when building snapshots.
/// </summary>
public sealed class LayerControllerBufferPool
{
    private readonly ArrayPool<PlanarPoint> _positionsPool;

    /// <summary>
    /// Initializes a new instance of the <see cref="LayerControllerBufferPool"/> class.
    /// </summary>
    /// <param name="positionsInitialCapacity">Initial capacity used when allocating position buffers.</param>
    /// <param name="positionsPool">Optional pool used to rent position buffers.</param>
    public LayerControllerBufferPool(int positionsInitialCapacity = 32, ArrayPool<PlanarPoint>? positionsPool = null)
    {
        if (positionsInitialCapacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(positionsInitialCapacity));
        }

        PositionsInitialCapacity = positionsInitialCapacity;
        _positionsPool = positionsPool ?? ArrayPool<PlanarPoint>.Shared;
    }

    /// <summary>
    /// Gets the default capacity used for per-controller position buffers.
    /// </summary>
    public int PositionsInitialCapacity { get; }

    internal PooledPlanarPointCollection LeasePositions(IReadOnlyList<PlanarPoint> positions)
    {
        if (positions is null)
        {
            throw new ArgumentNullException(nameof(positions));
        }

        if (positions.Count == 0)
        {
            return PooledPlanarPointCollection.Empty;
        }

        var buffer = _positionsPool.Rent(positions.Count);
        for (var i = 0; i < positions.Count; i++)
        {
            buffer[i] = positions[i];
        }

        return new PooledPlanarPointCollection(_positionsPool, buffer, positions.Count);
    }

    internal PooledPlanarPointCollection LeaseSinglePosition(PlanarPoint position)
    {
        var buffer = _positionsPool.Rent(1);
        buffer[0] = position;
        return new PooledPlanarPointCollection(_positionsPool, buffer, 1);
    }

    internal sealed class PooledPlanarPointCollection : IReadOnlyList<PlanarPoint>, IDisposable
    {
        public static PooledPlanarPointCollection Empty { get; } = new(Array.Empty<PlanarPoint>());

        private readonly ArrayPool<PlanarPoint>? _pool;
        private PlanarPoint[]? _buffer;
        private readonly int _count;

        public PooledPlanarPointCollection(ArrayPool<PlanarPoint> pool, PlanarPoint[] buffer, int count)
        {
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));
            _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            if (count < 0 || count > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            _count = count;
        }

        private PooledPlanarPointCollection(PlanarPoint[] buffer)
        {
            _pool = null;
            _buffer = buffer;
            _count = buffer.Length;
        }

        public int Count => _count;

        public bool IsPooled => _pool is not null;

        public PlanarPoint this[int index]
        {
            get
            {
                if (_buffer is null)
                {
                    throw new ObjectDisposedException(nameof(PooledPlanarPointCollection));
                }

                if ((uint)index >= (uint)_count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                return _buffer[index];
            }
        }

        public IEnumerator<PlanarPoint> GetEnumerator()
        {
            for (var i = 0; i < _count; i++)
            {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Dispose()
        {
            if (_buffer is { } buffer && _pool is { } pool)
            {
                pool.Return(buffer, clearArray: false);
            }

            _buffer = null;
        }
    }
}
