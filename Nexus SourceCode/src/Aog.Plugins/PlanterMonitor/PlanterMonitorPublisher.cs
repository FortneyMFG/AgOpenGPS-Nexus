using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Eventing;
using Aog.Core.V1;
using Google.Protobuf.WellKnownTypes;

namespace Aog.Plugins.PlanterMonitor;

/// <summary>
/// Publishes planter row status telemetry based on incoming population measurements.
/// </summary>
public sealed class PlanterMonitorPublisher
{
    private readonly IEventBus _eventBus;
    private readonly PlanterMonitorOptions _options;
    private readonly TimeProvider _timeProvider;
    private long _sequence;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlanterMonitorPublisher"/> class.
    /// </summary>
    /// <param name="eventBus">Event bus used to publish <see cref="PlanterRowStatus"/> messages.</param>
    /// <param name="options">Options controlling thresholds and metadata.</param>
    /// <param name="timeProvider">Optional time provider used to stamp telemetry headers.</param>
    public PlanterMonitorPublisher(IEventBus eventBus, PlanterMonitorOptions options, TimeProvider? timeProvider = null)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _options.Validate();
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Publishes planter row statuses for the supplied measurements.
    /// </summary>
    /// <param name="measurements">Measurements to evaluate.</param>
    /// <param name="cancellationToken">Token used to cancel the publish operation.</param>
    public async ValueTask PublishAsync(IEnumerable<RowPopulationMeasurement> measurements, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(measurements);

        foreach (var measurement in measurements)
        {
            var status = CreateStatus(measurement);
            await _eventBus.PublishAsync(status, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Publishes a planter row status for a single measurement.
    /// </summary>
    /// <param name="measurement">Measurement to evaluate.</param>
    /// <param name="cancellationToken">Token used to cancel the publish operation.</param>
    public ValueTask PublishAsync(RowPopulationMeasurement measurement, CancellationToken cancellationToken = default)
    {
        var status = CreateStatus(measurement);
        return _eventBus.PublishAsync(status, cancellationToken);
    }

    private PlanterRowStatus CreateStatus(RowPopulationMeasurement measurement)
    {
        if (measurement.RowIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(measurement.RowIndex), measurement.RowIndex, "Row index cannot be negative.");
        }

        if (_options.RowCount > 0 && measurement.RowIndex >= _options.RowCount)
        {
            throw new ArgumentOutOfRangeException(nameof(measurement.RowIndex), measurement.RowIndex, "Row index exceeds configured row count.");
        }

        ValidatePopulation(measurement.TargetPopulationPerMeter, nameof(measurement.TargetPopulationPerMeter));
        ValidatePopulation(measurement.ActualPopulationPerMeter, nameof(measurement.ActualPopulationPerMeter));

        var evaluation = Evaluate(measurement.TargetPopulationPerMeter, measurement.ActualPopulationPerMeter);

        var header = new Header
        {
            Sequence = (ulong)Interlocked.Increment(ref _sequence),
            Timestamp = Timestamp.FromDateTimeOffset(_timeProvider.GetUtcNow()),
            Source = _options.Source,
            Frame = _options.Frame
        };

        return new PlanterRowStatus
        {
            Header = header,
            RowIndex = (uint)measurement.RowIndex,
            TargetPopulationPerMeter = measurement.TargetPopulationPerMeter,
            ActualPopulationPerMeter = measurement.ActualPopulationPerMeter,
            SkipRate = evaluation.SkipRate,
            DoubleRate = evaluation.DoubleRate,
            Quality = evaluation.Quality
        };
    }

    private (double SkipRate, double DoubleRate, PlanterRowQuality Quality) Evaluate(double target, double actual)
    {
        if (target <= 0 || double.IsNaN(target))
        {
            return (0d, 0d, PlanterRowQuality.Unknown);
        }

        if (double.IsNaN(actual))
        {
            return (0d, 0d, PlanterRowQuality.Unknown);
        }

        var ratio = actual / target;
        var deviation = ratio - 1d;

        if (double.IsNaN(deviation))
        {
            return (0d, 0d, PlanterRowQuality.Unknown);
        }

        if (deviation <= -_options.SkipThreshold)
        {
            var relative = Math.Clamp((-deviation) / _options.SkipThreshold, 0d, 1d);
            return (relative, 0d, PlanterRowQuality.Skip);
        }

        if (deviation >= _options.DoubleThreshold)
        {
            var relative = Math.Clamp(deviation / _options.DoubleThreshold, 0d, 1d);
            return (0d, relative, PlanterRowQuality.Double);
        }

        return (0d, 0d, PlanterRowQuality.Ok);
    }

    private static void ValidatePopulation(double value, string parameterName)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Population values must be finite and non-negative.");
        }
    }
}
