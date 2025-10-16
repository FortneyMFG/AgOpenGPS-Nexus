namespace Nexus.Plugin.Cli.Abstractions;

/// <summary>
/// Describes metadata about a plugin CLI adapter supplied to a command module.
/// </summary>
/// <param name="PluginId">The manifest identifier of the plugin.</param>
/// <param name="PluginName">The human-friendly name of the plugin.</param>
/// <param name="PluginVersion">The version of the plugin resolved from the manifest.</param>
/// <param name="PluginDirectory">The directory that contains the plugin payload.</param>
/// <param name="ManifestPath">The path to the manifest that declared the adapter.</param>
/// <param name="AssemblyPath">The path to the CLI adapter assembly.</param>
public sealed record PluginCommandModuleDescriptor(
    string PluginId,
    string PluginName,
    string PluginVersion,
    string PluginDirectory,
    string ManifestPath,
    string AssemblyPath);
