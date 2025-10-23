using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Aog.UI.Avalonia.Mapping.Raster;

/// <summary>
/// Provides access to raster tiles for the mapping scene.
/// </summary>
public interface ITileSource
{
    /// <summary>
    /// Attempts to open the requested tile; returns <c>null</c> if the tile is unavailable.
    /// </summary>
    ValueTask<Stream?> OpenTileAsync(TileId tileId, CancellationToken cancellationToken = default);
}
