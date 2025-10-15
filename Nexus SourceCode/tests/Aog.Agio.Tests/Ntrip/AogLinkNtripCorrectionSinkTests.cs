using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aog.Agio.AogLink;
using Aog.Agio.Ntrip;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aog.Agio.Tests.Ntrip;

public sealed class AogLinkNtripCorrectionSinkTests
{
    [Fact]
    public async Task PublishAsync_ForwardsFramesToEnabledDrivers()
    {
        var driver = new RecordingDriver { IsEnabled = true };
        var sink = new AogLinkNtripCorrectionSink(new[] { driver }, NullLogger<AogLinkNtripCorrectionSink>.Instance);

        var payload = new byte[] { 0x01, 0x02, 0x03 };
        await sink.PublishAsync(payload, CancellationToken.None);

        Assert.Single(driver.Frames);
        var frame = driver.Frames[0];
        Assert.Equal(AogLinkMessageCatalog.GnssClass, frame.Header.MessageClass);
        Assert.Equal(AogLinkMessageCatalog.RtcmCorrectionsType, frame.Header.MessageType);
        Assert.Equal(payload, frame.Payload.ToArray());
    }

    [Fact]
    public async Task PublishAsync_DropsPayloadWhenAllDriversDisabled()
    {
        var driver = new RecordingDriver { IsEnabled = false };
        var sink = new AogLinkNtripCorrectionSink(new[] { driver }, NullLogger<AogLinkNtripCorrectionSink>.Instance);

        await sink.PublishAsync(new byte[] { 0x10 }, CancellationToken.None);

        Assert.Empty(driver.Frames);
    }

    private sealed class RecordingDriver : IAogLinkTransportDriver
    {
        public string Name => "recording";

        public bool IsEnabled { get; set; }

        public List<AogLinkFrame> Frames { get; } = new();

        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public ValueTask SendAsync(AogLinkFrame frame, CancellationToken cancellationToken)
        {
            Frames.Add(frame);
            return ValueTask.CompletedTask;
        }

        public async IAsyncEnumerable<AogLinkFrame> ReceiveAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            yield break;
        }
    }
}
