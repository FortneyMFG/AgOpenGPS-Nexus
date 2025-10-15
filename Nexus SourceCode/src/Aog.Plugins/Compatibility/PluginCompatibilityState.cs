namespace Aog.Plugins.Compatibility;

/// <summary>
/// Represents the aggregate state of a plugin or dependency evaluation.
/// </summary>
public enum PluginCompatibilityState
{
    /// <summary>
    /// No issues were detected.
    /// </summary>
    Healthy,

    /// <summary>
    /// Non-blocking issues were detected (soft/suggest dependencies).
    /// </summary>
    Warning,

    /// <summary>
    /// Blocking issues were detected.
    /// </summary>
    Blocked,
}
