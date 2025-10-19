using System.Threading;
using System.Threading.Tasks;

namespace Aog.Abstractions.Mapping;

/// <summary>
/// Provides mutation operations for layers.
/// </summary>
public interface ILayerCommands
{
    /// <summary>
    /// Creates a new layer.
    /// </summary>
    ValueTask<string> CreateAsync(LayerCreateRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an existing layer.
    /// </summary>
    ValueTask DeleteAsync(string layerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Appends a batch of edits to the layer journal.
    /// </summary>
    ValueTask AppendEditsAsync(string layerId, LayerEditBatch edits, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates layer metadata.
    /// </summary>
    ValueTask SetMetadataAsync(string layerId, LayerMetadataUpdate update, CancellationToken cancellationToken = default);
}
