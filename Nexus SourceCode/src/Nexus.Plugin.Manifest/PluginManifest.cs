using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Nexus.Plugin;

/// <summary>
/// Represents the authoritative manifest required for every Nexus plugin package.
/// </summary>
public sealed class PluginManifest
{
    private static readonly Regex SemVerPattern = new(
        "^(0|[1-9]\\d*)\\.(0|[1-9]\\d*)\\.(0|[1-9]\\d*)(?:-[-0-9A-Za-z.]+)?(?:\\+[0-9A-Za-z.-]+)?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [JsonPropertyName("id")]
    [Required]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    [Required]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("version")]
    [Required]
    public string Version { get; init; } = string.Empty;

    [JsonPropertyName("sdkVersion")]
    [Required]
    public string SdkVersion { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("entrypoints")]
    public PluginEntrypoints Entrypoints { get; init; } = new();

    [JsonPropertyName("capabilities")]
    public IReadOnlyList<string> Capabilities { get; init; } = Array.Empty<string>();

    [JsonPropertyName("requires")]
    public IReadOnlyList<PluginRequirement> Requires { get; init; } = Array.Empty<PluginRequirement>();

    [JsonPropertyName("assets")]
    public IReadOnlyDictionary<string, string> Assets { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("permissions")]
    public PluginPermissions Permissions { get; init; } = new();

    [JsonPropertyName("update")]
    public PluginUpdate? Update { get; init; }

    /// <summary>
    /// Validates the manifest for required fields and semantic version formats.
    /// </summary>
    public void Validate()
    {
        ValidateField(Id, nameof(Id));
        ValidateField(Name, nameof(Name));
        ValidateSemVer(Version, nameof(Version));
        ValidateSemVer(SdkVersion, nameof(SdkVersion));

        foreach (var requirement in Requires)
        {
            ValidateField(requirement.Id, "requires.id");
            ValidateField(requirement.Range, $"requires[{requirement.Id}].range");
        }
    }

    private static void ValidateField(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidDataException($"{fieldName} must be provided.");
        }
    }

    private static void ValidateSemVer(string version, string fieldName)
    {
        if (!SemVerPattern.IsMatch(version))
        {
            throw new InvalidDataException($"{fieldName} must be a semantic version (e.g. 1.0.0).");
        }
    }

    /// <summary>Loads a manifest from a JSON stream.</summary>
    public static async Task<PluginManifest> LoadAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        var manifest = await JsonSerializer.DeserializeAsync<PluginManifest>(stream, ManifestSerializer.Options, cancellationToken)
            .ConfigureAwait(false);
        if (manifest is null)
        {
            throw new InvalidDataException("Manifest JSON is empty.");
        }

        manifest.Validate();
        return manifest;
    }

    /// <summary>Loads a manifest from a path on disk.</summary>
    public static async Task<PluginManifest> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(path);
        return await LoadAsync(stream, cancellationToken).ConfigureAwait(false);
    }
}

public sealed class PluginEntrypoints
{
    [JsonPropertyName("core")]
    public string? Core { get; init; }

    [JsonPropertyName("ui")]
    public string? Ui { get; init; }

    [JsonPropertyName("agio")]
    public string? AgIo { get; init; }
}

public sealed class PluginRequirement
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("range")]
    public string Range { get; init; } = string.Empty;
}

public sealed class PluginPermissions
{
    [JsonPropertyName("network")]
    public bool? Network { get; init; }

    [JsonPropertyName("serial")]
    public bool? Serial { get; init; }
}

public sealed class PluginUpdate
{
    [JsonPropertyName("feed")]
    public string? Feed { get; init; }
}

internal static class ManifestSerializer
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = false,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = false,
        WriteIndented = true
    };
}

