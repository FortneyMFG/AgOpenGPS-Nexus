using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using Aog.Agio.Linux.Gpsd;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aog.Agio.Linux.Tests;

public sealed class GpsdBackgroundServiceTests
{
    [Fact]
    public async Task ExecuteAsync_WaitsForConfiguration_WhenSocketDisabled()
    {
        var options = new TestOptionsMonitor<GpsdClientOptions>(new GpsdClientOptions
        {
            SocketPath = string.Empty,
            ReconnectDelay = TimeSpan.FromMilliseconds(10),
        });
        var client = new GpsdClient(new ThrowingConnectionFactory(), NullLogger<GpsdClient>.Instance);
        var service = new GpsdBackgroundService(client, NullLogger<GpsdBackgroundService>.Instance, options);

        var executeAsync = typeof(GpsdBackgroundService).GetMethod(
            "ExecuteAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(executeAsync);

        using var cts = new CancellationTokenSource();
        var task = (Task)executeAsync!.Invoke(service, new object[] { cts.Token })!;

        await Task.Delay(TimeSpan.FromMilliseconds(50));
        Assert.False(task.IsCompleted);

        cts.Cancel();
        await task;
    }

    [Fact]
    public async Task ExecuteAsync_Restarts_WhenSocketEnabledAfterStartup()
    {
        var options = new TestOptionsMonitor<GpsdClientOptions>(new GpsdClientOptions
        {
            SocketPath = string.Empty,
            ReconnectDelay = TimeSpan.FromMilliseconds(10),
        });
        var factory = new RecordingGpsdConnectionFactory();
        var client = new GpsdClient(factory, NullLogger<GpsdClient>.Instance);
        var service = new GpsdBackgroundService(client, NullLogger<GpsdBackgroundService>.Instance, options);

        var executeAsync = typeof(GpsdBackgroundService).GetMethod(
            "ExecuteAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(executeAsync);

        using var cts = new CancellationTokenSource();
        var task = (Task)executeAsync!.Invoke(service, new object[] { cts.Token })!;

        await Task.Delay(TimeSpan.FromMilliseconds(100));
        Assert.Equal(0, factory.ConnectCount);

        options.Update(new GpsdClientOptions
        {
            SocketPath = "/tmp/gpsd.sock",
            ReconnectDelay = TimeSpan.FromMilliseconds(10),
        });

        await WaitForConditionAsync(() => factory.ConnectCount > 0, TimeSpan.FromSeconds(2));
        await WaitForConditionAsync(
            () => factory.WrittenLines.Contains(GpsdClientTestHelpers.WatchCommand, StringComparer.Ordinal),
            TimeSpan.FromSeconds(2));

        cts.Cancel();
        await task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    private sealed class ThrowingConnectionFactory : IGpsdConnectionFactory
    {
        public Task<Stream?> ConnectAsync(CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("GpsdBackgroundService should not attempt to connect when disabled.");
        }
    }

    private sealed class RecordingGpsdConnectionFactory : IGpsdConnectionFactory
    {
        private readonly List<string> _writtenLines = new();

        public int ConnectCount { get; private set; }

        public IReadOnlyList<string> WrittenLines => _writtenLines;

        public Task<Stream?> ConnectAsync(CancellationToken cancellationToken)
        {
            ConnectCount++;
            return Task.FromResult<Stream?>(new RecordingGpsdStream(_writtenLines));
        }
    }

    private sealed class RecordingGpsdStream : Stream
    {
        private readonly List<string> _writtenLines;
        private readonly StringBuilder _builder = new();

        public RecordingGpsdStream(List<string> writtenLines)
        {
            _writtenLines = writtenLines;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => 0;

        public override long Position
        {
            get => 0;
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public override int Read(byte[] buffer, int offset, int count) => 0;

        public override int Read(Span<byte> buffer) => 0;

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(0);

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
            => Write(buffer.AsSpan(offset, count));

        public override void Write(ReadOnlySpan<byte> buffer)
            => Append(buffer);

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            Append(buffer.Span);
            return ValueTask.CompletedTask;
        }

        private void Append(ReadOnlySpan<byte> buffer)
        {
            var text = Encoding.UTF8.GetString(buffer);
            _builder.Append(text);

            while (true)
            {
                var newlineIndex = _builder.ToString().IndexOf('\n');
                if (newlineIndex < 0)
                {
                    break;
                }

                var line = _builder.ToString(0, newlineIndex);
                _writtenLines.Add(line);
                _builder.Remove(0, newlineIndex + 1);
            }
        }
    }

    private static async Task WaitForConditionAsync(Func<bool> predicate, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (predicate())
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(20));
        }

        throw new TimeoutException("Condition was not met before the timeout expired.");
    }
}
