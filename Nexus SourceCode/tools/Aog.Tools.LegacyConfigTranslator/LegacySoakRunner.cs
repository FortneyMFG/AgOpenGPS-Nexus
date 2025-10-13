using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Aog.Agio.Legacy;
using Aog.Core.V1;
using Google.Protobuf.WellKnownTypes;

namespace Aog.Tools.LegacyConfigTranslator;

/// <summary>
/// Executes high-rate UDP and serial stress tests against the legacy codecs.
/// </summary>
public sealed class LegacySoakRunner
{
    public async Task<LegacySoakReport> RunAsync(LegacySoakOptions options)
    {
        var udpReport = await RunUdpSoakAsync(options).ConfigureAwait(false);
        var serialReport = RunSerialSoak(options);

        return new LegacySoakReport(
            StartedAtUtc: udpReport.StartedAtUtc,
            DurationRequestedSeconds: options.DurationSeconds,
            DurationElapsedSeconds: udpReport.ElapsedSeconds,
            Udp: udpReport.Report,
            Serial: serialReport);
    }

    private static async Task<(DateTimeOffset StartedAtUtc, double ElapsedSeconds, LegacyUdpSoakReport Report)> RunUdpSoakAsync(LegacySoakOptions options)
    {
        var poseCodec = new LegacyPoseCodec();
        var discoveryCodec = new LegacyDiscoveryCodec();
        var steerCodec = new LegacySteerCodec();
        var transport = new NullTransport();

        var poseObserver = new CountingPoseObserver();
        var discoveryObserver = new CountingDiscoveryObserver();
        var steerCommandObserver = new CountingSteerCommandObserver();
        var steerStateObserver = new CountingSteerStateObserver();
        var sectionObserver = new CountingSectionObserver();

        var frameStep = TimeSpan.FromSeconds(1.0 / (options.UdpRatePerStreamHz * 3.0));
        var startTime = DateTimeOffset.UtcNow;
        var timeProvider = new IncrementingTimeProvider(startTime, frameStep);
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

        var framesPerStream = (int)Math.Round(options.UdpRatePerStreamHz * options.DurationSeconds);
        framesPerStream = Math.Max(framesPerStream, 1);

        var stopwatch = Stopwatch.StartNew();

        for (var i = 0; i < framesPerStream; i++)
        {
            var pose = new Pose
            {
                LatitudeDeg = 51.0 + i * 1e-6,
                LongitudeDeg = -114.0 + i * 1e-6,
                HeadingRad = (i % 360) * Math.PI / 180.0,
                SpeedMetresPerSecond = 5.0 + (i % 5) * 0.2,
            };

            var metadata = new LegacyPoseMetadata
            {
                FixQuality = 4,
                SatellitesTracked = 18,
                HdopTimes100 = 75,
            };

            var frame = poseCodec.EncodePose(pose, metadata);
            await gateway.HandleDatagramAsync(frame).ConfigureAwait(false);

            var steerState = new SteerState
            {
                MeasuredWheelAngleDeg = Math.Sin(i * 0.05) * 12,
                AppliedEffort = 0.5,
                Engaged = true,
            };

            var steerStateMetadata = new LegacySteerStateMetadata
            {
                HeadingDeg = (i % 360),
                RollDeg = Math.Sin(i * 0.1),
                IsSteerSwitchOn = true,
                RawPwm = 180,
            };

            var stateFrame = steerCodec.EncodeSteerState(steerState, steerStateMetadata);
            await gateway.HandleDatagramAsync(stateFrame).ConfigureAwait(false);

            var steerCommand = new SteerCmd
            {
                TargetWheelAngleDeg = Math.Cos(i * 0.05) * 14,
                Enable = true,
            };

            var sectionMask = new SectionMask
            {
                SectionCount = 16,
                Mask = 0x00FF,
            };

            var steerMetadata = new LegacySteerCommandMetadata
            {
                SpeedKph = 12.5,
                GuidanceStatus = 1,
                TramControl = 0x01,
            };

            var commandFrame = steerCodec.EncodeSteerCommand(steerCommand, sectionMask, steerMetadata);
            await gateway.HandleDatagramAsync(commandFrame).ConfigureAwait(false);
        }

        // Emit one discovery frame at the end.
        var announcement = new LegacyDiscoveryAnnouncement
        {
            VendorId = 0x7C,
            ProductId = 0x01,
            VariantId = 0x02,
            McuId = (byte)LegacyDeviceMcu.Teensy,
            FirmwareMajor = 1,
            FirmwareMinor = 2,
            FirmwarePatch = 3,
            Capabilities = LegacyDeviceCapabilityFlags.CanBootloader | LegacyDeviceCapabilityFlags.OverTheAirUpdates,
            Health = LegacyDeviceHealthFlags.None,
        };

        var discoveryFrame = discoveryCodec.Encode(announcement);
        await gateway.HandleDatagramAsync(discoveryFrame).ConfigureAwait(false);

        stopwatch.Stop();

        var totalFrames = poseObserver.Count + steerStateObserver.Count + steerCommandObserver.Count + sectionObserver.Count + discoveryObserver.Count;
        var elapsedSeconds = Math.Max(stopwatch.Elapsed.TotalSeconds, 1e-6);

        var report = new LegacyUdpSoakReport(
            PoseFrames: poseObserver.Count,
            SteerCommandFrames: steerCommandObserver.Count,
            SteerStateFrames: steerStateObserver.Count,
            SectionFrames: sectionObserver.Count,
            DiscoveryFrames: discoveryObserver.Count,
            TotalFrames: totalFrames,
            TargetRatePerStreamHz: options.UdpRatePerStreamHz,
            EffectiveRateHz: totalFrames / elapsedSeconds,
            PoseSpacingStats: poseObserver.GetSpacingStats(),
            SteerStateSpacingStats: steerStateObserver.GetSpacingStats());

        return (startTime, elapsedSeconds, report);
    }

