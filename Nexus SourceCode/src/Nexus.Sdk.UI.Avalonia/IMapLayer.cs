namespace Nexus.Sdk.UI.Avalonia;

/// <summary>
/// Represents a map layer contributed by a plugin.
/// </summary>
public interface IMapLayer : IDisposable
{
    /// <summary>
    /// Identifier for the layer.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Called by the host when the layer is attached to the active surface.
    /// </summary>
    /// <param name="surface">Target map surface.</param>
    void Attach(IMapSurface surface);

    /// <summary>
    /// Called by the host when the layer is detached from the active surface.
    /// </summary>
    void Detach();
}
