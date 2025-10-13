using System;
using System.Threading;
using System.Threading.Tasks;

namespace Aog.Core.Simulation;

/// <summary>
/// Abstraction for publishing and subscribing to simulation messages.
/// </summary>
public interface ISimBus
{
    /// <summary>
    /// Subscribes to a message topic.
    /// </summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <param name="topic">The topic name.</param>
    /// <param name="handler">The handler invoked for each published message.</param>
    /// <returns>A disposable subscription that stops message delivery when disposed.</returns>
    IDisposable Subscribe<TMessage>(string topic, Func<SimMessage<TMessage>, CancellationToken, ValueTask> handler);

    /// <summary>
    /// Publishes a message to the specified topic.
    /// </summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <param name="topic">The topic name.</param>
    /// <param name="time">The simulation time associated with the message.</param>
    /// <param name="payload">The message payload.</param>
    /// <param name="cancellationToken">A token that cancels the publish operation.</param>
    ValueTask PublishAsync<TMessage>(string topic, SimTime time, TMessage payload, CancellationToken cancellationToken = default);
}
