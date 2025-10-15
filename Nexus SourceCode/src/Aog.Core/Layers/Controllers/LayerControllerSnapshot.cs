using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Aog.Core.Paths;

namespace Aog.Core.Layers.Controllers;

/// <summary>
/// Immutable snapshot emitted by a layer controller.
/// </summary>
public sealed class LayerControllerSnapshot : IDisposable
{
    private readonly IDisposable? _positionsOwner;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="LayerControllerSnapshot"/> class.
    /// </summary>
    public LayerControllerSnapshot(
        string controllerId,
        string layerId,
        DateTimeOffset snapshotTimestamp,
        bool containsFreshData,
        double normalizedValue,
        double engineeringValue,
        double quality,
        bool rateUnavailable,
        double areaSquareMeters,
        double numerator,
        double denominator,
        double minimumValue,
        double maximumValue,
        int sampleCount,
        DateTimeOffset? firstSampleTimestamp,
        DateTimeOffset? lastSampleTimestamp,
        IReadOnlyList<PlanarPoint> positions,
        IDisposable? positionsOwner = null)
    {
        ControllerId = controllerId ?? throw new ArgumentNullException(nameof(controllerId));
        LayerId = layerId ?? throw new ArgumentNullException(nameof(layerId));
        SnapshotTimestamp = snapshotTimestamp;
        ContainsFreshData = containsFreshData;
        NormalizedValue = normalizedValue;
        EngineeringValue = engineeringValue;
        Quality = quality;
        RateUnavailable = rateUnavailable;
        AreaSquareMeters = areaSquareMeters;
        Numerator = numerator;
        Denominator = denominator;
        MinimumValue = minimumValue;
        MaximumValue = maximumValue;
        SampleCount = sampleCount;
        FirstSampleTimestamp = firstSampleTimestamp;
        LastSampleTimestamp = lastSampleTimestamp;
        _positionsOwner = positionsOwner;
        Positions = NormalizePositions(positions, positionsOwner);
    }

    /// <summary>
    /// Gets the controller identifier.
    /// </summary>
    public string ControllerId { get; }

    /// <summary>
    /// Gets the layer identifier associated with the controller.
    /// </summary>
    public string LayerId { get; }

    /// <summary>
    /// Gets the timestamp when the snapshot was produced.
    /// </summary>
    public DateTimeOffset SnapshotTimestamp { get; }

    /// <summary>
    /// Gets a value indicating whether the snapshot contains fresh data since the previous emission.
    /// </summary>
    public bool ContainsFreshData { get; }

    /// <summary>
    /// Gets the normalized controller value.
    /// </summary>
    public double NormalizedValue { get; }

    /// <summary>
    /// Gets the engineering controller value.
    /// </summary>
    public double EngineeringValue { get; }

    /// <summary>
    /// Gets the aggregated quality metric.
    /// </summary>
    public double Quality { get; }

    /// <summary>
    /// Gets a value indicating whether the controller is reporting a rate-unavailable condition.
    /// </summary>
    public bool RateUnavailable { get; }

    /// <summary>
    /// Gets the total area represented by the snapshot.
    /// </summary>
    public double AreaSquareMeters { get; }

    /// <summary>
    /// Gets the accumulated numerator used for derived ratios.
    /// </summary>
    public double Numerator { get; }

    /// <summary>
    /// Gets the accumulated denominator used for derived ratios.
    /// </summary>
    public double Denominator { get; }

    /// <summary>
    /// Gets the minimum engineering value observed in the snapshot window.
    /// </summary>
    public double MinimumValue { get; }

    /// <summary>
    /// Gets the maximum engineering value observed in the snapshot window.
    /// </summary>
    public double MaximumValue { get; }

    /// <summary>
    /// Gets the number of samples captured in the snapshot window.
    /// </summary>
    public int SampleCount { get; }

    /// <summary>
    /// Gets the timestamp of the first sample in the snapshot window when available.
    /// </summary>
    public DateTimeOffset? FirstSampleTimestamp { get; }

    /// <summary>
    /// Gets the timestamp of the last sample in the snapshot window when available.
    /// </summary>
    public DateTimeOffset? LastSampleTimestamp { get; }

    /// <summary>
    /// Gets the recorded position history for the snapshot window.
    /// </summary>
    public IReadOnlyList<PlanarPoint> Positions { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _positionsOwner?.Dispose();
        _disposed = true;
    }

    private static IReadOnlyList<PlanarPoint> NormalizePositions(
        IReadOnlyList<PlanarPoint> positions,
        IDisposable? owner)
    {
        if (positions is null)
        {
            throw new ArgumentNullException(nameof(positions));
        }

        if (owner is not null)
        {
            return positions;
        }

        if (positions is ReadOnlyCollection<PlanarPoint> readOnly)
        {
            return readOnly;
        }

        if (positions.Count == 0)
        {
            return Array.Empty<PlanarPoint>();
        }

        return new ReadOnlyCollection<PlanarPoint>(new List<PlanarPoint>(positions));
    }
}
