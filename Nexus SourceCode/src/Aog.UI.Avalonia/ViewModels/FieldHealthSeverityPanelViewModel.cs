using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Surfaces severity guidance and provenance for the field health risk layers.
/// </summary>
public sealed class FieldHealthSeverityPanelViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FieldHealthSeverityPanelViewModel"/> class.
    /// </summary>
    /// <param name="layerDisplayName">Display name for the active risk layer.</param>
    /// <param name="statusMessage">Status message summarising current severity.</param>
    /// <param name="totalAreaDisplay">Formatted display of the impacted area.</param>
    /// <param name="lastSurveyedDisplay">Formatted display of the last scouting timestamp.</param>
    /// <param name="observerDisplay">Display value describing who recorded the observations.</param>
    /// <param name="filterSummary">Summary of the persisted history filter toggles.</param>
    /// <param name="entries">Severity entries to surface inside the panel.</param>
    public FieldHealthSeverityPanelViewModel(
        string layerDisplayName,
        string statusMessage,
        string totalAreaDisplay,
        string lastSurveyedDisplay,
        string observerDisplay,
        string filterSummary,
        IEnumerable<FieldHealthSeverityEntryViewModel> entries)
    {
        if (string.IsNullOrWhiteSpace(layerDisplayName))
        {
            throw new ArgumentException("Layer display name is required.", nameof(layerDisplayName));
        }

        if (string.IsNullOrWhiteSpace(statusMessage))
        {
            throw new ArgumentException("Status message is required.", nameof(statusMessage));
        }

        if (string.IsNullOrWhiteSpace(totalAreaDisplay))
        {
            throw new ArgumentException("Total area display is required.", nameof(totalAreaDisplay));
        }

        if (string.IsNullOrWhiteSpace(lastSurveyedDisplay))
        {
            throw new ArgumentException("Last surveyed display is required.", nameof(lastSurveyedDisplay));
        }

        if (string.IsNullOrWhiteSpace(observerDisplay))
        {
            throw new ArgumentException("Observer display is required.", nameof(observerDisplay));
        }

        if (string.IsNullOrWhiteSpace(filterSummary))
        {
            throw new ArgumentException("Filter summary is required.", nameof(filterSummary));
        }

        ArgumentNullException.ThrowIfNull(entries);

        LayerDisplayName = layerDisplayName.Trim();
        StatusMessage = statusMessage.Trim();
        TotalAreaDisplay = totalAreaDisplay.Trim();
        LastSurveyedDisplay = lastSurveyedDisplay.Trim();
        ObserverDisplay = observerDisplay.Trim();
        FilterSummary = filterSummary.Trim();
        Entries = entries.ToArray();
    }

    /// <summary>Gets the display name for the active risk layer.</summary>
    public string LayerDisplayName { get; }

    /// <summary>Gets the status message summarising the current severity mix.</summary>
    public string StatusMessage { get; }

    /// <summary>Gets the formatted display for the impacted area.</summary>
    public string TotalAreaDisplay { get; }

    /// <summary>Gets the formatted display for the last scouting timestamp.</summary>
    public string LastSurveyedDisplay { get; }

    /// <summary>Gets the display value describing who recorded the observations.</summary>
    public string ObserverDisplay { get; }

    /// <summary>Gets the summary of the persisted history filter toggles.</summary>
    public string FilterSummary { get; }

    /// <summary>Gets the severity entries surfaced inside the panel.</summary>
    public IReadOnlyList<FieldHealthSeverityEntryViewModel> Entries { get; }

    /// <summary>Gets a value indicating whether any severity entries are present.</summary>
    public bool HasEntries => Entries.Count > 0;

    /// <summary>
    /// Creates a sample field health severity panel used by the design-time shell.
    /// </summary>
    public static FieldHealthSeverityPanelViewModel CreateSample()
    {
        var entries = new[]
        {
            new FieldHealthSeverityEntryViewModel(
                severity: "Critical",
                displayName: "Critical – Standing water",
                summary: "Flooded rows with pooled water. Crop at risk without drainage.",
                recommendedAction: "Halt application and dispatch drainage crew before resuming field operations.",
                swatchColor: Color.FromArgb(0xFF, 0xD3, 0x2F, 0x2F),
                areaImpactDisplay: "2 zones · 3.8 ha",
                notes: "Drainage tiles overwhelmed after overnight storm. Headlands impassable."),
            new FieldHealthSeverityEntryViewModel(
                severity: "High",
                displayName: "High – Saturated wheel tracks",
                summary: "Surface saturation causing rutting risk and traction loss.",
                recommendedAction: "Delay heavy equipment entry and plan controlled traffic to limit compaction.",
                swatchColor: Color.FromArgb(0xFF, 0xFF, 0x8F, 0x00),
                areaImpactDisplay: "3 zones · 1.9 ha"),
            new FieldHealthSeverityEntryViewModel(
                severity: "Moderate",
                displayName: "Moderate – Weed pressure",
                summary: "Scouting reports emerging weed escapes along south edge.",
                recommendedAction: "Schedule targeted herbicide pass and monitor escape counts over next session.",
                swatchColor: Color.FromArgb(0xFF, 0xFF, 0xC1, 0x07),
                areaImpactDisplay: "4 zones · 1.1 ha",
                notes: "Escapes concentrated where sprayer boom lost pressure on prior pass."),
            new FieldHealthSeverityEntryViewModel(
                severity: "Low",
                displayName: "Low – Compaction watch",
                summary: "Moisture readings elevated but within acceptable tolerance.",
                recommendedAction: "Keep auto-notes active and review planter downforce trends during next run.",
                swatchColor: Color.FromArgb(0xFF, 0x4C, 0xAF, 0x50)),
            new FieldHealthSeverityEntryViewModel(
                severity: "None",
                displayName: "None – No active issues",
                summary: "Remaining zones report normal moisture and canopy vigour.",
                recommendedAction: "Continue planned operations. No follow-up required.",
                swatchColor: Color.FromArgb(0xFF, 0x60, 0x7D, 0x8B)),
        };

        return new FieldHealthSeverityPanelViewModel(
            layerDisplayName: "Flood risk (NE lowland)",
            statusMessage: "Monitor critical flooding risk before resuming application.",
            totalAreaDisplay: "7.2 ha impacted",
            lastSurveyedDisplay: "Last scouted 2025-04-02 15:20 UTC",
            observerDisplay: "Observed by agronomy:samantha",
            filterSummary: "Filters: Active + Monitor observations",
            entries);
    }
}
