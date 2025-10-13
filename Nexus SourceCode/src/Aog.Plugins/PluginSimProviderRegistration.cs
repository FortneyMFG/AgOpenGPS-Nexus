using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;
using Aog.Core.Simulation;

namespace Aog.Plugins;

/// <summary>
/// Represents the simulation catalog registration produced from a plugin manifest entry.
/// </summary>
public sealed class PluginSimProviderRegistration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginSimProviderRegistration"/> class.
    /// </summary>
    /// <param name="pluginId">Identifier of the plugin declaring the provider.</param>
    /// <param name="provider">The manifest entry describing the provider.</param>
    /// <exception cref="ArgumentException">Thrown when identifiers are missing.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the provider metadata cannot be converted to a descriptor.</exception>
    public PluginSimProviderRegistration(string pluginId, PluginSimProvider provider)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
        {
            throw new ArgumentException("Plugin identifier must be provided.", nameof(pluginId));
        }

        ArgumentNullException.ThrowIfNull(provider);

        if (string.IsNullOrWhiteSpace(provider.ProviderId))
        {
            throw new ArgumentException("Provider identifier must be provided.", nameof(provider));
        }

        if (string.IsNullOrWhiteSpace(provider.Type))
        {
            throw new InvalidOperationException($"Simulation provider '{provider.ProviderId}' must declare an implementation type.");
        }

        var normalizedTopics = NormalizeTopics(pluginId, provider);
        Descriptor = new SimulationProviderDescriptor(provider.ProviderId, normalizedTopics);
        Topics = normalizedTopics.AsReadOnly();

        PluginId = pluginId;
        ProviderType = provider.Type;
        Description = provider.Description;
        Settings = CloneSettings(provider.Settings);
    }

    /// <summary>Gets the plugin identifier that declared the provider.</summary>
    public string PluginId { get; }

    /// <summary>Gets the identifier used to reference the provider.</summary>
    public string ProviderId => Descriptor.ProviderId;

    /// <summary>Gets the fully qualified type used to materialise the provider.</summary>
    public string ProviderType { get; }

    /// <summary>Gets the optional human-readable description supplied by the manifest.</summary>
    public string? Description { get; }

    /// <summary>Gets the topics published by the provider as declared in the manifest.</summary>
    public IReadOnlyList<string> Topics { get; }

    /// <summary>Gets the descriptor registered with the simulation catalog.</summary>
    public SimulationProviderDescriptor Descriptor { get; }

    /// <summary>Gets provider-specific settings copied from the manifest.</summary>
    public IReadOnlyDictionary<string, JsonElement> Settings { get; }

    private static List<string> NormalizeTopics(string pluginId, PluginSimProvider provider)
    {
        if (provider.Topics is null || provider.Topics.Count == 0)
        {
            throw new InvalidOperationException(
                $"Plugin '{pluginId}' provider '{provider.ProviderId}' must declare at least one topic.");
        }

        var normalized = new List<string>(provider.Topics.Count);
        var unique = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0; i < provider.Topics.Count; i++)
        {
            var topic = provider.Topics[i];
            if (string.IsNullOrWhiteSpace(topic))
            {
                throw new InvalidOperationException(
                    $"Plugin '{pluginId}' provider '{provider.ProviderId}' contains an empty topic name at index {i}.");
            }

            if (!unique.Add(topic))
            {
                throw new InvalidOperationException(
                    $"Plugin '{pluginId}' provider '{provider.ProviderId}' declares duplicate topic '{topic}'.");
            }

            normalized.Add(topic);
        }

        return normalized;
    }

    private static IReadOnlyDictionary<string, JsonElement> CloneSettings(Dictionary<string, JsonElement> settings)
    {
        if (settings is null || settings.Count == 0)
        {
            return new ReadOnlyDictionary<string, JsonElement>(new Dictionary<string, JsonElement>(0, StringComparer.Ordinal));
        }

        var copy = new Dictionary<string, JsonElement>(settings.Count, StringComparer.Ordinal);
        foreach (var pair in settings)
        {
            if (string.IsNullOrWhiteSpace(pair.Key))
            {
                throw new InvalidOperationException("Provider settings keys must be non-empty strings.");
            }

            copy[pair.Key] = pair.Value.Clone();
        }

        return new ReadOnlyDictionary<string, JsonElement>(copy);
    }
}
