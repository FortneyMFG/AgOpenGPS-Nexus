using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aog.Agio.Linux.SocketCan;
using Aog.Core.V1;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SocketCANSharp;
using Xunit;

namespace Aog.Agio.Linux.Tests;

public sealed class SocketCanBackendTests
{
    [Fact]
    public async Task BackgroundService_PublishesTranslatedFrames()
    {
        var extendedId = SocketCanUtils.CreateCanIdWithFlags(0x18FF50E5, isEff: true, isRtr: true, isErr: false);
        var frames = new[]
        {
            new SocketCANSharp.CanFrame(SocketCanUtils.CreateCanIdWithFlags(0x123, isEff: false, isRtr: false, isErr: false), new byte[] { 0x01, 0x02, 0x03 }),
            new SocketCANSharp.CanFrame(extendedId, Array.Empty<byte>()),
        };

        var client = new FakeSocketCanClient("vcan0", frames);
        var factory = new FakeSocketCanClientFactory(client);
        var channel = new SocketCanFrameChannel();
        var options = new SocketCanOptions
        {
            InterfaceName = "vcan0",
            ReceiveTimeout = TimeSpan.FromMilliseconds(10),
            ReconnectDelay = TimeSpan.FromMilliseconds(10),
            SourcePrefix = "test/socketcan",
        };
        var optionsMonitor = new TestOptionsMonitor(options);

        var service = new SocketCanBackgroundService(
            factory,
            channel,
            optionsMonitor,
            new FixedTimeProvider(new DateTimeOffset(2024, 01, 01, 12, 00, 00, TimeSpan.Zero)),
            NullLogger<SocketCanBackgroundService>.Instance);

        await service.StartAsync(CancellationToken.None).ConfigureAwait(false);

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        await using var enumerator = channel.ReadAllAsync(cts.Token).GetAsyncEnumerator();

        Assert.True(await enumerator.MoveNextAsync());
        var first = enumerator.Current;
        Assert.Equal(0x123u, first.ArbitrationId);
        Assert.False(first.IsExtendedId);
        Assert.False(first.IsRemoteRequest);
        Assert.Equal("test/socketcan/vcan0", first.Header.Source);
        Assert.Equal<ulong>(1, first.Header.Sequence);
        Assert.Equal(new byte[] { 0x01, 0x02, 0x03 }, first.Payload.ToByteArray());
        Assert.Equal(new DateTimeOffset(2024, 01, 01, 12, 00, 00, TimeSpan.Zero), first.Header.Timestamp.ToDateTimeOffset());

        Assert.True(await enumerator.MoveNextAsync());
        var second = enumerator.Current;
        Assert.Equal(0x18FF50E5u, second.ArbitrationId);
        Assert.True(second.IsExtendedId);
        Assert.True(second.IsRemoteRequest);
        Assert.Equal(ByteString.Empty, second.Payload);
        Assert.Equal<ulong>(2, second.Header.Sequence);

        await service.StopAsync(CancellationToken.None).ConfigureAwait(false);
    }

