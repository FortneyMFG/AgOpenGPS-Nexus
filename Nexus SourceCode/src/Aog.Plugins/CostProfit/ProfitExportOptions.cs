using System;

namespace Aog.Plugins.CostProfit;

/// <summary>
/// Configuration applied when exporting profit layers and summaries.
/// </summary>
public sealed class ProfitExportOptions
{
    /// <summary>
    /// Gets or sets the identifier applied to generated layers.
    /// </summary>
    public string LayerId { get; set; } = "profit.layer";

    /// <summary>
    /// Gets or sets the layer kind metadata (e.g., ProfitLayer.v1).
    /// </summary>
    public string LayerKind { get; set; } = "ProfitLayer.v1";

    /// <summary>
    /// Gets or sets the units associated with profit cell values.
    /// </summary>
    public string Units { get; set; } = "USD/ha";

    /// <summary>
    /// Gets or sets the provenance source identifier.
    /// </summary>
    public string Source { get; set; } = "plugin:cost-profit";

    /// <summary>
    /// Gets or sets the provenance transform description.
    /// </summary>
    public string Transform { get; set; } = "profit/export/v1";

    /// <summary>
    /// Gets or sets the actor captured in provenance metadata.
    /// </summary>
    public string Actor { get; set; } = "plugin:cost-profit";

    /// <summary>
    /// Gets or sets the author recorded on created layer documents.
    /// </summary>
    public string CreatedBy { get; set; } = "plugin:cost-profit";

    /// <summary>
    /// Validates option values and throws when required fields are missing.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(LayerId))
        {
            throw new InvalidOperationException("LayerId must be provided.");
        }

        if (string.IsNullOrWhiteSpace(LayerKind))
        {
            throw new InvalidOperationException("LayerKind must be provided.");
        }

        if (string.IsNullOrWhiteSpace(Units))
        {
            throw new InvalidOperationException("Units must be provided.");
        }

        if (string.IsNullOrWhiteSpace(Source))
        {
            throw new InvalidOperationException("Source must be provided.");
        }

        if (string.IsNullOrWhiteSpace(Transform))
        {
            throw new InvalidOperationException("Transform must be provided.");
        }

        if (string.IsNullOrWhiteSpace(Actor))
        {
            throw new InvalidOperationException("Actor must be provided.");
        }

        if (string.IsNullOrWhiteSpace(CreatedBy))
        {
            throw new InvalidOperationException("CreatedBy must be provided.");
        }
    }
}
