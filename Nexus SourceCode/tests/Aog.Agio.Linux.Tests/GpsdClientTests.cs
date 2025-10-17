using System.Net.Sockets;
using System.Reflection;
using Aog.Agio.Linux.Gpsd;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Xunit.Sdk;

namespace Aog.Agio.Linux.Tests;

public sealed class GpsdClientTests
{
    [Fact]
    public async Task WatchAsync_ParsesTpvReports()
    {
        var feed = new[]
        {
            "{\"class\":\"VERSION\",\"release\":\"3.23\"}",
            "{\"class\":\"WATCH\",\"enable\":true,\"json\":true}",
            "{\"class\":\"TPV\",\"mode\":3,\"lat\":48.1173,\"lon\":11.5167,\"alt\":545.4,\"speed\":0.514,\"track\":84.4,\"time\":\"2024-01-01T12:35:19.000Z\"}",
            "{\"class\":\"TPV\",\"mode\":2,\"lat\":48.1174,\"lon\":11.5168,\"speed\":0.420,\"track\":83.0}",
        };

        var factory = new FakeGpsdConnectionFactory(feed);
        var client = new GpsdClient(factory, NullLogger<GpsdClient>.Instance);

        var reports = new List<GpsdTpvReport>();
        await foreach (var report in client.WatchAsync(CancellationToken.None))
        {
            reports.Add(report);
            if (reports.Count >= 2)
            {
                break;
            }
        }

        Assert.Equal(2, reports.Count);
        Assert.Equal(48.1173, reports[0].LatitudeDegrees);
        Assert.Equal(11.5167, reports[0].LongitudeDegrees);
        Assert.Equal(545.4, reports[0].AltitudeMeters);
        Assert.Equal(0.514, reports[0].SpeedMetersPerSecond);
        Assert.Equal(84.4, reports[0].TrackDegrees);
        Assert.Equal(3, reports[0].Mode);
        Assert.Equal(2, reports[1].Mode);
        Assert.Contains(GpsdClientTestHelpers.WatchCommand, factory.WrittenLines);
    }

    [Theory]
    [InlineData(91.0, 11.5167)]
    [InlineData(-91.0, 11.5167)]
    [InlineData(48.1173, 181.0)]
    [InlineData(48.1173, -181.0)]
    public async Task WatchAsync_SkipsOutOfRangeCoordinates(double latitude, double longitude)
    {
        var feed = new[]
        {
            "{\"class\":\"VERSION\",\"release\":\"3.23\"}",
            "{\"class\":\"WATCH\",\"enable\":true,\"json\":true}",
            $"{{\\"class\\":\\"TPV\\",\\"mode\\":3,\\"lat\\":{latitude},\\"lon\\":{longitude},\\"alt\\":545.4,\\"speed\\":0.514,\\"track\\":84.4,\\"time\\":\\"2024-01-01T12:35:19.000Z\\"}}",
        };

        var factory = new FakeGpsdConnectionFactory(feed);
        var client = new GpsdClient(factory, NullLogger<GpsdClient>.Instance);

        var reports = new List<GpsdTpvReport>();
        await foreach (var report in client.WatchAsync(CancellationToken.None))
        {
            reports.Add(report);
        }

        Assert.Empty(reports);
        Assert.Contains(GpsdClientTestHelpers.WatchCommand, factory.WrittenLines);
    }

