using System;
using System.Collections.Generic;
using System.Linq;

namespace Aog.Plugins.Compatibility;

/// <summary>
/// Represents the compatibility evaluation across the plugin bundle.
/// </summary>
public sealed class PluginCompatibilityReport
{
    public PluginCompatibilityReport(
        PluginCompatibilityState overallState,
        IReadOnlyList<PluginCompatibilityResult> plugins)
    {
        OverallState = overallState;
        Plugins = plugins ?? throw new ArgumentNullException(nameof(plugins));
    }

    /// <summary>Gets the aggregate state for the evaluated bundle.</summary>
    public PluginCompatibilityState OverallState { get; }

    /// <summary>Gets the per-plugin evaluation results.</summary>
    public IReadOnlyList<PluginCompatibilityResult> Plugins { get; }

    /// <summary>Gets a value indicating whether any plugin is blocked.</summary>
    public bool HasBlockingIssues => Plugins.Any(plugin => plugin.State == PluginCompatibilityState.Blocked);

    /// <summary>Gets a value indicating whether any plugin reported warnings.</summary>
    public bool HasWarnings => Plugins.Any(plugin => plugin.State == PluginCompatibilityState.Warning);
}
