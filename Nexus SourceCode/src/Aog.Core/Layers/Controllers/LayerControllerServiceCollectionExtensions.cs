using System;
using System.Buffers;
using Aog.Core.Paths;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Aog.Core.Layers.Controllers;

/// <summary>
/// Service registration helpers for layer controller infrastructure.
/// </summary>
public static class LayerControllerServiceCollectionExtensions
{
    /// <summary>
    /// Adds layer controller services and optionally registers descriptors.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration callback for controller descriptors.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddLayerControllers(
        this IServiceCollection services,
        Action<LayerControllerRegistryBuilder>? configure = null)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        services.TryAddSingleton<TimeProvider>(TimeProvider.System);
        services.TryAddSingleton<ArrayPool<PlanarPoint>>(ArrayPool<PlanarPoint>.Shared);

        services.TryAddSingleton<ILayerControllerRegistry, LayerControllerRegistry>();
        services.TryAddSingleton<ILayerControllerRuntimeFactory, LayerControllerRuntimeFactory>();
        services.TryAddSingleton<LayerControllerRuntime>(
            provider => provider.GetRequiredService<ILayerControllerRuntimeFactory>().CreateRuntime());

        if (configure is not null)
        {
            services.ConfigureLayerControllers(configure);
        }

        return services;
    }

    /// <summary>
    /// Registers additional layer controller descriptors.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration callback.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection ConfigureLayerControllers(
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

        services.AddSingleton<ILayerControllerRegistryConfigurator>(
            new DelegateLayerControllerRegistryConfigurator(configure));
        return services;
    }
}
