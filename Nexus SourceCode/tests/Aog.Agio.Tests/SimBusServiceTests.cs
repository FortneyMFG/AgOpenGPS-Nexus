using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aog.Agio.Sim;
using Aog.Core.Simulation;
using Aog.Core.V1;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Core.Testing;
using Xunit;

namespace Aog.Agio.Tests;

public sealed class SimBusServiceTests
{
    [Fact]
    public async Task GnssService_ForwardsMessagesPublishedOnSimBus()
    {
        var bus = new InMemorySimBus();
        var service = new SimGnssService(bus);
        var writer = new RecordingStreamWriter<Pose>();
        using var cts = new CancellationTokenSource();
        var context = CreateContext(cts.Token, "aog.agio.v1.GnssService/SubscribePose");

        var callTask = service.SubscribePose(new Empty(), writer, context);

        var pose = new Pose
        {
            LatitudeDeg = 51.1234,
            LongitudeDeg = -1.2345,
            SpeedMps = 3.2,
        };

        var simTime = SimTime.FromTick(1, TimeSpan.FromMilliseconds(10));
        await bus.PublishAsync(SimBusTopics.Pose, simTime, pose);

        await WaitForCountAsync(writer, expectedCount: 1, timeout: TimeSpan.FromSeconds(1));
        Assert.Same(pose, writer[0]);

        cts.Cancel();
        await callTask.WaitAsync(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task ImuService_StopsForwardingAfterCancellation()
    {
        var bus = new InMemorySimBus();
        var service = new SimImuService(bus);
        var writer = new RecordingStreamWriter<Imu>();
        using var cts = new CancellationTokenSource();
        var context = CreateContext(cts.Token, "aog.agio.v1.ImuService/SubscribeImu");

        var callTask = service.SubscribeImu(new Empty(), writer, context);

        cts.Cancel();
        await callTask.WaitAsync(TimeSpan.FromSeconds(1));

        var imu = new Imu { AccelXMps2 = 1.0 };
        await bus.PublishAsync(SimBusTopics.Imu, SimTime.FromTick(2, TimeSpan.FromMilliseconds(10)), imu);

        Assert.Equal(0, writer.Count);
    }

    private static async Task WaitForCountAsync<T>(RecordingStreamWriter<T> writer, int expectedCount, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (writer.Count >= expectedCount)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(10));
        }

        Assert.True(writer.Count >= expectedCount, $"Expected at least {expectedCount} messages, observed {writer.Count}.");
    }

    private static ServerCallContext CreateContext(CancellationToken token, string method)
    {
        return TestServerCallContext.Create(
            method: method,
            host: null,
            deadline: DateTime.UtcNow.AddMinutes(1),
            requestHeaders: new Metadata(),
            cancellationToken: token,
            peer: "ipv4:127.0.0.1",
            authContext: null,
            contextPropagationToken: null,
            responseTrailers: null,
            writeHeadersFunc: _ => Task.CompletedTask);
    }

    private sealed class RecordingStreamWriter<T> : IServerStreamWriter<T>
    {
        private readonly List<T> _messages = new();
        private readonly object _sync = new();

        public WriteOptions? WriteOptions { get; set; }

        public Task WriteAsync(T message)
        {
            lock (_sync)
            {
                _messages.Add(message);
            }

            return Task.CompletedTask;
        }

        public int Count
        {
            get
            {
                lock (_sync)
                {
                    return _messages.Count;
                }
            }
        }

        public T this[int index]
        {
            get
            {
                lock (_sync)
                {
                    return _messages[index];
                }
            }
        }
    }
}
