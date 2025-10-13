using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aog.Core.Simulation;

/// <summary>
/// In-memory implementation of <see cref="ISimBus"/> that delivers messages sequentially per topic.
/// </summary>
public sealed class InMemorySimBus : ISimBus
{
    private readonly Dictionary<string, TopicSubscriptions> _topics = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _gate = new();

    /// <inheritdoc />
    public IDisposable Subscribe<TMessage>(string topic, Func<SimMessage<TMessage>, CancellationToken, ValueTask> handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentNullException.ThrowIfNull(handler);

        var subscription = new Subscription(typeof(TMessage), topic, async (payload, time, token) =>
        {
            if (payload is not TMessage typed)
            {
                return;
            }

            var message = new SimMessage<TMessage>(topic, time, typed);
            await handler(message, token).ConfigureAwait(false);
        });

        lock (_gate)
        {
            if (!_topics.TryGetValue(topic, out var topicSubscriptions))
            {
                topicSubscriptions = new TopicSubscriptions();
                _topics[topic] = topicSubscriptions;
            }

            topicSubscriptions.Add(subscription);
        }

        return new SubscriptionHandle(this, subscription);
    }

    /// <inheritdoc />
    public async ValueTask PublishAsync<TMessage>(string topic, SimTime time, TMessage payload, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);

        TopicSubscriptions? topicSubscriptions;
        lock (_gate)
        {
            _topics.TryGetValue(topic, out topicSubscriptions);
        }

        if (topicSubscriptions is null)
        {
            return;
        }

        var subscribers = topicSubscriptions.GetSubscribers(typeof(TMessage));
        if (subscribers is null)
        {
            return;
        }

        var snapshot = subscribers.Snapshot();
        foreach (var subscription in snapshot)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await subscription.Invoke(payload, time, cancellationToken).ConfigureAwait(false);
        }
    }

    private void Remove(Subscription subscription)
    {
        lock (_gate)
        {
            if (!_topics.TryGetValue(subscription.Topic, out var topicSubscriptions))
            {
                return;
            }

            topicSubscriptions.Remove(subscription);

            if (topicSubscriptions.IsEmpty)
            {
                _topics.Remove(subscription.Topic);
            }
        }
    }

    private sealed class SubscriptionHandle : IDisposable
    {
        private readonly InMemorySimBus _owner;
        private Subscription? _subscription;

        public SubscriptionHandle(InMemorySimBus owner, Subscription subscription)
        {
            _owner = owner;
            _subscription = subscription;
        }

        public void Dispose()
        {
            var subscription = Interlocked.Exchange(ref _subscription, null);
            if (subscription is not null)
            {
                _owner.Remove(subscription);
            }
        }
    }

    private sealed class TopicSubscriptions
    {
        private readonly Dictionary<Type, Subscribers> _typedSubscribers = new();
        private readonly object _gate = new();

        public bool IsEmpty
        {
            get
            {
                lock (_gate)
                {
                    foreach (var subscribers in _typedSubscribers.Values)
                    {
                        if (!subscribers.IsEmpty)
                        {
                            return false;
                        }
                    }
                }

                return true;
            }
        }

        public void Add(Subscription subscription)
        {
            lock (_gate)
            {
                if (!_typedSubscribers.TryGetValue(subscription.MessageType, out var subscribers))
                {
                    subscribers = new Subscribers();
                    _typedSubscribers[subscription.MessageType] = subscribers;
                }

                subscribers.Add(subscription);
            }
        }

        public void Remove(Subscription subscription)
        {
            lock (_gate)
            {
                if (_typedSubscribers.TryGetValue(subscription.MessageType, out var subscribers))
                {
                    subscribers.Remove(subscription);
                    if (subscribers.IsEmpty)
                    {
                        _typedSubscribers.Remove(subscription.MessageType);
                    }
                }
            }
        }

        public Subscribers? GetSubscribers(Type messageType)
        {
            lock (_gate)
            {
                _typedSubscribers.TryGetValue(messageType, out var subscribers);
                return subscribers;
            }
        }
    }

    private sealed class Subscribers
    {
        private readonly object _gate = new();
        private readonly List<Subscription> _items = new();

        public bool IsEmpty
        {
            get
            {
                lock (_gate)
                {
                    return _items.Count == 0;
                }
            }
        }

        public void Add(Subscription subscription)
        {
            lock (_gate)
            {
                _items.Add(subscription);
            }
        }

        public void Remove(Subscription subscription)
        {
            lock (_gate)
            {
                _items.RemoveAll(item => item.Id == subscription.Id);
            }
        }

        public IReadOnlyList<Subscription> Snapshot()
        {
            lock (_gate)
            {
                return _items.ToArray();
            }
        }
    }

    private sealed class Subscription
    {
        private readonly Func<object?, SimTime, CancellationToken, ValueTask> _handler;

        public Subscription(Type messageType, string topic, Func<object?, SimTime, CancellationToken, ValueTask> handler)
        {
            MessageType = messageType;
            Topic = topic;
            _handler = handler;
        }

        public Guid Id { get; } = Guid.NewGuid();

        public Type MessageType { get; }

        public string Topic { get; }

        public ValueTask Invoke(object? payload, SimTime time, CancellationToken cancellationToken)
            => _handler(payload, time, cancellationToken);
    }
}
