using System;

namespace Aog.Plugins.Genetics;

/// <summary>
/// Configuration options controlling how the genetics layer ingest pipeline materializes plan and variety entries.
/// </summary>
public sealed class GeneticsLayerIngestOptions
{
    private string _planLayerId = "layer:genetics.plan";
    private string _varietyLayerId = "layer:genetics.variety";
    private string _defaultActor = "plugin:genetics";
    private string _defaultSource = "plugin:genetics";

    /// <summary>
    /// Gets or sets the layer identifier used for planned genetics features.
    /// </summary>
    public string PlanLayerId
    {
        get => _planLayerId;
        set => _planLayerId = !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : throw new ArgumentException("Plan layer identifier is required.", nameof(value));
    }

    /// <summary>
    /// Gets or sets the layer identifier used for as-applied genetics features.
    /// </summary>
    public string VarietyLayerId
    {
        get => _varietyLayerId;
        set => _varietyLayerId = !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : throw new ArgumentException("Variety layer identifier is required.", nameof(value));
    }

    /// <summary>
    /// Gets or sets the actor recorded when ingest requests omit the <c>createdBy</c> field.
    /// </summary>
    public string DefaultActor
    {
        get => _defaultActor;
        set => _defaultActor = !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : throw new ArgumentException("Default actor is required.", nameof(value));
    }

    /// <summary>
    /// Gets or sets the source recorded when ingest requests omit the <c>source</c> field.
    /// </summary>
    public string DefaultSource
    {
        get => _defaultSource;
        set => _defaultSource = !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : throw new ArgumentException("Default source is required.", nameof(value));
    }

    /// <summary>
    /// Validates the option values.
    /// </summary>
    public void Validate()
    {
        _ = PlanLayerId;
        _ = VarietyLayerId;
        _ = DefaultActor;
        _ = DefaultSource;
    }
}