    [Fact]
    public async Task BackgroundService_RetriesWhenInterfaceMissing()
    {
        var factory = new MissingInterfaceSocketCanClientFactory();
        var channel = new SocketCanFrameChannel();
        var options = new SocketCanOptions
        {
            InterfaceName = "vcan-missing",
            ReconnectDelay = TimeSpan.FromMilliseconds(10),
        };
        var monitor = new TestOptionsMonitor(options);

        var service = new SocketCanBackgroundService(
            factory,
            channel,
            monitor,
            TimeProvider.System,
            NullLogger<SocketCanBackgroundService>.Instance);

        await service.StartAsync(CancellationToken.None).ConfigureAwait(false);

        try
        {
            await WaitForAsync(() => factory.AttemptCount >= 2, TimeSpan.FromSeconds(1));
            var attemptsBefore = factory.AttemptCount;

            await Task.Delay(TimeSpan.FromMilliseconds(200)).ConfigureAwait(false);

            Assert.True(factory.AttemptCount > attemptsBefore, "SocketCAN listener stopped retrying unexpectedly.");
        }
        finally
        {
            await service.StopAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }

    [Fact]
[Fact]
public async Task BackgroundService_PublishesFrameImmediatelyAfterTimeout()
{
    var client = new FakeSocketCanClient("vcan0", Array.Empty<SocketCANSharp.CanFrame>());
    var factory = new FakeSocketCanClientFactory(client);
    var channel = new SocketCanFrameChannel();

    var initialOptions = new SocketCanOptions
    {
        InterfaceName = "vcan0",
        ReceiveTimeout = TimeSpan.FromMilliseconds(200),
        ReconnectDelay = TimeSpan.FromMilliseconds(10),
        SourcePrefix = "test/socketcan",
    };
    var monitor = new TestOptionsMonitor(initialOptions);

    var service = new SocketCanBackgroundService(
        factory,
        channel,
        monitor,
        TimeProvider.System,
        NullLogger<SocketCanBackgroundService>.Instance);

    await service.StartAsync(CancellationToken.None).ConfigureAwait(false);

    using var readCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    await using var enumerator = channel.ReadAllAsync(readCts.Token).GetAsyncEnumerator();
    var moveNextTask = enumerator.MoveNextAsync().AsTask();

    // Ensure at least one receive timeout has occurred so the pump is in "publish immediately" mode.
    await WaitForAsync(() => client.TimeoutCount > 0, TimeSpan.FromSeconds(1));

    var frame = new SocketCANSharp.CanFrame(
        SocketCanUtils.CreateCanIdWithFlags(0x456, isEff: false, isRtr: false, isErr: false),
        new byte[] { 0x0A, 0x0B });

    var stopwatch = Stopwatch.StartNew();
    client.EnqueueFrame(frame);

    Assert.True(await moveNextTask.ConfigureAwait(false));
    stopwatch.Stop();

    Assert.True(
        stopwatch.Elapsed < TimeSpan.FromMilliseconds(100),
        $"Frame was delayed by {stopwatch.Elapsed.TotalMilliseconds}ms");

    var published = enumerator.Current;
    Assert.Equal(0x456u, published.ArbitrationId);
    Assert.Equal(new byte[] { 0x0A, 0x0B }, published.Payload.ToByteArray());
}

[Fact]
public async Task BackgroundService_ReconfiguresWhenOptionsChange()
{
    var channel = new SocketCanFrameChannel();
    var firstClient = new PassiveSocketCanClient("vcan0");
    var secondClient = new PassiveSocketCanClient("vcan1");
    var factory = new TrackingSocketCanClientFactory(options =>
    {
        return options.InterfaceName switch
        {
            "vcan0" => firstClient,
            "vcan1" => secondClient,
            _ => throw new InvalidOperationException($"Unexpected interface '{options.InterfaceName}'."),
        };
    });

    var initialOptions = new SocketCanOptions
    {
        InterfaceName = "vcan0",
        ReceiveTimeout = TimeSpan.FromMilliseconds(10),
        ReconnectDelay = TimeSpan.FromMilliseconds(10),
        SourcePrefix = "test/socketcan",
    };
    var monitor = new TestOptionsMonitor(initialOptions);

    var service = new SocketCanBackgroundService(
        factory,
        channel,
        monitor,
        TimeProvider.System,
        NullLogger<SocketCanBackgroundService>.Instance);

    await service.StartAsync(CancellationToken.None).ConfigureAwait(false);

    await WaitForAsync(() => factory.CreatedInterfaces.Count >= 1, TimeSpan.FromSeconds(1));
    Assert.Equal("vcan0", factory.CreatedInterfaces[0]);

    monitor.Update(new SocketCanOptions
    {
        InterfaceName = "vcan1",
        ReceiveTimeout = initialOptions.ReceiveTimeout,
        ReconnectDelay = initialOptions.ReconnectDelay,
        SourcePrefix = initialOptions.SourcePrefix,
        IncludeVirtualInterfaces = initialOptions.IncludeVirtualInterfaces,
        ReceiveOwnMessages = initialOptions.ReceiveOwnMessages,
    });

    await WaitForAsync(() => factory.CreatedInterfaces.Count >= 2, TimeSpan.FromSeconds(1));
    Assert.Equal("vcan1", factory.CreatedInterfaces[1]);

    await WaitForAsync(() => firstClient.DisposeCount > 0, TimeSpan.FromSeconds(1));
    Assert.Equal(1, firstClient.DisposeCount);
}

        await service.StopAsync(CancellationToken.None).ConfigureAwait(false);
    }

