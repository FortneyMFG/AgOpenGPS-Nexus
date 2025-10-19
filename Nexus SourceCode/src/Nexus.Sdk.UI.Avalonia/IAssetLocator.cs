namespace Nexus.Sdk.UI.Avalonia;

/// <summary>
/// Resolves plugin assets packaged within the plugin archive.
/// </summary>
public interface IAssetLocator
{
    /// <summary>
    /// Attempts to open an asset stream.
    /// </summary>
    /// <param name="assetPath">Relative asset path (e.g. "assets/icon.png").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A stream if the asset exists; otherwise <c>null</c>.</returns>
    ValueTask<Stream?> OpenAssetAsync(string assetPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves an Avalonia resource URI for the provided asset path.
    /// </summary>
    /// <param name="assetPath">Relative asset path.</param>
    /// <returns>An Avalonia resource URI that can be consumed by XAML.</returns>
    Uri GetResourceUri(string assetPath);
}
