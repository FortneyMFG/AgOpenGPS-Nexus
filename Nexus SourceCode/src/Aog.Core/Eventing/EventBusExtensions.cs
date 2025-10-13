using System;
using System.Threading;
using System.Threading.Tasks;

namespace Aog.Core.Eventing;

/// <summary>
/// Convenience extensions for <see cref="IEventBus"/> implementations.
/// </summary>
public static class EventBusExtensions
{
    public static IDisposable Subscribe<TEvent>(this IEventBus bus, Func<TEvent, ValueTask> handler)
    {
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(handler);

        return bus.Subscribe<TEvent>((message, _) => handler(message));
    }

    public static IDisposable Subscribe<TEvent>(this IEventBus bus, Action<TEvent> handler)
    {
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(handler);

        return bus.Subscribe<TEvent>((message, _) =>
        {
            handler(message);
            return ValueTask.CompletedTask;
        });
    }

    public static ValueTask PublishAsync<TEvent>(this IEventBus bus, TEvent message)
        => bus.PublishAsync(message, CancellationToken.None);
}
