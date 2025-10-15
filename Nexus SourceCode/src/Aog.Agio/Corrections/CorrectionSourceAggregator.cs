using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aog.Agio.Corrections;

/// <summary>
/// Aggregates multiple correction providers and applies a failover policy.
/// </summary>
public sealed class CorrectionSourceAggregator : ICorrectionSource
{
    private readonly IReadOnlyList<ICorrectionSourceFactory> _orderedFactories;
    private readonly CorrectionSourceAggregatorOptions _options;
    private readonly ILogger<CorrectionSourceAggregator> _logger;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="CorrectionSourceAggregator"/> class.
    /// </summary>
    public CorrectionSourceAggregator(
        IEnumerable<ICorrectionSourceFactory> factories,
        IOptions<CorrectionSourceAggregatorOptions> options,
        ILogger<CorrectionSourceAggregator> logger,
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
        Name = "policy/corrections";

        var preferredOrder = _options.PreferredOrder?.ToList();
        _orderedFactories = BuildFactoryOrder(factories, _options.EnableNetworkSources, preferredOrder);
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public async Task<CorrectionSourceResult> RunAsync(
        Func<ReadOnlyMemory<byte>, CancellationToken, ValueTask> publish,
        CancellationToken cancellationToken)
    {
        if (publish is null)
        {
            throw new ArgumentNullException(nameof(publish));
        }

        if (_orderedFactories.Count == 0)
        {
            throw new InvalidOperationException("No GNSS correction sources are configured for the current policy.");
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            var anyStarted = false;

            foreach (var factory in _orderedFactories)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!_options.EnableNetworkSources && IsNetwork(factory.Kind))
                {
                    continue;
                }

                ICorrectionSource? source;
                try
                {
                    source = await factory.TryCreateAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return CorrectionSourceResult.Cancelled();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create GNSS correction source {Source}.", factory.Name);
                    continue;
                }

                if (source is null)
                {
                    _logger.LogDebug("GNSS correction source {Source} is unavailable.", factory.Name);
                    continue;
                }

                anyStarted = true;

                await using var activeSource = source;

                _logger.LogInformation("Activating GNSS correction source {Source}.", activeSource.Name);

                CorrectionSourceResult result;
                try
                {
                    result = await activeSource.RunAsync(publish, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return CorrectionSourceResult.Cancelled();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "GNSS correction source {Source} threw an unhandled exception.", activeSource.Name);
                    result = CorrectionSourceResult.Faulted(ex);
                }

                switch (result.Outcome)
                {
                    case CorrectionSourceOutcome.Completed:
                        _logger.LogInformation(
                            "GNSS correction source {Source} completed. Trying next provider after backoff.",
                            activeSource.Name);
                        break;

                    case CorrectionSourceOutcome.Cancelled:
                        _logger.LogInformation(
                            "GNSS correction source {Source} cancelled. Trying next provider after backoff.",
                            activeSource.Name);
                        break;

                    case CorrectionSourceOutcome.Faulted:
                        _logger.LogWarning(
                            result.Error,
                            "GNSS correction source {Source} faulted. Trying next provider.",
                            activeSource.Name);
                        break;

                    default:
                        _logger.LogWarning(
                            "GNSS correction source {Source} returned unexpected outcome {Outcome}. Trying next provider.",
                            activeSource.Name,
                            result.Outcome);
                        break;
                }

                try
                {
                    await DelayAsync(_options.SourceFailureBackoff, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return CorrectionSourceResult.Cancelled();
                }
            }

            if (!anyStarted)
            {
                _logger.LogWarning(
                    "No GNSS correction sources were available. Retrying after {Delay}.",
                    _options.ExhaustedBackoff);
            }

            try
            {
                await DelayAsync(_options.ExhaustedBackoff, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return CorrectionSourceResult.Cancelled();
            }
        }

        return CorrectionSourceResult.Cancelled();
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

    private static IReadOnlyList<ICorrectionSourceFactory> BuildFactoryOrder(
        IEnumerable<ICorrectionSourceFactory> factories,
        bool includeNetwork,
        IList<CorrectionSourceKind>? preferred)
    {
        var available = factories.ToList();
        var result = new List<ICorrectionSourceFactory>(available.Count);
        var added = new HashSet<ICorrectionSourceFactory>();

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

    private static bool IsNetwork(CorrectionSourceKind kind) => kind == CorrectionSourceKind.NetworkService;
}
