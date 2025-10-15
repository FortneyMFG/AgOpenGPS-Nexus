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

    private static LegacyCompatibilityBridge CreateBridge()
    {
        return new LegacyCompatibilityBridge(
            new LegacyPoseCodec(),
            new LegacySteerCodec(),
            new LegacyDiscoveryCodec(),
            NullLogger<LegacyCompatibilityBridge>.Instance);
    }
}
