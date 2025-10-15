using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aog.Plugins;

namespace Aog.Tools.PluginCompliance;

internal sealed class CapabilityReportGenerator
{
    private readonly PluginManifestLoader _loader = new();

    public async Task<IReadOnlyList<PluginCapabilityReport>> GenerateAsync(
        string manifestRoot,
        string? pluginFilter = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(manifestRoot))
        {
            throw new ArgumentException("Manifest root must be provided.", nameof(manifestRoot));
        }

        if (!Directory.Exists(manifestRoot))
        {
            throw new DirectoryNotFoundException($"Manifest root '{manifestRoot}' was not found.");
        }

        var results = new List<PluginCapabilityReport>();
        var manifestFiles = Directory.EnumerateFiles(manifestRoot, "*.json", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase);

        foreach (var manifestPath in manifestFiles)
        {
            await using var stream = File.OpenRead(manifestPath);
            PluginManifest manifest;
            try
            {
                manifest = await _loader.LoadAsync(stream, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is InvalidDataException or JsonException)
            {
                throw new InvalidDataException($"Failed to load manifest '{manifestPath}': {ex.Message}", ex);
            }

            if (!string.IsNullOrWhiteSpace(pluginFilter) &&
                !string.Equals(manifest.Id, pluginFilter, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(manifest.Name, pluginFilter, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var relativeManifest = Path.GetRelativePath(manifestRoot, manifestPath)
                .Replace(Path.DirectorySeparatorChar, '/');

            var capabilities = manifest.SupportedCapabilities
                .Select(capability =>
                {
                    var lease = manifest.CapabilityLeases.FirstOrDefault(entry =>
                        string.Equals(entry.Capability, capability, StringComparison.OrdinalIgnoreCase));

                    return new CapabilityLeaseReport(
                        capability,
                        lease?.Mode ?? PluginLeaseMode.Shared,
                        lease?.TimeoutSeconds ?? 0,
                        lease?.RecoveryStrategy ?? PluginLeaseRecoveryStrategy.GracefulDegradation);
                })
                .OrderBy(entry => entry.Capability, StringComparer.OrdinalIgnoreCase)
                .ToList();

            results.Add(new PluginCapabilityReport(
                manifest.Id,
                manifest.Name,
                manifest.Version,
                manifest.MinimumRuntimeVersion,
                relativeManifest,
                capabilities));
        }

        return results
            .OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => entry.Version, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}

internal sealed record PluginCapabilityReport(
    string Id,
    string Name,
    string Version,
    string MinimumRuntimeVersion,
    string Manifest,
    IReadOnlyList<CapabilityLeaseReport> Capabilities);

internal sealed record CapabilityLeaseReport(
    string Capability,
    PluginLeaseMode Mode,
    int TimeoutSeconds,
    PluginLeaseRecoveryStrategy RecoveryStrategy);
