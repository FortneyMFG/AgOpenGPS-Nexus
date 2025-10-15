namespace Aog.Plugins;

/// <summary>
/// Classification describing how strongly a dependency impacts compatibility.
/// </summary>
public enum PluginDependencyClassification
{
    /// <summary>
    /// Required for the plugin to start or remain healthy.
    /// </summary>
    Hard,

    /// <summary>
    /// Optional dependency that improves functionality when present.
    /// </summary>
    Soft,

    /// <summary>
    /// Recommended pairing surfaced to operators.
    /// </summary>
    Suggest,
}
