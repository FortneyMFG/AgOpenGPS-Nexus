namespace Aog.Plugins.Compatibility;

/// <summary>
/// Describes the type of dependency analysed during compatibility evaluation.
/// </summary>
public enum PluginDependencyKind
{
    /// <summary>
    /// Dependency on a runtime API surface (e.g. core runtime services).
    /// </summary>
    RuntimeApi,

    /// <summary>
    /// Dependency on a transport binding (e.g. agio://inventory).
    /// </summary>
    Transport,

    /// <summary>
    /// Dependency on another plugin manifest.
    /// </summary>
    Plugin,

    /// <summary>
    /// Minimum runtime version requirement.
    /// </summary>
    RuntimeVersion,

    /// <summary>
    /// Capability lease health (e.g. exclusive conflicts).
    /// </summary>
    Capability,

    /// <summary>
    /// Conformance profile requirements.
    /// </summary>
    Profile,

    /// <summary>
    /// Higher-order plugin relationships (peer, conflict, extends, replaces).
    /// </summary>
    Relationship,
}