    [Fact]
    public async Task BackgroundService_Continues_WhenSpeedAndTrackMissing()
    {
        var feed = new[]
        {
            "{\"class\":\"VERSION\",\"release\":\"3.23\"}",
            "{\"class\":\"WATCH\",\"enable\":true,\"json\":true}",
            "{\"class\":\"TPV\",\"mode\":3,\"lat\":48.1173,\"lon\":11.5167,\"alt\":545.4}",
            "{\"class\":\"TPV\",\"mode\":3,\"lat\":48.1174,\"lon\":11.5168,\"alt\":545.4,\"speed\":0.514,\"track\":84.4}",
        };

        var factory = new FakeGpsdConnectionFactory(feed);
        var client = new GpsdClient(factory, NullLogger<GpsdClient>.Instance);
        var options = new TestOptionsMonitor<GpsdClientOptions>(new GpsdClientOptions
        {
            SocketPath = "/tmp/gpsd.sock",
            ReconnectDelay = TimeSpan.FromSeconds(1),
        });

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var logger = new ThrowingLogger<GpsdBackgroundService>();
        var service = new GpsdBackgroundService(client, logger, options);

        var executeAsync = typeof(GpsdBackgroundService).GetMethod(
            "ExecuteAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(executeAsync);

        var task = (Task)executeAsync!.Invoke(service, new object[] { cts.Token })!;
        await task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Contains(GpsdClientTestHelpers.WatchCommand, factory.WrittenLines);
    }

    [Fact]
    public async Task BackgroundService_Continues_WhenCoreFieldsMissing()
    {
        var feed = new[]
        {
            "{\"class\":\"VERSION\",\"release\":\"3.23\"}",
            "{\"class\":\"WATCH\",\"enable\":true,\"json\":true}",
            "{\"class\":\"TPV\",\"mode\":3}",
            "{\"class\":\"TPV\",\"mode\":3,\"lat\":48.1174,\"lon\":11.5168,\"alt\":545.4,\"speed\":0.514,\"track\":84.4,\"time\":\"2024-01-01T12:35:19.000Z\"}",
        };

        var factory = new FakeGpsdConnectionFactory(feed);
        var client = new GpsdClient(factory, NullLogger<GpsdClient>.Instance);
        var options = new TestOptionsMonitor<GpsdClientOptions>(new GpsdClientOptions
        {
            SocketPath = "/tmp/gpsd.sock",
            ReconnectDelay = TimeSpan.FromSeconds(1),
        });

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var logger = new ThrowingLogger<GpsdBackgroundService>();
        var service = new GpsdBackgroundService(client, logger, options);

        var executeAsync = typeof(GpsdBackgroundService).GetMethod(
            "ExecuteAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(executeAsync);

        var task = (Task)executeAsync!.Invoke(service, new object[] { cts.Token })!;
        await task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Contains("?WATCH={\"enable\":true,\"json\":true}", factory.WrittenLines);
    }

    [Fact]
    public async Task WatchAsync_WritesWatchCommand()
    {
        var factory = new FakeGpsdConnectionFactory(Array.Empty<string>());
        var client = new GpsdClient(factory, NullLogger<GpsdClient>.Instance);

        await foreach (var _ in client.WatchAsync(CancellationToken.None))
        {
            // drain the async enumerable to ensure the command is written before exit
        }

        Assert.Single(factory.WrittenLines, GpsdClientTestHelpers.WatchCommand);
    }

    [Fact]
    public async Task GpsdStreamSession_SendAsyncHonorsCancellation()
    {
        var blockingStream = new BlockingGpsdStream();

        await using var session = new GpsdStreamSession(blockingStream, NullLogger.Instance);

        using var cts = new CancellationTokenSource();

        var sendTask = session.SendAsync("?WATCH=1", cts.Token);

        await blockingStream.WaitForWriteStartAsync().WaitAsync(TimeSpan.FromSeconds(1));

        cts.Cancel();

        var completedTask = await Task.WhenAny(sendTask, Task.Delay(TimeSpan.FromSeconds(1)));
        blockingStream.Release();

        Assert.Same(sendTask, completedTask);
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await sendTask);
        Assert.True(blockingStream.ObservedWriteCancellationToken.CanBeCanceled);
        Assert.True(blockingStream.ObservedWriteCancellationToken.IsCancellationRequested);
    }

    [Fact]
    public async Task WatchAsync_CancellationDuringSendAsyncTerminatesPromptly()
    {
        var blockingStream = new BlockingGpsdStream();
        var factory = new BlockingGpsdConnectionFactory(blockingStream);
        var client = new GpsdClient(factory, NullLogger<GpsdClient>.Instance);

        using var cts = new CancellationTokenSource();

        await using var enumerator = client.WatchAsync(cts.Token).GetAsyncEnumerator();

        var moveNextTask = enumerator.MoveNextAsync().AsTask();

        await blockingStream.WaitForWriteStartAsync().WaitAsync(TimeSpan.FromSeconds(1));

        cts.Cancel();

        var completedTask = await Task.WhenAny(moveNextTask, Task.Delay(TimeSpan.FromSeconds(1)));
        blockingStream.Release();

        Assert.Same(moveNextTask, completedTask);
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await moveNextTask);
        Assert.True(blockingStream.ObservedWriteCancellationToken.CanBeCanceled);
        Assert.True(blockingStream.ObservedWriteCancellationToken.IsCancellationRequested);
    }

