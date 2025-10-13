using System;
using System.Collections.Generic;
using System.Linq;

namespace Aog.Core.Simulation;

/// <summary>
/// In-memory catalog that keeps track of simulation providers discovered at runtime.
/// Providers are registered by plugins and later composed into an execution graph.
/// </summary>
public sealed class SimulationCatalog
{
    private readonly Dictionary<string, SimulationProviderDescriptor> _providers =
        new(StringComparer.Ordinal);

    /// <summary>
    /// Registers a provider descriptor. Provider identifiers must be unique.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="descriptor"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the provider identifier is already registered.</exception>
    public void Register(SimulationProviderDescriptor descriptor)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (!_providers.TryAdd(descriptor.ProviderId, descriptor))
        {
            throw new InvalidOperationException(
                $"A provider with id '{descriptor.ProviderId}' has already been registered.");
        }
    }

    /// <summary>
    /// Returns a registered provider descriptor.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown when the provider identifier is unknown.</exception>
    public SimulationProviderDescriptor GetRequired(string providerId)
    {
        if (!_providers.TryGetValue(providerId, out var descriptor))
        {
            throw new KeyNotFoundException(
                $"Simulation provider '{providerId}' has not been registered.");
        }

        return descriptor;
    }

    /// <summary>
    /// Returns all registered providers.
    /// </summary>
    public IReadOnlyCollection<SimulationProviderDescriptor> Providers => _providers.Values.ToArray();

    /// <summary>
    /// Builds the execution graph for the registered providers.
    /// </summary>
    public SimulationGraph BuildGraph() => SimulationGraphBuilder.Build(_providers.Values);
}
