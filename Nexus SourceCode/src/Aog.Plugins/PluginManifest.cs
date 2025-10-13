using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aog.Plugins;

/// <summary>
/// Represents the structured metadata describing a Nexus plugin bundle.
/// </summary>
public sealed class PluginManifest
{
    private static readonly StringComparer ApiComparer = StringComparer.Ordinal;

    /// <summary>
    /// Gets the semantic version of the manifest schema that produced this document.
    /// </summary>
    [JsonPropertyName("schemaVersion")]
    public required string SchemaVersion { get; init; }

    /// <summary>
    /// Gets the unique identifier for the plugin (reverse-DNS or similar).
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// Gets the human readable name of the plugin.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Gets the semantic version of the plugin implementation.
    /// </summary>
    [JsonPropertyName("version")]
    public required string Version { get; init; }

    /// <summary>
    /// Gets an optional long form description of the plugin.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>
    /// Gets the API versions required by the plugin, keyed by API name.
    /// </summary>
    [JsonPropertyName("requiredApis")]
    public Dictionary<string, string> RequiredApis { get; init; } = new(ApiComparer);

    /// <summary>
    /// Gets arbitrary plugin-level settings captured in the manifest.
    /// </summary>
    [JsonPropertyName("settings")]
    public Dictionary<string, JsonElement> Settings { get; init; } = new();

    /// <summary>
    /// Gets the simulation providers declared by the plugin.
    /// </summary>
    [JsonPropertyName("simProviders")]
    public List<PluginSimProvider> SimulationProviders { get; init; } = new();
}