    [Fact]
    public async Task WatchAsync_ThrowsWhenSocketUnavailable()
    {
        var factory = new NullGpsdConnectionFactory();
        var client = new GpsdClient(factory, NullLogger<GpsdClient>.Instance);

        await Assert.ThrowsAsync<GpsdSocketUnavailableException>(async () => await ConsumeAsync(client.WatchAsync(CancellationToken.None)));
    }

    [Fact]
    public async Task WatchAsync_PropagatesAccessDenied()
    {
        var factory = new AccessDeniedConnectionFactory();
        var client = new GpsdClient(factory, NullLogger<GpsdClient>.Instance);

        var exception = await Assert.ThrowsAsync<GpsdUnavailableException>(async () => await ConsumeAsync(client.WatchAsync(CancellationToken.None)));

        Assert.Contains("SocketError: AccessDenied", exception.Message);
        var inner = Assert.IsType<SocketException>(exception.InnerException);
        Assert.Equal(SocketError.AccessDenied, inner.SocketErrorCode);
    }

    private static async Task ConsumeAsync(IAsyncEnumerable<GpsdTpvReport> source)
    {
        await foreach (var _ in source)
        {
        }
    }

    private sealed class FakeGpsdConnectionFactory : IGpsdConnectionFactory
    {
        private readonly string _feed;
        private bool _connected;

        public FakeGpsdConnectionFactory(IEnumerable<string> lines)
        {
            _feed = string.Join("\n", lines) + "\n";
        }

        public List<string> WrittenLines { get; } = new();

        public Task<Stream?> ConnectAsync(CancellationToken cancellationToken)
        {
            if (_connected)
            {
                return Task.FromResult<Stream?>(null);
            }

            _connected = true;
            return Task.FromResult<Stream?>(new FakeGpsdStream(_feed, WrittenLines));
        }
    }

    private sealed class NullGpsdConnectionFactory : IGpsdConnectionFactory
    {
        public Task<Stream?> ConnectAsync(CancellationToken cancellationToken) => Task.FromResult<Stream?>(null);
    }

private sealed class BlockingGpsdConnectionFactory : IGpsdConnectionFactory
{
    private readonly BlockingGpsdStream _stream;
    private bool _connected;

    public BlockingGpsdConnectionFactory(BlockingGpsdStream stream)
    {
        _stream = stream;
    }

