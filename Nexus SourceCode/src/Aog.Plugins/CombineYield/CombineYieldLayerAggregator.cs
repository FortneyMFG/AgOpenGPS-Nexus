using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Eventing;
using Aog.Core.Layers;
using Aog.Core.V1;
using Google.Protobuf.WellKnownTypes;

namespace Aog.Plugins.CombineYield;

/// <summary>
/// Aggregates raw combine yield measurements into spatial layers for UI overlays and exports.
/// </summary>
public sealed class CombineYieldLayerAggregator
{
    private readonly struct CellAccumulator
    {
        public readonly double YieldSum;
        public readonly double MoistureSum;
        public readonly int Count;
        public readonly int MoistureCount;

        public CellAccumulator(double yieldSum, double moistureSum, int count, int moistureCount)
        {
            YieldSum = yieldSum;
            MoistureSum = moistureSum;
            Count = count;
            MoistureCount = moistureCount;
        }

        public CellAccumulator Add(double yield, double? moisture)
        {
            var newYieldSum = YieldSum + yield;
            var newCount = Count + 1;
            if (moisture.HasValue)
            {
                return new CellAccumulator(newYieldSum, MoistureSum + moisture.Value, newCount, MoistureCount + 1);
            }

            return new CellAccumulator(newYieldSum, MoistureSum, newCount, MoistureCount);
        }
    }

    private readonly struct RawCell
    {
        public RawCell(double averageYield, double? averageMoisture, int sampleCount, int moistureCount)
        {
            AverageYield = averageYield;
            AverageMoisture = averageMoisture;
            SampleCount = sampleCount;
            MoistureCount = moistureCount;
        }

        public double AverageYield { get; }

        public double? AverageMoisture { get; }

        public int SampleCount { get; }

        public int MoistureCount { get; }

        public bool HasMoisture => MoistureCount > 0;
    }

    private readonly struct SmoothedCell
    {
        public SmoothedCell(double yield, double? moisture, int sampleCount)
        {
            Yield = yield;
            Moisture = moisture;
            SampleCount = sampleCount;
        }

        public double Yield { get; }

        public double? Moisture { get; }

        public int SampleCount { get; }
    }

    private readonly struct WeightedValue
    {
        public WeightedValue(double value, int weight)
        {
            Value = value;
            Weight = weight;
        }

        public double Value { get; }

        public int Weight { get; }
    }

    private readonly IEventBus _eventBus;
    private readonly CombineYieldOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly Dictionary<(int Column, int Row), CellAccumulator> _cells = new();
    private DateTimeOffset _lastPublish = DateTimeOffset.MinValue;
    private long _sequence;

