using System;
using System.Collections.Generic;
using System.Linq;
using Aog.Core.Simulation;

namespace Aog.Plugins;

/// <summary>
/// Provides helpers for registering simulation providers declared in plugin manifests.
/// </summary>
public static class PluginManifestSimulationExtensions
{
    /// <summary>
    /// Registers the providers from the manifest with the supplied simulation catalog.
    /// </summary>
    /// <param name="manifest">The manifest containing provider definitions.</param>
    /// <param name="catalog">The simulation catalog that will receive the registrations.</param>
    /// <returns>The registrations produced for each provider entry.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="manifest"/> or <paramref name="catalog"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the manifest is missing provider metadata.</exception>
    public static IReadOnlyList<PluginSimProviderRegistration> RegisterSimulationProviders(
        this PluginManifest manifest,
        SimulationCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(catalog);

        if (string.IsNullOrWhiteSpace(manifest.Id))
        {
            throw new InvalidOperationException("Plugin manifest must include an id before registering providers.");
        }

        if (manifest.SimulationProviders is null || manifest.SimulationProviders.Count == 0)
        {
            throw new InvalidOperationException($"Plugin '{manifest.Id}' does not declare any simulation providers.");
        }

        var registrations = new List<PluginSimProviderRegistration>(manifest.SimulationProviders.Count);
        var manifestIds = new HashSet<string>(StringComparer.Ordinal);
        var catalogIds = new HashSet<string>(catalog.Providers.Select(provider => provider.ProviderId), StringComparer.Ordinal);

        foreach (var provider in manifest.SimulationProviders)
        {
            if (provider is null)
            {
                throw new InvalidOperationException($"Plugin '{manifest.Id}' contains a null simulation provider entry.");
            }

            var registration = new PluginSimProviderRegistration(manifest.Id, provider);
            if (!manifestIds.Add(registration.ProviderId))
            {
                throw new InvalidOperationException(
                    $"Plugin '{manifest.Id}' declares duplicate simulation provider '{registration.ProviderId}'.");
            }

            if (!catalogIds.Add(registration.ProviderId))
            {
                throw new InvalidOperationException(
                    $"Simulation provider '{registration.ProviderId}' from plugin '{manifest.Id}' conflicts with an existing provider.");
            }

            registrations.Add(registration);
        }

        foreach (var registration in registrations)
        {
            catalog.Register(registration.Descriptor);
        }

        return registrations;
    }
}
