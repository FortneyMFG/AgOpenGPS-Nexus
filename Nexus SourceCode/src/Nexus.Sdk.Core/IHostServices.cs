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
    /// Gets metadata about the plugin and host runtime.
    /// </summary>
    IPluginEnvironment Environment { get; }

    /// <summary>
    /// Gets the path helper scoped to the plugin.
    /// </summary>
    IPluginPaths Paths { get; }

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
    /// Attempts to resolve an optional feature contract exposed by the host (for example, UI surfaces).
    /// </summary>
    /// <typeparam name="TFeature">Feature contract requested.</typeparam>
    /// <returns>The resolved feature instance or <c>null</c> when not available.</returns>
    TFeature? GetFeature<TFeature>()
        where TFeature : class;
}
