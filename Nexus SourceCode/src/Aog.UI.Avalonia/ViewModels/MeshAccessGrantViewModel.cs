using System;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Describes a mesh share or subscribe grant shown in the mesh access panel.
/// </summary>
public sealed class MeshAccessGrantViewModel
{
    public MeshAccessGrantViewModel(string scopeDisplay, string tierDisplay, string layerDisplay, string? note = null)
    {
        ScopeDisplay = scopeDisplay ?? throw new ArgumentNullException(nameof(scopeDisplay));
        TierDisplay = tierDisplay ?? throw new ArgumentNullException(nameof(tierDisplay));
        LayerDisplay = layerDisplay ?? throw new ArgumentNullException(nameof(layerDisplay));
        Note = note;
    }

    /// <summary>Gets a text description of the season/job scope.</summary>
    public string ScopeDisplay { get; }

    /// <summary>Gets a formatted description of the tier(s) granted.</summary>
    public string TierDisplay { get; }

    /// <summary>Gets the layer namespaces included in the grant.</summary>
    public string LayerDisplay { get; }

    /// <summary>Gets an optional note with additional context.</summary>
    public string? Note { get; }

    /// <summary>Gets a value indicating whether a note is present.</summary>
    public bool HasNote => !string.IsNullOrWhiteSpace(Note);
}
