using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aog.Agio.Legacy;
using Aog.Core.V1;
using Google.Protobuf.WellKnownTypes;
using Xunit;

namespace Aog.Agio.Tests;

public sealed class TeensyBridgeRegressionTests
{
    [Fact]
    public async Task ReplayRecordedSession_ProducesExpectedTelemetry()
    {
        var frames = LoadRecordedSession("TestData/teensy_bridge_session1.txt");

        var poseCodec = new LegacyPoseCodec();
        var discoveryCodec = new LegacyDiscoveryCodec();
        var steerCodec = new LegacySteerCodec();
        var transport = new RecordingTransport();
        var poseObserver = new RecordingPoseObserver();
        var discoveryObserver = new RecordingDiscoveryObserver();
        var steerCommandObserver = new RecordingSteerCommandObserver();
        var steerStateObserver = new RecordingSteerStateObserver();
        var sectionObserver = new RecordingSectionObserver();
        var timeProvider = new IncrementingTimeProvider(new DateTimeOffset(2024, 05, 05, 12, 0, 0, TimeSpan.Zero), TimeSpan.FromMilliseconds(50));

        var gateway = new LegacyUdpGateway(
            poseCodec,
            discoveryCodec,
            steerCodec,
            transport,
            poseObserver,
            discoveryObserver,
            steerCommandObserver,
            steerStateObserver,
            sectionObserver,
            timeProvider);

        foreach (var frame in frames)
        {
            await gateway.HandleDatagramAsync(frame);
        }

        var announcement = Assert.Single(discoveryObserver.Announcements);
        Assert.Equal(0x7C, announcement.VendorId);
        Assert.Equal(0x01, announcement.ProductId);
        Assert.Equal(0x02, announcement.VariantId);
        Assert.Equal(0x10, announcement.McuId);
        Assert.Equal(1, announcement.FirmwareMajor);
        Assert.Equal(4, announcement.FirmwareMinor);
        Assert.Equal(2, announcement.FirmwarePatch);

        var pose = Assert.Single(poseObserver.Poses);
        Assert.NotNull(pose.Header);
        Assert.Equal("legacy/udp/main_gps", pose.Header.Source);
        Assert.Equal("earth", pose.Header.Frame);
        Assert.Equal(1UL, pose.Header.Sequence);
        Assert.Equal(51.123456, pose.LatitudeDeg, 6);
        Assert.Equal(-114.345678, pose.LongitudeDeg, 6);
        Assert.Equal(7.8 / 3.6, pose.SpeedMps, 6);

        var poseMetadata = Assert.Single(poseObserver.Metadata);
        Assert.Equal((ushort)17, poseMetadata.SatellitesTracked);
        Assert.Equal((byte)5, poseMetadata.FixQuality);
        Assert.Equal((ushort)95, poseMetadata.HdopTimes100);
        Assert.Equal((ushort)8, poseMetadata.AgeOfCorrectionsTimes100);

        var command = Assert.Single(steerCommandObserver.Commands);
        Assert.NotNull(command.Header);
        Assert.Equal("legacy/udp/steer_cmd", command.Header.Source);
        Assert.Equal(-3.75, command.TargetWheelAngleDeg, 2);

        var commandMetadata = Assert.Single(steerCommandObserver.Metadata);
        Assert.Equal(13.6, commandMetadata.SpeedKph, 1);
        Assert.Equal(1, commandMetadata.GuidanceStatus);
        Assert.Equal(0x2A, commandMetadata.TramControl);

        var section = Assert.Single(sectionObserver.Masks);
        Assert.Equal((uint)0x0F3C, section.Mask);
        Assert.Equal(16u, section.SectionCount);

        var state = Assert.Single(steerStateObserver.States);
        Assert.Equal(-3.2, state.MeasuredWheelAngleDeg, 1);
        Assert.True(state.Engaged);

        var stateMetadata = Assert.Single(steerStateObserver.Metadata);
        Assert.Equal(87.65, stateMetadata.HeadingDeg, 2);
        Assert.Equal(-1.25, stateMetadata.RollDeg, 2);
        Assert.True(stateMetadata.IsSteerSwitchOn);
        Assert.Equal(0x03, stateMetadata.SwitchByte);
        Assert.Equal(128, stateMetadata.RawPwm);

        Assert.Empty(transport.Frames);
    }

    private static IReadOnlyList<ReadOnlyMemory<byte>> LoadRecordedSession(string relativePath)
    {
        var baseDirectory = AppContext.BaseDirectory;
        var fullPath = System.IO.Path.Combine(baseDirectory, relativePath);
        var frames = new List<ReadOnlyMemory<byte>>();

        foreach (var line in File.ReadAllLines(fullPath))
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            var bytes = line
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(token => Convert.ToByte(token, 16))
                .ToArray();

            frames.Add(bytes);
        }

        return frames;
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

    private sealed class RecordingPoseObserver : ILegacyPoseObserver
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

    private sealed class RecordingDiscoveryObserver : ILegacyDiscoveryObserver
    {
        public List<LegacyDiscoveryAnnouncement> Announcements { get; } = new();

        public ValueTask OnDiscoveryAsync(LegacyDiscoveryAnnouncement announcement, CancellationToken cancellationToken)
        {
            Announcements.Add(announcement);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class RecordingSteerCommandObserver : ILegacySteerCommandObserver
    {
        public List<SteerCmd> Commands { get; } = new();
        public List<LegacySteerCommandMetadata> Metadata { get; } = new();

        public ValueTask OnSteerCommandAsync(SteerCmd command, LegacySteerCommandMetadata metadata, CancellationToken cancellationToken)
        {
            Commands.Add(command);
            Metadata.Add(metadata);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class RecordingSteerStateObserver : ILegacySteerStateObserver
    {
        public List<SteerState> States { get; } = new();
        public List<LegacySteerStateMetadata> Metadata { get; } = new();

        public ValueTask OnSteerStateAsync(SteerState state, LegacySteerStateMetadata metadata, CancellationToken cancellationToken)
        {
            States.Add(state);
            Metadata.Add(metadata);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class RecordingSectionObserver : ILegacySectionObserver
    {
        public List<SectionMask> Masks { get; } = new();

        public ValueTask OnSectionMaskAsync(SectionMask mask, CancellationToken cancellationToken)
        {
            Masks.Add(mask);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class IncrementingTimeProvider : TimeProvider
    {
        private DateTimeOffset _current;
        private readonly TimeSpan _step;

        public IncrementingTimeProvider(DateTimeOffset start, TimeSpan step)
        {
            _current = start;
            _step = step;
        }

        public override DateTimeOffset GetUtcNow()
        {
            var value = _current;
            _current = _current.Add(_step);
            return value;
        }
    }
}
