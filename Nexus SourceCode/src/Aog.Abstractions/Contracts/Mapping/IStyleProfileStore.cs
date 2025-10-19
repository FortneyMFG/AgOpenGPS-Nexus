using System.Threading;
using System.Threading.Tasks;

namespace Aog.Abstractions.Mapping;

/// <summary>
/// Stores user-overridden style preferences for layers.
/// </summary>
public interface IStyleProfileStore
{
    /// <summary>
    /// Gets the stored profile for the specified layer, if any.
    /// </summary>
    ValueTask<LayerStyleProfile?> GetAsync(string layerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a profile for the specified layer.
    /// </summary>
    ValueTask SetAsync(string layerId, LayerStyleProfile profile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the stored profile for the specified layer.
    /// </summary>
    ValueTask ClearAsync(string layerId, CancellationToken cancellationToken = default);
}
