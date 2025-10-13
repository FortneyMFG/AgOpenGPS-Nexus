using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Aog.Agio.Gnss;
using Aog.Core.V1;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aog.Agio.Tests;

public sealed class PositionSourceAggregatorTests
{
    [Fact]
    public async Task RunAsync_FallsBackWhenSerialSourceFaults()
    {
        var serialSource = new FakePositionSource(
            "serial/COM1",
            PositionSourceResult.Faulted(new InvalidOperationException("COM failure")));

        var gpsdSource = new FakePositionSource(
            "gpsd",
            PositionSourceResult.Cancelled(),
            new[] { CreatePose(42) });

        var serialFactory = new FakePositionSourceFactory(
            PositionSourceKind.SerialCom,
            "serial",
            _ => ValueTask.FromResult<IPositionSource?>(serialSource));

        var gpsdFactory = new FakePositionSourceFactory(
            PositionSourceKind.Gpsd,
            "gpsd",
            _ => ValueTask.FromResult<IPositionSource?>(gpsdSource));

        var aggregator = CreateAggregator(
            new[] { serialFactory, gpsdFactory },
            options =>
            {
                options.SourceFailureBackoff = TimeSpan.Zero;
                options.ExhaustedBackoff = TimeSpan.Zero;
            });

        var published = new List<Pose>();
        var result = await aggregator.RunAsync(
            (pose, token) =>
            {
                published.Add(pose);
                return ValueTask.CompletedTask;
            },
            CancellationToken.None);

        Assert.Equal(PositionSourceOutcome.Cancelled, result.Outcome);
        Assert.Single(published);
        Assert.True(serialSource.RunInvoked);
        Assert.True(serialSource.Disposed);
        Assert.True(gpsdSource.RunInvoked);
        Assert.True(gpsdSource.Disposed);
        Assert.Equal(1, serialFactory.InvocationCount);
        Assert.Equal(1, gpsdFactory.InvocationCount);
    }

    [Fact]
    public async Task RunAsync_UsesNetworkWhenEnabledAndHardwareUnavailable()
    {
        var serialFactory = new FakePositionSourceFactory(
            PositionSourceKind.SerialCom,
            "serial",
            _ => ValueTask.FromResult<IPositionSource?>(null));

        var udpSource = new FakePositionSource(
            "udp/9000",
            PositionSourceResult.Cancelled(),
            new[] { CreatePose(99) });

        var udpFactory = new FakePositionSourceFactory(
            PositionSourceKind.UdpNetwork,
            "udp",
            _ => ValueTask.FromResult<IPositionSource?>(udpSource));

        var aggregator = CreateAggregator(
            new[] { serialFactory, udpFactory },
            options =>
            {
                options.EnableNetworkFeeds = true;
                options.SourceFailureBackoff = TimeSpan.Zero;
                options.ExhaustedBackoff = TimeSpan.Zero;
                options.PreferredOrder = new List<PositionSourceKind>
                {
                    PositionSourceKind.SerialCom,
                    PositionSourceKind.UdpNetwork,
                };
            });

        var published = new List<Pose>();
        var result = await aggregator.RunAsync(
            (pose, token) =>
            {
                published.Add(pose);
                return ValueTask.CompletedTask;
            },
            CancellationToken.None);

        Assert.Equal(PositionSourceOutcome.Cancelled, result.Outcome);
        Assert.Single(published);
        Assert.Equal(1, serialFactory.InvocationCount);
        Assert.Equal(1, udpFactory.InvocationCount);
    }

