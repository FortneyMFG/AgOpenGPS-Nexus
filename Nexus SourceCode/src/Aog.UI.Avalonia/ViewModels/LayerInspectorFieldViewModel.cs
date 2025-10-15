using System;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Represents a labelled field surfaced within the layer inspector metadata lists.
/// </summary>
public sealed class LayerInspectorFieldViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LayerInspectorFieldViewModel"/> class.
    /// </summary>
    /// <param name="label">Human readable label for the field.</param>
    /// <param name="value">Value exposed to the operator.</param>
    /// <param name="description">Optional helper text describing the value.</param>
    public LayerInspectorFieldViewModel(string label, string value, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            throw new ArgumentException("Field label is required.", nameof(label));
        }

        ArgumentNullException.ThrowIfNull(value);

        Label = label.Trim();
        Value = value;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    /// <summary>Gets the label rendered next to the value.</summary>
    public string Label { get; }

    /// <summary>Gets the formatted value displayed in the inspector.</summary>
    public string Value { get; }

    /// <summary>Gets optional helper text describing the value.</summary>
    public string? Description { get; }

    /// <summary>Gets a value indicating whether the field exposes helper text.</summary>
    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);
}
