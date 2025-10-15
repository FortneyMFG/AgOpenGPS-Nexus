using System;
using System.Collections.Generic;

namespace Aog.Core.Layers.Controllers;

/// <summary>
/// Builder used to register layer controller descriptors with the runtime.
/// </summary>
public sealed class LayerControllerRegistryBuilder
{
    private readonly IList<Func<IServiceProvider, LayerControllerDescriptor>> _descriptorFactories;

    internal LayerControllerRegistryBuilder(IList<Func<IServiceProvider, LayerControllerDescriptor>> descriptorFactories)
    {
        _descriptorFactories = descriptorFactories ?? throw new ArgumentNullException(nameof(descriptorFactories));
    }

    /// <summary>
    /// Registers a controller descriptor.
    /// </summary>
    /// <param name="descriptor">Descriptor describing the controller.</param>
    /// <returns>The builder instance.</returns>
    public LayerControllerRegistryBuilder AddController(LayerControllerDescriptor descriptor)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        _descriptorFactories.Add(_ => descriptor);
        return this;
    }

    /// <summary>
    /// Registers a controller descriptor produced by a factory.
    /// </summary>
    /// <param name="factory">Factory invoked when the registry is materialized.</param>
    /// <returns>The builder instance.</returns>
    public LayerControllerRegistryBuilder AddController(Func<IServiceProvider, LayerControllerDescriptor> factory)
    {
        if (factory is null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        _descriptorFactories.Add(factory);
        return this;
    }
}
