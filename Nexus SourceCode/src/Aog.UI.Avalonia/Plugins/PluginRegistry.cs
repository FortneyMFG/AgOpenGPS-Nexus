using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Aog.UI.Avalonia.Plugins;

/// <summary>
/// Tracks plugins discovered on disk.
/// </summary>
public sealed class PluginRegistry
{
    private readonly ConcurrentDictionary<string, PluginDescriptor> _descriptors = new(StringComparer.OrdinalIgnoreCase);

    public IEnumerable<PluginDescriptor> GetDescriptors() => _descriptors.Values;

    public bool TryGetDescriptor(string pluginId, out PluginDescriptor descriptor)
        => _descriptors.TryGetValue(pluginId, out descriptor!);

    public async Task ScanAsync(string rootDirectory, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(rootDirectory))
        {
            return;
        }

        foreach (var directory in Directory.EnumerateDirectories(rootDirectory))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var manifestPath = Path.Combine(directory, "manifest.json");
                if (File.Exists(manifestPath))
                {
                    var descriptor = await PluginDescriptor.LoadAsync(directory, cancellationToken).ConfigureAwait(false);
                    _descriptors[descriptor.Id] = descriptor;
                    continue;
                }

                PluginDescriptor? selected = null;
                foreach (var versionDir in Directory.EnumerateDirectories(directory))
                {
                    var versionManifest = Path.Combine(versionDir, "manifest.json");
                    if (!File.Exists(versionManifest))
                    {
                        continue;
                    }

                    var candidate = await PluginDescriptor.LoadAsync(versionDir, cancellationToken).ConfigureAwait(false);
                    if (selected is null)
                    {
                        selected = candidate;
                        continue;
                    }

                    var currentVersion = selected.Manifest.Version;
                    var candidateVersion = candidate.Manifest.Version;
                    if (Version.TryParse(candidateVersion, out var parsedCandidate) &&
                        Version.TryParse(currentVersion, out var parsedCurrent))
                    {
                        if (parsedCandidate > parsedCurrent)
                        {
                            selected = candidate;
                        }
                    }
                    else
                    {
                        selected = candidate;
                    }
                }

                if (selected is not null)
                {
                    _descriptors[selected.Id] = selected;
                }
            }
            catch (Exception)
            {
                // Invalid manifests are ignored for now; diagnostics will capture the failure later.
            }
        }
    }
}
