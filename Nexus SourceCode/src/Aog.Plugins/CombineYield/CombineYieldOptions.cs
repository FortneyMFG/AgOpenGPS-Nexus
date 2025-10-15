using System;

namespace Aog.Plugins.CombineYield;

/// <summary>
/// Options controlling how combine yield telemetry is aggregated and published.
/// </summary>
public sealed class CombineYieldOptions
{
    /// <summary>
    /// Gets or sets the grid cell size used for aggregating samples (meters).
    /// </summary>
    public double CellSizeMeters { get; set; } = 10.0;

    /// <summary>
    /// Gets or sets the time between automatic layer publications.
    /// </summary>
    public TimeSpan PublishInterval { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets or sets the telemetry source identifier applied to published layers.
    /// </summary>
    public string Source { get; set; } = "sim";

    /// <summary>
    /// Gets or sets the transform metadata captured in provenance records.
    /// </summary>
    public string Transform { get; set; } = "aggregate:combine-yield";

    /// <summary>
    /// Gets or sets the actor recorded in provenance entries.
    /// </summary>
    public string Actor { get; set; } = "plugin:combine-yield";

    /// <summary>
    /// Gets or sets the coordinate frame identifier for published layers.
    /// </summary>
    public string Frame { get; set; } = "vehicle";

    /// <summary>
    /// Gets or sets the crop name associated with the layer. Used for UI labeling and exports.
    /// </summary>
    public string Crop { get; set; } = "Unknown";

    /// <summary>
    /// Validates the configured option values.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the configuration is invalid.</exception>
    public void Validate()
    {
        if (double.IsNaN(CellSizeMeters) || double.IsInfinity(CellSizeMeters) || CellSizeMeters <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(CellSizeMeters), CellSizeMeters, "Cell size must be a positive finite value.");
        }

        if (PublishInterval < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(PublishInterval), PublishInterval, "Publish interval cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(Source))
        {
            throw new ArgumentOutOfRangeException(nameof(Source), Source, "Source must be provided.");
        }

        if (string.IsNullOrWhiteSpace(Transform))
        {
            throw new ArgumentOutOfRangeException(nameof(Transform), Transform, "Transform must be provided.");
        }

        if (string.IsNullOrWhiteSpace(Actor))
        {
            throw new ArgumentOutOfRangeException(nameof(Actor), Actor, "Actor must be provided.");
        }

        if (string.IsNullOrWhiteSpace(Frame))
        {
            throw new ArgumentOutOfRangeException(nameof(Frame), Frame, "Frame must be provided.");
        }

        if (string.IsNullOrWhiteSpace(Crop))
        {
            throw new ArgumentOutOfRangeException(nameof(Crop), Crop, "Crop name must be provided.");
        }
    }
}
