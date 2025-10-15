using System.Text.Json.Serialization;

namespace Aog.Plugins;

/// <summary>
/// Declarative lease request captured in the plugin manifest.
/// </summary>
public sealed class PluginCapabilityLease
{
    [JsonPropertyName("capability")]
    public required string Capability { get; init; }

    [JsonPropertyName("mode")]
    public PluginLeaseMode Mode { get; init; } = PluginLeaseMode.Exclusive;

    [JsonPropertyName("timeoutSeconds")]
    public int TimeoutSeconds { get; init; } = 5;

    [JsonPropertyName("recovery")]
    public PluginLeaseRecoveryStrategy RecoveryStrategy { get; init; } = PluginLeaseRecoveryStrategy.GracefulDegradation;
}

/// <summary>Supported lease acquisition modes.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PluginLeaseMode
{
    Exclusive,
    Shared,
}

/// <summary>Recovery approaches when a lease is revoked.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PluginLeaseRecoveryStrategy
{
    GracefulDegradation,
    FailSafe,
}
