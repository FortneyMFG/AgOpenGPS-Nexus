using System;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Eventing;
using FluentAssertions;
using Xunit;

namespace Aog.Core.Tests.Eventing;

public class InMemoryEventBusTests
{
    [Fact]
    public async Task PublishAsync_ForwardsMessageToSubscriber()
    {
        var bus = new InMemoryEventBus();
        var received = 0;

        bus.Subscribe<int>((message, _) =>
        {
            received = message;
            return ValueTask.CompletedTask;
        });

        await bus.PublishAsync(42);

        received.Should().Be(42);
    }

    [Fact]
    public async Task PublishAsync_NotifiesMultipleSubscribers()
    {
        var bus = new InMemoryEventBus();
        var first = 0;
        var second = 0;

        bus.Subscribe<int>((message, _) =>
        {
            first = message;
            return ValueTask.CompletedTask;
        });

        bus.Subscribe<int>((message, _) =>
        {
            second = message;
            return ValueTask.CompletedTask;
        });

        await bus.PublishAsync(7);

        first.Should().Be(7);
        second.Should().Be(7);
    }

    [Fact]
    public async Task Dispose_UnsubscribesHandler()
    {
        var bus = new InMemoryEventBus();
        var callCount = 0;

        var subscription = bus.Subscribe<int>((_, _) =>
        {
            callCount++;
            return ValueTask.CompletedTask;
        });

        await bus.PublishAsync(1);
        subscription.Dispose();
        await bus.PublishAsync(1);

        callCount.Should().Be(1);
    }

    [Fact]
    public async Task PublishAsync_RespectsCancellation()
    {
        var bus = new InMemoryEventBus();
        var callCount = 0;
        var tcs = new TaskCompletionSource<bool>();

        bus.Subscribe<int>(async (_, token) =>
        {
            callCount++;
            tcs.TrySetResult(true);
            await Task.Delay(TimeSpan.FromSeconds(10), token);
        });

        bus.Subscribe<int>((_, _) =>
        {
            callCount++;
            return ValueTask.CompletedTask;
        });

        using var cts = new CancellationTokenSource();

        var publishTask = bus.PublishAsync(5, cts.Token).AsTask();
        await tcs.Task; // ensure first handler started
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => publishTask);
        callCount.Should().Be(1);
    }
}
