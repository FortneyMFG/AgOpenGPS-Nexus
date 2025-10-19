using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Plugin;

namespace Aog.UI.Avalonia.Plugins;

/// <summary>
/// Represents a plugin discovered on disk together with its manifest metadata.
/// </summary>
public sealed class PluginDescriptor
{
    private PluginDescriptor(PluginManifest manifest, string rootPath)
    {
        Manifest = manifest;
        RootPath = rootPath;
        LibDirectory = Path.Combine(rootPath, "lib");
        AssetsDirectory = Path.Combine(rootPath, "assets");
        NativeDirectory = Path.Combine(rootPath, "native");
        Version = Version.Parse(manifest.Version);
    }

    /// <summary>Unique plugin identifier.</summary>
    public string Id => Manifest.Id;

    /// <summary>Plugin version resolved from the manifest.</summary>
    public Version Version { get; }

    /// <summary>Resolved manifest.</summary>
    public PluginManifest Manifest { get; }

    /// <summary>Root directory where the plugin is unpacked.</summary>
    public string RootPath { get; }

    /// <summary>Directory containing managed assemblies.</summary>
    public string LibDirectory { get; }

    /// <summary>Directory containing assets shipped with the plugin.</summary>
    public string AssetsDirectory { get; }

    /// <summary>Directory containing native binaries shipped with the plugin.</summary>
    public string NativeDirectory { get; }

    public override string ToString() => $"{Id}@{Version}";

    /// <summary>
    /// Loads and validates the plugin manifest located in <paramref name="rootPath"/>.
    /// </summary>
    public static async Task<PluginDescriptor> LoadAsync(string rootPath, CancellationToken cancellationToken = default)
    {
        var manifestPath = Path.Combine(rootPath, "manifest.json");
        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException($"Plugin manifest '{manifestPath}' was not found.", manifestPath);
        }

        await using var stream = File.OpenRead(manifestPath);
        var manifest = await PluginManifest.LoadAsync(stream, cancellationToken).ConfigureAwait(false);
        return new PluginDescriptor(manifest, rootPath);
    }
}

