namespace Nexus.Sdk.AgIo;

/// <summary>
/// Coordinates AgIO sidecar lifecycles on behalf of plugins.
/// </summary>
public interface IAgIoProcessSupervisor
{
    /// <summary>
    /// Ensures a sidecar is running for the provided descriptor.
    /// </summary>
    /// <param name="descriptor">Sidecar descriptor.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask<AgIoSidecarStatus> EnsureStartedAsync(AgIoSidecarDescriptor descriptor, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the sidecar for the given plugin, if running.
    /// </summary>
    /// <param name="pluginId">Owning plugin identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask StopAsync(string pluginId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current status for a plugin's sidecar.
    /// </summary>
    /// <param name="pluginId">Owning plugin identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask<AgIoSidecarStatus?> GetStatusAsync(string pluginId, CancellationToken cancellationToken = default);
}
