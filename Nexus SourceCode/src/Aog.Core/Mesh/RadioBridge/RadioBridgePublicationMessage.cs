using System;
using System.Collections.Generic;

namespace Aog.Core.Mesh.RadioBridge;

/// <summary>
/// Envelope describing a mesh publication transported over the radio bridge.
/// </summary>
public sealed record RadioBridgePublicationMessage(
    string Topic,
    MeshDataTier Tier,
    DateTimeOffset PublishedAt,
    string PublisherDeviceId,
    IReadOnlyDictionary<string, string>? Metadata,
    byte[] Payload)
{
    /// <summary>
    /// Gets the length of the payload, accounting for null arrays.
    /// </summary>
    public int PayloadLength => Payload?.Length ?? 0;
}
