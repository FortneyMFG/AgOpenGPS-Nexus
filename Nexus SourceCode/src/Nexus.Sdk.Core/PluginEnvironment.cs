using System;

namespace Nexus.Sdk.Core;

/// <summary>
/// Describes metadata about the plugin instance resolved by the host.
/// </summary>
public interface IPluginEnvironment
{
    /// <summary>Identifier declared in the manifest.</summary>
    string PluginId { get; }

    /// <summary>Plugin version resolved by the host.</summary>
    Version PluginVersion { get; }

    /// <summary>Nexus SDK version provided by the host.</summary>
    Version SdkVersion { get; }
}

/// <summary>
/// Provides plugin-scoped storage paths.
/// </summary>
public interface IPluginPaths
{
    /// <summary>Base directory assigned to the plugin.</summary>
    string BaseDirectory { get; }
    /// <summary>Directory reserved for plugin logs.</summary>
    string LogDirectory { get; }
    /// <summary>Directory reserved for cache artifacts.</summary>
    string CacheDirectory { get; }
    /// <summary>Directory reserved for temporary files.</summary>
    string TempDirectory { get; }

    /// <summary>
    /// Resolves a path relative to the plugin base directory.
    /// </summary>
    string Resolve(string relativePath);
}
