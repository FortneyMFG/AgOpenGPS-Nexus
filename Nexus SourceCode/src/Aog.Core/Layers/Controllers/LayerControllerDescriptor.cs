using System;

namespace Aog.Core.Layers.Controllers;

/// <summary>
/// Describes the static characteristics of a layer controller instance.
/// </summary>
public sealed record LayerControllerDescriptor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LayerControllerDescriptor"/> class.
    /// </summary>
    /// <param name="controllerId">Unique identifier for the controller instance.</param>
    /// <param name="layerId">Registry identifier for the layer managed by the controller.</param>
    /// <param name="snapshotCadence">Desired cadence for publishing immutable snapshots.</param>
    /// <param name="aggregationStrategy">Aggregation strategy used when computing engineering values.</param>
    public LayerControllerDescriptor(
        string controllerId,
        string layerId,
        TimeSpan snapshotCadence,
        LayerAggregationStrategy aggregationStrategy)
    {
        if (string.IsNullOrWhiteSpace(controllerId))
        {
            throw new ArgumentException("Controller identifier is required.", nameof(controllerId));
        }

        if (string.IsNullOrWhiteSpace(layerId))
        {
            throw new ArgumentException("Layer identifier is required.", nameof(layerId));
        }

        if (snapshotCadence < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(snapshotCadence), snapshotCadence, "Snapshot cadence must be non-negative.");
        }

        ControllerId = controllerId;
        LayerId = layerId;
        SnapshotCadence = snapshotCadence;
        AggregationStrategy = aggregationStrategy;
    }

    /// <summary>
    /// Gets the unique identifier for the controller instance.
    /// </summary>
    public string ControllerId { get; }

    /// <summary>
    /// Gets the registry identifier for the layer managed by the controller.
    /// </summary>
    public string LayerId { get; }

    /// <summary>
    /// Gets the desired cadence for publishing immutable snapshots. A zero cadence emits snapshots on every poll.
    /// </summary>
    public TimeSpan SnapshotCadence { get; }

    /// <summary>
    /// Gets the aggregation strategy used when computing engineering values.
    /// </summary>
    public LayerAggregationStrategy AggregationStrategy { get; }
}
