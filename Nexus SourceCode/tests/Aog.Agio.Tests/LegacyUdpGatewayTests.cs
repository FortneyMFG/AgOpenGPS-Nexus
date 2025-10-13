using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aog.Agio.Legacy;
using Aog.Core.V1;
using Google.Protobuf.WellKnownTypes;
using Xunit;

namespace Aog.Agio.Tests;

public sealed class LegacyUdpGatewayTests
{
    [Fact]
    public async Task PublishPoseAsync_SendsEncodedFrame()
    {
        var poseCodec = new LegacyPoseCodec();
        var autoSteerCodec = new LegacyAutoSteerCodec();
        var transport = new RecordingTransport();
        var observer = new RecordingObserver();
        var gateway = new LegacyUdpGateway(poseCodec, autoSteerCodec, transport, observer, new FixedTimeProvider(DateTimeOffset.UtcNow));

        var pose = new Pose { LatitudeDeg = 51.2, LongitudeDeg = -114.1 };
        var metadata = new LegacyPoseMetadata { FixQuality = 4, SatellitesTracked = 17 };

        await gateway.PublishPoseAsync(pose, metadata);

        Assert.Single(transport.Frames);
        Assert.True(poseCodec.TryDecodePose(transport.Frames[0].Span, out var decodedPose, out var decodedMetadata));
        Assert.Equal(pose.LatitudeDeg, decodedPose.LatitudeDeg, 6);
        Assert.Equal(metadata.FixQuality, decodedMetadata.FixQuality);
        Assert.Equal(metadata.SatellitesTracked, decodedMetadata.SatellitesTracked);
    }

    [Fact]
    public async Task HandleDatagramAsync_ForwardsPoseToObserver()
    {
        var poseCodec = new LegacyPoseCodec();
        var autoSteerCodec = new LegacyAutoSteerCodec();
        var transport = new RecordingTransport();
        var observer = new RecordingObserver();
        var timestamp = new DateTimeOffset(2024, 05, 01, 12, 30, 00, TimeSpan.Zero);
        var gateway = new LegacyUdpGateway(poseCodec, autoSteerCodec, transport, observer, new FixedTimeProvider(timestamp));

        var pose = new Pose { LatitudeDeg = 51.123, LongitudeDeg = -114.456, HeadingRad = 1.5 };
        var frame = poseCodec.EncodePose(pose);

        await gateway.HandleDatagramAsync(frame);

        Assert.Single(observer.Poses);
        var observed = observer.Poses[0];
        Assert.NotNull(observed.Header);
        Assert.Equal("legacy/udp/main_gps", observed.Header.Source);
        Assert.Equal("earth", observed.Header.Frame);
        Assert.Equal(1UL, observed.Header.Sequence);
        Assert.Equal(Timestamp.FromDateTimeOffset(timestamp), observed.Header.Timestamp);
        Assert.Equal(pose.LatitudeDeg, observed.LatitudeDeg, 6);

        Assert.Single(observer.Metadata);
    }

    [Fact]
    public async Task HandleDatagramAsync_IgnoresInvalidFrame()
    {
        var poseCodec = new LegacyPoseCodec();
        var gateway = new LegacyUdpGateway(poseCodec, new LegacyAutoSteerCodec(), new RecordingTransport(), new RecordingObserver(), new FixedTimeProvider(DateTimeOffset.UtcNow));
        var invalid = new byte[LegacyPoseCodec.MainAntennaFrameLength];

        await gateway.HandleDatagramAsync(invalid);

        // No exception and no observer calls expected.
    }

    [Fact]
    public async Task PublishSteerCommandAsync_EncodesLegacyPgn()
    {
        var transport = new RecordingTransport();
        var gateway = new LegacyUdpGateway(new LegacyPoseCodec(), new LegacyAutoSteerCodec(), transport, new RecordingObserver(), new FixedTimeProvider(DateTimeOffset.UtcNow));

        var command = new SteerCmd { Enable = true, TargetWheelAngleDeg = 12.34 };

        await gateway.PublishSteerCommandAsync(command);

        Assert.Single(transport.Frames);
        var frame = transport.Frames[0].Span;
        Assert.Equal(LegacyAutoSteerCodec.AutoSteerFrameLength, frame.Length);
        Assert.Equal(LegacyPoseCodec.Sync0, frame[0]);
        Assert.Equal(LegacyPoseCodec.Sync1, frame[1]);
        Assert.Equal(LegacyAutoSteerCodec.AutoSteerDataPgn, frame[3]);
        Assert.Equal(LegacyAutoSteerCodec.AutoSteerPayloadLength, frame[4]);

        // steerAngleLo at payload index 3 -> frame[8]
        var steerAngle = frame[8] | (frame[9] << 8);
        Assert.Equal(1234, steerAngle);
        Assert.Equal(1, frame[7]);
    }

    [Fact]
    public async Task PublishSectionMaskAsync_UsesLatestSteerSnapshot()
    {
        var transport = new RecordingTransport();
        var gateway = new LegacyUdpGateway(new LegacyPoseCodec(), new LegacyAutoSteerCodec(), transport, new RecordingObserver(), new FixedTimeProvider(DateTimeOffset.UtcNow));

        await gateway.PublishSteerCommandAsync(new SteerCmd { Enable = true, TargetWheelAngleDeg = 5 });
        transport.Frames.Clear();

        var mask = new SectionMask { Mask = 0b_0000_0011_0000_0101 };
        await gateway.PublishSectionMaskAsync(mask);

        Assert.Single(transport.Frames);
        var frame = transport.Frames[0].Span;
        Assert.Equal(0b_0000_0101, frame[11]);
        Assert.Equal(0b_0000_0011, frame[12]);

        // Angle snapshot should be preserved (5 degrees * 100).
        var steerAngle = (short)(frame[8] | (frame[9] << 8));
        Assert.Equal(500, steerAngle);
    }

    private sealed class RecordingTransport : ILegacyUdpTransport
    {
        public List<ReadOnlyMemory<byte>> Frames { get; } = new();

        public ValueTask SendAsync(ReadOnlyMemory<byte> datagram, CancellationToken cancellationToken)
        {
            Frames.Add(datagram.ToArray());
            return ValueTask.CompletedTask;
        }
    }

    private sealed class RecordingObserver : ILegacyPoseObserver
    {
        public List<Pose> Poses { get; } = new();
        public List<LegacyPoseMetadata> Metadata { get; } = new();

        public ValueTask OnPoseAsync(Pose pose, LegacyPoseMetadata metadata, CancellationToken cancellationToken)
        {
            Poses.Add(pose);
            Metadata.Add(metadata);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _value;

        public FixedTimeProvider(DateTimeOffset value)
        {
            _value = value;
        }

        public override DateTimeOffset GetUtcNow() => _value;
    }
}
