using System;

namespace Aog.Plugins.FieldHealth;

/// <summary>
/// Persisted visibility toggles for field health observation history.
/// </summary>
public sealed record FieldHealthHistoryToggles
{
    /// <summary>
    /// Default toggle state used when no explicit preference has been provided.
    /// </summary>
    public static FieldHealthHistoryToggles Default { get; } = new(true, true, false);

    /// <summary>
    /// Initializes a new instance of the <see cref="FieldHealthHistoryToggles"/> record.
    /// </summary>
    public FieldHealthHistoryToggles(bool showActive, bool showMonitor, bool showResolved)
    {
        if (!showActive && !showMonitor && !showResolved)
        {
            throw new ArgumentException("At least one history toggle must be enabled.");
        }

        ShowActive = showActive;
        ShowMonitor = showMonitor;
        ShowResolved = showResolved;
    }

    /// <summary>
    /// Gets a value indicating whether active observations should be shown by default.
    /// </summary>
    public bool ShowActive { get; init; }

    /// <summary>
    /// Gets a value indicating whether observations marked for monitoring should be visible.
    /// </summary>
    public bool ShowMonitor { get; init; }

    /// <summary>
    /// Gets a value indicating whether resolved observations should remain visible.
    /// </summary>
    public bool ShowResolved { get; init; }
}
