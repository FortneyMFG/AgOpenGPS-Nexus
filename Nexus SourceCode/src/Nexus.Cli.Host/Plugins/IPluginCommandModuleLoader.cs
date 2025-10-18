using Nexus.Plugin.Cli.Abstractions;

namespace Nexus.Cli.Host.Plugins;

/// <summary>
/// Provides discovery for plugin-supplied CLI command handlers.
/// </summary>
public interface IPluginCommandModuleLoader
{
    /// <summary>
    /// Discovers and loads plugin command handlers.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used to abort discovery.</param>
    /// <returns>A collection of plugin command handlers.</returns>
    Task<IReadOnlyList<ICommandHandler>> LoadModulesAsync(CancellationToken cancellationToken);
}
