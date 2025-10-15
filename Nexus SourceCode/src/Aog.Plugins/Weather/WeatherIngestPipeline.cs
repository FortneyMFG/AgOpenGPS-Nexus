using System;
using System.Threading;
using System.Threading.Tasks;

namespace Aog.Plugins.Weather;

/// <summary>
/// Aggregates raw weather samples into normalized snapshots that align with session schema expectations.
/// </summary>
public sealed class WeatherIngestPipeline
{
    private readonly WeatherIngestOptions _options;
    private WeatherSnapshot? _latest;
    private WeatherSnapshot? _lastPublished;
    private WeatherSnapshot? _pending;
    private DateTimeOffset _lastPublishedAt = DateTimeOffset.MinValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="WeatherIngestPipeline"/> class.
    /// </summary>
    /// <param name="options">Pipeline configuration options.</param>
    public WeatherIngestPipeline(WeatherIngestOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _options.Validate();
    }

    /// <summary>
    /// Latest snapshot ingested, even if it has not yet been published.
    /// </summary>
    public WeatherSnapshot? LatestSnapshot => _latest;

    /// <summary>
    /// Ingests a sample and returns a snapshot when publication criteria are satisfied.
    /// </summary>
    public ValueTask<WeatherSnapshot?> IngestAsync(WeatherSample sample, CancellationToken cancellationToken = default)
    {
        if (sample is null)
        {
            throw new ArgumentNullException(nameof(sample));
        }

        sample.Validate();

        if (_latest is { } existing && !sample.ForcePublish && sample.CapturedAt <= existing.CapturedAt)
        {
            // Ignore stale samples unless forced.
            return ValueTask.FromResult<WeatherSnapshot?>(null);
        }

        var merged = WeatherSnapshot.Merge(_options.MergePartialSamples ? _latest : null, sample, _options.MergePartialSamples);
        merged = WeatherSnapshot.NormalizeDerivedValues(merged);
        merged = WeatherSnapshot.Clamp(merged);
        _latest = merged;

        if (ShouldPublish(sample, merged))
        {
            return PublishAsync(merged);
        }

        _pending = merged;
        return ValueTask.FromResult<WeatherSnapshot?>(null);
    }

    /// <summary>
    /// Forces publication of the most recent pending snapshot, respecting interval spacing.
    /// </summary>
    public ValueTask<WeatherSnapshot?> FlushAsync(CancellationToken cancellationToken = default)
    {
        if (_pending is null)
        {
            return ValueTask.FromResult<WeatherSnapshot?>(null);
        }

        var candidate = _pending;
        if (_lastPublished is not null && _options.MinimumPublishInterval > TimeSpan.Zero)
        {
            var earliestAllowed = _lastPublishedAt + _options.MinimumPublishInterval;
            if (candidate.CapturedAt < earliestAllowed)
            {
                candidate = candidate with { CapturedAt = earliestAllowed };
            }
        }

        return PublishAsync(candidate);
    }

    private bool ShouldPublish(WeatherSample sample, WeatherSnapshot candidate)
    {
        if (_lastPublished is null)
        {
            return true;
        }

        if (sample.ForcePublish)
        {
            return true;
        }

        if (_options.MinimumPublishInterval == TimeSpan.Zero)
        {
            return !IsEquivalent(candidate, _lastPublished);
        }

        var interval = candidate.CapturedAt - _lastPublishedAt;
        if (interval < _options.MinimumPublishInterval)
        {
            return false;
        }

        return !IsEquivalent(candidate, _lastPublished);
    }

    private ValueTask<WeatherSnapshot?> PublishAsync(WeatherSnapshot snapshot)
    {
        _latest = snapshot;
        _lastPublished = snapshot;
        _lastPublishedAt = snapshot.CapturedAt;
        _pending = null;
        return ValueTask.FromResult<WeatherSnapshot?>(snapshot);
    }

    private bool IsEquivalent(WeatherSnapshot left, WeatherSnapshot right)
    {
        if (!Equals(left.Source, right.Source))
        {
            return false;
        }

        return AreClose(left.TemperatureC, right.TemperatureC)
            && AreClose(left.HumidityPct, right.HumidityPct)
            && AreClose(left.WindKph, right.WindKph)
            && AreClose(left.WindDirectionDeg, right.WindDirectionDeg)
            && AreClose(left.WindGustKph, right.WindGustKph)
            && AreClose(left.RainfallMm, right.RainfallMm)
            && AreClose(left.PressureKpa, right.PressureKpa)
            && AreClose(left.DewPointC, right.DewPointC)
            && AreClose(left.WetBulbC, right.WetBulbC)
            && AreClose(left.DeltaTC, right.DeltaTC)
            && AreClose(left.EvapotranspirationMm, right.EvapotranspirationMm)
            && AreClose(left.SolarIrradianceWm2, right.SolarIrradianceWm2)
            && AreClose(left.UvIndex, right.UvIndex)
            && AreClose(left.CloudCoverPct, right.CloudCoverPct)
            && AreClose(left.VisibilityKm, right.VisibilityKm)
            && AreClose(left.SoilTempC, right.SoilTempC)
            && AreClose(left.SoilMoisturePct, right.SoilMoisturePct)
            && AreClose(left.LeafWetnessPct, right.LeafWetnessPct);
    }

    private bool AreClose(double? left, double? right)
    {
        if (!left.HasValue && !right.HasValue)
        {
            return true;
        }

        if (!left.HasValue || !right.HasValue)
        {
            return false;
        }

        return Math.Abs(left.Value - right.Value) <= _options.ChangeEpsilon;
    }
}
