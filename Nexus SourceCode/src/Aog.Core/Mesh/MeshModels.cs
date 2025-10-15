using System;
using System.Collections.Generic;

namespace Aog.Core.Mesh;

/// <summary>
/// Represents the mesh data tiers defined by ADR-047.
/// </summary>
[Flags]
public enum MeshDataTier
{
    /// <summary>
    /// No tiers selected.
    /// </summary>
    None = 0,

    /// <summary>
    /// Presence heartbeats and pose updates.
    /// </summary>
    Presence = 1 << 0,

    /// <summary>
    /// Trail polylines representing machine coverage history.
    /// </summary>
    Trails = 1 << 1,

    /// <summary>
    /// Coverage tiles or rasters published to the mesh.
    /// </summary>
    Coverage = 1 << 2,

    /// <summary>
    /// Layer deltas (for example zone edits) published by plugins.
    /// </summary>
    Layers = 1 << 3,

    /// <summary>
    /// Convenience flag that represents all known tiers.
    /// </summary>
    All = Presence | Trails | Coverage | Layers,
}

/// <summary>
/// Describes the publication rights a device has for a specific season/job scope.
/// </summary>
public sealed record MeshShareGrant(
    string SeasonId,
    string JobId,
    MeshDataTier Tiers,
    IReadOnlyCollection<string>? LayerNamespaces = null);

/// <summary>
/// Collection of <see cref="MeshShareGrant"/> entries for a device.
/// </summary>
public sealed record MeshShareProfile(IReadOnlyList<MeshShareGrant> Grants)
{
    /// <summary>
    /// Gets an empty share profile.
    /// </summary>
    public static MeshShareProfile Empty { get; } = new(Array.Empty<MeshShareGrant>());
}

/// <summary>
/// Describes the subscription rights a device has for a specific season/job scope.
/// </summary>
public sealed record MeshSubscribeGrant(
    string SeasonId,
    string JobId,
    MeshDataTier Tiers,
    IReadOnlyCollection<string>? LayerNamespaces = null);

/// <summary>
/// Collection of <see cref="MeshSubscribeGrant"/> entries for a device.
/// </summary>
public sealed record MeshSubscribeProfile(IReadOnlyList<MeshSubscribeGrant> Grants)
{
    /// <summary>
    /// Gets an empty subscribe profile.
    /// </summary>
    public static MeshSubscribeProfile Empty { get; } = new(Array.Empty<MeshSubscribeGrant>());
}

/// <summary>
/// Request used to register or update a mesh device.
/// </summary>
public sealed record MeshDeviceRegistration(
    string DeviceId,
    string Label,
    IReadOnlyCollection<string>? Capabilities = null,
    MeshShareProfile? ShareProfile = null,
    MeshSubscribeProfile? SubscribeProfile = null);

/// <summary>
/// Represents a publish request performed by a device.
/// </summary>
public sealed record MeshPublishRequest(
    string DeviceId,
    string Topic,
    MeshDataTier Tier,
    ReadOnlyMemory<byte> Payload,
    DateTimeOffset? PublishedAt = null,
    IReadOnlyDictionary<string, string>? Metadata = null);

/// <summary>
/// Describes a subscription request issued by a device.
/// </summary>
public sealed record MeshSubscriptionRequest(
    string DeviceId,
    string? SeasonId = null,
    string? JobId = null,
    IReadOnlyCollection<string>? LayerNamespaces = null,
    MeshDataTier TierMask = MeshDataTier.All);

/// <summary>
/// Describes the state of a mesh presence heartbeat.
/// </summary>
public enum MeshPresenceState
{
    /// <summary>
    /// Device is online and broadcasting presence.
    /// </summary>
    Online,

    /// <summary>
    /// Device explicitly signalled it is going offline.
    /// </summary>
    Offline,
}

/// <summary>
/// Holds pose information included in presence broadcasts.
/// </summary>
public sealed record MeshPose(
    double Latitude,
    double Longitude,
    double? AltitudeMeters = null,
    double? HeadingDegrees = null,
    double? SpeedMetersPerSecond = null);

/// <summary>
/// Identifies the session context in which a device is operating.
/// </summary>
public sealed record MeshSessionDescriptor(
    string SeasonId,
    string JobId,
    string? SessionId = null,
    string? LayerNamespace = null);

/// <summary>
/// Represents a presence heartbeat update emitted by a device.
/// </summary>
public sealed record MeshPresenceUpdate(
    string DeviceId,
    MeshSessionDescriptor Session,
    MeshPose Pose,
    MeshPresenceState State = MeshPresenceState.Online,
    IReadOnlyDictionary<string, string>? Metadata = null,
    DateTimeOffset? Timestamp = null);

/// <summary>
/// Snapshot of the most recent presence heartbeat from a device.
/// </summary>
public sealed record MeshPresenceSnapshot(
    string DeviceId,
    MeshPresenceState State,
    MeshSessionDescriptor Session,
    MeshPose Pose,
    DateTimeOffset UpdatedAt,
    IReadOnlyDictionary<string, string> Metadata);

/// <summary>
/// Publication delivered to mesh subscribers.
/// </summary>
public sealed class MeshPublication
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MeshPublication"/> class.
    /// </summary>
    public MeshPublication(
        string publisherDeviceId,
        string topic,
        string seasonId,
        string jobId,
        string layerNamespace,
        MeshDataTier tier,
        DateTimeOffset publishedAt,
        ReadOnlyMemory<byte> payload,
        IReadOnlyDictionary<string, string>? metadata = null,
        object? state = null)
    {
        PublisherDeviceId = publisherDeviceId;
        Topic = topic;
        SeasonId = seasonId;
        JobId = jobId;
        LayerNamespace = layerNamespace;
        Tier = tier;
        PublishedAt = publishedAt;
        Payload = payload;
        Metadata = metadata;
        State = state;
    }

    /// <summary>
    /// Gets the identifier of the publishing device.
    /// </summary>
    public string PublisherDeviceId { get; }

    /// <summary>
    /// Gets the fully qualified mesh topic.
    /// </summary>
    public string Topic { get; }

    /// <summary>
    /// Gets the season identifier parsed from the topic.
    /// </summary>
    public string SeasonId { get; }

    /// <summary>
    /// Gets the job identifier parsed from the topic.
    /// </summary>
    public string JobId { get; }

    /// <summary>
    /// Gets the layer namespace parsed from the topic.
    /// </summary>
    public string LayerNamespace { get; }

    /// <summary>
    /// Gets the data tier of the publication.
    /// </summary>
    public MeshDataTier Tier { get; }

    /// <summary>
    /// Gets the timestamp associated with the publication.
    /// </summary>
    public DateTimeOffset PublishedAt { get; }

    /// <summary>
    /// Gets the raw payload for the publication.
    /// </summary>
    public ReadOnlyMemory<byte> Payload { get; }

    /// <summary>
    /// Gets optional metadata key-value pairs associated with the publication.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; }

    /// <summary>
    /// Gets an optional strongly typed state payload (used for presence snapshots).
    /// </summary>
    public object? State { get; }
}
