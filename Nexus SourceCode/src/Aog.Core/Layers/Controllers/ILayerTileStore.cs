using System.Threading;
using System.Threading.Tasks;

namespace Aog.Core.Layers.Controllers;

/// <summary>
/// Abstraction representing a persistence backend for controller-derived TileStore payloads.
/// </summary>
public interface ILayerTileStore
{
    /// <summary>
    /// Persists a controller snapshot to the TileStore.
    /// </summary>
    /// <param name="request">Request describing the tile write operation.</param>
    /// <param name="cancellationToken">Token used to cancel the write operation.</param>
    ValueTask WriteAsync(LayerTileWriteRequest request, CancellationToken cancellationToken = default);
}
