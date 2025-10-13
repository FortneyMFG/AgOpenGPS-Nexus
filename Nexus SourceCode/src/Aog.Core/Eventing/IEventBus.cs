using System.Threading;
using System.Threading.Tasks;

namespace Aog.Core.Eventing;

/// <summary>
/// Provides an abstraction for in-process event publication and subscription.
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Subscribes the specified asynchronous handler to events of type <typeparamref name="TEvent"/>.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event to subscribe to.</typeparam>
    /// <param name="handler">The handler that will be invoked for each published event.</param>
    /// <returns>
    /// A disposable subscription that can be disposed to stop receiving further events.
    /// </returns>
    IDisposable Subscribe<TEvent>(Func<TEvent, CancellationToken, ValueTask> handler);

    /// <summary>
    /// Publishes the specified event to all registered handlers.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event being published.</typeparam>
    /// <param name="message">The event message.</param>
    /// <param name="cancellationToken">A token used to cancel the publish operation.</param>
    ValueTask PublishAsync<TEvent>(TEvent message, CancellationToken cancellationToken = default);
}
