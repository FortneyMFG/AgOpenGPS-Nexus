using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Aog.Plugins.CombineYield;

/// <summary>
/// Describes a yield import request destined for the combine yield pipeline.
/// </summary>
public sealed class YieldImportRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="YieldImportRequest"/> class.
    /// </summary>
    /// <param name="measurements">Normalized yield measurements to ingest.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="measurements"/> is null.</exception>
    public YieldImportRequest(IEnumerable<CombineYieldMeasurement> measurements)
    {
        if (measurements is null)
        {
            throw new ArgumentNullException(nameof(measurements));
        }

        var list = measurements.ToList();
        Measurements = new ReadOnlyCollection<CombineYieldMeasurement>(list);
    }

    /// <summary>
    /// Gets the normalized yield measurements supplied by the caller.
    /// </summary>
    public IReadOnlyList<CombineYieldMeasurement> Measurements { get; }

    /// <summary>
    /// Gets or sets the aggregation options that control smoothing, binning, and metadata.
    /// </summary>
    public CombineYieldOptions Options { get; set; } = new();

    /// <summary>
    /// Gets or sets the units associated with the imported dataset (e.g., bu/ac, kg/ha).
    /// </summary>
    public string Units { get; set; } = "kg/ha";

    /// <summary>
    /// Gets or sets the timestamp applied to the generated layer header.
    /// </summary>
    public DateTimeOffset? Timestamp { get; set; }
}
