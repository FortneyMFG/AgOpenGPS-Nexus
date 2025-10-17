using Aog.Agio.Legacy;
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
