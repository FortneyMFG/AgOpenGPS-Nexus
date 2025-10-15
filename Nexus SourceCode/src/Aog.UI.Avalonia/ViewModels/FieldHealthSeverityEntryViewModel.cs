using System;
using Avalonia.Media;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Represents a single severity level surfaced inside the field health panel.
/// </summary>
public sealed class FieldHealthSeverityEntryViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FieldHealthSeverityEntryViewModel"/> class.
    /// </summary>
    /// <param name="severity">Short label describing the severity classification.</param>
    /// <param name="displayName">Human readable name rendered for the severity entry.</param>
    /// <param name="summary">Short summary of what the severity means in the current context.</param>
    /// <param name="recommendedAction">Recommended operator action for the severity.</param>
    /// <param name="swatchColor">Colour used for the severity swatch.</param>
    /// <param name="areaImpactDisplay">Optional formatted display of the affected area/count.</param>
    /// <param name="notes">Optional supplemental notes displayed below the entry.</param>
    public FieldHealthSeverityEntryViewModel(
        string severity,
        string displayName,
        string summary,
        string recommendedAction,
        Color swatchColor,
        string? areaImpactDisplay = null,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(severity))
        {
            throw new ArgumentException("Severity label is required.", nameof(severity));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(summary))
        {
            throw new ArgumentException("Summary is required.", nameof(summary));
        }

        if (string.IsNullOrWhiteSpace(recommendedAction))
        {
            throw new ArgumentException("Recommended action is required.", nameof(recommendedAction));
        }

        Severity = severity.Trim();
        DisplayName = displayName.Trim();
        Summary = summary.Trim();
        RecommendedAction = recommendedAction.Trim();
        AreaImpactDisplay = string.IsNullOrWhiteSpace(areaImpactDisplay) ? null : areaImpactDisplay.Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        Color = swatchColor;
        SwatchBrush = new SolidColorBrush(swatchColor);
    }

    /// <summary>Gets the severity classification label.</summary>
    public string Severity { get; }

    /// <summary>Gets the display name rendered for the severity entry.</summary>
    public string DisplayName { get; }

    /// <summary>Gets a short summary describing the severity.</summary>
    public string Summary { get; }

    /// <summary>Gets the recommended action for operators.</summary>
    public string RecommendedAction { get; }

    /// <summary>Gets an optional formatted display of the affected area or observation count.</summary>
    public string? AreaImpactDisplay { get; }

    /// <summary>Gets optional supplemental notes that expand on the severity.</summary>
    public string? Notes { get; }

    /// <summary>Gets the colour used for the severity swatch.</summary>
    public Color Color { get; }

    /// <summary>Gets a brush used to render the severity swatch in the UI.</summary>
    public IBrush SwatchBrush { get; }

    /// <summary>Gets a value indicating whether the entry includes affected area metadata.</summary>
    public bool HasAreaImpact => !string.IsNullOrWhiteSpace(AreaImpactDisplay);

    /// <summary>Gets a value indicating whether the entry includes supplemental notes.</summary>
    public bool HasNotes => !string.IsNullOrWhiteSpace(Notes);
}
