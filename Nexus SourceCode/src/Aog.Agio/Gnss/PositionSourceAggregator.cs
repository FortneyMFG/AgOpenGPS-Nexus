using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.V1;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aog.Agio.Gnss;

/// <summary>
/// Aggregates multiple GNSS providers and applies a failover policy.
/// </summary>
public sealed class PositionSourceAggregator : IPositionSource
{
    private readonly IReadOnlyList<IPositionSourceFactory> _orderedFactories;
    private readonly PositionSourceAggregatorOptions _options;
    private readonly ILogger<PositionSourceAggregator> _logger;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="PositionSourceAggregator"/> class.
    /// </summary>
    public PositionSourceAggregator(
        IEnumerable<IPositionSourceFactory> factories,
        IOptions<PositionSourceAggregatorOptions> options,
        ILogger<PositionSourceAggregator> logger,
        TimeProvider? timeProvider = null)
    {
        if (factories is null)
        {
            throw new ArgumentNullException(nameof(factories));
        }

        _options = (options ?? throw new ArgumentNullException(nameof(options))).Value
            ?? throw new ArgumentException("Options are required.", nameof(options));
        _options.Validate();

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? TimeProvider.System;
        Name = "policy/gnss";

        var preferredOrder = _options.PreferredOrder?.ToList();
        _orderedFactories = BuildFactoryOrder(factories, _options.EnableNetworkFeeds, preferredOrder);
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public async Task<PositionSourceResult> RunAsync(
        Func<Pose, CancellationToken, ValueTask> publish,
        CancellationToken cancellationToken)
    {
        if (publish is null)
        {
            throw new ArgumentNullException(nameof(publish));
        }

        if (_orderedFactories.Count == 0)
        {
            throw new InvalidOperationException("No GNSS position sources are configured for the current policy.");
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            var anyStarted = false;

            foreach (var factory in _orderedFactories)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!_options.EnableNetworkFeeds && IsNetwork(factory.Kind))
                {
                    continue;
                }

                IPositionSource? source;
                try
                {
                    source = await factory.TryCreateAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return PositionSourceResult.Cancelled();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create GNSS position source {Source}.", factory.Name);
                    continue;
                }

                if (source is null)
                {
                    _logger.LogDebug("GNSS position source {Source} is unavailable.", factory.Name);
                    continue;
                }

                anyStarted = true;

                await using var activeSource = source;

                _logger.LogInformation("Activating GNSS position source {Source}.", activeSource.Name);

                PositionSourceResult result;
                try
                {
                    result = await activeSource.RunAsync(publish, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return PositionSourceResult.Cancelled();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "GNSS position source {Source} threw an unhandled exception.", activeSource.Name);
                    result = PositionSourceResult.Faulted(ex);
                }

                switch (result.Outcome)
                {
                    case PositionSourceOutcome.Completed:
                        _logger.LogInformation("GNSS position source {Source} completed. Stopping aggregator.", activeSource.Name);
                        return result;

                    case PositionSourceOutcome.Cancelled:
                        _logger.LogInformation("GNSS position source {Source} cancelled. Stopping aggregator.", activeSource.Name);
                        return result;

                    case PositionSourceOutcome.Faulted:
                        _logger.LogWarning(result.Error, "GNSS position source {Source} faulted. Trying next provider.", activeSource.Name);
                        break;
                }

                try
                {
                    await DelayAsync(_options.SourceFailureBackoff, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return PositionSourceResult.Cancelled();
                }
            }

            if (!anyStarted)
            {
                _logger.LogWarning("No GNSS position sources were available. Retrying after {Delay}.", _options.ExhaustedBackoff);
            }

            try
            {
                await DelayAsync(_options.ExhaustedBackoff, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return PositionSourceResult.Cancelled();
            }
        }

        return PositionSourceResult.Cancelled();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private async ValueTask DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        if (delay <= TimeSpan.Zero)
        {
            return;
        }

        await _timeProvider.Delay(delay, cancellationToken).ConfigureAwait(false);
    }

    private static IReadOnlyList<IPositionSourceFactory> BuildFactoryOrder(
        IEnumerable<IPositionSourceFactory> factories,
        bool includeNetwork,
        IList<PositionSourceKind>? preferred)
    {
        var available = factories.ToList();
        var result = new List<IPositionSourceFactory>(available.Count);
        var added = new HashSet<IPositionSourceFactory>();

        if (preferred is { Count: > 0 })
        {
            foreach (var kind in preferred)
            {
                foreach (var candidate in available.Where(f => f.Kind == kind))
                {
                    if (!includeNetwork && IsNetwork(candidate.Kind))
                    {
                        continue;
                    }

                    if (added.Add(candidate))
                    {
                        result.Add(candidate);
                    }
                }
            }
        }

        foreach (var candidate in available)
        {
            if (!includeNetwork && IsNetwork(candidate.Kind))
            {
                continue;
            }

            if (added.Add(candidate))
            {
                result.Add(candidate);
            }
        }

        return result;
    }

    private static bool IsNetwork(PositionSourceKind kind) => kind is PositionSourceKind.TcpNetwork or PositionSourceKind.UdpNetwork;
}
