namespace Nexus.Sdk.UI.Avalonia;

/// <summary>
/// Entry point exposed by the host for map integrations.
/// </summary>
public interface IMapHost
{
    /// <summary>
    /// Gets the currently active map surface.
    /// </summary>
    IMapSurface ActiveSurface { get; }

    /// <summary>
    /// Registers a layer provider with the map host.
    /// </summary>
    /// <param name="provider">Layer provider to register.</param>
    void RegisterLayerProvider(ILayerProvider provider);

    /// <summary>
    /// Registers a tool provider with the map host.
    /// </summary>
    /// <param name="provider">Tool provider to register.</param>
    void RegisterToolProvider(IToolProvider provider);
}
