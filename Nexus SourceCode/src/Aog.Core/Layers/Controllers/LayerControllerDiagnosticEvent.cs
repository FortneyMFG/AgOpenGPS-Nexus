using System;

namespace Aog.Core.Layers.Controllers;

/// <summary>
/// Immutable diagnostics payload emitted whenever a layer controller produces a snapshot.
/// </summary>
public sealed record LayerControllerDiagnosticEvent
{
    /// <summary>
    /// Gets the identifier of the controller that produced the snapshot.
    /// </summary>
    public required string ControllerId { get; init; }

    /// <summary>
    /// Gets the logical layer identifier associated with the controller.
    /// </summary>
    public required string LayerId { get; init; }

    /// <summary>
    /// Gets the timestamp when the snapshot was recorded.
    /// </summary>
    public required DateTimeOffset SnapshotTimestamp { get; init; }

    /// <summary>
    /// Gets a value indicating whether the snapshot contains fresh data since the previous emission.
    /// </summary>
    public required bool ContainsFreshData { get; init; }

    /// <summary>
    /// Gets the normalized value (0–1) reported by the controller.
    /// </summary>
    public required double NormalizedValue { get; init; }

    /// <summary>
    /// Gets the engineering value reported by the controller.
    /// </summary>
    public required double EngineeringValue { get; init; }

    /// <summary>
    /// Gets the aggregated quality metric (0–1).
    /// </summary>
    public required double Quality { get; init; }

    /// <summary>
    /// Gets a value indicating whether the controller surfaced a rate-unavailable state.
    /// </summary>
    public required bool RateUnavailable { get; init; }

    /// <summary>
    /// Gets the effective area represented by the snapshot in square metres.
    /// </summary>
    public required double AreaSquareMeters { get; init; }

    /// <summary>
    /// Gets the numerator accumulated during the aggregation window.
    /// </summary>
    public required double Numerator { get; init; }

    /// <summary>
    /// Gets the denominator accumulated during the aggregation window.
    /// </summary>
    public required double Denominator { get; init; }

    /// <summary>
    /// Gets the minimum engineering value observed in the aggregation window.
    /// </summary>
    public required double MinimumValue { get; init; }

    /// <summary>
    /// Gets the maximum engineering value observed in the aggregation window.
    /// </summary>
    public required double MaximumValue { get; init; }

    /// <summary>
    /// Gets the number of samples aggregated in the snapshot window.
    /// </summary>
    public required int SampleCount { get; init; }

    /// <summary>
    /// Gets the timestamp of the first sample in the aggregation window when available.
    /// </summary>
    public DateTimeOffset? FirstSampleTimestamp { get; init; }

    /// <summary>
    /// Gets the timestamp of the last sample in the aggregation window when available.
    /// </summary>
    public DateTimeOffset? LastSampleTimestamp { get; init; }

    /// <summary>
    /// Creates a diagnostics event from a layer controller snapshot.
    /// </summary>
    /// <param name="snapshot">Snapshot produced by the controller runtime.</param>
    /// <returns>A populated diagnostics event.</returns>
    public static LayerControllerDiagnosticEvent FromSnapshot(LayerControllerSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return new LayerControllerDiagnosticEvent
        {
            ControllerId = snapshot.ControllerId,
            LayerId = snapshot.LayerId,
            SnapshotTimestamp = snapshot.SnapshotTimestamp,
            ContainsFreshData = snapshot.ContainsFreshData,
            NormalizedValue = snapshot.NormalizedValue,
            EngineeringValue = snapshot.EngineeringValue,
            Quality = snapshot.Quality,
            RateUnavailable = snapshot.RateUnavailable,
            AreaSquareMeters = snapshot.AreaSquareMeters,
            Numerator = snapshot.Numerator,
            Denominator = snapshot.Denominator,
            MinimumValue = snapshot.MinimumValue,
            MaximumValue = snapshot.MaximumValue,
            SampleCount = snapshot.SampleCount,
            FirstSampleTimestamp = snapshot.FirstSampleTimestamp,
            LastSampleTimestamp = snapshot.LastSampleTimestamp
        };
    }
}
