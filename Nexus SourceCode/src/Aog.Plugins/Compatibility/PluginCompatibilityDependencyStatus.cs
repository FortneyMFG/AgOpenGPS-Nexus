namespace Aog.Plugins.Compatibility;

/// <summary>
/// Represents the compatibility state for a specific dependency.
/// </summary>
/// <param name="Kind">Type of dependency being evaluated.</param>
/// <param name="Identifier">Identifier describing the dependency (API name, plugin id, capability, etc.).</param>
/// <param name="Classification">Strength of the dependency (hard/soft/suggest).</param>
/// <param name="State">Resulting compatibility state.</param>
/// <param name="Message">Human-readable description providing remediation guidance.</param>
public sealed record PluginCompatibilityDependencyStatus(
    PluginDependencyKind Kind,
    string Identifier,
    PluginDependencyClassification Classification,
    PluginCompatibilityState State,
    string Message);
