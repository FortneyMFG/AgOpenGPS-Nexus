using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace Aog.UI.Avalonia.Plugins;

/// <summary>
/// Collectible load context isolating plugin assemblies.
/// </summary>
public sealed class PluginLoadContext : AssemblyLoadContext
{
    private static readonly HashSet<string> SharedAssemblyPrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Nexus.Sdk",
        "Aog.Core",
        "Aog.Plugins",
        "Aog.UI.Avalonia"
    };

    private readonly PluginDescriptor _descriptor;

    public PluginLoadContext(PluginDescriptor descriptor)
        : base($"Plugin:{descriptor.Id}", isCollectible: true)
    {
        _descriptor = descriptor;
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (SharedAssemblyPrefixes.Contains(assemblyName.Name ?? string.Empty))
        {
            return Default.LoadFromAssemblyName(assemblyName);
        }

        var candidate = Path.Combine(_descriptor.LibDirectory, assemblyName.Name + ".dll");
        if (File.Exists(candidate))
        {
            return LoadFromAssemblyPath(candidate);
        }

        return null;
    }
}
