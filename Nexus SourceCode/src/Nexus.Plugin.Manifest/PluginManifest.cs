using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Nexus.Plugin.Manifest;

/// <summary>
/// Represents the authoritative manifest for a Nexus plugin package.
/// </summary>
public sealed class PluginManifest
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// Plugin identifier (reverse-DNS format).
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Human readable plugin name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Semantic version of the plugin.
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// Supported SDK version range (npm-style).
    /// </summary>
    public required string SdkVersion { get; init; }

    /// <summary>
    /// Optional dependency constraints expressed as plugin id =&gt; semver range.
    /// </summary>
    public IDictionary<string, string> Requires { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Entry point type names contributed by the plugin.
    /// </summary>
    public PluginManifestEntrypoints Entrypoints { get; init; } = new();

    /// <summary>
    /// Declared plugin capabilities (window, blocks, tools, layers, etc.).
    /// </summary>
    public IList<string> Capabilities { get; init; } = new List<string>();

    /// <summary>
    /// Additional asset metadata.
    /// </summary>
    public IDictionary<string, string> Assets { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Optional update feed metadata.
    /// </summary>
    public PluginManifestUpdate? Update { get; init; }

    /// <summary>
    /// Optional permission metadata requested by the plugin.
    /// </summary>
    public PluginManifestPermissions Permissions { get; init; } = new();

    /// <summary>
    /// Serializes the manifest to JSON.
    /// </summary>
    public string ToJson() => JsonSerializer.Serialize(this, SerializerOptions);

    /// <summary>
    /// Deserializes a manifest from JSON.
    /// </summary>
    /// <param name="json">Manifest JSON payload.</param>
    public static PluginManifest FromJson(string json)
    {
        var manifest = JsonSerializer.Deserialize<PluginManifest>(json, SerializerOptions);
        if (manifest is null)
        {
            throw new InvalidOperationException("Unable to parse plugin manifest.");
        }

        manifest.Validate();
        return manifest;
    }

    /// <summary>
    /// Validates the manifest and throws if an invariant is violated.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Id))
        {
            throw new InvalidOperationException("Plugin id is required.");
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new InvalidOperationException("Plugin name is required.");
        }

        if (string.IsNullOrWhiteSpace(Version))
        {
            throw new InvalidOperationException("Plugin version is required.");
        }

        if (string.IsNullOrWhiteSpace(SdkVersion))
        {
            throw new InvalidOperationException("Plugin sdkVersion is required.");
        }

        if (Capabilities.Count == 0)
        {
            throw new InvalidOperationException("At least one capability must be declared.");
        }
    }

    /// <summary>
    /// Generates the JSON schema that documents the manifest contract.
    /// </summary>
    public static JsonObject GenerateJsonSchema()
    {
        return new JsonObject
        {
            ["$schema"] = "https://json-schema.org/draft/2020-12/schema",
            ["title"] = "Nexus Plugin Manifest",
            ["type"] = "object",
            ["required"] = new JsonArray("id", "name", "version", "sdkVersion", "entrypoints", "capabilities"),
            ["properties"] = new JsonObject
            {
                ["id"] = new JsonObject
                {
                    ["type"] = "string",
                    ["pattern"] = "^[a-z0-9]+(\\.[a-z0-9-]+)+$",
                    ["description"] = "Unique plugin identifier expressed in reverse-DNS notation."
                },
                ["name"] = new JsonObject
                {
                    ["type"] = "string",
                    ["minLength"] = 1,
                    ["description"] = "Human readable plugin name."
                },
                ["version"] = new JsonObject
                {
                    ["type"] = "string",
                    ["pattern"] = "^\\d+\\.\\d+\\.\\d+(-[0-9A-Za-z.-]+)?$",
                    ["description"] = "Semantic version of the plugin package."
                },
                ["sdkVersion"] = new JsonObject
                {
                    ["type"] = "string",
                    ["description"] = "Semantic version range describing the supported Nexus SDK."
                },
                ["requires"] = new JsonObject
                {
                    ["type"] = "object",
                    ["additionalProperties"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["description"] = "Semantic version range for the required plugin."
                    }
                },
                ["entrypoints"] = new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["core"] = new JsonObject { ["type"] = new JsonArray("string", "null") },
                        ["ui"] = new JsonObject { ["type"] = new JsonArray("string", "null") },
                        ["agio"] = new JsonObject { ["type"] = new JsonArray("string", "null") }
                    },
                    ["required"] = new JsonArray("core", "ui", "agio"),
                    ["additionalProperties"] = false
                },
                ["capabilities"] = new JsonObject
                {
                    ["type"] = "array",
                    ["items"] = new JsonObject { ["type"] = "string" },
                    ["minItems"] = 1,
                    ["uniqueItems"] = true
                },
                ["assets"] = new JsonObject
                {
                    ["type"] = "object",
                    ["additionalProperties"] = new JsonObject { ["type"] = "string" }
                },
                ["update"] = new JsonObject
                {
                    ["type"] = new JsonArray("object", "null"),
                    ["properties"] = new JsonObject
                    {
                        ["feed"] = new JsonObject { ["type"] = new JsonArray("string", "null") }
                    },
                    ["additionalProperties"] = false
                },
                ["permissions"] = new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["network"] = new JsonObject { ["type"] = "boolean" },
                        ["serial"] = new JsonObject { ["type"] = "boolean" }
                    },
                    ["additionalProperties"] = false
                }
            },
            ["additionalProperties"] = false
        };
    }

    /// <summary>
    /// Writes the manifest schema to a stream.
    /// </summary>
    /// <param name="stream">Target stream.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async ValueTask WriteSchemaAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        var schema = GenerateJsonSchema();
        await using var writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true);
        await writer.WriteAsync(schema.ToJsonString(new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true,
        }).AsMemory(), cancellationToken);
        await writer.FlushAsync();
    }
}

/// <summary>
/// Declares plugin entrypoint types.
/// </summary>
public sealed class PluginManifestEntrypoints
{
    public string? Core { get; init; }
    public string? Ui { get; init; }
    public string? Agio { get; init; }
}

/// <summary>
/// Update metadata for a plugin.
/// </summary>
public sealed class PluginManifestUpdate
{
    public string? Feed { get; init; }
}

/// <summary>
/// Permissions requested by the plugin at install time.
/// </summary>
public sealed class PluginManifestPermissions
{
    public bool Network { get; init; }
    public bool Serial { get; init; }
}
