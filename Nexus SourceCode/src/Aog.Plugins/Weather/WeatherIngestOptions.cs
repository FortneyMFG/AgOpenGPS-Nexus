using System;

namespace Aog.Plugins.Weather;

/// <summary>
/// Configuration options controlling how the weather ingest pipeline publishes updates.
/// </summary>
public sealed class WeatherIngestOptions
{
    /// <summary>
    /// Minimum interval between published snapshots. Pending updates can be flushed manually.
    /// </summary>
    public TimeSpan MinimumPublishInterval { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// When <c>true</c>, partial samples merge with previously known values instead of resetting missing fields to null.
    /// </summary>
    public bool MergePartialSamples { get; set; } = true;

    /// <summary>
    /// Tolerance used to detect significant changes between published snapshots.
    /// </summary>
    public double ChangeEpsilon { get; set; } = 1e-3;

    /// <summary>
    /// Validates option values.
    /// </summary>
    public void Validate()
    {
        if (MinimumPublishInterval < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(MinimumPublishInterval), "Minimum publish interval cannot be negative.");
        }

        if (!double.IsFinite(ChangeEpsilon) || ChangeEpsilon < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ChangeEpsilon), "Change epsilon must be a finite non-negative value.");
        }
    }
}