    public Task<Stream?> ConnectAsync(CancellationToken cancellationToken)
    {
        if (_connected)
        {
            return Task.FromResult<Stream?>(null);
        }

        _connected = true;
        return Task.FromResult<Stream?>(_stream);
    }
}

private sealed class AccessDeniedConnectionFactory : IGpsdConnectionFactory
{
    public Task<Stream?> ConnectAsync(CancellationToken cancellationToken)
    {
        throw new SocketException((int)SocketError.AccessDenied);
    }
}

        }
    }

    private sealed class FakeGpsdStream : Stream
    {
        private readonly byte[] _readBuffer;
        private int _readPosition;
        private readonly List<string> _writtenLines;
        private readonly List<byte> _writeBuffer = new();

        public FakeGpsdStream(string feed, List<string> writtenLines)
        {
            _readBuffer = System.Text.Encoding.UTF8.GetBytes(feed);
            _writtenLines = writtenLines;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => _readBuffer.Length;
        public override long Position
        {
            get => _readPosition;
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public override int Read(byte[] buffer, int offset, int count)
        {
            return Read(buffer.AsSpan(offset, count));
        }

        public override int Read(Span<byte> buffer)
        {
            var remaining = _readBuffer.Length - _readPosition;
            if (remaining <= 0)
            {
                return 0;
            }

            var toCopy = Math.Min(buffer.Length, remaining);
            _readBuffer.AsSpan(_readPosition, toCopy).CopyTo(buffer);
            _readPosition += toCopy;
            return toCopy;
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return new ValueTask<int>(Read(buffer.Span));
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            AppendWritten(new ReadOnlySpan<byte>(buffer, offset, count));
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            AppendWritten(buffer.Span);
            return ValueTask.CompletedTask;
        }

        private void AppendWritten(ReadOnlySpan<byte> data)
        {
            foreach (var b in data)
            {
                if (b == (byte)'\n')
                {
                    if (_writeBuffer.Count > 0)
                    {
                        var line = System.Text.Encoding.UTF8.GetString(_writeBuffer.ToArray()).TrimEnd('\r');
                        if (line.Length > 0)
                        {
                            _writtenLines.Add(line);
                        }
                        _writeBuffer.Clear();
                    }
                }
                else
                {
                    _writeBuffer.Add(b);
                }
            }
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();
    }

private sealed class BlockingGpsdStream : Stream
{
    private readonly TaskCompletionSource<bool> _writeStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly CancellationTokenSource _releaseCts = new();

    public CancellationToken ObservedWriteCancellationToken { get; private set; }

    public Task WaitForWriteStartAsync() => _writeStarted.Task;

    public void Release() => _releaseCts.Cancel();

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => 0;
    public override long Position
    {
        get => 0;
        set => throw new NotSupportedException();
    }

    public override void Flush() { }

    public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public override int Read(byte[] buffer, int offset, int count) => 0;

    public override int Read(Span<byte> buffer) => 0;

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        => new(0);

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => Task.FromResult(0);

    public override void Write(byte[] buffer, int offset, int count)
        => throw new NotSupportedException("Synchronous writes are not supported by the blocking test stream.");

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => WaitForReleaseAsync(cancellationToken);

    public override Task WriteAsync(byte[] buffer, int offset, int count)
        => WaitForReleaseAsync(CancellationToken.None);

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        => new(WaitForReleaseAsync(cancellationToken));

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    private Task WaitForReleaseAsync(CancellationToken cancellationToken)
    {
        ObservedWriteCancellationToken = cancellationToken;
        _writeStarted.TrySetResult(true);
        return WaitForCancellationAsync(cancellationToken);
    }

    private async Task WaitForCancellationAsync(CancellationToken cancellationToken)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _releaseCts.Token);
        try
        {
            await Task.Delay(Timeout.Infinite, linkedCts.Token).ConfigureAwait(false);
        }
        finally
        {
            linkedCts.Cancel();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _releaseCts.Cancel();
            _releaseCts.Dispose();
        }

        base.Dispose(disposing);
    }
}

private sealed class ThrowingLogger<T> : ILogger<T>
{
    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        where TState : notnull
    {
        if (logLevel >= LogLevel.Error)
        {
            throw new XunitException($"Unexpected {logLevel} log: {formatter(state, exception)}");
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();

        public void Dispose() { }
    }
}

        }
    }
}
