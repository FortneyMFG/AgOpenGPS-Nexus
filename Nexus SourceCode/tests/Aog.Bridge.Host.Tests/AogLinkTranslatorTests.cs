using Aog.Bridge.Host.AogLink;
using Aog.Bridge.Host.Tests.Support;
using Aog.Core.V1;
using Aog.Link.V1;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aog.Bridge.Host.Tests;

public sealed class AogLinkTranslatorTests
{
    [Fact]
    public void CreateSteerCommand_TracksPendingAck()
    {
        var time = new TestTimeProvider(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var translator = CreateTranslator(time);

        var command = new SteerCmd
        {
            Header = new Header { Timestamp = Timestamp.FromDateTime(DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)) },
            TargetWheelAngleDeg = 2.5,
            Enable = true,
        };

        var envelope = translator.CreateSteerCommand(command, destination: 1);
        Assert.True(envelope.Header.Flags.NeedsAck);

        Assert.True(translator.TryCollectResends(TimeSpan.FromMilliseconds(100), 1, out var resends));
        Assert.Empty(resends);

        time.Advance(TimeSpan.FromMilliseconds(150));
        Assert.True(translator.TryCollectResends(TimeSpan.FromMilliseconds(100), 1, out resends));
        Assert.Single(resends);

        var ack = new CommandAck
        {
            AcknowledgedSequence = envelope.Header.Sequence,
            Status = AckStatus.AckStatusOk,
        };

        Assert.True(translator.TryHandleAck(ack, out var acknowledged));
        Assert.Equal(envelope.Header.Sequence, acknowledged?.Header?.Sequence);
    }

    [Fact]
    public void TryTranslateIncoming_StampsTelemetry()
    {
        var translator = CreateTranslator();
        var pose = new Pose
        {
            LatitudeDeg = 10,
            LongitudeDeg = 20,
        };

        var envelope = translator.CreatePoseTelemetry(pose);

        Assert.True(translator.TryTranslateIncoming(envelope, out var message, out var kind));
        Assert.Equal(AogLinkMessageKind.Pose, kind);

        var decoded = Assert.IsType<Pose>(message);
        Assert.Equal(pose.LatitudeDeg, decoded.LatitudeDeg);
        Assert.NotNull(decoded.Header);
        Assert.Equal("aog-link", decoded.Header.Source);
    }

    private static AogLinkTranslator CreateTranslator(TimeProvider? timeProvider = null)
    {
        var identity = new AogLinkNodeIdentity("bridge", "0.1.0", NodeRole.NodeRoleHost, NodePriority.NodePriorityHigh);
        return new AogLinkTranslator(identity, timeProvider ?? TimeProvider.System, NullLogger<AogLinkTranslator>.Instance);
    }
}
