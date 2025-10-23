namespace Nexus.Sdk.Core;

/// <summary>
/// Dispatches strongly typed events between plugins and the host.
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Publishes an event to every subscriber.
    /// </summary>
    /// <typeparam name="TEvent">Event payload type.</typeparam>
    /// <param name="event">Event instance.</param>
    /// <param name="cancellationToken">Token used to cancel dispatch.</param>
    ValueTask PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : notnull;

    /// <summary>
    /// Subscribes to events of a given type.
    /// </summary>
    /// <typeparam name="TEvent">Event payload type.</typeparam>
    /// <param name="cancellationToken">Token used to cancel the subscription.</param>
    /// <returns>An asynchronous stream that yields events as they are published.</returns>
    IAsyncEnumerable<TEvent> SubscribeAsync<TEvent>(CancellationToken cancellationToken = default) where TEvent : notnull;
}
