using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aog.Core.Lifecycle.Seasons;

/// <summary>
/// Source that produces season snapshots discovered from a particular provider.
/// </summary>
public interface ISeasonDiscoverySource
{
    /// <summary>
    /// Identifier of the provider that owns the discovered seasons.
    /// </summary>
    string ProviderId { get; }

    /// <summary>
    /// Streams full season snapshots when discovery detects a change.
    /// </summary>
    IAsyncEnumerable<IReadOnlyList<SeasonDocument>?> WatchAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Coordinates discovery sources and forwards season snapshots to the synchronisation orchestrator.
/// </summary>
public sealed class SeasonDiscoveryWatcher : IAsyncDisposable
{
    private readonly SeasonSyncOrchestrator _orchestrator;
    private readonly IReadOnlyList<ISeasonDiscoverySource> _sources;
    private readonly ILogger<SeasonDiscoveryWatcher>? _logger;
    private readonly List<Task> _runningTasks = new();
    private CancellationTokenSource? _cts;
    private bool _hasStarted;
    private bool _isStopping;

    public SeasonDiscoveryWatcher(
        SeasonSyncOrchestrator orchestrator,
        IEnumerable<ISeasonDiscoverySource> sources,
        ILogger<SeasonDiscoveryWatcher>? logger = null)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        if (sources is null)
        {
            throw new ArgumentNullException(nameof(sources));
        }

        var resolvedSources = sources.ToArray();
        if (resolvedSources.Length == 0)
        {
            _logger = logger;
            _sources = Array.Empty<ISeasonDiscoverySource>();
            return;
        }

        var providerIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var source in resolvedSources)
        {
            if (source is null)
            {
                throw new ArgumentException("Season discovery sources cannot contain null entries.", nameof(sources));
            }

            if (string.IsNullOrWhiteSpace(source.ProviderId))
            {
                throw new ArgumentException("Season discovery sources must provide a provider identifier.", nameof(sources));
            }

            if (!providerIds.Add(source.ProviderId))
            {
                throw new ArgumentException($"Duplicate season discovery provider identifier '{source.ProviderId}'.", nameof(sources));
            }
        }

        _sources = resolvedSources;
        _logger = logger;
    }

    /// <summary>
    /// Starts processing season discovery updates.
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_hasStarted)
        {
            throw new InvalidOperationException("Season discovery watcher has already been started.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        _hasStarted = true;

        if (_sources.Count == 0)
        {
            _logger?.LogWarning("Season discovery watcher started without configured sources.");
            return Task.CompletedTask;
        }

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        foreach (var source in _sources)
        {
            _runningTasks.Add(RunSourceAsync(source, _cts.Token));
        }

        _logger?.LogInformation("Season discovery watcher started for {Count} source(s).", _sources.Count);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops processing discovery updates and waits for active loops to complete.
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_cts is null)
        {
            return;
        }

        if (_isStopping)
        {
            return;
        }

        _isStopping = true;
        _cts.Cancel();

        try
        {
            if (_runningTasks.Count > 0)
            {
                await Task.WhenAll(_runningTasks).WaitAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellation when stopping.
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Season discovery watcher encountered an error while stopping.");
        }
        finally
        {
            _cts.Dispose();
            _cts = null;
            _runningTasks.Clear();
            _isStopping = false;
            _logger?.LogInformation("Season discovery watcher stopped.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
    }

    private async Task RunSourceAsync(ISeasonDiscoverySource source, CancellationToken cancellationToken)
    {
        _logger?.LogInformation("Season discovery watcher running for provider {ProviderId}.", source.ProviderId);

        try
        {
            await foreach (var snapshot in source.WatchAsync(cancellationToken).WithCancellation(cancellationToken))
            {
                if (snapshot is null)
                {
                    _logger?.LogWarning(
                        "Season discovery source {ProviderId} produced a null snapshot. Skipping.",
                        source.ProviderId);
                    continue;
                }

                try
                {
                    await _orchestrator.ApplySnapshotAsync(source.ProviderId, snapshot, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(
                        ex,
                        "Failed to apply season snapshot from provider {ProviderId}.",
                        source.ProviderId);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger?.LogDebug("Season discovery watcher cancelled for provider {ProviderId}.", source.ProviderId);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Season discovery watcher terminated for provider {ProviderId}.", source.ProviderId);
        }
        finally
        {
            _logger?.LogInformation("Season discovery watcher exiting for provider {ProviderId}.", source.ProviderId);
        }
    }
}
