using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Aog.Core.Layers.Controllers;

/// <summary>
/// Dependency injection helpers for configuring layer controllers.
/// </summary>
public static class LayerControllerServiceCollectionExtensions
{
    /// <summary>
    /// Registers layer controller services and descriptors.
    /// </summary>
    /// <param name="services">Service collection to configure.</param>
    /// <param name="configure">Delegate used to register controller descriptors.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddLayerControllerRuntime(
        this IServiceCollection services,
        Action<LayerControllerRegistryBuilder> configure)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var builder = new LayerControllerRegistryBuilder();
        configure(builder);
        var descriptors = builder.Build();

        services.AddSingleton<LayerControllerBufferPool>();
        services.AddSingleton<IReadOnlyList<LayerControllerDescriptor>>(descriptors);
        services.AddSingleton(provider =>
        {
            var timeProvider = provider.GetService<TimeProvider>();
            var bufferPool = provider.GetRequiredService<LayerControllerBufferPool>();
            return new LayerControllerRuntime(descriptors, timeProvider, bufferPool);
        });

        return services;
    }
}
