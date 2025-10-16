using Nexus.Plugin.Cli.Abstractions;

namespace Nexus.Cli.Host.Plugins;

/// <summary>
/// Provides discovery for plugin-supplied CLI command modules.
/// </summary>
public interface IPluginCommandModuleLoader
{
    /// <summary>
    /// Discovers and loads plugin command modules.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used to abort discovery.</param>
    /// <returns>A collection of plugin command modules.</returns>
    Task<IReadOnlyList<ICommandModule>> LoadModulesAsync(CancellationToken cancellationToken);
}
