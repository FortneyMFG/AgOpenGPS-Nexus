using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aog.Abstractions.Mapping;

/// <summary>
/// Resolves tile sources produced by the core runtime.
/// </summary>
public interface ITileCatalog
{
    /// <summary>
    /// Enumerates all known tile sources.
    /// </summary>
    IAsyncEnumerable<TileSource> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves a tile source for a specific layer.
    /// </summary>
    ValueTask<TileSource?> ResolveAsync(string layerId, CancellationToken cancellationToken = default);
}
