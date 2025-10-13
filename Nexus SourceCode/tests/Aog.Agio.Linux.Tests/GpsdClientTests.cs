using System.Runtime.CompilerServices;
using Aog.Agio.Linux;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aog.Agio.Linux.Tests;

public sealed class GpsdClientTests
{
    [Fact]
    public async Task WatchAsync_YieldsTpvReports()
    {
        var lines = new[]
        {
            "{\"class\":\"VERSION\",\"release\":\"3.24\"}",
            "{\"class\":\"TPV\",\"device\":\"/dev/ttyACM0\",\"mode\":3,\"time\":\"2024-02-12T18:32:10.000Z\",\"lat\":48.123456,\"lon\":11.543210,\"alt\":512.4,\"speed\":2.50,\"track\":93.4}",
            "{\"class\":\"TPV\",\"device\":\"/dev/ttyACM0\",\"mode\":2,\"time\":\"2024-02-12T18:32:11.000Z\",\"lat\":48.123400,\"lon\":11.543250,\"alt\":512.5,\"speed\":2.75,\"track\":95.0}",
            "{\"class\":\"SKY\",\"uSat\":12}",
        };

        var transport = new FakeGpsdTransport(lines);
        var client = new GpsdClient(transport, NullLogger<GpsdClient>.Instance);

        var reports = new List<GpsdTpvReport>();
        await foreach (var report in client.WatchAsync(CancellationToken.None))
        {
            reports.Add(report);
        }

        Assert.Equal(GpsdClientTestHelpers.WatchCommand, transport.LastCommand);
        Assert.Equal(2, reports.Count);

        var first = reports[0];
        Assert.Equal("/dev/ttyACM0", first.Device);
        Assert.Equal(3, first.Mode);
        Assert.Equal(48.123456, first.LatitudeDegrees);
        Assert.Equal(11.543210, first.LongitudeDegrees);
        Assert.Equal(512.4, first.AltitudeMeters);
        Assert.Equal(2.50, first.SpeedMetersPerSecond);
        Assert.Equal(93.4, first.TrackDegrees);
    }

    [Fact]
    public async Task WatchAsync_IgnoresMalformedJson()
    {
        var lines = new[]
        {
            "{\"class\":\"TPV\",\"device\":\"/dev/ttyACM0\",\"mode\":3,\"time\":\"2024-02-12T18:32:12.000Z\",\"lat\":48.2,\"lon\":11.5,\"alt\":510.0}",
            "{\"class\":\"TPV\",\"lat\":48.2,\"lon\":11.5",
            "not json",
        };

        var transport = new FakeGpsdTransport(lines);
        var client = new GpsdClient(transport, NullLogger<GpsdClient>.Instance);

        var reports = new List<GpsdTpvReport>();
        await foreach (var report in client.WatchAsync(CancellationToken.None))
        {
            reports.Add(report);
        }

        Assert.Single(reports);
    }

    private sealed class FakeGpsdTransport : IGpsdTransport
    {
        private readonly string[] _lines;

        public FakeGpsdTransport(string[] lines)
        {
            _lines = lines;
        }

        public string? LastCommand { get; private set; }

        public Task<IGpsdSession> ConnectAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IGpsdSession>(new FakeGpsdSession(this, _lines));
        }

        private sealed class FakeGpsdSession : IGpsdSession
        {
            private readonly FakeGpsdTransport _owner;
            private readonly string[] _lines;

            public FakeGpsdSession(FakeGpsdTransport owner, string[] lines)
            {
                _owner = owner;
                _lines = lines;
            }

            public Task SendAsync(string command, CancellationToken cancellationToken)
            {
                _owner.LastCommand = command;
                return Task.CompletedTask;
            }

            public async IAsyncEnumerable<string> ReadLinesAsync([EnumeratorCancellation] CancellationToken cancellationToken)
            {
                foreach (var line in _lines)
                {
                    await Task.Yield();
                    yield return line;
                }
            }

            public ValueTask DisposeAsync()
            {
                return ValueTask.CompletedTask;
            }
        }
    }
}

internal static class GpsdClientTestHelpers
{
    public const string WatchCommand = "?WATCH={\"enable\":true,\"json\":true}";
}
