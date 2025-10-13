using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Aog.Plugins;

/// <summary>
/// Provides helpers for reading and validating Nexus plugin manifests.
/// </summary>
public sealed class PluginManifestLoader
{
    private static readonly Regex SemanticVersionPattern = new(
        "^(0|[1-9]\\d*)\\.(0|[1-9]\\d*)\\.(0|[1-9]\\d*)(?:-[-0-9A-Za-z.]+)?(?:\\+[0-9A-Za-z.-]+)?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex ManifestIdPattern = new(
        "^[a-z0-9]+(\\.[a-z0-9_-]+)+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = false,
        PropertyNameCaseInsensitive = false,
    };

    /// <summary>
    /// Loads a plugin manifest from a UTF-8 encoded stream.
    /// </summary>
    /// <param name="stream">The stream containing the JSON manifest.</param>
    /// <param name="cancellationToken">Token used to cancel the read operation.</param>
    /// <returns>The parsed <see cref="PluginManifest"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when the manifest is missing required fields or is malformed.</exception>
    public async Task<PluginManifest> LoadAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        var manifest = await JsonSerializer.DeserializeAsync<PluginManifest>(stream, SerializerOptions, cancellationToken)
            .ConfigureAwait(false);

        if (manifest is null)
        {
            throw new InvalidDataException("Plugin manifest JSON did not contain an object.");
        }

        Validate(manifest);
        return manifest;
    }

    /// <summary>
    /// Loads a plugin manifest from the provided file path.
    /// </summary>
    /// <param name="path">The path to the JSON manifest.</param>
    /// <param name="cancellationToken">Token used to cancel the read operation.</param>
    /// <returns>The parsed <see cref="PluginManifest"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is null or whitespace.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the manifest file is missing.</exception>
    /// <exception cref="InvalidDataException">Thrown when the manifest is missing required fields or is malformed.</exception>
    public async Task<PluginManifest> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("A manifest path must be provided.", nameof(path));
        }

        await using var stream = File.OpenRead(path);
        return await LoadAsync(stream, cancellationToken).ConfigureAwait(false);
    }

    private static void Validate(PluginManifest manifest)
    {
        if (string.IsNullOrWhiteSpace(manifest.SchemaVersion))
        {
            throw new InvalidDataException("Manifest schemaVersion is required.");
        }

        if (!SemanticVersionPattern.IsMatch(manifest.SchemaVersion))
        {
            throw new InvalidDataException("Manifest schemaVersion must be a semantic version (e.g. 1.0.0).");
        }

        if (string.IsNullOrWhiteSpace(manifest.Id))
        {
            throw new InvalidDataException("Manifest id is required.");
        }

        if (!ManifestIdPattern.IsMatch(manifest.Id))
        {
            throw new InvalidDataException("Manifest id must follow reverse-DNS syntax (e.g. org.example.plugin).");
        }

        if (string.IsNullOrWhiteSpace(manifest.Name))
        {
            throw new InvalidDataException("Manifest name is required.");
        }

        if (string.IsNullOrWhiteSpace(manifest.Version))
        {
            throw new InvalidDataException("Manifest version is required.");
        }

        if (!SemanticVersionPattern.IsMatch(manifest.Version))
        {
            throw new InvalidDataException("Manifest version must be a semantic version (e.g. 1.0.0).");
        }

        if (manifest.RequiredApis is null || manifest.RequiredApis.Count == 0)
        {
            throw new InvalidDataException("Manifest must declare at least one required API.");
        }

        foreach (var pair in manifest.RequiredApis)
        {
            if (string.IsNullOrWhiteSpace(pair.Key))
            {
                throw new InvalidDataException("Required API names must be non-empty.");
            }

            if (string.IsNullOrWhiteSpace(pair.Value))
            {
                throw new InvalidDataException($"Required API '{pair.Key}' must specify a version requirement.");
            }
        }

        if (manifest.SimulationProviders is null || manifest.SimulationProviders.Count == 0)
        {
            throw new InvalidDataException("Manifest must declare at least one simulation provider.");
        }

        foreach (var provider in manifest.SimulationProviders)
        {
            if (provider is null)
            {
                throw new InvalidDataException("Simulation provider entries cannot be null.");
            }

            if (string.IsNullOrWhiteSpace(provider.ProviderId))
            {
                throw new InvalidDataException("Simulation provider entries must include a providerId.");
            }

            if (string.IsNullOrWhiteSpace(provider.Type))
            {
                throw new InvalidDataException($"Simulation provider '{provider.ProviderId}' must include a type.");
            }

            if (provider.Topics is not null)
            {
                for (var i = 0; i < provider.Topics.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(provider.Topics[i]))
                    {
                        throw new InvalidDataException(
                            $"Simulation provider '{provider.ProviderId}' contains an empty topic name at index {i}.");
                    }
                }
            }
        }
    }
}
