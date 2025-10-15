using System;
using System.Threading;
using System.Threading.Tasks;

namespace Aog.Core.Layers.Controllers;

/// <summary>
/// Converts layer controller snapshots into TileStore write requests.
/// </summary>
public sealed class LayerControllerTileWriter
{
    private readonly ILayerTileStore _tileStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="LayerControllerTileWriter"/> class.
    /// </summary>
    /// <param name="tileStore">TileStore sink used to persist controller outputs. When <c>null</c>, a no-op sink is used.</param>
    public LayerControllerTileWriter(ILayerTileStore? tileStore = null)
    {
        _tileStore = tileStore ?? NullTileStore.Instance;
    }

    /// <summary>
    /// Writes the supplied snapshot to the TileStore.
    /// </summary>
    /// <param name="snapshot">Snapshot captured by the controller runtime.</param>
    /// <param name="cancellationToken">Token used to cancel the write operation.</param>
    public ValueTask WriteAsync(LayerControllerSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var request = LayerTileWriteRequest.FromSnapshot(snapshot);
        return _tileStore.WriteAsync(request, cancellationToken);
    }

    private sealed class NullTileStore : ILayerTileStore
    {
        public static NullTileStore Instance { get; } = new();

        public ValueTask WriteAsync(LayerTileWriteRequest request, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }
}
