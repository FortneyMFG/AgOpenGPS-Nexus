using System;
using System.Collections.Generic;

namespace Aog.Plugins.CombineYield;

/// <summary>
/// Normalizes raw yield sensor samples into <see cref="CombineYieldMeasurement"/> values ready for aggregation.
/// </summary>
public sealed class YieldSensorNormalizer
{
    private readonly YieldSensorNormalizationOptions _options;
    private readonly Queue<(DateTimeOffset Timestamp, CombineYieldMeasurement Measurement)> _lagBuffer = new();
    private double? _flowEma;
    private double? _moistureEma;

    /// <summary>
    /// Initializes a new instance of the <see cref="YieldSensorNormalizer"/> class.
    /// </summary>
    /// <param name="options">Normalization options.</param>
    public YieldSensorNormalizer(YieldSensorNormalizationOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _options.Validate();
    }

    /// <summary>
    /// Attempts to normalize a raw sensor sample.
    /// </summary>
    /// <param name="sample">Raw sensor sample.</param>
    /// <param name="measurement">Normalized measurement when the method returns <c>true</c>.</param>
    /// <returns><c>true</c> when a measurement is available for downstream processing.</returns>
    public bool TryNormalize(in YieldSensorSample sample, out CombineYieldMeasurement measurement)
    {
        sample.Validate();

        var produced = TryNormalizeCore(in sample, out var normalized);
        if (_options.Lag <= TimeSpan.Zero)
        {
            if (produced)
            {
                measurement = normalized;
                return true;
            }

            measurement = default;
            return false;
        }

        if (produced)
        {
            _lagBuffer.Enqueue((sample.Timestamp, normalized));
        }

        return TryDequeueReady(sample.Timestamp, out measurement);
    }

    /// <summary>
    /// Drains any remaining lagged measurements without requiring additional samples.
    /// </summary>
    public IEnumerable<CombineYieldMeasurement> FlushLagged()
    {
        while (_lagBuffer.Count > 0)
        {
            yield return _lagBuffer.Dequeue().Measurement;
        }
    }

    private bool TryNormalizeCore(in YieldSensorSample sample, out CombineYieldMeasurement measurement)
    {
        measurement = default;

        if (sample.GroundSpeedMetersPerSecond < _options.MinimumGroundSpeedMps)
        {
            return false;
        }

        if (sample.SwathWidthMeters < _options.MinimumSwathWidthMeters)
        {
            return false;
        }

        var calibratedFlow = sample.MassFlowKgPerSecond * _options.MassFlowGain + _options.MassFlowOffset;
        if (!double.IsFinite(calibratedFlow) || calibratedFlow < _options.MinimumFlowKgPerSecond)
        {
            return false;
        }

        var flow = ApplyFlowSmoothing(calibratedFlow);
        if (!double.IsFinite(flow) || flow < _options.MinimumFlowKgPerSecond)
        {
            return false;
        }

        var areaRate = sample.GroundSpeedMetersPerSecond * sample.SwathWidthMeters;
        if (!double.IsFinite(areaRate) || areaRate <= 0)
        {
            return false;
        }

        var yieldKgPerHectare = flow / areaRate * 10_000d;
        if (!double.IsFinite(yieldKgPerHectare))
        {
            return false;
        }

        if (_options.MinimumYieldKgPerHectare.HasValue)
        {
            yieldKgPerHectare = Math.Max(yieldKgPerHectare, _options.MinimumYieldKgPerHectare.Value);
        }

        if (_options.MaximumYieldKgPerHectare.HasValue)
        {
            yieldKgPerHectare = Math.Min(yieldKgPerHectare, _options.MaximumYieldKgPerHectare.Value);
        }

        double? moisture = null;
        if (sample.MoisturePercent.HasValue)
        {
            var calibratedMoisture = sample.MoisturePercent.Value * _options.MoistureGain + _options.MoistureOffset;
            if (double.IsFinite(calibratedMoisture))
            {
                var smoothedMoisture = ApplyMoistureSmoothing(calibratedMoisture);
                if (smoothedMoisture.HasValue && double.IsFinite(smoothedMoisture.Value))
                {
                    var value = smoothedMoisture.Value;
                    if (_options.ClampMoistureToValidRange)
                    {
                        value = Math.Clamp(value, 0, 100);
                    }

                    moisture = value;
                }
            }
        }

        measurement = new CombineYieldMeasurement(sample.EastingMeters, sample.NorthingMeters, yieldKgPerHectare, moisture);
        return true;
    }

    private double ApplyFlowSmoothing(double value)
    {
        var alpha = _options.FlowSmoothingFactor.GetValueOrDefault();
        if (alpha <= 0)
        {
            _flowEma = value;
            return value;
        }

        _flowEma = _flowEma.HasValue ? _flowEma.Value + alpha * (value - _flowEma.Value) : value;
        return _flowEma.Value;
    }

    private double? ApplyMoistureSmoothing(double value)
    {
        var alpha = _options.MoistureSmoothingFactor.GetValueOrDefault();
        if (alpha <= 0)
        {
            _moistureEma = value;
            return value;
        }

        _moistureEma = _moistureEma.HasValue ? _moistureEma.Value + alpha * (value - _moistureEma.Value) : value;
        return _moistureEma.Value;
    }

    private bool TryDequeueReady(DateTimeOffset referenceTimestamp, out CombineYieldMeasurement measurement)
    {
        while (_lagBuffer.Count > 0)
        {
            var entry = _lagBuffer.Peek();
            if (referenceTimestamp - entry.Timestamp >= _options.Lag)
            {
                _lagBuffer.Dequeue();
                measurement = entry.Measurement;
                return true;
            }

            break;
        }

        measurement = default;
        return false;
    }
}
