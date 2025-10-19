namespace Nexus.Sdk.Core;

/// <summary>
/// Entry point executed by the host when a plugin is loaded.
/// </summary>
public interface IPluginEntrypoint
{
    /// <summary>
    /// Called once when the plugin is activated.
    /// </summary>
    /// <param name="services">The host service facade that exposes host capabilities.</param>
    void Initialize(IHostServices services);

    /// <summary>
    /// Called when the plugin is being unloaded.
    /// </summary>
    /// <param name="cancellationToken">Token that is triggered when shutdown should abort.</param>
    ValueTask ShutdownAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Core plugins are loaded inside the orchestrator process and may contribute services to it.
/// </summary>
public interface ICoreEntrypoint : IPluginEntrypoint
{
}
