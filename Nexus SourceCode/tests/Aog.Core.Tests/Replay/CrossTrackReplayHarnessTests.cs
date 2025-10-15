using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Eventing;
using Aog.Core.Logging;
using Aog.Core.Replay;
using Aog.Core.V1;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Aog.Core.Tests.Replay;

public sealed class CrossTrackReplayHarnessTests
{
    [Fact]
    public async Task ReplaySlice_ComputesCrossTrackMetrics()
    {
        var tempDirectory = CreateTempDirectory();
        try
        {
            var scenario = await WriteCrossTrackReplayAsync(tempDirectory);

            var playbackBus = new InMemoryEventBus();
            using var harness = new CrossTrackReplayHarness(playbackBus, scenario.Start, scenario.End);

            var timeProvider = new ManualTimeProvider();
            timeProvider.SetUtcNow(scenario.StartTimestamp);

            var replayOptions = new TelemetryReplayOptions
            {
                InputDirectory = tempDirectory
            };

            await using var controller = await TelemetryReplayController.CreateAsync(
                playbackBus,
                replayOptions,
                timeProvider);

            await controller.PlayAsync();
            await AdvanceUntilAsync(timeProvider, () => harness.SampleCount == scenario.SampleCount, TimeSpan.FromMilliseconds(100));

            controller.State.IsPlaying.Should().BeFalse();
            controller.State.Position.Should().Be(controller.State.Duration);

            harness.SampleCount.Should().Be(scenario.SampleCount);

            var summary = harness.CreateSummary();

            summary.MaxAbsoluteError.Should().BeApproximately(1.5, 0.05);
            summary.FinalAbsoluteError.Should().BeLessThan(0.1);
            summary.Duration.Should().Be(TimeSpan.FromSeconds(scenario.SampleCount - 1));
            summary.MonotonicImprovement.Should().BeTrue();
            summary.RootMeanSquareError.Should().BeGreaterThan(0.5);
            summary.RootMeanSquareError.Should().BeLessThan(0.9);
        }
        finally
        {
            TryDeleteDirectory(tempDirectory);
        }
    }

    private static async Task<CrossTrackScenario> WriteCrossTrackReplayAsync(string directory)
    {
        Directory.CreateDirectory(directory);

        var options = new TelemetryParquetLogger.TelemetryParquetLoggerOptions
        {
            OutputDirectory = directory
        };

        var eventBus = new InMemoryEventBus();
        await using var logger = await TelemetryParquetLogger.CreateAsync(eventBus, options);

        var startTimestamp = DateTime.SpecifyKind(new DateTime(2024, 1, 1, 8, 0, 0), DateTimeKind.Utc);
        const double baseLatitude = 52.0;
        const double baseLongitude = -1.0;
        const double metresPerDegreeLatitude = 111_320.0;
        var metresPerDegreeLongitude = metresPerDegreeLatitude * Math.Cos(baseLatitude * Math.PI / 180);

        var crossTrackOffsets = new[] { 1.5, 1.0, 0.6, 0.25, 0.12, 0.05, 0.02 };
        const double alongTrackStep = 5.0;

        var alongTrack = 0.0;
        ulong sequence = 1;

        foreach (var offset in crossTrackOffsets)
        {
            var timestamp = startTimestamp + TimeSpan.FromSeconds(sequence - 1);
            var latitude = baseLatitude + (alongTrack / metresPerDegreeLatitude);
            var longitude = baseLongitude + (offset / metresPerDegreeLongitude);

            var pose = new Pose
            {
                Header = new Header
                {
                    Sequence = sequence,
                    Frame = "earth",
                    Source = "sim",
                    Timestamp = Timestamp.FromDateTime(timestamp)
                },
                LatitudeDeg = latitude,
                LongitudeDeg = longitude,
                AltitudeM = 150,
                HeadingRad = 0,
                RollRad = 0,
                PitchRad = 0,
                SpeedMps = 5,
                YawRateRadps = 0
            };

            await eventBus.PublishAsync(pose);

            alongTrack += alongTrackStep;
            sequence++;
        }

        var startPoint = new GeoCoordinate(baseLatitude, baseLongitude);
        var endPoint = new GeoCoordinate(
            baseLatitude + ((alongTrack - alongTrackStep) / metresPerDegreeLatitude),
            baseLongitude);

        return new CrossTrackScenario(startPoint, endPoint, crossTrackOffsets.Length, startTimestamp);
    }

