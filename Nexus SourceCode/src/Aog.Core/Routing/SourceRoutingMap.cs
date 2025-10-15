using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Eventing;

namespace Aog.Core.Routing;

/// <summary>
/// Maintains the runtime routing map and publishes change events.
/// </summary>
public sealed class SourceRoutingMap
{
    private readonly IEventBus _eventBus;
    private readonly Dictionary<string, StreamRoute> _routes = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _gate = new();

    public SourceRoutingMap(IEventBus eventBus)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
    }

    /// <summary>
    /// Returns a snapshot of the currently configured routes.
    /// </summary>
    public IReadOnlyCollection<StreamRoute> Routes
    {
        get
        {
            lock (_gate)
            {
                return _routes.Values.ToArray();
            }
        }
    }

    /// <summary>
    /// Attempts to retrieve the configured route for the specified stream.
    /// </summary>
    public bool TryGetRoute(string stream, [MaybeNullWhen(false)] out StreamRoute route)
    {
        if (string.IsNullOrWhiteSpace(stream))
        {
            throw new ArgumentException("Stream identifier is required.", nameof(stream));
        }

        lock (_gate)
        {
            return _routes.TryGetValue(stream, out route!);
        }
    }

    /// <summary>
    /// Applies a full routing snapshot, replacing any existing entries and emitting
    /// <see cref="StreamRouteChangedEvent"/> notifications for differences.
    /// </summary>
    public async Task ApplyAsync(IEnumerable<StreamRoute> routes, CancellationToken cancellationToken = default)
    {
        if (routes is null)
        {
            throw new ArgumentNullException(nameof(routes));
        }

        var next = BuildLookup(routes);
        List<StreamRouteChangedEvent> changes;

        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            var previous = new Dictionary<string, StreamRoute>(_routes, StringComparer.OrdinalIgnoreCase);
            changes = CalculateChanges(previous, next);

            _routes.Clear();
            foreach (var kvp in next)
            {
                _routes.Add(kvp.Key, kvp.Value);
            }
        }

        if (changes.Count != 0)
        {
            await PublishChangesAsync(changes, CancellationToken.None).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Applies a single route, updating or creating the entry as needed.
    /// </summary>
    public async Task SetRouteAsync(StreamRoute route, CancellationToken cancellationToken = default)
    {
        if (route is null)
        {
            throw new ArgumentNullException(nameof(route));
        }

        StreamRoute? previous;
        var changed = false;

        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            if (!_routes.TryGetValue(route.Stream, out previous) || previous != route)
            {
                _routes[route.Stream] = route;
                changed = true;
            }
        }

        if (changed)
        {
            await _eventBus.PublishAsync(new StreamRouteChangedEvent(route.Stream, previous, route), CancellationToken.None)
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Removes a route for the specified stream if present.
    /// </summary>
    public async Task RemoveRouteAsync(string stream, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(stream))
        {
            throw new ArgumentException("Stream identifier is required.", nameof(stream));
        }

        StreamRoute? previous = null;
        var removed = false;

        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            if (_routes.TryGetValue(stream, out previous))
            {
                _routes.Remove(stream);
                removed = true;
            }
        }

        if (removed && previous is not null)
        {
            await _eventBus.PublishAsync(new StreamRouteChangedEvent(stream, previous, null), CancellationToken.None)
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Applies routes defined via <see cref="SourceRoutingOptions"/>.
    /// </summary>
    public Task ApplyAsync(SourceRoutingOptions options, CancellationToken cancellationToken = default)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        return ApplyAsync(options.ToRoutes(), cancellationToken);
    }

    private static Dictionary<string, StreamRoute> BuildLookup(IEnumerable<StreamRoute> routes)
    {
        var lookup = new Dictionary<string, StreamRoute>(StringComparer.OrdinalIgnoreCase);
        foreach (var route in routes)
        {
            if (route is null)
            {
                continue;
            }

            if (!lookup.TryAdd(route.Stream, route))
            {
                throw new InvalidOperationException($"Duplicate route configured for stream '{route.Stream}'.");
            }
        }

        return lookup;
    }

    private static List<StreamRouteChangedEvent> CalculateChanges(
        IReadOnlyDictionary<string, StreamRoute> previous,
        IReadOnlyDictionary<string, StreamRoute> next)
    {
        var changes = new List<StreamRouteChangedEvent>();

        foreach (var kvp in next)
        {
            previous.TryGetValue(kvp.Key, out var existing);
            if (!EqualityComparer<StreamRoute?>.Default.Equals(existing, kvp.Value))
            {
                changes.Add(new StreamRouteChangedEvent(kvp.Key, existing, kvp.Value));
            }
        }

        foreach (var kvp in previous)
        {
            if (!next.ContainsKey(kvp.Key))
            {
                changes.Add(new StreamRouteChangedEvent(kvp.Key, kvp.Value, null));
            }
        }

        return changes;
    }

    private async Task PublishChangesAsync(IEnumerable<StreamRouteChangedEvent> changes, CancellationToken cancellationToken)
    {
        foreach (var change in changes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _eventBus.PublishAsync(change, cancellationToken).ConfigureAwait(false);
        }
    }
}
