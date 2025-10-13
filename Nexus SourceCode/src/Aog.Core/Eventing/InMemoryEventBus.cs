using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aog.Core.Eventing;

/// <summary>
/// In-memory implementation of <see cref="IEventBus"/> that executes handlers sequentially.
/// </summary>
public sealed class InMemoryEventBus : IEventBus
{
    private readonly Dictionary<Type, Subscribers> _subscriptions = new();
    private readonly object _gate = new();

    public IDisposable Subscribe<TEvent>(Func<TEvent, CancellationToken, ValueTask> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var subscription = new Subscription(typeof(TEvent), async (message, token) =>
        {
            if (message is not TEvent typed)
            {
                return;
            }

            await handler(typed, token).ConfigureAwait(false);
        });

        Subscribers subscribers;
        lock (_gate)
        {
            if (!_subscriptions.TryGetValue(subscription.EventType, out subscribers))
            {
                subscribers = new Subscribers();
                _subscriptions[subscription.EventType] = subscribers;
            }
        }

        subscribers.Add(subscription);
        return new SubscriptionHandle(this, subscription);
    }

    public async ValueTask PublishAsync<TEvent>(TEvent message, CancellationToken cancellationToken = default)
    {
        Subscribers? subscribers;
        lock (_gate)
        {
            _subscriptions.TryGetValue(typeof(TEvent), out subscribers);
        }

        if (subscribers is null)
        {
            return;
        }

        var snapshot = subscribers.Snapshot();
        foreach (var subscription in snapshot)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await subscription.Invoke(message, cancellationToken).ConfigureAwait(false);
        }
    }

    private void Remove(Subscription subscription)
    {
        lock (_gate)
        {
            if (_subscriptions.TryGetValue(subscription.EventType, out var subscribers))
            {
                subscribers.Remove(subscription);

                if (subscribers.IsEmpty)
                {
                    _subscriptions.Remove(subscription.EventType);
                }
            }
        }
    }

    private sealed class SubscriptionHandle : IDisposable
    {
        private readonly InMemoryEventBus _owner;
        private Subscription? _subscription;

        public SubscriptionHandle(InMemoryEventBus owner, Subscription subscription)
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

    private sealed class Subscription
    {
        private readonly Func<object?, CancellationToken, ValueTask> _handler;

        public Subscription(Type eventType, Func<object?, CancellationToken, ValueTask> handler)
        {
            EventType = eventType;
            _handler = handler;
        }

        public Type EventType { get; }

        public ValueTask Invoke(object? message, CancellationToken cancellationToken)
            => _handler(message, cancellationToken);

        public Guid Id { get; } = Guid.NewGuid();
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
}
