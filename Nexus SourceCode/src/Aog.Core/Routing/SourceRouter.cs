using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Eventing;

namespace Aog.Core.Routing;

/// <summary>
/// Manages per-topic routing across simulation, replay, and hardware sources.
/// </summary>
public sealed class SourceRouter
{
    private readonly IEventBus _eventBus;
    private readonly Dictionary<string, TopicState> _topics = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _preferredSources;
    private readonly Queue<PendingPublication> _pendingPublications = new();
    private readonly object _gate = new();
    private bool _isPublishing;

    /// <summary>
    /// Initializes a new instance of the <see cref="SourceRouter"/> class.
    /// </summary>
    public SourceRouter(IEventBus eventBus, RoutingOptions? options = null)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _preferredSources = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (options?.PreferredSources is { Count: > 0 })
        {
            foreach (var kvp in options.PreferredSources)
            {
                var topic = NormalizeTopic(kvp.Key, nameof(RoutingOptions.PreferredSources));
                var source = NormalizeSourceId(kvp.Value, nameof(RoutingOptions.PreferredSources));
                _preferredSources[topic] = source;
            }
        }
    }

    /// <summary>
    /// Registers a producer for the specified topic.
    /// </summary>
    public ValueTask<TopicRoute?> RegisterSourceAsync(
        string topic,
        string sourceId,
        SourceKind kind,
        int priorityOffset = 0,
        CancellationToken cancellationToken = default)
    {
        var normalizedTopic = NormalizeTopic(topic, nameof(topic));
        var normalizedSource = NormalizeSourceId(sourceId, nameof(sourceId));

        ValueTask<TopicRoute?> publication;
        var startPublisher = false;

        lock (_gate)
        {
            var state = GetOrCreateState(normalizedTopic);
            var previous = state.CurrentRoute;
            state.Sources[normalizedSource] = new TopicSourceRegistration(normalizedSource, kind, priorityOffset);
            state.CurrentRoute = ComputeRoute(state);

            publication = EnqueuePublishIfChangedLocked(
                normalizedTopic,
                previous,
                state.CurrentRoute,
                cancellationToken,
                out startPublisher);
        }

        if (startPublisher)
        {
            _ = ProcessPublicationQueueAsync();
        }

        return publication;
    }

    /// <summary>
    /// Unregisters a producer from the specified topic.
    /// </summary>
    public ValueTask<TopicRoute?> UnregisterSourceAsync(
        string topic,
        string sourceId,
        CancellationToken cancellationToken = default)
    {
        var normalizedTopic = NormalizeTopic(topic, nameof(topic));
        var normalizedSource = NormalizeSourceId(sourceId, nameof(sourceId));

        ValueTask<TopicRoute?> publication;
        var startPublisher = false;

        lock (_gate)
        {
            if (!_topics.TryGetValue(normalizedTopic, out var state))
            {
                return ValueTask.FromResult<TopicRoute?>(null);
            }

            var previous = state.CurrentRoute;
            if (!state.Sources.Remove(normalizedSource))
            {
                return ValueTask.FromResult(previous);
            }

            state.CurrentRoute = ComputeRoute(state);
            var current = state.CurrentRoute;

            publication = EnqueuePublishIfChangedLocked(
                normalizedTopic,
                previous,
                current,
                cancellationToken,
                out startPublisher);

            if (state.Sources.Count == 0 && state.OverrideSourceId is null && !_preferredSources.ContainsKey(normalizedTopic))
            {
                _topics.Remove(normalizedTopic);
            }
        }

        if (startPublisher)
        {
            _ = ProcessPublicationQueueAsync();
        }

        return publication;
    }

    /// <summary>
    /// Overrides the preferred source for the topic until cleared.
    /// </summary>
    public ValueTask<TopicRoute?> SetPreferredSourceAsync(
        string topic,
        string? sourceId,
        CancellationToken cancellationToken = default)
    {
        var normalizedTopic = NormalizeTopic(topic, nameof(topic));
        var normalizedSource = string.IsNullOrWhiteSpace(sourceId)
            ? null
            : NormalizeSourceId(sourceId!, nameof(sourceId));

        ValueTask<TopicRoute?> publication;
        var startPublisher = false;

        lock (_gate)
        {
            var state = GetOrCreateState(normalizedTopic);
            var previous = state.CurrentRoute;
            state.OverrideSourceId = normalizedSource;
            state.CurrentRoute = ComputeRoute(state);
            var current = state.CurrentRoute;

            publication = EnqueuePublishIfChangedLocked(
                normalizedTopic,
                previous,
                current,
                cancellationToken,
                out startPublisher);

            if (state.Sources.Count == 0 && state.OverrideSourceId is null && !_preferredSources.ContainsKey(normalizedTopic))
            {
                _topics.Remove(normalizedTopic);
            }
        }

        if (startPublisher)
        {
            _ = ProcessPublicationQueueAsync();
        }

        return publication;
    }

    /// <summary>
    /// Gets the currently active route for the topic, if any.
    /// </summary>
    public TopicRoute? GetCurrentRoute(string topic)
    {
        var normalizedTopic = NormalizeTopic(topic, nameof(topic));

        lock (_gate)
        {
            return _topics.TryGetValue(normalizedTopic, out var state)
                ? state.CurrentRoute
                : null;
        }
    }

    private ValueTask<TopicRoute?> EnqueuePublishIfChangedLocked(
        string topic,
        TopicRoute? previous,
        TopicRoute? current,
        CancellationToken cancellationToken,
        out bool startPublisher)
    {
        if (Equals(previous, current))
        {
            startPublisher = false;
            return ValueTask.FromResult(current);
        }

        var completion = new TaskCompletionSource<TopicRoute?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingPublications.Enqueue(new PendingPublication(
            new TopicRouteChanged(topic, previous, current),
            cancellationToken,
            completion,
            current));

        startPublisher = !_isPublishing;
        if (startPublisher)
        {
            _isPublishing = true;
        }

        return new ValueTask<TopicRoute?>(completion.Task);
    }

    private async Task ProcessPublicationQueueAsync()
    {
        while (true)
        {
            PendingPublication pending;

            lock (_gate)
            {
                if (_pendingPublications.Count == 0)
                {
                    _isPublishing = false;
                    return;
                }

                pending = _pendingPublications.Dequeue();
            }

            try
            {
                await _eventBus.PublishAsync(pending.Change, pending.CancellationToken)
                    .ConfigureAwait(false);
                pending.Completion.TrySetResult(pending.Result);
            }
            catch (OperationCanceledException ex)
            {
                if (pending.CancellationToken.IsCancellationRequested)
                {
                    pending.Completion.TrySetCanceled(pending.CancellationToken);
                }
                else if (ex.CancellationToken.CanBeCanceled)
                {
                    pending.Completion.TrySetCanceled(ex.CancellationToken);
                }
                else
                {
                    pending.Completion.TrySetCanceled();
                }
            }
            catch (Exception ex)
            {
                pending.Completion.TrySetException(ex);
            }
        }
    }

    private TopicState GetOrCreateState(string topic)
    {
        if (!_topics.TryGetValue(topic, out var state))
        {
            state = new TopicState(topic);
            _topics[topic] = state;
        }

        return state;
    }

    private TopicRoute? ComputeRoute(TopicState state)
    {
        if (state.Sources.Count == 0)
        {
            return null;
        }

        var preferredId = ResolvePreferredSourceId(state);
        if (preferredId is not null && state.Sources.TryGetValue(preferredId, out var preferred))
        {
            return new TopicRoute(state.Topic, preferred.SourceId, preferred.Kind);
        }

        TopicSourceRegistration? best = null;
        foreach (var candidate in state.Sources.Values)
        {
            if (best is null)
            {
                best = candidate;
                continue;
            }

            if (candidate.Priority > best.Priority)
            {
                best = candidate;
                continue;
            }

            if (candidate.Priority == best.Priority &&
                string.Compare(candidate.SourceId, best.SourceId, StringComparison.OrdinalIgnoreCase) < 0)
            {
                best = candidate;
            }
        }

        return best is null ? null : new TopicRoute(state.Topic, best.SourceId, best.Kind);
    }

    private string? ResolvePreferredSourceId(TopicState state)
    {
        if (state.OverrideSourceId is not null && state.Sources.ContainsKey(state.OverrideSourceId))
        {
            return state.OverrideSourceId;
        }

        if (_preferredSources.TryGetValue(state.Topic, out var configured) && state.Sources.ContainsKey(configured))
        {
            return configured;
        }

        return null;
    }

    private static string NormalizeTopic(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Topic identifier is required.", parameterName);
        }

        return value.Trim();
    }

    private static string NormalizeSourceId(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Source identifier is required.", parameterName);
        }

        return value.Trim();
    }

    private sealed class PendingPublication
    {
        public PendingPublication(
            TopicRouteChanged change,
            CancellationToken cancellationToken,
            TaskCompletionSource<TopicRoute?> completion,
            TopicRoute? result)
        {
            Change = change;
            CancellationToken = cancellationToken;
            Completion = completion;
            Result = result;
        }

        public TopicRouteChanged Change { get; }

        public CancellationToken CancellationToken { get; }

        public TaskCompletionSource<TopicRoute?> Completion { get; }

        public TopicRoute? Result { get; }
    }

    private sealed class TopicState
    {
        public TopicState(string topic)
        {
            Topic = topic;
        }

        public string Topic { get; }

        public Dictionary<string, TopicSourceRegistration> Sources { get; } =
            new(StringComparer.OrdinalIgnoreCase);

        public string? OverrideSourceId { get; set; }

        public TopicRoute? CurrentRoute { get; set; }
    }

    private sealed class TopicSourceRegistration
    {
        public TopicSourceRegistration(string sourceId, SourceKind kind, int priorityOffset)
        {
            SourceId = sourceId;
            Kind = kind;
            PriorityOffset = priorityOffset;
        }

        public string SourceId { get; }

        public SourceKind Kind { get; }

        public int PriorityOffset { get; }

        public int Priority => ((int)Kind * 1000) + PriorityOffset;
    }
}
