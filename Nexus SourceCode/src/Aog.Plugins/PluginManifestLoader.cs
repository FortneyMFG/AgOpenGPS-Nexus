using System;
using System.IO;
using System.Linq;
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

        if (manifest.SupportedCapabilities is null)
        {
            throw new InvalidDataException("Manifest supportedCapabilities cannot be null; omit the section to leave it empty.");
        }

        var supportedCapabilities = manifest.SupportedCapabilities;

        foreach (var capability in supportedCapabilities)
        {
            if (string.IsNullOrWhiteSpace(capability))
            {
                throw new InvalidDataException("Supported capability names must be non-empty.");
            }
        }

        if (manifest.RequiredTransports is null)
        {
            throw new InvalidDataException("Manifest requiredTransports cannot be null; omit the section to leave it empty.");
        }

        var requiredTransports = manifest.RequiredTransports;

        foreach (var transport in requiredTransports)
        {
            if (string.IsNullOrWhiteSpace(transport))
            {
                throw new InvalidDataException("Required transport names must be non-empty.");
            }
        }

        var minimumRuntimeVersion = string.IsNullOrWhiteSpace(manifest.MinimumRuntimeVersion)
            ? "1.0.0"
            : manifest.MinimumRuntimeVersion;

        if (!SemanticVersionPattern.IsMatch(minimumRuntimeVersion))
        {
            throw new InvalidDataException("Manifest minimumRuntimeVersion must be a semantic version (e.g. 1.0.0).");
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

            if (provider.Topics is null || provider.Topics.Count == 0)
            {
                throw new InvalidDataException($"Simulation provider '{provider.ProviderId}' must declare at least one topic.");
            }

            if (provider.Topics is not null)
            {
                var uniqueTopics = new HashSet<string>(StringComparer.Ordinal);
                for (var i = 0; i < provider.Topics.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(provider.Topics[i]))
                    {
                        throw new InvalidDataException(
                            $"Simulation provider '{provider.ProviderId}' contains an empty topic name at index {i}.");
                    }

                    if (!uniqueTopics.Add(provider.Topics[i]))
                    {
                        throw new InvalidDataException(
                            $"Simulation provider '{provider.ProviderId}' declares duplicate topic '{provider.Topics[i]}'.");
                    }
                }
            }
        }

        if (manifest.CapabilityLeases is null)
        {
            throw new InvalidDataException("Manifest leases cannot be null; omit the section to leave it empty.");
        }

        var capabilityLeases = manifest.CapabilityLeases;

        if (!capabilityLeases.Any())
        {
            return;
        }

        var seenLeaseCapabilities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var lease in capabilityLeases)
        {
            if (lease is null)
            {
                throw new InvalidDataException("Lease entries cannot be null.");
            }

            if (string.IsNullOrWhiteSpace(lease.Capability))
            {
                throw new InvalidDataException("Lease capability names must be non-empty.");
            }

            if (!supportedCapabilities.Contains(lease.Capability, StringComparer.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"Lease capability '{lease.Capability}' must be listed in supportedCapabilities.");
            }

            if (!seenLeaseCapabilities.Add(lease.Capability))
            {
                throw new InvalidDataException($"Duplicate lease declaration for capability '{lease.Capability}'.");
            }

            if (lease.TimeoutSeconds <= 0)
            {
                throw new InvalidDataException($"Lease timeout must be positive for capability '{lease.Capability}'.");
            }
        }
    }
}
