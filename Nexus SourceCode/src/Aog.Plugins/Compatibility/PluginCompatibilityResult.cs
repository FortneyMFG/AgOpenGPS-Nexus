using System;
using System.Collections.Generic;

namespace Aog.Plugins.Compatibility;

/// <summary>
/// Aggregated compatibility state for a plugin manifest.
/// </summary>
public sealed class PluginCompatibilityResult
{
    public PluginCompatibilityResult(
        string pluginId,
        string name,
        string version,
        bool isOfficialBundleMember,
        PluginCompatibilityState state,
        IReadOnlyList<PluginCompatibilityDependencyStatus> issues,
        IReadOnlyList<string> capabilities)
    {
        PluginId = pluginId ?? throw new ArgumentNullException(nameof(pluginId));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Version = version ?? throw new ArgumentNullException(nameof(version));
        IsOfficialBundleMember = isOfficialBundleMember;
        State = state;
        Issues = issues ?? throw new ArgumentNullException(nameof(issues));
        Capabilities = capabilities ?? throw new ArgumentNullException(nameof(capabilities));
    }

    /// <summary>Gets the manifest identifier.</summary>
    public string PluginId { get; }

    /// <summary>Gets the human readable plugin name.</summary>
    public string Name { get; }

    /// <summary>Gets the semantic version declared in the manifest.</summary>
    public string Version { get; }

    /// <summary>Gets a value indicating whether the plugin is part of the official bundle.</summary>
    public bool IsOfficialBundleMember { get; }

    /// <summary>Gets the aggregate state for the plugin.</summary>
    public PluginCompatibilityState State { get; }

    /// <summary>Gets the dependency issues detected during evaluation.</summary>
    public IReadOnlyList<PluginCompatibilityDependencyStatus> Issues { get; }

    /// <summary>Gets the capabilities advertised by the plugin.</summary>
    public IReadOnlyList<string> Capabilities { get; }
}
