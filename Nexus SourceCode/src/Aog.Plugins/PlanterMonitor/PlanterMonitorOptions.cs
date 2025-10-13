using System;

namespace Aog.Plugins.PlanterMonitor;

/// <summary>
/// Provides configuration for the <see cref="PlanterMonitorPublisher"/>.
/// </summary>
public sealed class PlanterMonitorOptions
{
    /// <summary>
    /// Gets or sets the skip detection threshold expressed as a relative fraction (0-1).
    /// Values greater than this threshold will classify the row as experiencing skips.
    /// </summary>
    public double SkipThreshold { get; init; } = 0.2;

    /// <summary>
    /// Gets or sets the double detection threshold expressed as a relative fraction (0-1).
    /// Values greater than this threshold will classify the row as experiencing doubles.
    /// </summary>
    public double DoubleThreshold { get; init; } = 0.2;

    /// <summary>
    /// Gets or sets the optional planter row count. When greater than zero, row indices must fall within this range.
    /// </summary>
    public int RowCount { get; init; }

    /// <summary>
    /// Gets or sets the coordinate frame recorded in published telemetry headers.
    /// </summary>
    public string Frame { get; init; } = "vehicle";

    /// <summary>
    /// Gets or sets the source identifier recorded in published telemetry headers.
    /// </summary>
    public string Source { get; init; } = "planter.monitor";

    /// <summary>
    /// Validates the option values and throws when invalid data is encountered.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when thresholds are non-positive or row count is negative.</exception>
    /// <exception cref="ArgumentException">Thrown when frame or source identifiers are blank.</exception>
    public void Validate()
    {
        if (double.IsNaN(SkipThreshold) || SkipThreshold <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(SkipThreshold), SkipThreshold, "Skip threshold must be positive.");
        }

        if (double.IsNaN(DoubleThreshold) || DoubleThreshold <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(DoubleThreshold), DoubleThreshold, "Double threshold must be positive.");
        }

        if (RowCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(RowCount), RowCount, "Row count cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(Frame))
        {
            throw new ArgumentException("Frame identifier must be provided.", nameof(Frame));
        }

        if (string.IsNullOrWhiteSpace(Source))
        {
            throw new ArgumentException("Source identifier must be provided.", nameof(Source));
        }
    }
}
