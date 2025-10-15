using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Eventing;

namespace Aog.Core.Layers.Controllers;

/// <summary>
/// Publishes controller diagnostics derived from runtime snapshots onto the shared event bus.
/// </summary>
public sealed class LayerControllerDiagnosticsPublisher
{
    private readonly LayerControllerRuntime _runtime;
    private readonly IEventBus _eventBus;
    private readonly LayerControllerTileWriter _tileWriter;

    /// <summary>
    /// Initializes a new instance of the <see cref="LayerControllerDiagnosticsPublisher"/> class.
    /// </summary>
    /// <param name="runtime">Runtime producing controller snapshots.</param>
    /// <param name="eventBus">Event bus used to broadcast diagnostics.</param>
    /// <param name="tileWriter">Optional TileStore writer used to persist controller outputs.</param>
    public LayerControllerDiagnosticsPublisher(
        LayerControllerRuntime runtime,
        IEventBus eventBus,
        LayerControllerTileWriter? tileWriter = null)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _tileWriter = tileWriter ?? new LayerControllerTileWriter();
    }

    /// <summary>
    /// Collects due snapshots from the runtime and publishes diagnostics events.
    /// </summary>
    /// <param name="force">When <c>true</c>, forces all controllers to emit regardless of cadence.</param>
    /// <param name="emitHoldFrames">When <c>true</c>, hold frames are emitted when no fresh data is present.</param>
    /// <param name="timestamp">Optional timestamp override applied to snapshot collection.</param>
    /// <param name="cancellationToken">Token used to cancel the publish operation.</param>
    /// <returns>A task that completes when all snapshots have been processed.</returns>
    public async ValueTask PublishSnapshotsAsync(
        bool force = false,
        bool emitHoldFrames = true,
        DateTimeOffset? timestamp = null,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<LayerControllerSnapshot> snapshots = _runtime.CollectDueSnapshots(force, emitHoldFrames, timestamp);
        var snapshotIndex = 0;

        try
        {
            for (; snapshotIndex < snapshots.Count; snapshotIndex++)
            {
                var snapshot = snapshots[snapshotIndex];

                try
                {
                    var diagnosticEvent = LayerControllerDiagnosticEvent.FromSnapshot(snapshot);
                    await _eventBus.PublishAsync(diagnosticEvent, cancellationToken).ConfigureAwait(false);
                    await _tileWriter.WriteAsync(snapshot, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    snapshot.Dispose();
                }
            }
        }
        catch
        {
            DisposeRemainingSnapshots(snapshotIndex + 1);
            throw;
        }

        void DisposeRemainingSnapshots(int startIndex)
        {
            for (var index = startIndex; index < snapshots.Count; index++)
            {
                snapshots[index].Dispose();
            }
        }
    }
}