    private static LegacySerialSoakReport RunSerialSoak(LegacySoakOptions options)
    {
        var steerCodec = new LegacySteerCodec();
        var totalFrames = Math.Max((int)Math.Round(options.SerialRateHz * options.DurationSeconds), 1);
        var encodedFrames = 0;
        var decodedFrames = 0;
        var failures = 0;

        var random = new Random(42);
        var stopwatch = Stopwatch.StartNew();

        for (var i = 0; i < totalFrames; i++)
        {
            var steerCommand = new SteerCmd
            {
                Enable = true,
                TargetWheelAngleDeg = (random.NextDouble() * 2.0 - 1.0) * 18.0,
            };

            var metadata = new LegacySteerCommandMetadata
            {
                SpeedKph = 20 + random.NextDouble() * 5,
                GuidanceStatus = 1,
                TramControl = (byte)(random.Next(0, 4)),
            };

            var sectionMask = new SectionMask
            {
                SectionCount = 16,
                Mask = (uint)random.Next(0, 0xFFFF),
            };

            var frame = steerCodec.EncodeSteerCommand(steerCommand, sectionMask, metadata);
            encodedFrames++;

            var encodedBuffer = new byte[LegacySerialFrameCodec.GetMaxEncodedLength(frame.Length)];
            var written = LegacySerialFrameCodec.Encode(frame, encodedBuffer);

            var decoded = new byte[frame.Length];
            if (LegacySerialFrameCodec.TryDecode(encodedBuffer.AsSpan(0, written), decoded, out var decodedLength) && decodedLength == frame.Length)
            {
                decodedFrames++;
            }
            else
            {
                failures++;
            }
        }

        stopwatch.Stop();
        var elapsedSeconds = Math.Max(stopwatch.Elapsed.TotalSeconds, 1e-6);
        var effectiveRate = decodedFrames / elapsedSeconds;

        return new LegacySerialSoakReport(
            FramesEncoded: encodedFrames,
            FramesDecoded: decodedFrames,
            DecodeFailures: failures,
            EffectiveRateHz: effectiveRate);
    }

    private sealed class NullTransport : ILegacyUdpTransport
    {
        public ValueTask SendAsync(ReadOnlyMemory<byte> datagram, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    }

    private sealed class CountingPoseObserver : ILegacyPoseObserver
    {
        private DateTimeOffset? _lastTimestamp;
        private TimeSpan? _minSpacing;
        private TimeSpan? _maxSpacing;

        public int Count { get; private set; }

        public ValueTask OnPoseAsync(Pose pose, LegacyPoseMetadata metadata, CancellationToken cancellationToken)
        {
            Count++;
            UpdateSpacing(pose.Header);
            return ValueTask.CompletedTask;
        }

