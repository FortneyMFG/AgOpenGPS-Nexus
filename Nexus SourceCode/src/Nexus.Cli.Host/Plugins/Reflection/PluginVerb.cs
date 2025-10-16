namespace Nexus.Cli.Host.Plugins.Reflection;

/// <summary>
/// Represents a verb surfaced by a plugin reflection service.
/// </summary>
public sealed record PluginVerb(
    string Name,
    string Description,
    IReadOnlyList<PluginVerbOption> Options);
