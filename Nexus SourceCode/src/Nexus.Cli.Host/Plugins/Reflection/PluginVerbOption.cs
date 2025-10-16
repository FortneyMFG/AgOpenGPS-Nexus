namespace Nexus.Cli.Host.Plugins.Reflection;

/// <summary>
/// Describes an option contributed by a reflected plugin verb.
/// </summary>
public sealed record PluginVerbOption(
    string Name,
    string Description,
    bool Required,
    PluginVerbOptionKind Kind,
    string? DefaultValue);

/// <summary>
/// Represents the supported option types returned by plugin reflection services.
/// </summary>
public enum PluginVerbOptionKind
{
    String,
    Int32,
    Double,
    Bool,
}
