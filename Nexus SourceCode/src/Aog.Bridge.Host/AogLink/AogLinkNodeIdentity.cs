using System;
using System.Security.Cryptography;
using System.Text;
using Aog.Link.V1;

namespace Aog.Bridge.Host.AogLink;

/// <summary>
/// Provides a stable node identity for the bridge when emitting AOG-Link frames.
/// </summary>
public sealed class AogLinkNodeIdentity
{
    /// <summary>
    /// Constructs a new <see cref="AogLinkNodeIdentity"/> from the configured bridge options.
    /// </summary>
    /// <param name="nodeId">Human readable identifier configured for the bridge.</param>
    /// <param name="firmwareVersion">Firmware version exposed to the link.</param>
    /// <param name="role">Role advertised on the AOG-Link bus.</param>
    /// <param name="priority">Priority used when arbitrating between multiple publishers.</param>
    public AogLinkNodeIdentity(string nodeId, string firmwareVersion, NodeRole role, NodePriority priority)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
            throw new ArgumentException("Node identifier is required.", nameof(nodeId));
        if (string.IsNullOrWhiteSpace(firmwareVersion))
            throw new ArgumentException("Firmware version is required.", nameof(firmwareVersion));

        NodeId = nodeId;
        FirmwareVersion = firmwareVersion;
        Role = role;
        Priority = priority;
        Address = ComputeStableAddress(nodeId);
    }

    /// <summary>
    /// Gets the human-readable node identifier.
    /// </summary>
    public string NodeId { get; }

    /// <summary>
    /// Gets the firmware version string advertised to peers.
    /// </summary>
    public string FirmwareVersion { get; }

    /// <summary>
    /// Gets the numeric node address derived from <see cref="NodeId"/>.
    /// </summary>
    public ushort Address { get; }

    /// <summary>
    /// Gets the role advertised on the bus.
    /// </summary>
    public NodeRole Role { get; }

    /// <summary>
    /// Gets the node priority used when arbitrating telemetry.
    /// </summary>
    public NodePriority Priority { get; }

    /// <summary>
    /// Materialises the protobuf identity descriptor for emission on the wire.
    /// </summary>
    public NodeIdentity ToProto() => new()
    {
        NodeId = Address,
        Role = Role,
        Priority = Priority,
        HardwareModel = NodeId,
        FirmwareVersion = FirmwareVersion,
    };

    private static ushort ComputeStableAddress(string nodeId)
    {
        // We derive a deterministic 16-bit identifier from the configured node id using
        // SHA-256. Truncation is acceptable because the Bridge performs arbitration on
        // collisions and operators can override the node id when necessary.
        var buffer = Encoding.UTF8.GetBytes(nodeId);
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(buffer, hash);
        return (ushort)((hash[0] << 8) | hash[1]);
    }
}
