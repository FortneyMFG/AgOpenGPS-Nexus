using System;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Eventing;
using Aog.Core.V1;

namespace Aog.Core.Logging;

/// <summary>
/// Publishes plugin telemetry payloads onto the shared event bus so downstream loggers can persist them.
/// </summary>
public sealed class EventBusPluginTelemetrySink
{
    private readonly IEventBus _eventBus;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventBusPluginTelemetrySink"/> class.
    /// </summary>
    /// <param name="eventBus">Event bus used to forward plugin telemetry events.</param>
    public EventBusPluginTelemetrySink(IEventBus eventBus)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
    }

    /// <summary>
    /// Publishes a plugin telemetry payload onto the event bus.
    /// </summary>
    /// <param name="pluginId">Identifier of the plugin emitting the payload.</param>
    /// <param name="topic">Logical topic name within the plugin.</param>
    /// <param name="payload">Raw payload bytes.</param>
    /// <param name="header">Optional telemetry header supplied by the plugin.</param>
    /// <param name="cancellationToken">Token used to cancel the publish operation.</param>
    /// <returns>A task that completes when the event bus has accepted the payload.</returns>
    public ValueTask PublishAsync(
        string pluginId,
        string topic,
        ReadOnlyMemory<byte> payload,
        Header? header = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
        {
            throw new ArgumentException("Plugin identifier must be provided.", nameof(pluginId));
        }

        if (string.IsNullOrWhiteSpace(topic))
        {
            throw new ArgumentException("Topic must be provided.", nameof(topic));
        }

        var normalizedPluginId = pluginId.Trim();
        var normalizedTopic = topic.Trim();

        var telemetryEvent = new PluginTelemetryEvent
        {
            Header = header,
            PluginId = normalizedPluginId,
            Topic = normalizedTopic,
            Payload = payload
        };

        return _eventBus.PublishAsync(telemetryEvent, cancellationToken);
    }
}
