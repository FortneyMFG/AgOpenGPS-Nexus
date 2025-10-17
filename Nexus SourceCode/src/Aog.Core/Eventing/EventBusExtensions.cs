using System;
using System.Threading;
using System.Threading.Tasks;

namespace Aog.Core.Eventing;

/// <summary>
/// Convenience extensions for <see cref="IEventBus"/> implementations.
/// </summary>
public static class EventBusExtensions
{
    /// <summary>
    /// Subscribes a handler that returns a <see cref="ValueTask"/> without a cancellation token parameter.
    /// </summary>
    /// <typeparam name="TEvent">The event type to subscribe to.</typeparam>
    /// <param name="bus">Event bus receiving the subscription.</param>
    /// <param name="handler">Handler invoked when the event is published.</param>
    /// <returns>A disposable subscription used to unregister the handler.</returns>
    public static IDisposable Subscribe<TEvent>(this IEventBus bus, Func<TEvent, ValueTask> handler)
    {
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(handler);

        return bus.Subscribe<TEvent>((message, _) => handler(message));
    }

    /// <summary>
    /// Subscribes a synchronous handler to the specified event type.
    /// </summary>
    /// <typeparam name="TEvent">The event type to subscribe to.</typeparam>
    /// <param name="bus">Event bus receiving the subscription.</param>
    /// <param name="handler">Handler invoked when the event is published.</param>
    /// <returns>A disposable subscription used to unregister the handler.</returns>
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

    /// <summary>
    /// Publishes an event without providing a cancellation token.
    /// </summary>
    /// <typeparam name="TEvent">The event type being published.</typeparam>
    /// <param name="bus">Event bus that should broadcast the event.</param>
    /// <param name="message">The event payload.</param>
    /// <returns>A task that completes when all handlers have been invoked.</returns>
    public static ValueTask PublishAsync<TEvent>(this IEventBus bus, TEvent message)
        => bus.PublishAsync(message, CancellationToken.None);
}
