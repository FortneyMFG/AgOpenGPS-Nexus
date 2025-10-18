using Aog.Agio.Legacy;
using Aog.Core.V1;
using Aog.Bridge.Host.AogLink.Legacy;
using Aog.Link.V1;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aog.Bridge.Host.Tests;

public sealed class LegacyCompatibilityBridgeTests
{
    [Fact]
    public void TryConvertLegacyPose_DecodesToLinkEnvelope()
    {
        var poseCodec = new LegacyPoseCodec();
        var bridge = CreateBridge();
        var pose = new Pose { LatitudeDeg = 51.5, LongitudeDeg = -0.12 };
        var frame = poseCodec.EncodePose(pose);

        Assert.True(bridge.TryConvertLegacyFrame(frame, out var envelope));
        Assert.Equal(MessageType.LinkMessageTypeTelemetryPose, envelope.Header.MessageType);
        Assert.NotNull(envelope.Pose);
        Assert.Equal(pose.LatitudeDeg, envelope.Pose.LatitudeDeg);
        Assert.Equal(pose.LongitudeDeg, envelope.Pose.LongitudeDeg);
    }

    // --- SectionMask behaviors ---

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

    // --- Remote/engaged status semantics ---

    [Fact]
    public void TryConvertLegacySteerCommand_RemoteStatusDoesNotEngage()
    {
        var steerCodec = new LegacySteerCodec();
        var bridge = CreateBridge();
        var command = new SteerCmd { TargetWheelAngleDeg = 3.0, Enable = false };

        // Remote + tram/GPS bits set but NOT engaged
        var metadata = new LegacySteerCommandMetadata
        {
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

        // Preserve other status bits; encoder should set engaged appropriately
        var metadata = new LegacySteerCommandMetadata
        {
            GuidanceStatus = 0b0000_0110,
        };

        var frame = steerCodec.EncodeSteerCommand(command, metadata: metadata);

        Assert.True(bridge.TryConvertLegacyFrame(frame, out var envelope));
        Assert.Equal(MessageType.LinkMessageTypeCommandSteer, envelope.Header.MessageType);
        Assert.NotNull(envelope.SteerCommand);
        Assert.True(envelope.SteerCommand.Enable);
    }

    [Fact]
    public void TryConvertLegacySteerState_AttachesHeadingMetadata()
    {
        var bridge = CreateBridge();
        var steerCodec = new LegacySteerCodec();
        var sourceState = new Aog.Core.V1.SteerState
        {
            MeasuredWheelAngleDeg = 1.5,
            AppliedEffort = 0.25,
            HeadingErrorRad = 0,
        };

        var metadata = new LegacySteerStateMetadata { HeadingDeg = 87.5 };
        var frame = steerCodec.EncodeSteerState(sourceState, metadata);

        Assert.True(bridge.TryConvertLegacyFrame(frame, out var envelope));
        var steerState = Assert.IsType<Aog.Core.V1.SteerState>(envelope.SteerState);

        Assert.Equal(MessageType.LinkMessageTypeTelemetrySteerState, envelope.Header.MessageType);
        Assert.Equal(0, steerState.HeadingErrorRad);
        Assert.True(steerState.TryGetLegacyHeadingDegrees(out var heading));
        Assert.Equal(metadata.HeadingDeg, heading, 3);
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
