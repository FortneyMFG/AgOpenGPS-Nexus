using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aog.Agio.Linux;
using Aog.Core.V1;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Core.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using SocketCANSharp;
using SocketCANSharp.Network;
using Xunit;
using Xunit.Sdk;

namespace Aog.Agio.Linux.Tests;

public sealed class SocketCanBackendTests
{
    [Fact]
    public async Task SocketCanService_ForwardsFramesFromInterface()
    {
        if (!OperatingSystem.IsLinux())
        {
            return;
        }

        await using var vcan = await VcanTestInterface.CreateAsync();

        var options = Options.Create(new SocketCanOptions
        {
            InterfaceName = vcan.Name,
            PollInterval = TimeSpan.FromMilliseconds(5),
        });
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero));
        var service = new SocketCanBusService();
        var listener = new SocketCanListenerService(
            NullLogger<SocketCanListenerService>.Instance,
            options,
            service,
            timeProvider);

        await listener.StartAsync(CancellationToken.None);

        try
        {
            var writer = new RecordingStreamWriter<CanFrame>();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var context = CreateContext(cts.Token, "aog.agio.v1.CanBusService/SubscribeFrames");

            var callTask = service.SubscribeFrames(new Empty(), writer, context);

            await Task.Delay(TimeSpan.FromMilliseconds(100), CancellationToken.None);

            await SendFrameAsync(vcan.Name, new SocketCANSharp.CanFrame(0x123, new byte[] { 0x11, 0x22, 0x33 }));

            await WaitForCountAsync(writer, expectedCount: 1, TimeSpan.FromSeconds(2));
            var frame = writer[0];

            Assert.Equal(0x123u, frame.ArbitrationId);
            Assert.Equal(new byte[] { 0x11, 0x22, 0x33 }, frame.Payload.ToByteArray());
            Assert.False(frame.IsExtendedId);
            Assert.False(frame.IsRemoteRequest);
            Assert.Equal(vcan.Name, frame.Header.Source);
            Assert.Equal(timeProvider.GetUtcNow().UtcDateTime, frame.Header.Timestamp.ToDateTime());
            Assert.True(frame.Header.Sequence > 0);

            cts.Cancel();
            await callTask.WaitAsync(TimeSpan.FromSeconds(1));
        }
        finally
        {
            await listener.StopAsync(CancellationToken.None);
        }
    }

    private static async Task SendFrameAsync(string interfaceName, SocketCANSharp.CanFrame frame)
    {
        var iface = CanNetworkInterface.GetAllInterfaces(includeVirtual: true)
            .FirstOrDefault(candidate => string.Equals(candidate.Name, interfaceName, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Interface '{interfaceName}' was not found.");

        using var socket = new RawCanSocket();
        socket.Bind(iface);
        socket.Write(frame);

        // Allow the kernel to deliver the frame to listeners.
        await Task.Delay(TimeSpan.FromMilliseconds(50));
    }

    private static async Task WaitForCountAsync(RecordingStreamWriter<CanFrame> writer, int expectedCount, TimeSpan timeout)
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

        Assert.True(writer.Count >= expectedCount, $"Expected at least {expectedCount} frame(s), observed {writer.Count}.");
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

    private sealed class VcanTestInterface : IAsyncDisposable
    {
        private VcanTestInterface(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public static async Task<VcanTestInterface> CreateAsync()
        {
            if (!OperatingSystem.IsLinux())
                throw new SkipException("SocketCAN requires Linux.");

            await RunCommandAsync("modprobe", ignoreErrors: false, "vcan");

            var suffix = Guid.NewGuid().ToString("N").Substring(0, 8);
            var name = $"vcan{suffix}";

            await RunCommandAsync("ip", ignoreErrors: false, "link", "add", "dev", name, "type", "vcan");
            var created = true;
            try
            {
                await RunCommandAsync("ip", ignoreErrors: false, "link", "set", "up", name);
                return new VcanTestInterface(name);
            }
            catch
            {
                created = false;
                throw;
            }
            finally
            {
                if (!created)
                {
                    await RunCommandAsync("ip", ignoreErrors: true, "link", "delete", name);
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (!OperatingSystem.IsLinux())
            {
                return;
            }

            await RunCommandAsync("ip", ignoreErrors: true, "link", "delete", Name);
        }

        private static async Task RunCommandAsync(string fileName, bool ignoreErrors, params string[] arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = string.Join(' ', arguments),
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };

            try
            {
                using var process = Process.Start(startInfo)
                    ?? throw new InvalidOperationException($"Failed to start '{fileName}'.");

                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    var error = await process.StandardError.ReadToEndAsync();
                    if (ignoreErrors)
                    {
                        return;
                    }

                    throw new SkipException($"Command '{fileName} {string.Join(' ', arguments)}' failed: {error.Trim()}");
                }
            }
            catch (Win32Exception ex)
            {
                throw new SkipException($"Command '{fileName}' is required for SocketCAN tests: {ex.Message}");
            }
            catch (InvalidOperationException ex) when (!ignoreErrors)
            {
                throw new SkipException(ex.Message);
            }
        }
    }
}
