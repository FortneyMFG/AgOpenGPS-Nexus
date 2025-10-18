using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Simulation;
using FluentAssertions;
using Xunit;

namespace Aog.Core.Tests.Simulation;

public static class InMemorySimBusTests
{
    [Fact]
    public static async Task PublishAsync_DeliversMessagesInSubscriptionOrder()
    {
        var bus = new InMemorySimBus();
        var received = new List<string>();

        bus.Subscribe<int>("pose", (message, _) =>
        {
            received.Add($"first:{message.Payload}");
            return ValueTask.CompletedTask;
        });

        bus.Subscribe<int>("pose", (message, _) =>
        {
            received.Add($"second:{message.Payload}");
            return ValueTask.CompletedTask;
        });

        var time = SimTime.FromTick(0, TimeSpan.FromMilliseconds(10));
        await bus.PublishAsync("pose", time, 42);

        received.Should().Equal("first:42", "second:42");
    }

    [Fact]
    public static async Task DisposingSubscription_PreventsFurtherDelivery()
    {
        var bus = new InMemorySimBus();
        var count = 0;

        var subscription = bus.Subscribe<int>("pose", (_, _) =>
        {
            count++;
            return ValueTask.CompletedTask;
        });

        var time = SimTime.FromTick(0, TimeSpan.FromMilliseconds(1));
        await bus.PublishAsync("pose", time, 1);

        subscription.Dispose();
        await bus.PublishAsync("pose", time.Advance(TimeSpan.FromMilliseconds(1)), 2);

        count.Should().Be(1);
    }

    [Fact]
    public static async Task PublishAsync_WithNoSubscribers_CompletesSilently()
    {
        var bus = new InMemorySimBus();
        var time = SimTime.FromTick(0, TimeSpan.FromMilliseconds(5));

        var act = () => bus.PublishAsync("pose", time, 1).AsTask();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public static void Subscribe_WithNullHandler_Throws()
    {
        var bus = new InMemorySimBus();
        Action act = () => bus.Subscribe<int>("pose", null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public static void Subscribe_WithEmptyTopic_Throws()
    {
        var bus = new InMemorySimBus();
        Action act = () => bus.Subscribe<int>(string.Empty, (_, _) => ValueTask.CompletedTask);

        act.Should().Throw<ArgumentException>();
    }
}
