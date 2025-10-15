using System.Collections.Generic;

namespace Aog.Core.Layers.Controllers;

/// <summary>
/// Provides access to the registered layer controller descriptors.
/// </summary>
public interface ILayerControllerRegistry
{
    /// <summary>
    /// Gets the descriptors registered with the controller registry.
    /// </summary>
    /// <returns>The ordered descriptors.</returns>
    IReadOnlyList<LayerControllerDescriptor> GetDescriptors();
}