        public SpacingStats GetSpacingStats() => new(Count, _minSpacing?.TotalMilliseconds ?? 0, _maxSpacing?.TotalMilliseconds ?? 0);

        private void UpdateSpacing(Header? header)
        {
            if (header is null || header.Timestamp is null)
            {
                return;
            }

            var timestamp = header.Timestamp.ToDateTimeOffset();
            if (_lastTimestamp is { } previous)
            {
                var delta = timestamp - previous;
                if (_minSpacing is null || delta < _minSpacing)
                {
                    _minSpacing = delta;
                }

                if (_maxSpacing is null || delta > _maxSpacing)
                {
                    _maxSpacing = delta;
                }
            }

            _lastTimestamp = timestamp;
        }
    }

    private sealed class CountingDiscoveryObserver : ILegacyDiscoveryObserver
    {
        public int Count { get; private set; }

        public ValueTask OnDiscoveryAsync(LegacyDiscoveryAnnouncement announcement, CancellationToken cancellationToken)
        {
            Count++;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class CountingSteerCommandObserver : ILegacySteerCommandObserver
    {
        public int Count { get; private set; }

        public ValueTask OnSteerCommandAsync(SteerCmd command, LegacySteerCommandMetadata metadata, CancellationToken cancellationToken)
        {
            Count++;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class CountingSteerStateObserver : ILegacySteerStateObserver
    {
        private DateTimeOffset? _lastTimestamp;
        private TimeSpan? _minSpacing;
        private TimeSpan? _maxSpacing;

        public int Count { get; private set; }

        public ValueTask OnSteerStateAsync(SteerState state, LegacySteerStateMetadata metadata, CancellationToken cancellationToken)
        {
            Count++;
            UpdateSpacing(state.Header);
            return ValueTask.CompletedTask;
        }

        public SpacingStats GetSpacingStats() => new(Count, _minSpacing?.TotalMilliseconds ?? 0, _maxSpacing?.TotalMilliseconds ?? 0);

        private void UpdateSpacing(Header? header)
        {
            if (header is null || header.Timestamp is null)
            {
                return;
            }

            var timestamp = header.Timestamp.ToDateTimeOffset();
            if (_lastTimestamp is { } previous)
            {
                var delta = timestamp - previous;
                if (_minSpacing is null || delta < _minSpacing)
                {
                    _minSpacing = delta;
                }

                if (_maxSpacing is null || delta > _maxSpacing)
                {
                    _maxSpacing = delta;
                }
            }

            _lastTimestamp = timestamp;
        }
    }

    private sealed class CountingSectionObserver : ILegacySectionObserver
    {
        public int Count { get; private set; }

        public ValueTask OnSectionMaskAsync(SectionMask mask, CancellationToken cancellationToken)
        {
            Count++;
            return ValueTask.CompletedTask;
        }
    }
}

public sealed record LegacySoakOptions(double DurationSeconds, double UdpRatePerStreamHz, double SerialRateHz);

public sealed record SpacingStats(int Samples, double MinMilliseconds, double MaxMilliseconds);

public sealed record LegacyUdpSoakReport(
    int PoseFrames,
    int SteerCommandFrames,
    int SteerStateFrames,
    int SectionFrames,
    int DiscoveryFrames,
    int TotalFrames,
    double TargetRatePerStreamHz,
    double EffectiveRateHz,
    SpacingStats PoseSpacingStats,
    SpacingStats SteerStateSpacingStats);

public sealed record LegacySerialSoakReport(
    int FramesEncoded,
    int FramesDecoded,
    int DecodeFailures,
    double EffectiveRateHz);

public sealed record LegacySoakReport(
    DateTimeOffset StartedAtUtc,
    double DurationRequestedSeconds,
    double DurationElapsedSeconds,
    LegacyUdpSoakReport Udp,
    LegacySerialSoakReport Serial);

internal sealed class IncrementingTimeProvider : TimeProvider
{
    private readonly TimeSpan _step;
    private DateTimeOffset _current;

    public IncrementingTimeProvider(DateTimeOffset start, TimeSpan step)
    {
        _current = start - step;
        _step = step <= TimeSpan.Zero ? TimeSpan.FromMilliseconds(1) : step;
    }

    public override DateTimeOffset GetUtcNow()
    {
        _current += _step;
        return _current;
    }
}
