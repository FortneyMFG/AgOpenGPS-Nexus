using System;
using System.Globalization;
using System.Linq;
using Aog.Core.Layers;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Presentation model that summarizes a committed layer edit event for display in the toolbar.
/// </summary>
public sealed class ZoneEditorJournalEntryViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ZoneEditorJournalEntryViewModel"/> class.
    /// </summary>
    /// <param name="sequence">Sequence number within the current edit session.</param>
    /// <param name="entry">Layer edit entry returned by the journal.</param>
    public ZoneEditorJournalEntryViewModel(int sequence, LayerEditEventEntry entry)
    {
        Sequence = sequence;
        Entry = entry ?? throw new ArgumentNullException(nameof(entry));
        Tool = entry.Tool;
        Timestamp = entry.CreatedAt;
        OperationSummary = BuildOperationSummary(entry);
        DetailSummary = BuildDetailSummary(entry);
    }

    /// <summary>Gets the sequential identifier used in the UI.</summary>
    public int Sequence { get; }

    /// <summary>Gets the raw layer edit entry.</summary>
    public LayerEditEventEntry Entry { get; }

    /// <summary>Gets the tool used to produce the edit.</summary>
    public string Tool { get; }

    /// <summary>Gets the timestamp recorded by the journal.</summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>Gets a concise summary of the operations contained in the entry.</summary>
    public string OperationSummary { get; }

    /// <summary>Gets a human readable description of geometry/attribute changes.</summary>
    public string DetailSummary { get; }

    private static string BuildOperationSummary(LayerEditEventEntry entry)
    {
        var operation = entry.Operations.FirstOrDefault();
        if (operation is null)
        {
            return "No operations";
        }

        var action = operation.Type switch
        {
            "create" => "Created",
            "update" => "Updated",
            "delete" => "Deleted",
            "merge" => "Merged",
            "split" => "Split",
            _ => operation.Type,
        };

        return $"{action} {operation.FeatureId}";
    }

    private static string BuildDetailSummary(LayerEditEventEntry entry)
    {
        var operation = entry.Operations.FirstOrDefault();
        if (operation is null)
        {
            return "No operation details captured.";
        }

        var detail = operation.Summary;
        if (detail is null)
        {
            return "Geometry recorded without quantitative summary.";
        }

        var area = detail.AreaDeltaSqMeters.HasValue
            ? detail.AreaDeltaSqMeters.Value.ToString("N0", CultureInfo.InvariantCulture)
            : "0";
        var perimeter = detail.PerimeterDeltaMeters.HasValue
            ? detail.PerimeterDeltaMeters.Value.ToString("N1", CultureInfo.InvariantCulture)
            : "0.0";

        var changedAttributes = detail.ChangedAttributes.Count > 0
            ? string.Join(", ", detail.ChangedAttributes)
            : "none";

        return $"ΔArea {area} m², ΔPerimeter {perimeter} m, attrs: {changedAttributes}";
    }
}
