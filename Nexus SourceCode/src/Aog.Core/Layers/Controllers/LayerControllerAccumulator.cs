using System;
using System.Collections.Generic;
using Aog.Core.Paths;

namespace Aog.Core.Layers.Controllers;

/// <summary>
/// Maintains rolling state for a layer controller between snapshot emissions.
/// </summary>
internal sealed class LayerControllerAccumulator
{
    private readonly List<PlanarPoint> _positions = new();

    private double _normalizedSum;
    private double _engineeringWeightedSum;
    private double _engineeringContributionSum;
    private double _qualitySum;
    private double _areaSum;
    private double _numeratorSum;
    private double _denominatorSum;
    private double _minimumValue = double.PositiveInfinity;
    private double _maximumValue = double.NegativeInfinity;

    private int _sampleCount;
    private bool _rateUnavailable;
    private bool _hasEverReceivedSample;

    private DateTimeOffset? _windowFirstSample;
    private DateTimeOffset? _windowLastSample;

    private double? _lastNormalized;
    private double? _lastEngineering;
    private double? _lastQuality;
    private bool? _lastRateUnavailable;

    private PlanarPoint? _lastPosition;

    private double? _lastSnapshotNormalized;
    private double? _lastSnapshotEngineering;
    private double? _lastSnapshotQuality;
    private bool? _lastSnapshotRateUnavailable;
    private double? _lastSnapshotMinimum;
    private double? _lastSnapshotMaximum;
    private double? _lastSnapshotArea;
    private double? _lastSnapshotNumerator;
    private double? _lastSnapshotDenominator;
    private int? _lastSnapshotSampleCount;
    private DateTimeOffset? _lastSnapshotFirstSample;
    private DateTimeOffset? _lastSnapshotLastSample;
    private PlanarPoint[]? _lastSnapshotPositions;

    public bool HasSamples => _sampleCount > 0 || _areaSum > 0d || _positions.Count > 0;

    public bool HasEverReceivedSample => _hasEverReceivedSample;

    public void AddSample(LayerControllerSample sample)
    {
        _hasEverReceivedSample = true;
        _lastNormalized = sample.NormalizedValue;
        _lastEngineering = sample.EngineeringValue;
        _lastQuality = sample.Quality;
        _lastRateUnavailable = sample.RateUnavailable;
        _lastPosition = sample.Position;

        if (_sampleCount == 0)
        {
            _windowFirstSample = sample.Timestamp;
        }

        _windowLastSample = sample.Timestamp;
        _sampleCount++;

        _positions.Add(sample.Position);

        var area = sample.AreaSquareMeters;
        _areaSum += area;
        _normalizedSum += sample.NormalizedValue * area;
        _qualitySum += sample.Quality * area;

        var engineeringContribution = sample.EngineeringContribution ?? (sample.EngineeringValue * area);
        _engineeringContributionSum += engineeringContribution;
        _engineeringWeightedSum += sample.EngineeringValue * area;

        var numeratorContribution = sample.NumeratorContribution ?? (sample.NormalizedValue * area);
        var denominatorContribution = sample.DenominatorContribution ?? area;
        _numeratorSum += numeratorContribution;
        _denominatorSum += denominatorContribution;

        if (sample.EngineeringValue < _minimumValue)
        {
            _minimumValue = sample.EngineeringValue;
        }

        if (sample.EngineeringValue > _maximumValue)
        {
            _maximumValue = sample.EngineeringValue;
        }

        if (sample.RateUnavailable)
        {
            _rateUnavailable = true;
        }
    }

