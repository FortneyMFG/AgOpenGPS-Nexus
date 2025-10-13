using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aog.Agio.Windows;
using Aog.Core.V1;
using Google.Protobuf.WellKnownTypes;
using Xunit;

namespace Aog.Agio.Tests;

public sealed class WindowsLocationPosePublisherTests
{
    [Fact]
    public async Task StartAsync_Publishes_Initial_Position()
    {
        var initial = CreatePosition(52.1, 13.4, 120, headingDeg: 45, speedMps: 3.2, timestamp: DateTimeOffset.UtcNow);
        var geolocator = new FakeGeolocator(initial);
        var collector = new PoseCollector();
        var publisher = new WindowsLocationPosePublisher(geolocator, collector.PublishAsync);

        await publisher.StartAsync(CancellationToken.None);

        Assert.Single(collector.Poses);
        var pose = collector.Poses[0];
        Assert.Equal(initial.Coordinate.Latitude, pose.LatitudeDeg);
        Assert.Equal(initial.Coordinate.Longitude, pose.LongitudeDeg);
        Assert.Equal(initial.Coordinate.Altitude, pose.AltitudeM);
        Assert.Equal(initial.Coordinate.SpeedMps, pose.SpeedMps);
        Assert.Equal(Math.PI / 4, pose.HeadingRad, 5); // 45 degrees
        Assert.Equal(0UL, pose.Header.Sequence);
        Assert.Equal("earth", pose.Header.Frame);
        Assert.Equal("windows.location", pose.Header.Source);
        Assert.Equal(initial.Timestamp, pose.Header.Timestamp.ToDateTimeOffset());
    }

    [Fact]
    public async Task PositionChanged_Publishes_New_Pose_With_Incremented_Sequence()
    {
        var initial = CreatePosition(10, 20, 0, headingDeg: null, speedMps: null, timestamp: DateTimeOffset.UtcNow);
        var update = CreatePosition(11, 21, 5, headingDeg: 180, speedMps: 1.5, timestamp: DateTimeOffset.UtcNow.AddSeconds(5));
        var geolocator = new FakeGeolocator(initial);
        var collector = new PoseCollector();
        var publisher = new WindowsLocationPosePublisher(geolocator, collector.PublishAsync);

        await publisher.StartAsync(CancellationToken.None);

        var task = collector.WaitForNextAsync();
        geolocator.RaisePositionChanged(update);
        var pose = await task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.Equal(2, collector.Poses.Count); // initial + update
        Assert.Equal(update.Coordinate.Latitude, pose.LatitudeDeg);
        Assert.Equal(update.Coordinate.Longitude, pose.LongitudeDeg);
        Assert.Equal(update.Coordinate.Altitude, pose.AltitudeM);
        Assert.Equal(update.Coordinate.SpeedMps, pose.SpeedMps);
        Assert.Equal(Math.PI, pose.HeadingRad, 5);
        Assert.Equal(1UL, pose.Header.Sequence);
        Assert.Equal(update.Timestamp, pose.Header.Timestamp.ToDateTimeOffset());
    }

    [Fact]
    public async Task Uses_DefaultAltitude_When_Missing()
    {
        var initial = CreatePosition(0, 0, altitude: null, headingDeg: null, speedMps: null, timestamp: DateTimeOffset.UtcNow);
        var options = new WindowsLocationPoseOptions { DefaultAltitudeM = 42 };
        var geolocator = new FakeGeolocator(initial);
        var collector = new PoseCollector();
        using var publisher = new WindowsLocationPosePublisher(geolocator, collector.PublishAsync, options);

        await publisher.StartAsync(CancellationToken.None);

        Assert.Single(collector.Poses);
        Assert.Equal(42, collector.Poses[0].AltitudeM);
    }

    [Fact]
    public async Task Throws_When_Started_Twice()
    {
        var geolocator = new FakeGeolocator(CreatePosition(1, 1, 0, null, null, DateTimeOffset.UtcNow));
        var publisher = new WindowsLocationPosePublisher(geolocator, (_, _) => ValueTask.CompletedTask);

        await publisher.StartAsync(CancellationToken.None);
        await Assert.ThrowsAsync<InvalidOperationException>(() => publisher.StartAsync(CancellationToken.None));
    }

    [Fact]
    public void Create_Throws_On_NonWindows()
    {
        Assert.Throws<PlatformNotSupportedException>(() => WindowsRuntimeGeolocator.Create());
    }

    private static WinRtGeoposition CreatePosition(
        double latitude,
        double longitude,
        double? altitude,
        double? headingDeg,
        double? speedMps,
        DateTimeOffset timestamp)
    {
        var coordinate = new WinRtGeocoordinate(latitude, longitude, altitude, accuracy: 5, headingDeg, speedMps);
        return new WinRtGeoposition(timestamp, coordinate);
    }

    private sealed class FakeGeolocator : IWinRtGeolocator
    {
        private readonly Queue<WinRtGeoposition> _positions = new();

        public FakeGeolocator(WinRtGeoposition initial)
        {
            _positions.Enqueue(initial);
        }

        public event EventHandler<WinRtPositionChangedEventArgs>? PositionChanged;

        public Task<WinRtGeoposition> GetGeopositionAsync(CancellationToken cancellationToken)
        {
            if (_positions.Count == 0)
            {
                throw new InvalidOperationException("No positions queued.");
            }

            return Task.FromResult(_positions.Dequeue());
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public void RaisePositionChanged(WinRtGeoposition position)
        {
            PositionChanged?.Invoke(this, new WinRtPositionChangedEventArgs(position));
        }
    }

    private sealed class PoseCollector
    {
        private readonly List<Pose> _poses = new();
        private TaskCompletionSource<Pose> _tcs = CreateCompletionSource();

        public IReadOnlyList<Pose> Poses => _poses;

        public ValueTask PublishAsync(Pose pose, CancellationToken cancellationToken)
        {
            lock (_poses)
            {
                _poses.Add(pose);
                _tcs.TrySetResult(pose);
                _tcs = CreateCompletionSource();
            }

            return ValueTask.CompletedTask;
        }

        public Task<Pose> WaitForNextAsync()
        {
            lock (_poses)
            {
                return _tcs.Task;
            }
        }

        private static TaskCompletionSource<Pose> CreateCompletionSource()
            => new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
