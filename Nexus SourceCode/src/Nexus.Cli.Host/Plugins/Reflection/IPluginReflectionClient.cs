namespace Nexus.Cli.Host.Plugins.Reflection;

/// <summary>
/// Provides access to plugin CLI reflection services exposed over gRPC.
/// </summary>
public interface IPluginReflectionClient
{
    /// <summary>
    /// Lists verbs exposed by the plugin at the specified reflection endpoint.
    /// </summary>
    /// <param name="endpoint">The URI of the plugin reflection service.</param>
    /// <param name="cancellationToken">A token used to cancel the request.</param>
    /// <returns>A collection of reflected verbs.</returns>
    Task<IReadOnlyList<PluginVerb>> ListVerbsAsync(Uri endpoint, CancellationToken cancellationToken);
}
