using System;
using System.Threading.Tasks;
using Aog.Core.Eventing;
using Aog.Core.Logging;
using Aog.Core.V1;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Xunit;

namespace Aog.Core.Tests.Logging;

public class EventBusPluginTelemetrySinkTests
{
    [Fact]
    public async Task PublishAsync_Forwards_Event_To_EventBus()
    {
        var bus = new InMemoryEventBus();
        var sink = new EventBusPluginTelemetrySink(bus);
        PluginTelemetryEvent? captured = null;
        using var subscription = bus.Subscribe<PluginTelemetryEvent>(message =>
        {
            captured = message;
            return ValueTask.CompletedTask;
        });

        var header = new Header
        {
            Sequence = 42,
            Timestamp = Timestamp.FromDateTime(DateTime.SpecifyKind(new DateTime(2024, 4, 2, 10, 30, 0), DateTimeKind.Utc)),
            Source = "plugin-host"
        };
        var payload = new byte[] { 0x10, 0x20, 0x30 };

        await sink.PublishAsync("  demo.plugin  ", "  status  ", payload, header);

        captured.Should().NotBeNull();
        captured!.Header.Should().NotBeNull();
        captured.Header!.Sequence.Should().Be(header.Sequence);
        captured.Header.Timestamp.Should().Be(header.Timestamp);
        captured.Header.Source.Should().Be(header.Source);
        captured.PluginId.Should().Be("demo.plugin");
        captured.Topic.Should().Be("status");
        captured.Payload.ToArray().Should().Equal(payload);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task PublishAsync_Requires_PluginId(string? pluginId)
    {
        var sink = new EventBusPluginTelemetrySink(new InMemoryEventBus());

        Func<Task> action = async () => await sink.PublishAsync(pluginId!, "topic", Array.Empty<byte>());

        await action.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("pluginId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task PublishAsync_Requires_Topic(string? topic)
    {
        var sink = new EventBusPluginTelemetrySink(new InMemoryEventBus());

        Func<Task> action = async () => await sink.PublishAsync("plugin", topic!, Array.Empty<byte>());

        await action.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("topic");
    }
}
