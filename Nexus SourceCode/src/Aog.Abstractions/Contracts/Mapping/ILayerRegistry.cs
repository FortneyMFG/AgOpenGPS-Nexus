using System.Collections.Generic;
using System.Threading;

namespace Aog.Abstractions.Mapping;

/// <summary>
/// Exposes the registered layers and change streams from the core runtime.
/// </summary>
public interface ILayerRegistry
{
    /// <summary>
    /// Gets a snapshot of registered layers.
    /// </summary>
    IReadOnlyList<LayerDescriptor> GetLayers();

    /// <summary>
    /// Opens a change stream for the specified layer.
    /// </summary>
    /// <param name="layerId">Target layer identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ILayerStream<LayerChange> Watch(string layerId, CancellationToken cancellationToken = default);
}
