using System;
using System.Collections.Generic;
using System.Linq;

namespace Aog.Plugins.FieldHealth;

/// <summary>
/// Encapsulates historical transitions and persisted toggle state for a field health layer.
/// </summary>
public sealed class FieldHealthLayerHistory
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FieldHealthLayerHistory"/> class.
    /// </summary>
    public FieldHealthLayerHistory(IReadOnlyList<FieldHealthHistoryEntry> entries, FieldHealthHistoryToggles toggles)
    {
        Entries = entries?.ToArray() ?? throw new ArgumentNullException(nameof(entries));
        Toggles = toggles ?? throw new ArgumentNullException(nameof(toggles));
    }

    /// <summary>
    /// Chronological history entries describing observation transitions.
    /// </summary>
    public IReadOnlyList<FieldHealthHistoryEntry> Entries { get; }

    /// <summary>
    /// Persisted history toggle selections.
    /// </summary>
    public FieldHealthHistoryToggles Toggles { get; }
}
