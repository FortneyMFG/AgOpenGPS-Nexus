using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Eventing;
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

    private readonly IEventBus _eventBus;
    private readonly CombineYieldOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly Dictionary<(int Column, int Row), CellAccumulator> _cells = new();
    private DateTimeOffset _lastPublish = DateTimeOffset.MinValue;
    private long _sequence;

    /// <summary>
    /// Initializes a new instance of the <see cref="CombineYieldLayerAggregator"/> class.
    /// </summary>
    /// <param name="eventBus">Event bus used to publish <see cref="CombineYieldLayer"/> messages.</param>
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
    /// Creates an immutable snapshot of the current aggregated cells.
    /// </summary>
    public CombineYieldLayer CreateLayerSnapshot()
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

        var layer = CreateLayer(timestamp, advanceSequence: true);
        await _eventBus.PublishAsync(layer, cancellationToken).ConfigureAwait(false);
        _lastPublish = timestamp;
    }

    private CombineYieldLayer CreateLayer(DateTimeOffset timestamp, bool advanceSequence)
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

        foreach (var entry in _cells.OrderBy(e => e.Key.Row).ThenBy(e => e.Key.Column))
        {
            var accumulator = entry.Value;
            var cell = new CombineYieldCell
            {
                Column = (uint)entry.Key.Column,
                Row = (uint)entry.Key.Row,
                AverageYieldKgPerHectare = accumulator.YieldSum / accumulator.Count,
                AverageMoisturePercent = accumulator.MoistureCount > 0 ? accumulator.MoistureSum / accumulator.MoistureCount : 0d,
                SampleCount = (uint)accumulator.Count
            };

            layer.Cells.Add(cell);
        }

        return layer;
    }
}
