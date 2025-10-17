using Aog.Agio.Legacy;
using Aog.Bridge.Host.AogLink.Legacy;
using Aog.Link.V1;
using Microsoft.Extensions.Logging.Abstractions;
using Aog.Core.V1;

namespace Aog.Bridge.Host.Tests;

public sealed class LegacyCompatibilityBridgeTests
{
    [Fact]
    public void TryConvertLegacyPose_DecodesToLinkEnvelope()
    {
        var poseCodec = new LegacyPoseCodec();
        var bridge = CreateBridge();
        var pose = new Aog.Core.V1.Pose { LatitudeDeg = 51.5, LongitudeDeg = -0.12 };
        var frame = poseCodec.EncodePose(pose);

        Assert.True(bridge.TryConvertLegacyFrame(frame, out var envelope));
        Assert.Equal(MessageType.LinkMessageTypeTelemetryPose, envelope.Header.MessageType);
        Assert.NotNull(envelope.Pose);
        Assert.Equal(pose.LatitudeDeg, envelope.Pose.LatitudeDeg);
    }

    [Fact]
    public void TryConvertLegacySteerCommand_RemoteStatusDoesNotEngage()
    {
        var steerCodec = new LegacySteerCodec();
        var bridge = CreateBridge();
        var command = new SteerCmd { TargetWheelAngleDeg = 3.0, Enable = false };
        var metadata = new LegacySteerCommandMetadata
        {
            // Remote + tram (or gps) bits set, but engaged bit (0x01) remains clear
            GuidanceStatus = 0b0000_0110,
        };
        var frame = steerCodec.EncodeSteerCommand(command, metadata: metadata);

        Assert.True(bridge.TryConvertLegacyFrame(frame, out var envelope));
        Assert.Equal(MessageType.LinkMessageTypeCommandSteer, envelope.Header.MessageType);
        Assert.NotNull(envelope.SteerCommand);
        Assert.False(envelope.SteerCommand.Enable);
    }

    [Fact]
    public void TryConvertLegacySteerCommand_EngagedBitEnablesCommand()
    {
        var steerCodec = new LegacySteerCodec();
        var bridge = CreateBridge();
        var command = new SteerCmd { TargetWheelAngleDeg = -2.5, Enable = true };
        var metadata = new LegacySteerCommandMetadata
        {
            // Preserve other status bits; engaged bit should be set by the encoder
            GuidanceStatus = 0b0000_0110,
        };
        var frame = steerCodec.EncodeSteerCommand(command, metadata: metadata);

        Assert.True(bridge.TryConvertLegacyFrame(frame, out var envelope));
        Assert.Equal(MessageType.LinkMessageTypeCommandSteer, envelope.Header.MessageType);
        Assert.NotNull(envelope.SteerCommand);
        Assert.True(envelope.SteerCommand.Enable);
    }

    private static LegacyCompatibilityBridge CreateBridge()
    {
        return new LegacyCompatibilityBridge(
            new LegacyPoseCodec(),
            new LegacySteerCodec(),
            new LegacyDiscoveryCodec(),
            NullLogger<LegacyCompatibilityBridge>.Instance);
    }
}
