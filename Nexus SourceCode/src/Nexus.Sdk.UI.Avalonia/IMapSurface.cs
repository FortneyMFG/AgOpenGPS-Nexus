using Avalonia.Controls;

namespace Nexus.Sdk.UI.Avalonia;

/// <summary>
/// Represents the active map surface provided by the host.
/// </summary>
public interface IMapSurface
{
    /// <summary>
    /// Gets the underlying Avalonia control hosting the surface.
    /// </summary>
    Control View { get; }

    /// <summary>
    /// Requests the surface to redraw.
    /// </summary>
    void Invalidate();
}
