using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aog.Plugins;

/// <summary>
/// Describes a simulation provider bundled with a plugin manifest.
/// </summary>
public sealed class PluginSimProvider
{
    /// <summary>
    /// Gets the unique provider identifier referenced by the simulation graph.
    /// </summary>
    [JsonPropertyName("providerId")]
    public required string ProviderId { get; init; }

    /// <summary>
    /// Gets the fully-qualified type that implements the provider.
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <summary>
    /// Gets an optional human-readable description of the provider.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>
    /// Gets the topic identifiers published by this provider.
    /// </summary>
    [JsonPropertyName("topics")]
    public List<string> Topics { get; init; } = new();

    /// <summary>
    /// Gets provider-specific settings declared in the manifest.
    /// </summary>
    [JsonPropertyName("settings")]
    public Dictionary<string, JsonElement> Settings { get; init; } = new();
}
