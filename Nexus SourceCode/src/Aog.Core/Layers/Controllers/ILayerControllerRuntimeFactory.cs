namespace Aog.Core.Layers.Controllers;

/// <summary>
/// Factory responsible for creating configured <see cref="LayerControllerRuntime"/> instances.
/// </summary>
public interface ILayerControllerRuntimeFactory
{
    /// <summary>
    /// Creates a new <see cref="LayerControllerRuntime"/>.
    /// </summary>
    /// <returns>A configured runtime.</returns>
    LayerControllerRuntime CreateRuntime();
}
