using System;

namespace Aog.Core.Layers.Controllers;

/// <summary>
/// Describes a write operation that persists controller aggregates into the TileStore.
/// </summary>
public sealed record LayerTileWriteRequest
{
    /// <summary>
    /// Gets the controller identifier associated with the snapshot.
    /// </summary>
    public required string ControllerId { get; init; }

    /// <summary>
    /// Gets the layer identifier represented by the snapshot.
    /// </summary>
    public required string LayerId { get; init; }

    /// <summary>
    /// Gets the timestamp when the snapshot was produced.
    /// </summary>
    public required DateTimeOffset SnapshotTimestamp { get; init; }

    /// <summary>
    /// Gets the engineering value that should be persisted.
    /// </summary>
    public required double EngineeringValue { get; init; }

    /// <summary>
    /// Gets the normalized value (0–1) associated with the snapshot.
    /// </summary>
    public required double NormalizedValue { get; init; }

    /// <summary>
    /// Gets the accumulated weight (area) used when blending the value into a tile.
    /// </summary>
    public required double Weight { get; init; }

    /// <summary>
    /// Gets the aggregated quality metric (0–1).
    /// </summary>
    public required double Quality { get; init; }

    /// <summary>
    /// Gets a value indicating whether the controller reported a rate-unavailable condition.
    /// </summary>
    public required bool RateUnavailable { get; init; }

    /// <summary>
    /// Gets the minimum engineering value observed in the aggregation window.
    /// </summary>
    public required double MinimumValue { get; init; }

    /// <summary>
    /// Gets the maximum engineering value observed in the aggregation window.
    /// </summary>
    public required double MaximumValue { get; init; }

    /// <summary>
    /// Gets the numerator accumulated during the aggregation window.
    /// </summary>
    public required double Numerator { get; init; }

    /// <summary>
    /// Gets the denominator accumulated during the aggregation window.
    /// </summary>
    public required double Denominator { get; init; }

    /// <summary>
    /// Gets a value indicating whether the snapshot contains fresh data.
    /// </summary>
    public required bool ContainsFreshData { get; init; }

    /// <summary>
    /// Gets the timestamp of the first sample contributing to the snapshot when available.
    /// </summary>
    public DateTimeOffset? FirstSampleTimestamp { get; init; }

    /// <summary>
    /// Gets the timestamp of the last sample contributing to the snapshot when available.
    /// </summary>
    public DateTimeOffset? LastSampleTimestamp { get; init; }

    /// <summary>
    /// Creates a <see cref="LayerTileWriteRequest"/> from the supplied snapshot.
    /// </summary>
    /// <param name="snapshot">Snapshot produced by a controller runtime.</param>
    /// <returns>A populated write request.</returns>
    public static LayerTileWriteRequest FromSnapshot(LayerControllerSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var weight = snapshot.ContainsFreshData ? snapshot.AreaSquareMeters : 0d;

        return new LayerTileWriteRequest
        {
            ControllerId = snapshot.ControllerId,
            LayerId = snapshot.LayerId,
            SnapshotTimestamp = snapshot.SnapshotTimestamp,
            EngineeringValue = snapshot.EngineeringValue,
            NormalizedValue = snapshot.NormalizedValue,
            Weight = weight,
            Quality = snapshot.Quality,
            RateUnavailable = snapshot.RateUnavailable,
            MinimumValue = snapshot.MinimumValue,
            MaximumValue = snapshot.MaximumValue,
            Numerator = snapshot.Numerator,
            Denominator = snapshot.Denominator,
            ContainsFreshData = snapshot.ContainsFreshData,
            FirstSampleTimestamp = snapshot.FirstSampleTimestamp,
            LastSampleTimestamp = snapshot.LastSampleTimestamp
        };
    }
}
