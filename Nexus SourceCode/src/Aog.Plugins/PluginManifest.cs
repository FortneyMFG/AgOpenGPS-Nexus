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
    /// Gets the capabilities advertised by the plugin bundle.
    /// </summary>
    [JsonPropertyName("supportedCapabilities")]
    public List<string> SupportedCapabilities { get; init; } = new();

    /// <summary>
    /// Gets the detailed capability/profile descriptors offered by the plugin.
    /// </summary>
    [JsonPropertyName("provides")]
    public PluginManifestProvides Provides { get; init; } = new();

    /// <summary>
    /// Gets the dependency declarations expressed by the plugin.
    /// </summary>
    [JsonPropertyName("requires")]
    public PluginManifestRequirements Requires { get; init; } = new();

    /// <summary>
    /// Gets the transports required for the plugin to function.
    /// </summary>
    [JsonPropertyName("requiredTransports")]
    public List<string> RequiredTransports { get; init; } = new();

    /// <summary>
    /// Gets the minimum Nexus runtime version compatible with the plugin.
    /// </summary>
    [JsonPropertyName("minimumRuntimeVersion")]
    public string? MinimumRuntimeVersion { get; init; }

    /// <summary>
    /// Gets the simulation providers declared by the plugin.
    /// </summary>
    [JsonPropertyName("simProviders")]
    public List<PluginSimProvider> SimulationProviders { get; init; } = new();

    /// <summary>
    /// Gets the lease declarations describing how the plugin acquires capabilities.
    /// </summary>
    [JsonPropertyName("leases")]
    public List<PluginCapabilityLease> CapabilityLeases { get; init; } = new();
}

/// <summary>Represents the declared outputs of a plugin bundle.</summary>
public sealed class PluginManifestProvides
{
    [JsonPropertyName("capabilities")]
    public List<PluginCapabilityDescriptor> Capabilities { get; init; } = new();

    [JsonPropertyName("profiles")]
    public List<PluginProfileDescriptor> Profiles { get; init; } = new();
}

/// <summary>Represents dependency declarations made by a plugin bundle.</summary>
public sealed class PluginManifestRequirements
{
    [JsonPropertyName("capabilities")]
    public List<PluginCapabilityRequirement> Capabilities { get; init; } = new();

    [JsonPropertyName("profiles")]
    public List<PluginProfileRequirement> Profiles { get; init; } = new();

    [JsonPropertyName("peerOf")]
    public List<PluginRelationshipRequirement> PeerOf { get; init; } = new();

    [JsonPropertyName("conflictsWith")]
    public List<PluginRelationshipRequirement> ConflictsWith { get; init; } = new();

    [JsonPropertyName("replaces")]
    public List<PluginRelationshipRequirement> Replaces { get; init; } = new();

    [JsonPropertyName("extends")]
    public List<PluginRelationshipRequirement> Extends { get; init; } = new();
}

/// <summary>Describes a capability exposed by a plugin.</summary>
public sealed class PluginCapabilityDescriptor
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("version")]
    public required string Version { get; init; }

    [JsonPropertyName("features")]
    public Dictionary<string, string> Features { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>Describes a conformance profile exposed by a plugin.</summary>
public sealed class PluginProfileDescriptor
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("version")]
    public required string Version { get; init; }
}

/// <summary>Declares a capability dependency.</summary>
public sealed class PluginCapabilityRequirement
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("range")]
    public required string Range { get; init; }

    [JsonPropertyName("features")]
    public Dictionary<string, string> Features { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("classification")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PluginDependencyClassification Classification { get; init; } = PluginDependencyClassification.Hard;
}

/// <summary>Declares a conformance profile dependency.</summary>
public sealed class PluginProfileRequirement
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("range")]
    public required string Range { get; init; }

    [JsonPropertyName("classification")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PluginDependencyClassification Classification { get; init; } = PluginDependencyClassification.Hard;
}

/// <summary>Declares relationships such as peer, conflict, or replace semantics.</summary>
public sealed class PluginRelationshipRequirement
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("range")]
    public string? Range { get; init; }

    [JsonPropertyName("reason")]
    public string? Reason { get; init; }

    [JsonPropertyName("classification")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PluginDependencyClassification Classification { get; init; } = PluginDependencyClassification.Hard;
}
