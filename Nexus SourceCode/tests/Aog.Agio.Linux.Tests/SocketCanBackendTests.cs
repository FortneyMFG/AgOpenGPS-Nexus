using System;
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
        var options = Options.Create(new SocketCanOptions
        {
            InterfaceName = "vcan0",
            ReceiveTimeout = TimeSpan.FromMilliseconds(10),
            ReconnectDelay = TimeSpan.FromMilliseconds(10),
            SourcePrefix = "test/socketcan",
        });

        var service = new SocketCanBackgroundService(
            factory,
            channel,
            options,
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
    public async Task SocketCanBusService_ForwardsFramesToSubscribers()
    {
        var channel = new SocketCanFrameChannel();
        var service = new SocketCanBusService(channel);
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
    public async Task SocketCanBusService_DropsSlowSubscribers()
    {
        var channel = new SocketCanFrameChannel();
        var service = new SocketCanBusService(channel);
        var slowWriter = new SlowServerStreamWriter<CanFrame>(TimeSpan.FromMilliseconds(200));
        var slowContext = new TestServerCallContext(CancellationToken.None);

        var slowCallTask = service.SubscribeFrames(new Empty(), slowWriter, slowContext);

        for (var i = 0; i < 50; i++)
        {
            await channel.PublishAsync(new CanFrame
            {
                Header = new Header(),
                ArbitrationId = (uint)i,
            }, CancellationToken.None).ConfigureAwait(false);
        }

        await WaitForAsync(() => slowCallTask.IsCompleted, TimeSpan.FromSeconds(5));
        await slowCallTask.ConfigureAwait(false);

        Assert.True(slowWriter.Messages.Count < 50);

        var fastWriter = new TestServerStreamWriter<CanFrame>();
        using var fastCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var fastContext = new TestServerCallContext(fastCts.Token);

        var fastCallTask = service.SubscribeFrames(new Empty(), fastWriter, fastContext);

        var finalFrame = new CanFrame { Header = new Header(), ArbitrationId = 0xABC };
        await channel.PublishAsync(finalFrame, CancellationToken.None).ConfigureAwait(false);

        await WaitForAsync(() => fastWriter.Messages.Count > 0, TimeSpan.FromSeconds(1));
        var received = Assert.Single(fastWriter.Messages);
        Assert.Equal(0xABCu, received.ArbitrationId);

        fastCts.Cancel();
        await fastCallTask.ConfigureAwait(false);
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

    private sealed class FakeSocketCanClient : ISocketCanClient
    {
        private readonly Queue<SocketCANSharp.CanFrame> _frames;

        public FakeSocketCanClient(string interfaceName, IEnumerable<SocketCANSharp.CanFrame> frames)
        {
            InterfaceName = interfaceName;
            _frames = new Queue<SocketCANSharp.CanFrame>(frames);
        }

        public string InterfaceName { get; }

        public SocketCanFrameReadResult ReadFrame(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_frames.Count == 0)
            {
                return SocketCanFrameReadResult.Timeout();
            }

            return SocketCanFrameReadResult.FromFrame(_frames.Dequeue());
        }

        public void Dispose()
        {
        }
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _now;

        public FixedTimeProvider(DateTimeOffset now) => _now = now;

        public override DateTimeOffset GetUtcNow() => _now;
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
