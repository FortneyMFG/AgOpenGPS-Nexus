using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aog.Tools.RadioBridge;

/// <summary>
/// Represents a provisioning profile for a RadioBridge endpoint.
/// </summary>
public sealed class RadioBridgeProvisioningProfile
{
    /// <summary>
    /// Serializer configuration shared by the CLI and tests.
    /// </summary>
    public static JsonSerializerOptions SerializerOptions { get; } = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    /// <summary>
    /// Gets or sets the device identifier.
    /// </summary>
    public required string DeviceId { get; init; }

    /// <summary>
    /// Gets or sets the display label.
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// Gets or sets the hexadecimal pre-shared key used for encryption.
    /// </summary>
    public required string PreSharedKey { get; init; }

    /// <summary>
    /// Gets or sets the timestamp when the profile was generated.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Gets or sets the topic grants allowed for this radio bridge endpoint.
    /// </summary>
    public required IReadOnlyList<TopicGrant> Topics { get; init; }

    /// <summary>
    /// Describes a mesh scope grant encoded into the provisioning profile.
    /// </summary>
    public sealed record TopicGrant(string SeasonId, string JobId, string TierMask);
}
