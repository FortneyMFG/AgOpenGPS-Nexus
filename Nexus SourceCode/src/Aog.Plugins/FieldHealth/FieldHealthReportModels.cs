using System;
using System.Collections.Generic;
using System.Linq;

namespace Aog.Plugins.FieldHealth;

/// <summary>
/// Snapshot of field health data used to prepare report sections.
/// </summary>
public sealed class FieldHealthReportSnapshot
{
    /// <summary>
    /// Initialises a new instance of the <see cref="FieldHealthReportSnapshot"/> class.
    /// </summary>
    /// <param name="layers">Field health layer metadata included in the snapshot.</param>
    /// <param name="scopeDisplayName">Optional human readable display name for the report scope.</param>
    /// <param name="summary">Optional summary text describing overall field health context.</param>
    public FieldHealthReportSnapshot(
        IEnumerable<FieldHealthLayerMetadata> layers,
        string? scopeDisplayName = null,
        string? summary = null)
    {
        ArgumentNullException.ThrowIfNull(layers);

        Layers = layers
            .Select(layer => layer ?? throw new ArgumentException("Snapshot layers cannot contain null entries.", nameof(layers)))
            .ToArray();
        ScopeDisplayName = string.IsNullOrWhiteSpace(scopeDisplayName) ? null : scopeDisplayName;
        Summary = summary;
    }

    /// <summary>
    /// Field health layers included in the snapshot.
    /// </summary>
    public IReadOnlyList<FieldHealthLayerMetadata> Layers { get; }

    /// <summary>
    /// Human readable scope display name when available.
    /// </summary>
    public string? ScopeDisplayName { get; }

    /// <summary>
    /// Optional narrative summary associated with the snapshot.
    /// </summary>
    public string? Summary { get; }
}

/// <summary>
/// Options controlling behaviour of the field health report section contributor.
/// </summary>
public sealed class FieldHealthReportSectionOptions
{
    /// <summary>
    /// Initialises a new instance of the <see cref="FieldHealthReportSectionOptions"/> class.
    /// </summary>
    /// <param name="highlightObservationCount">Number of observations to highlight per layer.</param>
    public FieldHealthReportSectionOptions(int highlightObservationCount = 3)
    {
        if (highlightObservationCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(highlightObservationCount), highlightObservationCount, "Highlight observation count must be non-negative.");
        }

        HighlightObservationCount = highlightObservationCount;
    }

    /// <summary>
    /// Default options used by the section contributor.
    /// </summary>
    public static FieldHealthReportSectionOptions Default { get; } = new();

    /// <summary>
    /// Gets the number of observations to highlight per layer.
    /// </summary>
    public int HighlightObservationCount { get; }
}

/// <summary>
/// Structured payload returned by the field health report section.
/// </summary>
/// <param name="ScopeIdentifier">Identifier of the scope used to generate the report.</param>
/// <param name="ScopeDisplayName">Optional human readable scope name.</param>
/// <param name="SeverityTotals">Aggregated severity counts across all layers.</param>
/// <param name="TotalAreaHectares">Total affected area across all layers.</param>
/// <param name="LastSurveyedAt">Timestamp of the most recent survey across all layers.</param>
/// <param name="Layers">Layer summaries included in the report.</param>
/// <param name="Summary">Optional narrative summary.</param>
public sealed record FieldHealthReportPayload(
    string ScopeIdentifier,
    string? ScopeDisplayName,
    FieldHealthSeverityCounts SeverityTotals,
    double TotalAreaHectares,
    DateTimeOffset? LastSurveyedAt,
    IReadOnlyList<FieldHealthReportLayerSummary> Layers,
    string? Summary);

/// <summary>
/// Summarises field health information for a single layer.
/// </summary>
/// <param name="Kind">Layer kind (risk.flood, risk.compaction, etc.).</param>
/// <param name="ObservationCount">Number of observations captured in the layer.</param>
/// <param name="TotalAreaHectares">Total hectares affected by the observations.</param>
/// <param name="SeverityCounts">Per-severity observation counts.</param>
/// <param name="LastSurveyedAt">Timestamp of the most recent observation.</param>
/// <param name="Notes">Layer level notes when available.</param>
/// <param name="Tags">Tags associated with the layer.</param>
/// <param name="Highlights">Highlighted observations for the layer.</param>
public sealed record FieldHealthReportLayerSummary(
    string Kind,
    int ObservationCount,
    double TotalAreaHectares,
    FieldHealthSeverityCounts SeverityCounts,
    DateTimeOffset? LastSurveyedAt,
    string? Notes,
    IReadOnlyList<string> Tags,
    IReadOnlyList<FieldHealthReportObservationSummary> Highlights);

/// <summary>
/// Highlighted observation surfaced in field health reports.
/// </summary>
/// <param name="FeatureId">Identifier of the observation feature.</param>
/// <param name="Severity">Observation severity.</param>
/// <param name="ObservedAt">Observation timestamp.</param>
/// <param name="Observer">Actor who recorded the observation.</param>
/// <param name="Status">Lifecycle status of the observation when available.</param>
/// <param name="AreaHectares">Affected area in hectares when recorded.</param>
/// <param name="Label">Optional label for the observation.</param>
/// <param name="Notes">Optional notes captured with the observation.</param>
public sealed record FieldHealthReportObservationSummary(
    string FeatureId,
    FieldHealthSeverity Severity,
    DateTimeOffset ObservedAt,
    string Observer,
    FieldHealthObservationStatus? Status,
    double? AreaHectares,
    string? Label,
    string? Notes);
