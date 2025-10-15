using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Eventing;

namespace Aog.Plugins.CombineYield;

/// <summary>
/// Provides helper routines for importing normalized yield measurements into the aggregation pipeline.
/// </summary>
public sealed class YieldImportService
{
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="YieldImportService"/> class.
    /// </summary>
    /// <param name="timeProvider">Optional time provider to control layer timestamps.</param>
    public YieldImportService(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Imports the supplied measurements and returns the aggregated layer with metadata.
    /// </summary>
    /// <param name="request">Import request describing measurements and options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<YieldImportResult> ImportAsync(YieldImportRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (request.Measurements.Count == 0)
        {
            throw new ArgumentException("At least one measurement must be supplied.", nameof(request));
        }

        var options = CloneOptions(request.Options ?? new CombineYieldOptions());
        var provider = request.Timestamp.HasValue ? new FakeTimeProvider(request.Timestamp.Value) : _timeProvider;
        var aggregator = new CombineYieldLayerAggregator(new InMemoryEventBus(), options, provider);

        foreach (var measurement in request.Measurements)
        {
            await aggregator.IngestAsync(measurement, cancellationToken).ConfigureAwait(false);
        }

        var publication = aggregator.CreateLayerSnapshot();
        return new YieldImportResult(publication, request.Units, request.Measurements.Count);
    }

    private static CombineYieldOptions CloneOptions(CombineYieldOptions source)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        var clone = new CombineYieldOptions
        {
            CellSizeMeters = source.CellSizeMeters,
            PublishInterval = source.PublishInterval,
            Source = source.Source,
            Transform = source.Transform,
            Actor = source.Actor,
            Frame = source.Frame,
            Crop = source.Crop,
            Projection = source.Projection,
            CalibrationProfileId = source.CalibrationProfileId,
            CalibrationAppliedAt = source.CalibrationAppliedAt,
            CalibrationSource = source.CalibrationSource,
            CalibrationSensorModel = source.CalibrationSensorModel,
            CalibrationNotes = source.CalibrationNotes,
            SmoothingMethod = source.SmoothingMethod,
            SmoothingKernelSize = source.SmoothingKernelSize,
            SmoothingWindowSeconds = source.SmoothingWindowSeconds,
            SmoothingLagCompensationSeconds = source.SmoothingLagCompensationSeconds,
            SmoothingPasses = source.SmoothingPasses,
            OutlierClampFraction = source.OutlierClampFraction,
            AggregationBasis = source.AggregationBasis,
            AggregationScopes = source.AggregationScopes?.ToArray() ?? new[] { "job", "field" },
            BinningScheme = source.BinningScheme,
            BinningBinCount = source.BinningBinCount,
            CustomBinBreaks = source.CustomBinBreaks?.ToArray(),
            CustomBinLabels = source.CustomBinLabels?.ToArray()
        };

        foreach (var pair in source.CalibrationFactors)
        {
            clone.CalibrationFactors[pair.Key] = pair.Value;
        }

        return clone;
    }
}
