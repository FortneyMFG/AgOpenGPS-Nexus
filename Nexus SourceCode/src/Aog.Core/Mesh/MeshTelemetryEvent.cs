using System;

namespace Aog.Core.Mesh;

/// <summary>
/// Event raised when the retention worker captures a mesh publication for logging.
/// </summary>
public sealed record MeshTelemetryEvent(
    long Sequence,
    string PublisherDeviceId,
    string Topic,
    string SeasonId,
    string JobId,
    string LayerNamespace,
    MeshDataTier Tier,
    DateTimeOffset PublishedAt,
    byte[] Payload,
    string MetadataJson,
    string? PresenceJson);
