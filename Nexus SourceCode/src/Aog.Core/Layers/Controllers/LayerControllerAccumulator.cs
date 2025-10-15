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
        var area = _areaSum;

        var normalized = area > 0d
            ? _normalizedSum / area
            : _lastNormalized ?? 0d;

        var engineering = aggregationStrategy switch
        {
            LayerAggregationStrategy.Sum => _engineeringContributionSum,
            LayerAggregationStrategy.Hold when _lastEngineering.HasValue => _lastEngineering.Value,
            LayerAggregationStrategy.Hold => throw new InvalidOperationException("Hold strategy requires at least one observed sample."),
            _ when area > 0d => _engineeringWeightedSum / area,
            _ when includeHoldValues && _lastEngineering.HasValue => _lastEngineering.Value,
            _ => throw new InvalidOperationException("No engineering samples recorded in the current window."),
        };

        var quality = area > 0d
            ? Math.Clamp(_qualitySum / area, 0d, 1d)
            : (_lastQuality ?? 0d);

        var rateUnavailable = containsFreshData
            ? _rateUnavailable
            : (_lastRateUnavailable ?? false);

        var minimum = containsFreshData
            ? _minimumValue
            : (_lastEngineering ?? double.NaN);

        var maximum = containsFreshData
            ? _maximumValue
            : (_lastEngineering ?? double.NaN);

        var numerator = containsFreshData || _numeratorSum != 0d
            ? _numeratorSum
            : (_lastNormalized ?? 0d) * area;

        var denominator = containsFreshData || _denominatorSum != 0d
            ? _denominatorSum
            : area;

        var positions = containsFreshData
            ? (IReadOnlyList<PlanarPoint>)_positions.ToArray()
            : _lastPosition is { } lastPosition
                ? new[] { lastPosition }
                : Array.Empty<PlanarPoint>();

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
            containsFreshData ? _minimumValue : minimum,
            containsFreshData ? _maximumValue : maximum,
            containsFreshData ? _sampleCount : 0,
            containsFreshData ? _windowFirstSample : null,
            containsFreshData ? _windowLastSample : null,
            positions);

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
