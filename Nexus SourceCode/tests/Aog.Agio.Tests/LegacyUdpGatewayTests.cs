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
        var codec = new LegacyPoseCodec();
        var transport = new RecordingTransport();
        var observer = new RecordingObserver();
        var gateway = new LegacyUdpGateway(codec, transport, observer, new FixedTimeProvider(DateTimeOffset.UtcNow));

        var pose = new Pose { LatitudeDeg = 51.2, LongitudeDeg = -114.1 };
        var metadata = new LegacyPoseMetadata { FixQuality = 4, SatellitesTracked = 17 };

        await gateway.PublishPoseAsync(pose, metadata);

        Assert.Single(transport.Frames);
        Assert.True(codec.TryDecodePose(transport.Frames[0].Span, out var decodedPose, out var decodedMetadata));
        Assert.Equal(pose.LatitudeDeg, decodedPose.LatitudeDeg, 6);
        Assert.Equal(metadata.FixQuality, decodedMetadata.FixQuality);
        Assert.Equal(metadata.SatellitesTracked, decodedMetadata.SatellitesTracked);
    }

    [Fact]
    public async Task HandleDatagramAsync_ForwardsPoseToObserver()
    {
        var codec = new LegacyPoseCodec();
        var transport = new RecordingTransport();
        var observer = new RecordingObserver();
        var timestamp = new DateTimeOffset(2024, 05, 01, 12, 30, 00, TimeSpan.Zero);
        var gateway = new LegacyUdpGateway(codec, transport, observer, new FixedTimeProvider(timestamp));

        var pose = new Pose { LatitudeDeg = 51.123, LongitudeDeg = -114.456, HeadingRad = 1.5 };
        var frame = codec.EncodePose(pose);

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
        var codec = new LegacyPoseCodec();
        var gateway = new LegacyUdpGateway(codec, new RecordingTransport(), new RecordingObserver(), new FixedTimeProvider(DateTimeOffset.UtcNow));
        var invalid = new byte[LegacyPoseCodec.MainAntennaFrameLength];

        await gateway.HandleDatagramAsync(invalid);

        // No exception and no observer calls expected.
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
