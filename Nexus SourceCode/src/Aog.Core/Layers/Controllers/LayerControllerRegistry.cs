using System;
using System.Collections.Generic;

namespace Aog.Core.Layers.Controllers;

internal sealed class LayerControllerRegistry : ILayerControllerRegistry
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IReadOnlyList<Func<IServiceProvider, LayerControllerDescriptor>> _descriptorFactories;
    private readonly object _gate = new();

    private IReadOnlyList<LayerControllerDescriptor>? _cachedDescriptors;

    public LayerControllerRegistry(
        IServiceProvider serviceProvider,
        IEnumerable<ILayerControllerRegistryConfigurator> configurators)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        if (configurators is null)
        {
            throw new ArgumentNullException(nameof(configurators));
        }

        var configuration = new LayerControllerRegistryConfiguration();
        var builder = new LayerControllerRegistryBuilder(configuration.DescriptorFactories);

        foreach (var configurator in configurators)
        {
            configurator.Configure(builder);
        }

        _descriptorFactories = configuration.DescriptorFactories.ToArray();
    }

    public IReadOnlyList<LayerControllerDescriptor> GetDescriptors()
    {
        if (_cachedDescriptors is { } descriptors)
        {
            return descriptors;
        }

        lock (_gate)
        {
            if (_cachedDescriptors is { } existing)
            {
                return existing;
            }

            var materialized = new List<LayerControllerDescriptor>(_descriptorFactories.Count);
            var identifiers = new HashSet<string>(StringComparer.Ordinal);

            foreach (var factory in _descriptorFactories)
            {
                var descriptor = factory(_serviceProvider);

                if (descriptor is null)
                {
                    throw new InvalidOperationException("Layer controller registrations must not produce null descriptors.");
                }

                if (!identifiers.Add(descriptor.ControllerId))
                {
                    throw new InvalidOperationException(
                        $"Duplicate layer controller identifier '{descriptor.ControllerId}' detected.");
                }

                materialized.Add(descriptor);
            }

            if (materialized.Count == 0)
            {
                throw new InvalidOperationException("At least one layer controller must be registered.");
            }

            _cachedDescriptors = materialized.AsReadOnly();
            return _cachedDescriptors;
        }
    }

    private sealed class LayerControllerRegistryConfiguration
    {
        public List<Func<IServiceProvider, LayerControllerDescriptor>> DescriptorFactories { get; } = new();
    }
}

internal interface ILayerControllerRegistryConfigurator
{
    void Configure(LayerControllerRegistryBuilder builder);
}

internal sealed class DelegateLayerControllerRegistryConfigurator : ILayerControllerRegistryConfigurator
{
    private readonly Action<LayerControllerRegistryBuilder> _configure;

    public DelegateLayerControllerRegistryConfigurator(Action<LayerControllerRegistryBuilder> configure)
    {
        _configure = configure ?? throw new ArgumentNullException(nameof(configure));
    }

    public void Configure(LayerControllerRegistryBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        _configure(builder);
    }
}