    [Fact]
    public async Task RunAsync_ThrowsWhenOnlyNetworkSourcesButPolicyDisallowsThem()
    {
        var tcpFactory = new FakePositionSourceFactory(
            PositionSourceKind.TcpNetwork,
            "tcp",
            _ => ValueTask.FromResult<IPositionSource?>(null));

        var aggregator = CreateAggregator(
            new[] { tcpFactory },
            options =>
            {
                options.EnableNetworkFeeds = false;
                options.PreferredOrder = new List<PositionSourceKind>
                {
                    PositionSourceKind.SerialCom,
                    PositionSourceKind.Gpsd,
                    PositionSourceKind.TcpNetwork,
                };
            });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            aggregator.RunAsync((pose, token) => ValueTask.CompletedTask, CancellationToken.None));
    }

    [Fact]
    public async Task RunAsync_WaitsBeforeRetryingWhenNoSourcesAvailable()
    {
        var serialFactory = new FakePositionSourceFactory(
            PositionSourceKind.SerialCom,
            "serial",
            _ => ValueTask.FromResult<IPositionSource?>(null));

        var timeProvider = new RecordingTimeProvider();

        var aggregator = CreateAggregator(
            new[] { serialFactory },
            options =>
            {
                options.EnableNetworkFeeds = true;
                options.SourceFailureBackoff = TimeSpan.Zero;
                options.ExhaustedBackoff = TimeSpan.FromSeconds(3);
            },
            timeProvider);

        using var cts = new CancellationTokenSource();
        var runTask = aggregator.RunAsync((pose, token) => ValueTask.CompletedTask, cts.Token);

        await WaitUntilAsync(() => timeProvider.Delays.Count > 0, TimeSpan.FromSeconds(1));
        cts.Cancel();

        var result = await runTask;
        Assert.Equal(PositionSourceOutcome.Cancelled, result.Outcome);
        Assert.Contains(TimeSpan.FromSeconds(3), timeProvider.Delays);
    }

    private static PositionSourceAggregator CreateAggregator(
        IEnumerable<IPositionSourceFactory> factories,
        Action<PositionSourceAggregatorOptions>? configure,
        TimeProvider? timeProvider = null)
    {
        var options = new PositionSourceAggregatorOptions();
        configure?.Invoke(options);
        var logger = new TestLogger<PositionSourceAggregator>();
        return new PositionSourceAggregator(factories, Options.Create(options), logger, timeProvider);
    }

    private static Pose CreatePose(int sequence)
    {
        return new Pose
        {
            Header = new Header
            {
                Sequence = (ulong)sequence,
                Timestamp = Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow),
            },
            LatitudeDeg = 51.0,
            LongitudeDeg = -114.0,
        };
    }

    private static async Task WaitUntilAsync(Func<bool> predicate, TimeSpan timeout)
    {
        var sw = Stopwatch.StartNew();
        while (!predicate())
        {
            if (sw.Elapsed > timeout)
            {
                throw new TimeoutException("Condition was not met within the allotted time.");
            }

            await Task.Delay(10);
        }
    }

    private sealed class FakePositionSourceFactory : IPositionSourceFactory
    {
        private readonly Func<CancellationToken, ValueTask<IPositionSource?>> _creator;

        public FakePositionSourceFactory(PositionSourceKind kind, string name, Func<CancellationToken, ValueTask<IPositionSource?>> creator)
        {
            Kind = kind;
            Name = name;
            _creator = creator;
        }

        public string Name { get; }

        public PositionSourceKind Kind { get; }

        public int InvocationCount { get; private set; }

        public async ValueTask<IPositionSource?> TryCreateAsync(CancellationToken cancellationToken)
        {
            InvocationCount++;
            return await _creator(cancellationToken);
        }
    }

    private sealed class FakePositionSource : IPositionSource
    {
        private readonly IReadOnlyList<Pose> _poses;
        private readonly PositionSourceResult _result;

        public FakePositionSource(string name, PositionSourceResult result, IReadOnlyList<Pose>? poses = null)
        {
            Name = name;
            _result = result;
            _poses = poses ?? Array.Empty<Pose>();
        }

        public string Name { get; }

        public bool RunInvoked { get; private set; }

        public bool Disposed { get; private set; }

        public async Task<PositionSourceResult> RunAsync(Func<Pose, CancellationToken, ValueTask> publish, CancellationToken cancellationToken)
        {
            RunInvoked = true;
            foreach (var pose in _poses)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await publish(pose, cancellationToken);
            }

            return _result;
        }

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class RecordingTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow = DateTimeOffset.UtcNow;

        public List<TimeSpan> Delays { get; } = new();

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public override long GetTimestamp() => Stopwatch.GetTimestamp();

        public override TimeSpan GetElapsedTime(long startingTimestamp) => TimeProvider.System.GetElapsedTime(startingTimestamp);

        public override TimeSpan GetElapsedTime(long startingTimestamp, long endingTimestamp) => TimeProvider.System.GetElapsedTime(startingTimestamp, endingTimestamp);

        public override ValueTask Delay(TimeSpan delay, CancellationToken cancellationToken = default)
        {
            Delays.Add(delay);
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.CompletedTask;
        }
    }
}
