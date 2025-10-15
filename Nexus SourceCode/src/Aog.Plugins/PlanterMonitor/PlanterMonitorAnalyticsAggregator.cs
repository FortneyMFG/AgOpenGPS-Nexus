using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using Aog.Core.V1;

namespace Aog.Plugins.PlanterMonitor;

/// <summary>
/// Aggregates planter row population measurements into analytics snapshots suitable for dashboards and telemetry sinks.
/// </summary>
public sealed class PlanterMonitorAnalyticsAggregator
{
    private readonly PlanterMonitorOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly Dictionary<int, RowAccumulator> _rows;
    private long _totalMeasurements;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlanterMonitorAnalyticsAggregator"/> class.
    /// </summary>
    /// <param name="options">Options controlling thresholds and metadata validation.</param>
    /// <param name="timeProvider">Optional time provider used when stamping analytics snapshots.</param>
    public PlanterMonitorAnalyticsAggregator(PlanterMonitorOptions options, TimeProvider? timeProvider = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _options.Validate();
        _timeProvider = timeProvider ?? TimeProvider.System;
        _rows = new Dictionary<int, RowAccumulator>(_options.RowCount > 0 ? _options.RowCount : 0);
    }

    /// <summary>
    /// Ingests a single measurement into the aggregator.
    /// </summary>
    /// <param name="measurement">Measurement to ingest.</param>
    public void Ingest(RowPopulationMeasurement measurement)
    {
        PlanterMonitorMath.ValidateMeasurement(_options, measurement);

        var (skipRate, doubleRate, quality) = PlanterMonitorMath.Evaluate(
            _options,
            measurement.TargetPopulationPerMeter,
            measurement.ActualPopulationPerMeter);

        if (!_rows.TryGetValue(measurement.RowIndex, out var accumulator))
        {
            accumulator = new RowAccumulator(measurement.RowIndex);
            _rows.Add(measurement.RowIndex, accumulator);
        }

        accumulator.Update(measurement, skipRate, doubleRate, quality);
        Interlocked.Increment(ref _totalMeasurements);
    }

    /// <summary>
    /// Ingests a batch of measurements into the aggregator.
    /// </summary>
    /// <param name="measurements">Measurements to ingest.</param>
    public void Ingest(IEnumerable<RowPopulationMeasurement> measurements)
    {
        ArgumentNullException.ThrowIfNull(measurements);

        foreach (var measurement in measurements)
        {
            Ingest(measurement);
        }
    }

    /// <summary>
    /// Creates an immutable analytics snapshot based on the current aggregated state.
    /// </summary>
    public PlanterMonitorAnalyticsSnapshot CreateSnapshot()
    {
        var timestamp = _timeProvider.GetUtcNow();
        var totalMeasurements = Interlocked.Read(ref _totalMeasurements);

        if (_rows.Count == 0)
        {
            return new PlanterMonitorAnalyticsSnapshot(
                timestamp,
                totalMeasurements,
                activeRowCount: 0,
                skipRowCount: 0,
                doubleRowCount: 0,
                unknownRowCount: 0,
                averagePopulationErrorPerMeter: 0d,
                averageSkipRate: 0d,
                averageDoubleRate: 0d,
                maxSkipRate: 0d,
                maxDoubleRate: 0d,
                overallQuality: PlanterRowQuality.Unknown,
                rows: Array.Empty<PlanterRowAnalytics>());
        }

        var rows = new List<PlanterRowAnalytics>(_rows.Count);
        var ordered = _rows.Values.OrderBy(static accumulator => accumulator.RowIndex);
        double totalErrorSum = 0d;
        double totalSkipSum = 0d;
        double totalDoubleSum = 0d;
        int skipRowCount = 0;
        int doubleRowCount = 0;
        int unknownRowCount = 0;
        double maxSkipRate = 0d;
        double maxDoubleRate = 0d;
        var overallQuality = PlanterRowQuality.Ok;

        foreach (var accumulator in ordered)
        {
            var summary = accumulator.CreateSummary();
            rows.Add(summary);

            totalErrorSum += summary.AveragePopulationErrorPerMeter * summary.SampleCount;
            totalSkipSum += summary.AverageSkipRate * summary.SampleCount;
            totalDoubleSum += summary.AverageDoubleRate * summary.SampleCount;

            maxSkipRate = Math.Max(maxSkipRate, summary.LatestSkipRate);
            maxDoubleRate = Math.Max(maxDoubleRate, summary.LatestDoubleRate);
            overallQuality = MoreSevere(overallQuality, summary.LatestQuality);

            switch (summary.LatestQuality)
            {
                case PlanterRowQuality.Skip:
                    skipRowCount++;
                    break;
                case PlanterRowQuality.Double:
                    doubleRowCount++;
                    break;
                case PlanterRowQuality.Unknown:
                    unknownRowCount++;
                    break;
            }
        }

        var activeRowCount = rows.Count;
        var totalSamples = rows.Sum(static r => r.SampleCount);

        var averageError = totalSamples > 0 ? totalErrorSum / totalSamples : 0d;
        var averageSkip = totalSamples > 0 ? totalSkipSum / totalSamples : 0d;
        var averageDouble = totalSamples > 0 ? totalDoubleSum / totalSamples : 0d;

        return new PlanterMonitorAnalyticsSnapshot(
            timestamp,
            totalMeasurements,
            activeRowCount,
            skipRowCount,
            doubleRowCount,
            unknownRowCount,
            averageError,
            averageSkip,
            averageDouble,
            maxSkipRate,
            maxDoubleRate,
            overallQuality,
            new ReadOnlyCollection<PlanterRowAnalytics>(rows));
    }

