using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Eventing;
using Aog.Core.Layers.Controllers;
using Aog.Core.V1;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Aog.Core.Tests.Layers;

public sealed class PoseStreamIngestionServiceTests : IAsyncDisposable
{
    private readonly InMemoryEventBus _eventBus = new();
    private readonly FakeTimeProvider _timeProvider = new();
    private readonly PoseStreamIngestionService _service;

    public PoseStreamIngestionServiceTests()
    {
        _timeProvider.SetUtcNow(DateTimeOffset.UtcNow);
        _service = new PoseStreamIngestionService(_eventBus, _timeProvider);
    }

    [Fact]
    public async Task PublishPose_ShouldExposePlanarFrames()
    {
        var frames = new List<LayerControllerPoseFrame>();
        using var subscription = _service.Subscribe((frame, _) =>
        {
            lock (frames)
            {
                frames.Add(frame);
            }

            return ValueTask.CompletedTask;
        });

        var start = DateTimeOffset.UtcNow;
        var second = start + TimeSpan.FromMilliseconds(120);

        await _eventBus.PublishAsync(new Pose
        {
            Header = new Header
            {
                Timestamp = Timestamp.FromDateTimeOffset(start)
            },
            LatitudeDeg = 51.5000,
            LongitudeDeg = -0.1200,
            SpeedMps = 5.0
        });

        await _eventBus.PublishAsync(new Pose
        {
            Header = new Header
            {
                Timestamp = Timestamp.FromDateTimeOffset(second)
            },
            LatitudeDeg = 51.5001,
            LongitudeDeg = -0.1199,
            SpeedMps = 5.2
        });

        frames.Should().HaveCount(2);

        var first = frames[0];
        first.Elapsed.Should().Be(TimeSpan.Zero);
        first.Position.Easting.Should().BeApproximately(0, 1e-9);
        first.Position.Northing.Should().BeApproximately(0, 1e-9);
        first.PreviousPosition.Should().BeNull();

        var secondFrame = frames[1];
        secondFrame.Elapsed.Should().Be(second - start);
        secondFrame.PreviousPosition.Should().Be(first.Position);

        var expectedOffset = ComputeExpectedOffset(
            firstPose: (51.5000, -0.1200),
            secondPose: (51.5001, -0.1199));

        secondFrame.Position.Easting.Should().BeApproximately(expectedOffset.Easting, 1e-3);
        secondFrame.Position.Northing.Should().BeApproximately(expectedOffset.Northing, 1e-3);

        _service.TryGetLatestFrame(out var latest).Should().BeTrue();
        latest.Should().Be(secondFrame);
    }

    [Fact]
    public async Task PublishPose_ShouldFallbackToTimeProviderWhenTimestampMissing()
    {
        var captured = new TaskCompletionSource<LayerControllerPoseFrame>(TaskCreationOptions.RunContinuationsAsynchronously);

        using var subscription = _service.Subscribe((frame, _) =>
        {
            captured.TrySetResult(frame);
            return ValueTask.CompletedTask;
        });

        var expected = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        _timeProvider.SetUtcNow(expected);

        await _eventBus.PublishAsync(new Pose
        {
            Header = new Header(),
            LatitudeDeg = 10,
            LongitudeDeg = 20,
            SpeedMps = 3
        });

        var frame = await captured.Task.WaitAsync(TimeSpan.FromSeconds(1));
        frame.Timestamp.Should().Be(expected);
        frame.Elapsed.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void TryGetLatestFrame_ShouldReturnFalseWhenNoPoseReceived()
    {
        _service.TryGetLatestFrame(out _).Should().BeFalse();
    }

    public ValueTask DisposeAsync()
    {
        _service.Dispose();
        return ValueTask.CompletedTask;
    }

    private static (double Easting, double Northing) ComputeExpectedOffset(
        (double LatitudeDeg, double LongitudeDeg) firstPose,
        (double LatitudeDeg, double LongitudeDeg) secondPose)
    {
        const double earthRadius = 6_378_137d;

        var originLatRad = DegreesToRadians(firstPose.LatitudeDeg);
        var originLonRad = DegreesToRadians(firstPose.LongitudeDeg);
        var secondLatRad = DegreesToRadians(secondPose.LatitudeDeg);
        var secondLonRad = DegreesToRadians(secondPose.LongitudeDeg);

        var deltaLat = secondLatRad - originLatRad;
        var deltaLon = secondLonRad - originLonRad;
        var meanLat = (secondLatRad + originLatRad) / 2d;

        var east = earthRadius * deltaLon * Math.Cos(meanLat);
        var north = earthRadius * deltaLat;

        return (east, north);
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180d;
}
