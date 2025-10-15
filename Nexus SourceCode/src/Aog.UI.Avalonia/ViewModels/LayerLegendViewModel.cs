using System;
using System.Collections.Generic;
using System.Linq;
using Aog.UI.Avalonia.Models;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Aggregates legend entries generated from the registered map layers.
/// </summary>
public sealed class LayerLegendViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LayerLegendViewModel"/> class.
    /// </summary>
    /// <param name="entries">Legend entries to expose.</param>
    public LayerLegendViewModel(IEnumerable<LayerLegendEntryViewModel> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        Entries = entries.ToArray();
    }

    /// <summary>Gets the entries rendered in the legend control.</summary>
    public IReadOnlyList<LayerLegendEntryViewModel> Entries { get; }

    /// <summary>Gets a value indicating whether the legend currently contains entries.</summary>
    public bool HasEntries => Entries.Count > 0;

    /// <summary>
    /// Builds a <see cref="LayerLegendViewModel"/> from the supplied map layers.
    /// </summary>
    /// <param name="layers">Map layers to materialise into legend entries.</param>
    public static LayerLegendViewModel FromLayers(IEnumerable<MapLayer> layers)
    {
        ArgumentNullException.ThrowIfNull(layers);

        var legendEntries = layers
            .Select(layer => new LayerLegendEntryViewModel(
                layer.LayerId,
                layer.DisplayName,
                layer.Style.MinimumValue,
                layer.Style.MaximumValue,
                layer.Style.Units,
                layer.Style.GradientStart,
                layer.Style.GradientEnd,
                layer.Style.IsPlanned,
                layer.Description ?? (layer.Style.IsPlanned
                    ? "Target metadata sourced from the prescription controller."
                    : "Live rate samples aggregated from the section controller.")))
            .ToArray();

        return new LayerLegendViewModel(legendEntries);
    }
}