    private static PlanterRowQuality MoreSevere(PlanterRowQuality current, PlanterRowQuality candidate)
    {
        var currentScore = GetSeverityScore(current);
        var candidateScore = GetSeverityScore(candidate);
        return candidateScore > currentScore ? candidate : current;
    }

    private static int GetSeverityScore(PlanterRowQuality quality) => quality switch
    {
        PlanterRowQuality.Double => 3,
        PlanterRowQuality.Skip => 3,
        PlanterRowQuality.Unknown => 1,
        PlanterRowQuality.Ok => 0,
        _ => 0,
    };

    private sealed class RowAccumulator
    {
        private double _targetSum;
        private double _actualSum;
        private double _errorSum;
        private double _skipRateSum;
        private double _doubleRateSum;

        public RowAccumulator(int rowIndex)
        {
            if (rowIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rowIndex));
            }

            RowIndex = rowIndex;
        }

        public int RowIndex { get; }

        public int SampleCount { get; private set; }

        public PlanterRowQuality LatestQuality { get; private set; } = PlanterRowQuality.Unknown;

        public double LatestSkipRate { get; private set; }

        public double LatestDoubleRate { get; private set; }

        public double LatestTarget { get; private set; }

        public double LatestActual { get; private set; }

        public int SkipCount { get; private set; }

        public int DoubleCount { get; private set; }

        public int UnknownCount { get; private set; }

        public int OkCount { get; private set; }

        public void Update(RowPopulationMeasurement measurement, double skipRate, double doubleRate, PlanterRowQuality quality)
        {
            SampleCount++;
            _targetSum += measurement.TargetPopulationPerMeter;
            _actualSum += measurement.ActualPopulationPerMeter;
            _errorSum += Math.Abs(measurement.ActualPopulationPerMeter - measurement.TargetPopulationPerMeter);
            _skipRateSum += skipRate;
            _doubleRateSum += doubleRate;

            LatestQuality = quality;
            LatestSkipRate = skipRate;
            LatestDoubleRate = doubleRate;
            LatestTarget = measurement.TargetPopulationPerMeter;
            LatestActual = measurement.ActualPopulationPerMeter;

            switch (quality)
            {
                case PlanterRowQuality.Skip:
                    SkipCount++;
                    break;
                case PlanterRowQuality.Double:
                    DoubleCount++;
                    break;
                case PlanterRowQuality.Ok:
                    OkCount++;
                    break;
                case PlanterRowQuality.Unknown:
                    UnknownCount++;
                    break;
            }
        }

        public PlanterRowAnalytics CreateSummary()
        {
            var averageTarget = SampleCount > 0 ? _targetSum / SampleCount : 0d;
            var averageActual = SampleCount > 0 ? _actualSum / SampleCount : 0d;
            var averageError = SampleCount > 0 ? _errorSum / SampleCount : 0d;
            var averageSkip = SampleCount > 0 ? _skipRateSum / SampleCount : 0d;
            var averageDouble = SampleCount > 0 ? _doubleRateSum / SampleCount : 0d;

            return new PlanterRowAnalytics(
                RowIndex,
                SampleCount,
                averageTarget,
                averageActual,
                averageError,
                averageSkip,
                averageDouble,
                SkipCount,
                DoubleCount,
                UnknownCount,
                LatestQuality,
                LatestSkipRate,
                LatestDoubleRate,
                LatestTarget,
                LatestActual);
        }
    }
}