    private static async Task AdvanceUntilAsync(
        ManualTimeProvider provider,
        Func<bool> predicate,
        TimeSpan step,
        int maxSteps = 200)
    {
        for (var i = 0; i < maxSteps && !predicate(); i++)
        {
            provider.Advance(step);
            await Task.Yield();
        }

        predicate().Should().BeTrue("replay did not reach the expected state within the allotted steps");
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "nexus-cross-track-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
            // Swallow cleanup failures so tests do not flake on locked files.
        }
    }

    private readonly record struct CrossTrackScenario(
        GeoCoordinate Start,
        GeoCoordinate End,
        int SampleCount,
        DateTime StartTimestamp);

    private readonly record struct GeoCoordinate(double LatitudeDeg, double LongitudeDeg);

    private sealed class CrossTrackReplayHarness : IDisposable
    {
        private const double EarthRadiusMetres = 6_378_137.0;

        private readonly InMemoryEventBus _eventBus;
        private readonly GeoCoordinate _start;
        private readonly GeoCoordinate _end;
        private readonly IDisposable _subscription;
        private readonly System.Collections.Generic.List<CrossTrackSample> _samples = new();
        private readonly double _headingRadians;

        private DateTime? _firstTimestamp;

        public CrossTrackReplayHarness(InMemoryEventBus eventBus, GeoCoordinate start, GeoCoordinate end)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _start = start;
            _end = end;
            _headingRadians = ComputeHeadingRadians(start, end);
            _subscription = _eventBus.Subscribe<Pose>(OnPoseAsync);
        }

        public int SampleCount => _samples.Count;

        public CrossTrackSummary CreateSummary()
        {
            if (_samples.Count == 0)
            {
                return new CrossTrackSummary(Array.Empty<CrossTrackSample>(), 0, 0, TimeSpan.Zero, true, 0);
            }

            var maxAbsolute = _samples.Max(sample => Math.Abs(sample.ErrorMeters));
            var finalAbsolute = Math.Abs(_samples[^1].ErrorMeters);
            var duration = _samples[^1].Offset;
            var monotonic = true;

            for (var i = 1; i < _samples.Count; i++)
            {
                if (Math.Abs(_samples[i].ErrorMeters) > Math.Abs(_samples[i - 1].ErrorMeters) + 1e-6)
                {
                    monotonic = false;
                    break;
                }
            }

            var rms = Math.Sqrt(_samples.Average(sample => sample.ErrorMeters * sample.ErrorMeters));

            return new CrossTrackSummary(
                _samples.ToArray(),
                maxAbsolute,
                finalAbsolute,
                duration,
                monotonic,
                rms);
        }

        public void Dispose()
        {
            _subscription.Dispose();
        }

        private ValueTask OnPoseAsync(Pose pose, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(pose);

            var timestamp = pose.Header?.Timestamp?.ToDateTime() ?? _firstTimestamp ?? DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
            _firstTimestamp ??= timestamp;

            var offset = timestamp - _firstTimestamp.Value;
            var crossTrack = ComputeCrossTrackMeters(pose);

            _samples.Add(new CrossTrackSample(offset, crossTrack));

            return ValueTask.CompletedTask;
        }

        private double ComputeCrossTrackMeters(Pose pose)
        {
            var target = new GeoCoordinate(pose.LatitudeDeg, pose.LongitudeDeg);
            var (east, north) = ToLocalMeters(_start, target);

            var sin = Math.Sin(_headingRadians);
            var cos = Math.Cos(_headingRadians);
            var crossTrack = (-north * sin) + (east * cos);

            return crossTrack;
        }

        private static (double East, double North) ToLocalMeters(GeoCoordinate anchor, GeoCoordinate target)
        {
            var anchorLatRad = anchor.LatitudeDeg * Math.PI / 180;
            var deltaLat = (target.LatitudeDeg - anchor.LatitudeDeg) * Math.PI / 180;
            var deltaLon = (target.LongitudeDeg - anchor.LongitudeDeg) * Math.PI / 180;

            var north = deltaLat * EarthRadiusMetres;
            var east = deltaLon * EarthRadiusMetres * Math.Cos(anchorLatRad);

            return (east, north);
        }

        private static double ComputeHeadingRadians(GeoCoordinate start, GeoCoordinate end)
        {
            var (east, north) = ToLocalMeters(start, end);
            if (Math.Abs(east) < 1e-9 && Math.Abs(north) < 1e-9)
            {
                return 0;
            }

            return Math.Atan2(east, north);
        }

        private readonly record struct CrossTrackSample(TimeSpan Offset, double ErrorMeters);

        public readonly record struct CrossTrackSummary(
            CrossTrackSample[] Samples,
            double MaxAbsoluteError,
            double FinalAbsoluteError,
            TimeSpan Duration,
            bool MonotonicImprovement,
            double RootMeanSquareError);
    }
}
