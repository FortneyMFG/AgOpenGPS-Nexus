using Aog.Agio.Legacy;
using Aog.Core.V1;
using Aog.Bridge.Host.AogLink.Legacy;
using Aog.Link.V1;
using Microsoft.Extensions.Logging.Abstractions;

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
    public void TryConvertLegacySteerCommand_WithZeroSectionMask_DropsMaskPayload()
    {
        var steerCodec = new LegacySteerCodec();
        var bridge = CreateBridge();
        var command = new SteerCmd { Enable = true, TargetWheelAngleDeg = 10.0 };
        var frame = steerCodec.EncodeSteerCommand(
            command,
            new SectionMask
            {
                SectionCount = 16,
                Mask = 0u,
            });

        Assert.True(bridge.TryConvertLegacyFrame(frame, out var envelope));
        Assert.Equal(MessageType.LinkMessageTypeCommandSteer, envelope.Header.MessageType);
        Assert.NotNull(envelope.SteerCommand);
        Assert.Null(envelope.SectionMask);
    }

    [Fact]
    public void TryConvertLegacySteerCommand_WithNonZeroSectionMask_PopulatesPayload()
    {
        var steerCodec = new LegacySteerCodec();
        var bridge = CreateBridge();
        var command = new SteerCmd { Enable = true, TargetWheelAngleDeg = 15.0 };
        var expectedMask = new SectionMask
        {
            SectionCount = 16,
            Mask = 0b11u,
        };
        var frame = steerCodec.EncodeSteerCommand(command, expectedMask);

        Assert.True(bridge.TryConvertLegacyFrame(frame, out var envelope));
        Assert.Equal(MessageType.LinkMessageTypeCommandSteer, envelope.Header.MessageType);
        Assert.NotNull(envelope.SteerCommand);
        Assert.NotNull(envelope.SectionMask);
        Assert.Equal(expectedMask.Mask, envelope.SectionMask.Mask);
        Assert.Equal(expectedMask.SectionCount, envelope.SectionMask.SectionCount);
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
