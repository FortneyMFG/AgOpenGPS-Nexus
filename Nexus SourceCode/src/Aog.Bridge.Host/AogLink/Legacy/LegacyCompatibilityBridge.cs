using System;
using System.Linq;
using Aog.Agio.Legacy;
using Aog.Core.V1;
using Aog.Link.V1;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;

namespace Aog.Bridge.Host.AogLink.Legacy;

/// <summary>
/// Bridges AOG-Link envelopes with the legacy UDP PGN representations so legacy modules
/// remain operational during migration.
/// </summary>
public sealed class LegacyCompatibilityBridge
{
    private readonly LegacyPoseCodec _poseCodec;
    private readonly LegacySteerCodec _steerCodec;
    private readonly LegacyDiscoveryCodec _discoveryCodec;
    private readonly ILogger<LegacyCompatibilityBridge> _logger;

    public LegacyCompatibilityBridge(
        LegacyPoseCodec poseCodec,
        LegacySteerCodec steerCodec,
        LegacyDiscoveryCodec discoveryCodec,
        ILogger<LegacyCompatibilityBridge> logger)
    {
        _poseCodec = poseCodec ?? throw new ArgumentNullException(nameof(poseCodec));
        _steerCodec = steerCodec ?? throw new ArgumentNullException(nameof(steerCodec));
        _discoveryCodec = discoveryCodec ?? throw new ArgumentNullException(nameof(discoveryCodec));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Attempts to translate a legacy datagram into an AOG-Link envelope.
    /// </summary>
    public bool TryConvertLegacyFrame(ReadOnlySpan<byte> datagram, out LinkEnvelope envelope)
    {
        envelope = new LinkEnvelope();

        if (_discoveryCodec.TryDecode(datagram, out var announcement))
        {
            var announce = new DiscoveryAnnounce
            {
                Identity = new NodeIdentity
                {
                    NodeId = announcement.VendorId,
                    FirmwareVersion = announcement.FirmwareVersion,
                    HardwareModel = ((LegacyDeviceMcu)announcement.McuId).ToString(),
                    Priority = NodePriority.NodePriorityDefault,
                    Role = NodeRole.NodeRoleController,
                },
                SessionId = 0,
            };

            announce.CapabilityIds.AddRange(announcement
                .ToCapabilityDescriptors()
                .Select(descriptor => descriptor.Name));

            envelope.Header = BuildSystemHeader(MessageType.LinkMessageTypeDiscoveryAnnounce, announce.CalculateSize());
            envelope.DiscoveryAnnounce = announce;
            return true;
        }

        if (_steerCodec.TryDecodeSteerState(datagram, out var steerState, out var steerMetadata))
        {
            StampHeader(steerState, "legacy/pgn/steer_state", "vehicle");
            steerState.HeadingErrorRad = LegacyAngles.DegreesToRadians(steerMetadata.HeadingDeg);
            steerState.LateralErrorM = 0;

            envelope.Header = BuildTelemetryHeader(MessageType.LinkMessageTypeTelemetrySteerState, steerState.CalculateSize());
            envelope.SteerState = steerState;
            return true;
        }

        if (_steerCodec.TryDecodeSteerCommand(datagram, out var steerCmd, out var steerCommandMetadata, out var sectionMask))
        {
            StampHeader(steerCmd, "legacy/pgn/steer_cmd", "vehicle");
            steerCmd.Enable = steerCommandMetadata.GuidanceStatus != 0;

            envelope.Header = BuildCommandHeader(MessageType.LinkMessageTypeCommandSteer, steerCmd.CalculateSize(), needsAck: true);
            envelope.SteerCommand = steerCmd;

            if (sectionMask.SectionCount > 0)
            {
                StampHeader(sectionMask, "legacy/pgn/sections", "implement");
                envelope.SectionMask = sectionMask;
            }

            return true;
        }

        if (_poseCodec.TryDecodePose(datagram, out var pose, out _))
        {
            StampHeader(pose, "legacy/pgn/gps", "earth");

            envelope.Header = BuildTelemetryHeader(MessageType.LinkMessageTypeTelemetryPose, pose.CalculateSize());
            envelope.Pose = pose;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to translate an AOG-Link envelope into a legacy datagram.
    /// </summary>
    public bool TryConvertToLegacy(LinkEnvelope envelope, out byte[] datagram)
    {
        if (envelope is null)
            throw new ArgumentNullException(nameof(envelope));

        datagram = Array.Empty<byte>();

        switch (envelope.Header?.MessageType)
        {
            case MessageType.LinkMessageTypeTelemetryPose when envelope.Pose is not null:
                datagram = _poseCodec.EncodePose(envelope.Pose, new LegacyPoseMetadata());
                return true;
            case MessageType.LinkMessageTypeTelemetrySteerState when envelope.SteerState is not null:
                datagram = _steerCodec.EncodeSteerState(envelope.SteerState, new LegacySteerStateMetadata());
                return true;
            case MessageType.LinkMessageTypeCommandSteer when envelope.SteerCommand is not null:
                datagram = _steerCodec.EncodeSteerCommand(
                    envelope.SteerCommand,
                    envelope.SectionMask,
                    new LegacySteerCommandMetadata());
                return true;
            case MessageType.LinkMessageTypeDiscoveryAnnounce:
                // The bridge originates discovery responses on behalf of the legacy devices; announcements are relayed as-is.
                _logger.LogDebug("Ignoring attempt to emit discovery announce back onto legacy PGNs.");
                return false;
        }

        return false;
    }

    private static void StampHeader(Pose pose, string source, string frame)
    {
        pose.Header ??= new Header();
        pose.Header.Source = source;
        pose.Header.Frame = frame;
        pose.Header.Timestamp ??= Timestamp.FromDateTime(DateTime.UtcNow);
    }

    private static void StampHeader(SteerState state, string source, string frame)
    {
        state.Header ??= new Header();
        state.Header.Source = source;
        state.Header.Frame = frame;
        state.Header.Timestamp ??= Timestamp.FromDateTime(DateTime.UtcNow);
    }

    private static void StampHeader(SteerCmd command, string source, string frame)
    {
        command.Header ??= new Header();
        command.Header.Source = source;
        command.Header.Frame = frame;
        command.Header.Timestamp ??= Timestamp.FromDateTime(DateTime.UtcNow);
    }

    private static void StampHeader(SectionMask mask, string source, string frame)
    {
        mask.Header ??= new Header();
        mask.Header.Source = source;
        mask.Header.Frame = frame;
        mask.Header.Timestamp ??= Timestamp.FromDateTime(DateTime.UtcNow);
    }

    private static FrameHeader BuildTelemetryHeader(MessageType type, int payloadSize) => new()
    {
        Version = 1,
        MessageClass = LinkClass.Telemetry,
        MessageType = type,
        PayloadLength = (uint)payloadSize,
    };

    private static FrameHeader BuildSystemHeader(MessageType type, int payloadSize) => new()
    {
        Version = 1,
        MessageClass = LinkClass.System,
        MessageType = type,
        PayloadLength = (uint)payloadSize,
    };

    private static FrameHeader BuildCommandHeader(MessageType type, int payloadSize, bool needsAck) => new()
    {
        Version = 1,
        MessageClass = LinkClass.Command,
        MessageType = type,
        PayloadLength = (uint)payloadSize,
        Flags = new FrameFlags
        {
            NeedsAck = needsAck,
        },
    };
}

internal static class LegacyAngles
{
    public static double DegreesToRadians(double value) => value * Math.PI / 180.0;
}
