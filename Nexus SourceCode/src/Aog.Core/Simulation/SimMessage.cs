using System;

namespace Aog.Core.Simulation;

/// <summary>
/// Represents a message flowing through the simulation bus.
/// </summary>
/// <typeparam name="TMessage">The message payload type.</typeparam>
public readonly record struct SimMessage<TMessage>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SimMessage{TMessage}"/> struct.
    /// </summary>
    /// <param name="topic">The message topic.</param>
    /// <param name="time">The simulation time associated with the message.</param>
    /// <param name="payload">The payload.</param>
    public SimMessage(string topic, SimTime time, TMessage payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        Topic = topic;
        Time = time;
        Payload = payload;
    }

    /// <summary>
    /// Gets the message topic.
    /// </summary>
    public string Topic { get; }

    /// <summary>
    /// Gets the simulation time associated with the message.
    /// </summary>
    public SimTime Time { get; }

    /// <summary>
    /// Gets the payload.
    /// </summary>
    public TMessage Payload { get; }
}