    public LayerControllerSnapshot CreateSnapshot(
        string controllerId,
        string layerId,
        DateTimeOffset snapshotTimestamp,
        LayerAggregationStrategy aggregationStrategy,
        bool includeHoldValues,
        bool resetAfterEmission)
    {
        if (!_hasEverReceivedSample)
        {
            throw new InvalidOperationException("No samples have been ingested for this controller.");
        }

        if (!includeHoldValues && !HasSamples)
        {
            throw new InvalidOperationException("No samples recorded in the current window.");
        }

        var containsFreshData = HasSamples;
        var area = containsFreshData ? _areaSum : (_lastSnapshotArea ?? 0d);

        var normalized = containsFreshData
            ? (area > 0d ? _normalizedSum / area : _lastSnapshotNormalized ?? _lastNormalized ?? 0d)
            : (_lastSnapshotNormalized ?? _lastNormalized ?? 0d);

        double engineering;
        if (containsFreshData)
        {
            engineering = aggregationStrategy switch
            {
                LayerAggregationStrategy.Sum => _engineeringContributionSum,
                LayerAggregationStrategy.Hold when _lastEngineering.HasValue => _lastEngineering.Value,
                LayerAggregationStrategy.Hold => throw new InvalidOperationException("Hold strategy requires at least one observed sample."),
                _ when area > 0d => _engineeringWeightedSum / area,
                _ when _lastSnapshotEngineering.HasValue => _lastSnapshotEngineering.Value,
                _ when includeHoldValues && _lastEngineering.HasValue => _lastEngineering.Value,
                _ => throw new InvalidOperationException("No engineering samples recorded in the current window."),
            };
        }
        else if (_lastSnapshotEngineering.HasValue)
        {
            engineering = _lastSnapshotEngineering.Value;
        }
        else if (includeHoldValues && _lastEngineering.HasValue)
        {
            engineering = _lastEngineering.Value;
        }
        else
        {
            throw new InvalidOperationException("No engineering samples recorded in the current window.");
        }

        var quality = containsFreshData
            ? (area > 0d
                ? Math.Clamp(_qualitySum / area, 0d, 1d)
                : Math.Clamp(_lastSnapshotQuality ?? _lastQuality ?? 0d, 0d, 1d))
            : Math.Clamp(_lastSnapshotQuality ?? _lastQuality ?? 0d, 0d, 1d);

        var rateUnavailable = containsFreshData
            ? _rateUnavailable
            : (_lastSnapshotRateUnavailable ?? _lastRateUnavailable ?? false);

        var minimum = containsFreshData
            ? _minimumValue
            : (_lastSnapshotMinimum ?? _lastEngineering ?? double.NaN);

        var maximum = containsFreshData
            ? _maximumValue
            : (_lastSnapshotMaximum ?? _lastEngineering ?? double.NaN);

        var numerator = containsFreshData
            ? _numeratorSum
            : (_lastSnapshotNumerator ?? (_lastNormalized ?? 0d) * area);

        var denominator = containsFreshData
            ? _denominatorSum
            : (_lastSnapshotDenominator ?? area);

        PlanarPoint[]? positionsArray = null;
        IReadOnlyList<PlanarPoint> positions;
        if (containsFreshData)
        {
            positionsArray = _positions.ToArray();
            positions = positionsArray;
        }
        else if (_lastSnapshotPositions is { } snapshotPositions)
        {
            positions = snapshotPositions;
        }
        else if (_lastPosition is { } lastPosition)
        {
            positions = new[] { lastPosition };
        }
        else
        {
            positions = Array.Empty<PlanarPoint>();
        }

        var snapshotSampleCount = containsFreshData ? _sampleCount : (_lastSnapshotSampleCount ?? 0);
        var snapshotFirstSample = containsFreshData ? _windowFirstSample : _lastSnapshotFirstSample;
        var snapshotLastSample = containsFreshData ? _windowLastSample : _lastSnapshotLastSample;

        var emittedMinimum = containsFreshData ? _minimumValue : minimum;
        var emittedMaximum = containsFreshData ? _maximumValue : maximum;

        var snapshot = new LayerControllerSnapshot(
            controllerId,
            layerId,
            snapshotTimestamp,
            containsFreshData,
            normalized,
            engineering,
            quality,
            rateUnavailable,
            area,
            numerator,
            denominator,
            emittedMinimum,
            emittedMaximum,
            snapshotSampleCount,
            snapshotFirstSample,
            snapshotLastSample,
            positions);

        if (containsFreshData)
        {
            _lastSnapshotNormalized = normalized;
            _lastSnapshotEngineering = engineering;
            _lastSnapshotQuality = quality;
            _lastSnapshotRateUnavailable = rateUnavailable;
            _lastSnapshotArea = area;
            _lastSnapshotNumerator = numerator;
            _lastSnapshotDenominator = denominator;
            _lastSnapshotMinimum = emittedMinimum;
            _lastSnapshotMaximum = emittedMaximum;
            _lastSnapshotSampleCount = snapshotSampleCount;
            _lastSnapshotFirstSample = snapshotFirstSample;
            _lastSnapshotLastSample = snapshotLastSample;
            _lastSnapshotPositions = positionsArray;
        }

        if (resetAfterEmission)
        {
            ResetWindow();
        }

        return snapshot;
    }

    private void ResetWindow()
    {
        _normalizedSum = 0d;
        _engineeringWeightedSum = 0d;
        _engineeringContributionSum = 0d;
        _qualitySum = 0d;
        _areaSum = 0d;
        _numeratorSum = 0d;
        _denominatorSum = 0d;
        _minimumValue = double.PositiveInfinity;
        _maximumValue = double.NegativeInfinity;
        _sampleCount = 0;
        _rateUnavailable = false;
        _windowFirstSample = null;
        _windowLastSample = null;
        _positions.Clear();
    }
}
