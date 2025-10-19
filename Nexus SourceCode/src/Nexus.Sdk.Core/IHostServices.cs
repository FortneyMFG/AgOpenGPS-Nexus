using Microsoft.Extensions.Logging;

namespace Nexus.Sdk.Core;

/// <summary>
/// Provides a thin facade over services exposed by the Nexus host to plugins.
/// </summary>
public interface IHostServices
{
    /// <summary>
    /// Gets the identifier of the plugin that owns the entry point.
    /// </summary>
    string PluginId { get; }

    /// <summary>
    /// Provides access to the application's service provider.
    /// </summary>
    IServiceProvider Services { get; }

    /// <summary>
    /// Creates loggers scoped to the plugin.
    /// </summary>
    ILoggerFactory LoggerFactory { get; }

    /// <summary>
    /// Gets the host event bus abstraction.
    /// </summary>
    IEventBus EventBus { get; }

    /// <summary>
    /// Gets the host command bus abstraction.
    /// </summary>
    ICommandBus CommandBus { get; }

    /// <summary>
    /// Gets the plugin settings store abstraction.
    /// </summary>
    ISettingsStore Settings { get; }

    /// <summary>
    /// Gets the telemetry pipeline abstraction.
    /// </summary>
    ITelemetry Telemetry { get; }

    /// <summary>
    /// Resolves a storage path for the plugin within the host data directory.
    /// </summary>
    /// <param name="relativePath">Relative path requested by the plugin.</param>
    /// <returns>A fully qualified storage path owned by the plugin.</returns>
    string GetStoragePath(string relativePath);
}