    /// <summary>
    /// Initializes a new instance of the <see cref="CombineYieldLayerAggregator"/> class.
    /// </summary>
    /// <param name="eventBus">Event bus used to publish <see cref="CombineYieldLayerPublication"/> messages.</param>
    /// <param name="options">Aggregation options.</param>
    /// <param name="timeProvider">Optional time provider for deterministic testing.</param>
    public CombineYieldLayerAggregator(IEventBus eventBus, CombineYieldOptions options, TimeProvider? timeProvider = null)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _options.Validate();
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Adds a measurement to the aggregator and publishes a layer when required.
    /// </summary>
    /// <param name="measurement">Measurement to ingest.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async ValueTask IngestAsync(CombineYieldMeasurement measurement, CancellationToken cancellationToken = default)
    {
        measurement.Validate();

        var column = (int)Math.Floor(measurement.EastingMeters / _options.CellSizeMeters);
        var row = (int)Math.Floor(measurement.NorthingMeters / _options.CellSizeMeters);

        var key = (column, row);
        if (_cells.TryGetValue(key, out var accumulator))
        {
            _cells[key] = accumulator.Add(measurement.YieldKgPerHectare, measurement.MoisturePercent);
        }
        else
        {
            _cells[key] = new CellAccumulator(measurement.YieldKgPerHectare, measurement.MoisturePercent ?? 0d, 1, measurement.MoisturePercent.HasValue ? 1 : 0);
        }

        var now = _timeProvider.GetUtcNow();
        if (ShouldPublish(now))
        {
            await PublishLayerAsync(now, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Forces a layer publication using the latest aggregated state.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public ValueTask FlushAsync(CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        return PublishLayerAsync(now, cancellationToken);
    }

    /// <summary>
    /// Creates an immutable snapshot of the current aggregated cells and provenance.
    /// </summary>
    public CombineYieldLayerPublication CreateLayerSnapshot()
    {
        var now = _timeProvider.GetUtcNow();
        return CreateLayer(now, advanceSequence: false);
    }

    private bool ShouldPublish(DateTimeOffset now)
    {
        if (_cells.Count == 0)
        {
            return false;
        }

        if (_options.PublishInterval == TimeSpan.Zero)
        {
            return true;
        }

        return now - _lastPublish >= _options.PublishInterval;
    }

    private async ValueTask PublishLayerAsync(DateTimeOffset timestamp, CancellationToken cancellationToken)
    {
        if (_cells.Count == 0)
        {
            return;
        }

        var publication = CreateLayer(timestamp, advanceSequence: true);
        await _eventBus.PublishAsync(publication, cancellationToken).ConfigureAwait(false);
        _lastPublish = timestamp;
    }

    private CombineYieldLayerPublication CreateLayer(DateTimeOffset timestamp, bool advanceSequence)
    {
        var header = new Header
        {
            Sequence = advanceSequence ? (ulong)Interlocked.Increment(ref _sequence) : (ulong)Volatile.Read(ref _sequence),
            Timestamp = Timestamp.FromDateTimeOffset(timestamp),
            Source = _options.Source,
            Frame = _options.Frame
        };

        var layer = new CombineYieldLayer
        {
            Header = header,
            Crop = _options.Crop,
            CellSizeMeters = _options.CellSizeMeters
        };

        var rawCells = CreateRawCells();
        var smoothedCells = SmoothCells(rawCells);
        ApplyOutlierClamp(smoothedCells, rawCells);

        foreach (var entry in smoothedCells.OrderBy(e => e.Key.Row).ThenBy(e => e.Key.Column))
        {
            var cell = entry.Value;
            var layerCell = new CombineYieldCell
            {
                Column = (uint)entry.Key.Column,
                Row = (uint)entry.Key.Row,
                AverageYieldKgPerHectare = cell.Yield,
                AverageMoisturePercent = cell.Moisture.GetValueOrDefault(),
                SampleCount = (uint)cell.SampleCount
            };

            layer.Cells.Add(layerCell);
        }

        var hash = ComputeHash(layer);
        var provenance = new LayerProvenance(
            _options.Source,
            _options.Transform,
            hash,
            timestamp,
            _options.Actor);

        var metadata = CreateMetadata(timestamp, smoothedCells);

        return new CombineYieldLayerPublication(layer, provenance, metadata);
    }

    private static string ComputeHash(CombineYieldLayer layer)
    {
        using var sha = SHA256.Create();
        var builder = new StringBuilder();
        builder.AppendFormat(
            CultureInfo.InvariantCulture,
            "{0};{1:F3};",
            layer.Crop,
            layer.CellSizeMeters);

        foreach (var cell in layer.Cells.OrderBy(cell => cell.Row).ThenBy(cell => cell.Column))
        {
            builder.AppendFormat(
                CultureInfo.InvariantCulture,
                "{0},{1},{2:G17},{3:G17},{4};",
                cell.Column,
                cell.Row,
                cell.AverageYieldKgPerHectare,
                cell.AverageMoisturePercent,
                cell.SampleCount);
        }

        var bytes = Encoding.UTF8.GetBytes(builder.ToString());
        var hash = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }

    private Dictionary<(int Column, int Row), RawCell> CreateRawCells()
    {
        var raw = new Dictionary<(int Column, int Row), RawCell>(_cells.Count);
        foreach (var entry in _cells)
        {
            var accumulator = entry.Value;
            var averageYield = accumulator.Count > 0 ? accumulator.YieldSum / accumulator.Count : 0d;
            double? averageMoisture = null;
            if (accumulator.MoistureCount > 0)
            {
                averageMoisture = accumulator.MoistureSum / accumulator.MoistureCount;
            }

            raw[entry.Key] = new RawCell(averageYield, averageMoisture, accumulator.Count, accumulator.MoistureCount);
        }

        return raw;
    }

    private Dictionary<(int Column, int Row), SmoothedCell> SmoothCells(IReadOnlyDictionary<(int Column, int Row), RawCell> rawCells)
    {
        var radius = _options.SmoothingKernelSize / 2;
        var result = new Dictionary<(int Column, int Row), SmoothedCell>(rawCells.Count);

        foreach (var entry in rawCells)
        {
            var (column, row) = entry.Key;
            var cell = entry.Value;

            double yieldSum = 0;
            int yieldWeight = 0;
            double moistureSum = 0;
            int moistureWeight = 0;

            for (var dy = -radius; dy <= radius; dy++)
            {
                for (var dx = -radius; dx <= radius; dx++)
                {
                    var neighborKey = (column + dx, row + dy);
                    if (!rawCells.TryGetValue(neighborKey, out var neighbor))
                    {
                        continue;
                    }

                    yieldSum += neighbor.AverageYield * neighbor.SampleCount;
                    yieldWeight += neighbor.SampleCount;

                    if (neighbor.HasMoisture)
                    {
                        moistureSum += neighbor.AverageMoisture!.Value * neighbor.SampleCount;
                        moistureWeight += neighbor.SampleCount;
                    }
                }
            }

            var smoothedYield = yieldWeight > 0 ? yieldSum / yieldWeight : cell.AverageYield;
            double? smoothedMoisture = null;
            if (moistureWeight > 0)
            {
                smoothedMoisture = moistureSum / moistureWeight;
            }
            else if (cell.HasMoisture)
            {
                smoothedMoisture = cell.AverageMoisture;
            }

            result[entry.Key] = new SmoothedCell(smoothedYield, smoothedMoisture, cell.SampleCount);
        }

        return result;
    }

    private void ApplyOutlierClamp(IDictionary<(int Column, int Row), SmoothedCell> smoothedCells, IReadOnlyDictionary<(int Column, int Row), RawCell> rawCells)
    {
        if (_options.OutlierClampFraction <= 0 || rawCells.Count == 0)
        {
            return;
        }

        var rawYields = rawCells.Values
            .Select(cell => cell.AverageYield)
            .Where(double.IsFinite)
            .ToList();

        if (rawYields.Count == 0)
        {
            return;
        }

        var min = rawYields.Min();
        var max = rawYields.Max();
        var range = max - min;
        if (range <= 0)
        {
            return;
        }

        var extension = range * _options.OutlierClampFraction;
        var lower = Math.Max(0, min - extension);
        var upper = max + extension;

        foreach (var key in smoothedCells.Keys.ToList())
        {
            var cell = smoothedCells[key];
            var clampedYield = Math.Clamp(cell.Yield, lower, upper);
            smoothedCells[key] = new SmoothedCell(clampedYield, cell.Moisture, cell.SampleCount);
        }
    }

    private YieldLayerMetadata CreateMetadata(
        DateTimeOffset timestamp,
        IReadOnlyDictionary<(int Column, int Row), SmoothedCell> smoothedCells)
    {
        var weightedValues = smoothedCells.Values
            .Where(cell => cell.SampleCount > 0 && double.IsFinite(cell.Yield))
            .Select(cell => new WeightedValue(cell.Yield, cell.SampleCount))
            .ToList();

        var totalSamples = weightedValues.Sum(value => value.Weight);
        var mean = totalSamples > 0 ? weightedValues.Sum(value => value.Value * value.Weight) / totalSamples : 0;
        var min = weightedValues.Count > 0 ? weightedValues.Min(value => value.Value) : 0;
        var max = weightedValues.Count > 0 ? weightedValues.Max(value => value.Value) : 0;
        var median = totalSamples > 0 ? ComputeWeightedQuantile(weightedValues, totalSamples, 0.5) : 0;
        var stdDev = totalSamples > 0
            ? Math.Sqrt(weightedValues.Sum(value => value.Weight * Math.Pow(value.Value - mean, 2)) / totalSamples)
            : 0;

        var cellAreaHa = (_options.CellSizeMeters * _options.CellSizeMeters) / 10_000d;
        var totalMassKg = smoothedCells.Values.Sum(cell => cell.Yield * cellAreaHa);

        var binning = CreateBinningMetadata(weightedValues, totalSamples, min, max);

        var grid = new YieldGridMetadata(_options.CellSizeMeters, _options.Projection);
        var smoothing = new YieldSmoothingMetadata(
            _options.SmoothingMethod,
            _options.SmoothingWindowSeconds ?? 0,
            _options.SmoothingLagCompensationSeconds ?? 0,
            _options.SmoothingPasses);

        var calibration = new YieldCalibrationMetadata(
            _options.CalibrationProfileId,
            _options.CalibrationAppliedAt,
            _options.CalibrationSource,
            _options.CalibrationSensorModel,
            _options.CalibrationNotes,
            new ReadOnlyDictionary<string, double>(new Dictionary<string, double>(_options.CalibrationFactors)));

        var scopes = Array.AsReadOnly(_options.AggregationScopes.ToArray());

        var aggregation = new YieldAggregationMetadata(
            _options.AggregationBasis,
            scopes,
            timestamp,
            binning);

        var statistics = new YieldStatisticsMetadata(
            totalSamples,
            mean,
            median,
            stdDev,
            min,
            max,
            totalMassKg);

        return new YieldLayerMetadata(grid, smoothing, calibration, aggregation, statistics);
    }

    private YieldBinningMetadata CreateBinningMetadata(List<WeightedValue> values, int totalSamples, double min, double max)
    {
        var scheme = _options.BinningScheme switch
        {
            YieldBinningScheme.EqualInterval => "equalInterval",
            YieldBinningScheme.Custom => "custom",
            _ => "quantile"
        };

        return _options.BinningScheme switch
        {
            YieldBinningScheme.Custom =>
                YieldBinningMetadata.Create(
                    scheme,
                    Math.Max((_options.CustomBinBreaks?.Length ?? 1) - 1, 0),
                    _options.CustomBinBreaks,
                    _options.CustomBinLabels),
            YieldBinningScheme.EqualInterval =>
                YieldBinningMetadata.Create(
                    scheme,
                    _options.BinningBinCount,
                    ComputeEqualIntervalBreaks(min, max, _options.BinningBinCount),
                    null),
            _ =>
                YieldBinningMetadata.Create(
                    scheme,
                    _options.BinningBinCount,
                    ComputeQuantileBreaks(values, totalSamples, _options.BinningBinCount),
                    null)
        };
    }

    private static IReadOnlyList<double> ComputeEqualIntervalBreaks(double min, double max, int binCount)
    {
        var actualCount = Math.Max(binCount, 1);
        var breaks = new double[actualCount + 1];

        if (!double.IsFinite(min) || !double.IsFinite(max))
        {
            Array.Fill(breaks, 0);
            return breaks;
        }

        var range = max - min;
        if (range <= 0)
        {
            for (var i = 0; i < breaks.Length; i++)
            {
                breaks[i] = min;
            }

            return breaks;
        }

        var step = range / actualCount;
        for (var i = 0; i <= actualCount; i++)
        {
            breaks[i] = min + step * i;
        }

        return breaks;
    }

    private static IReadOnlyList<double> ComputeQuantileBreaks(List<WeightedValue> values, int totalSamples, int binCount)
    {
        var actualCount = Math.Max(binCount, 1);
        var breaks = new double[actualCount + 1];

        if (values.Count == 0 || totalSamples <= 0)
        {
            Array.Fill(breaks, 0);
            return breaks;
        }

        values.Sort((a, b) => a.Value.CompareTo(b.Value));

        for (var i = 0; i <= actualCount; i++)
        {
            var quantile = actualCount == 0 ? 0 : (double)i / actualCount;
            breaks[i] = ComputeWeightedQuantile(values, totalSamples, quantile);
        }

        return breaks;
    }

    private static double ComputeWeightedQuantile(List<WeightedValue> sortedValues, int totalWeight, double quantile)
    {
        if (sortedValues.Count == 0)
        {
            return 0;
        }

        if (totalWeight <= 0)
        {
            return sortedValues[0].Value;
        }

        quantile = Math.Clamp(quantile, 0, 1);
        var target = quantile * (totalWeight - 1);
        var cumulative = 0;

        foreach (var value in sortedValues)
        {
            cumulative += value.Weight;
            if (target < cumulative)
            {
                return value.Value;
            }
        }

        return sortedValues[^1].Value;
    }
}

/// <summary>
/// Publication containing a combine yield layer and associated provenance.
/// </summary>
/// <param name="Layer">Aggregated layer payload.</param>
/// <param name="Provenance">Provenance metadata describing the aggregation.</param>
/// <param name="Metadata">Metadata describing smoothing, calibration, aggregation, and statistics.</param>
public sealed record CombineYieldLayerPublication(CombineYieldLayer Layer, LayerProvenance Provenance, YieldLayerMetadata Metadata);
