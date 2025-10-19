namespace Nexus.Sdk.UI.Avalonia;

/// <summary>
/// Provides layer factories to the host.
/// </summary>
public interface ILayerProvider
{
    /// <summary>
    /// Gets the layer factories exposed by the plugin.
    /// </summary>
    IEnumerable<LayerFactory> GetLayerFactories();
}
