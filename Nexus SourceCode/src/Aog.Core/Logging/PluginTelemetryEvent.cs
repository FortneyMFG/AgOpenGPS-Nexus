using System;
using Aog.Core.V1;

namespace Aog.Core.Logging;

/// <summary>
/// Represents a telemetry payload emitted by a plugin that should be captured in replay logs.
/// </summary>
public sealed class PluginTelemetryEvent
{
    /// <summary>
    /// Optional telemetry header describing sequence, timestamp, and source information.
    /// </summary>
    public Header? Header { get; init; }

    /// <summary>
    /// Identifier of the plugin emitting the payload.
    /// </summary>
    public string PluginId { get; init; } = string.Empty;

    /// <summary>
    /// Logical topic within the plugin namespace.
    /// </summary>
    public string Topic { get; init; } = string.Empty;

    /// <summary>
    /// Raw payload emitted by the plugin. Interpretation is plugin-defined.
    /// </summary>
    public ReadOnlyMemory<byte> Payload { get; init; }
}