    [Fact]
    public async Task SocketCanBusService_ForwardsFramesToSubscribers()
    {
        var channel = new SocketCanFrameChannel();
        var service = new SocketCanBusService(channel, NullLogger<SocketCanBusService>.Instance);
        var writer = new TestServerStreamWriter<CanFrame>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var context = new TestServerCallContext(cts.Token);

        var callTask = service.SubscribeFrames(new Empty(), writer, context);

        var frame = new CanFrame { Header = new Header(), ArbitrationId = 0x123 }; // minimal payload
        await channel.PublishAsync(frame, CancellationToken.None);

        await WaitForAsync(() => writer.Messages.Count > 0, TimeSpan.FromSeconds(1));
        Assert.Same(frame, Assert.Single(writer.Messages));

        cts.Cancel();
        await callTask.ConfigureAwait(false);
    }

    [Fact]
[Fact]
public async Task SocketCanBusService_DropsSlowSubscribers()
{
    // Service-level: verifies a slow gRPC writer triggers drop,
    // and a later fast subscriber still receives fresh frames.
    var channel = new SocketCanFrameChannel(subscriberCapacity: 4, maxSubscriberBackpressure: TimeSpan.FromMilliseconds(50));
    var service = new SocketCanBusService(channel, NullLogger<SocketCanBusService>.Instance);

    // Slow subscriber simulates backpressure on the server stream.
    var slowWriter = new SlowServerStreamWriter<CanFrame>(TimeSpan.FromMilliseconds(200));
    using var slowCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
    var slowContext = new TestServerCallContext(slowCts.Token);
    var slowCallTask = service.SubscribeFrames(new Empty(), slowWriter, slowContext);

    // Fast subscriber should continue receiving frames while the slow peer is evicted.
    var fastWriter = new TestServerStreamWriter<CanFrame>();
    using var fastCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    var fastContext = new TestServerCallContext(fastCts.Token);
    var fastCallTask = service.SubscribeFrames(new Empty(), fastWriter, fastContext);

    // Publish a burst that should overflow the slow subscriber buffer and trigger eviction.
    for (var i = 0; i < 64; i++)
    {
        await channel.PublishAsync(new CanFrame
        {
            Header = new Header(),
            ArbitrationId = (uint)i,
        }, CancellationToken.None).ConfigureAwait(false);
    }

    await WaitForAsync(() => fastWriter.Messages.Count >= 10, TimeSpan.FromSeconds(2));
    await WaitForAsync(() => slowCallTask.IsCompleted, TimeSpan.FromSeconds(5));
    await slowCallTask.ConfigureAwait(false);

    // If the slow subscriber had kept up, it would have the full burst.
    Assert.True(slowWriter.Messages.Count < 64);

    // Publish another frame to ensure the fast subscriber continues to receive updates.
    var finalFrame = new CanFrame { Header = new Header(), ArbitrationId = 0xABC };
    await channel.PublishAsync(finalFrame, CancellationToken.None).ConfigureAwait(false);

    await WaitForAsync(
        () => fastWriter.Messages.Exists(frame => frame.ArbitrationId == 0xABCu),
        TimeSpan.FromSeconds(2));

    fastCts.Cancel();
    await fastCallTask.ConfigureAwait(false);
}

[Fact]
public async Task FrameChannel_DropsSlowSubscribersAndKeepsFastOnesLive()
{
    // Channel-level: verifies bounded per-subscriber queue and backpressure timeout.
    var channel = new SocketCanFrameChannel(subscriberCapacity: 4, maxSubscriberBackpressure: TimeSpan.FromMilliseconds(50));
    using var fastCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    using var slowCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

    var fastFrames = new ConcurrentQueue<CanFrame>();

    var fastTask = Task.Run(async () =>
    {
        try
        {
            await foreach (var frame in channel.ReadAllAsync(fastCts.Token))
            {
                fastFrames.Enqueue(frame);
            }
        }
        catch (OperationCanceledException) when (fastCts.IsCancellationRequested) { }
    });

    var slowCompletion = new TaskCompletionSource<Exception?>(TaskCreationOptions.RunContinuationsAsynchronously);

    var slowTask = Task.Run(async () =>
    {
        await using var enumerator = channel.ReadAllAsync(slowCts.Token).GetAsyncEnumerator();
        try
        {
            while (await enumerator.MoveNextAsync())
            {
                // Artificial slowness to trip backpressure handling
                await Task.Delay(200, slowCts.Token);
            }

            slowCompletion.TrySetResult(null);
        }
        catch (Exception ex)
        {
            slowCompletion.TrySetResult(ex);
        }
    });

    for (var i = 0; i < 100; i++)
    {
        var frame = new CanFrame
        {
            Header = new Header { Sequence = (ulong)(i + 1) },
            ArbitrationId = (uint)i,
        };

        await channel.PublishAsync(frame, CancellationToken.None);
    }

    await WaitForAsync(() => fastFrames.Count >= 100, TimeSpan.FromSeconds(2));
    await WaitForAsync(() => slowCompletion.Task.IsCompleted, TimeSpan.FromSeconds(2));

    var slowResult = await slowCompletion.Task.ConfigureAwait(false);
    Assert.IsType<OperationCanceledException>(slowResult);
    Assert.Contains(fastFrames, frame => frame.ArbitrationId == 99);

    fastCts.Cancel();
    slowCts.Cancel();

    await Task.WhenAll(fastTask, slowTask);
}

    }

    private static async Task WaitForAsync(Func<bool> condition, TimeSpan timeout)
    {
        var stopwatch = Stopwatch.StartNew();
        while (!condition())
        {
            if (stopwatch.Elapsed >= timeout)
            {
                throw new TimeoutException("Condition was not satisfied within the allotted time.");
            }

            await Task.Delay(10);
        }
    }

    private sealed class FakeSocketCanClientFactory : ISocketCanClientFactory
    {
        private readonly ISocketCanClient _client;

        public FakeSocketCanClientFactory(ISocketCanClient client) => _client = client;

        public ValueTask<ISocketCanClient> CreateAsync(SocketCanOptions options, CancellationToken cancellationToken)
        {
            return new ValueTask<ISocketCanClient>(_client);
        }
    }

    private sealed class MissingInterfaceSocketCanClientFactory : ISocketCanClientFactory
    {
        private int _attemptCount;

        public int AttemptCount => Volatile.Read(ref _attemptCount);

        public ValueTask<ISocketCanClient> CreateAsync(SocketCanOptions options, CancellationToken cancellationToken)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            cancellationToken.ThrowIfCancellationRequested();
            Interlocked.Increment(ref _attemptCount);
            return ValueTask.FromException<ISocketCanClient>(new SocketCanInterfaceNotFoundException(options.InterfaceName));
        }
    }

    private sealed class TrackingSocketCanClientFactory : ISocketCanClientFactory
    {
        private readonly Func<SocketCanOptions, ISocketCanClient> _factory;

        public TrackingSocketCanClientFactory(Func<SocketCanOptions, ISocketCanClient> factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public List<string> CreatedInterfaces { get; } = new();

        public ValueTask<ISocketCanClient> CreateAsync(SocketCanOptions options, CancellationToken cancellationToken)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            cancellationToken.ThrowIfCancellationRequested();
            CreatedInterfaces.Add(options.InterfaceName ?? string.Empty);
            return new ValueTask<ISocketCanClient>(_factory(options));
        }
    }

    private sealed class FakeSocketCanClient : ISocketCanClient
    {
        private readonly ConcurrentQueue<SocketCANSharp.CanFrame> _frames;
        private int _timeoutCount;

        public FakeSocketCanClient(string interfaceName, IEnumerable<SocketCANSharp.CanFrame> frames)
        {
            InterfaceName = interfaceName;
            _frames = new ConcurrentQueue<SocketCANSharp.CanFrame>(frames);
        }

        public string InterfaceName { get; }

        public int TimeoutCount => Volatile.Read(ref _timeoutCount);

        public void EnqueueFrame(SocketCANSharp.CanFrame frame) => _frames.Enqueue(frame);

        public SocketCanFrameReadResult ReadFrame(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_frames.TryDequeue(out var frame))
            {
                return SocketCanFrameReadResult.FromFrame(frame);
            }

            Interlocked.Increment(ref _timeoutCount);
            return SocketCanFrameReadResult.Timeout();
        }

        public void Dispose()
        {
        }
    }

    private sealed class PassiveSocketCanClient : ISocketCanClient
    {
        public PassiveSocketCanClient(string interfaceName)
        {
            InterfaceName = interfaceName;
        }

        public string InterfaceName { get; }

        public int DisposeCount { get; private set; }

        public SocketCanFrameReadResult ReadFrame(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return SocketCanFrameReadResult.Timeout();
        }

        public void Dispose()
        {
            DisposeCount++;
        }
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _now;

        public FixedTimeProvider(DateTimeOffset now) => _now = now;

        public override DateTimeOffset GetUtcNow() => _now;
    }

    private sealed class TestOptionsMonitor : IOptionsMonitor<SocketCanOptions>
    {
        private readonly object _gate = new();
        private SocketCanOptions _current;
        private List<Action<SocketCanOptions, string>> _listeners = new();

        public TestOptionsMonitor(SocketCanOptions current)
        {
            _current = current ?? throw new ArgumentNullException(nameof(current));
        }

        public SocketCanOptions CurrentValue
        {
            get
            {
                lock (_gate)
                {
                    return _current;
                }
            }
        }

        public SocketCanOptions Get(string? name) => CurrentValue;

        public IDisposable OnChange(Action<SocketCanOptions, string> listener)
        {
            if (listener is null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            lock (_gate)
            {
                _listeners = new List<Action<SocketCanOptions, string>>(_listeners) { listener };
            }

            return new ChangeHandle(this, listener);
        }

        public void Update(SocketCanOptions options, string name = Options.DefaultName)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            List<Action<SocketCanOptions, string>> listeners;
            lock (_gate)
            {
                _current = options;
                listeners = _listeners;
            }

            foreach (var listener in listeners)
            {
                listener(options, name);
            }
        }

        private void Unregister(Action<SocketCanOptions, string> listener)
        {
            lock (_gate)
            {
                _listeners = new List<Action<SocketCanOptions, string>>(_listeners);
                _listeners.Remove(listener);
            }
        }

        private sealed class ChangeHandle : IDisposable
        {
            private TestOptionsMonitor? _monitor;
            private Action<SocketCanOptions, string>? _listener;

            public ChangeHandle(TestOptionsMonitor monitor, Action<SocketCanOptions, string> listener)
            {
                _monitor = monitor;
                _listener = listener;
            }

            public void Dispose()
            {
                var listener = Interlocked.Exchange(ref _listener, null);
                var monitor = Interlocked.Exchange(ref _monitor, null);
                if (listener is null || monitor is null)
                {
                    return;
                }

                monitor.Unregister(listener);
            }
        }
    }

    private sealed class TestServerStreamWriter<T> : IServerStreamWriter<T>
    {
        public List<T> Messages { get; } = new();

        public WriteOptions? WriteOptions { get; set; }

        public Task WriteAsync(T message)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class SlowServerStreamWriter<T> : IServerStreamWriter<T>
    {
        private readonly TimeSpan _delay;

        public SlowServerStreamWriter(TimeSpan delay)
        {
            _delay = delay;
        }

        public List<T> Messages { get; } = new();

        public WriteOptions? WriteOptions { get; set; }

        public async Task WriteAsync(T message)
        {
            await Task.Delay(_delay).ConfigureAwait(false);
            Messages.Add(message);
        }
    }

    private sealed class TestServerCallContext : ServerCallContext
    {
        private readonly CancellationToken _token;
        private readonly Metadata _requestHeaders = new();
        private readonly Metadata _trailers = new();

        public TestServerCallContext(CancellationToken token)
        {
            _token = token;
        }

        protected override string MethodCore => "test";
        protected override string HostCore => "localhost";
        protected override string PeerCore => "peer";
        protected override DateTime DeadlineCore => DateTime.UtcNow.AddMinutes(1);
        protected override Metadata RequestHeadersCore => _requestHeaders;
        protected override CancellationToken CancellationTokenCore => _token;
        protected override Metadata ResponseTrailersCore => _trailers;
        protected override Status StatusCore { get; set; }
        protected override WriteOptions? WriteOptionsCore { get; set; }
        protected override AuthContext AuthContextCore { get; } = new(string.Empty, new Dictionary<string, List<AuthProperty>>());

        protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions options)
        {
            return new ContextPropagationToken(this, options);
        }

        protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) => Task.CompletedTask;
    }
}
