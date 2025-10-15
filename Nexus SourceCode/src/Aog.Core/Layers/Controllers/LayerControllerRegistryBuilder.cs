using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Aog.Core.Layers.Controllers;

/// <summary>
/// Builder used to register layer controller descriptors for dependency injection.
/// </summary>
public sealed class LayerControllerRegistryBuilder
{
    private readonly List<LayerControllerDescriptor> _descriptors = new();
    private readonly HashSet<string> _controllerIds = new(StringComparer.Ordinal);
    private bool _built;

    /// <summary>
    /// Adds a controller descriptor to the registry.
    /// </summary>
    /// <param name="descriptor">Descriptor to register.</param>
    /// <returns>The current builder instance.</returns>
    public LayerControllerRegistryBuilder Add(LayerControllerDescriptor descriptor)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        EnsureNotBuilt();

        if (!_controllerIds.Add(descriptor.ControllerId))
        {
            throw new InvalidOperationException($"Controller '{descriptor.ControllerId}' is already registered.");
        }

        _descriptors.Add(descriptor);
        return this;
    }

    /// <summary>
    /// Adds a controller descriptor to the registry using individual parameter values.
    /// </summary>
    public LayerControllerRegistryBuilder Add(
        string controllerId,
        string layerId,
        TimeSpan snapshotCadence,
        LayerAggregationStrategy aggregationStrategy)
    {
        return Add(new LayerControllerDescriptor(controllerId, layerId, snapshotCadence, aggregationStrategy));
    }

    /// <summary>
    /// Builds an immutable set of descriptors for dependency injection.
    /// </summary>
    /// <returns>A read-only list of registered descriptors.</returns>
    public IReadOnlyList<LayerControllerDescriptor> Build()
    {
        EnsureNotBuilt();

        if (_descriptors.Count == 0)
        {
            throw new InvalidOperationException("At least one layer controller descriptor must be registered.");
        }

        _built = true;
        return new ReadOnlyCollection<LayerControllerDescriptor>(_descriptors.ToArray());
    }

    private void EnsureNotBuilt()
    {
        if (_built)
        {
            throw new InvalidOperationException("The registry has already been built.");
        }
    }
}
