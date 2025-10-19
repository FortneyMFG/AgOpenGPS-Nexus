using Avalonia.Controls;

namespace Nexus.Sdk.UI.Avalonia;

/// <summary>
/// Represents a map tool contributed by a plugin.
/// </summary>
public interface IMapTool : IDisposable
{
    /// <summary>
    /// Display name of the tool.
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Gets the view associated with the tool, if any.
    /// </summary>
    Control? View { get; }

    /// <summary>
    /// Activates the tool against the provided map surface.
    /// </summary>
    /// <param name="surface">Map surface supplied by the host.</param>
    /// <param name="cancellationToken">Cancellation token controlling activation.</param>
    ValueTask ActivateAsync(IMapSurface surface, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates the tool.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token controlling shutdown.</param>
    ValueTask DeactivateAsync(CancellationToken cancellationToken = default);
}
