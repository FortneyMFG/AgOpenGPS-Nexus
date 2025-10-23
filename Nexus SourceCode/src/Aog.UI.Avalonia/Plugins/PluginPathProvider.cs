using System;
using System.Collections.Generic;
using System.IO;

namespace Aog.UI.Avalonia.Plugins;

/// <summary>
/// Resolves plugin probing directories for the current process.
/// </summary>
internal sealed class PluginPathProvider
{
    private readonly Lazy<IReadOnlyList<string>> _paths;

    public PluginPathProvider()
    {
        _paths = new Lazy<IReadOnlyList<string>>(DiscoverPaths);
    }

    public IReadOnlyList<string> Paths => _paths.Value;

    private static IReadOnlyList<string> DiscoverPaths()
    {
        var list = new List<string>();
        var baseDirectory = AppContext.BaseDirectory;
        var bundled = Path.Combine(baseDirectory, "plugins");
        if (Directory.Exists(bundled))
        {
            list.Add(bundled);
        }

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (!string.IsNullOrWhiteSpace(appData))
        {
            var userRoot = Path.Combine(appData, "Nexus", "plugins");
            Directory.CreateDirectory(userRoot);
            list.Add(userRoot);
        }

        return list;
    }
}

