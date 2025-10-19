using System.Collections.Generic;
using Aog.UI.Avalonia.Models;
using Aog.UI.Avalonia.Settings;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Represents the data required by the system summary dialog.
/// </summary>
public interface ISystemSummaryViewModel
{
    /// <summary>Gets the description of the current runtime platform.</summary>
    string PlatformDescription { get; }

    /// <summary>Gets the map layers displayed in the dialog.</summary>
    IReadOnlyList<MapLayer> MapLayers { get; }

    /// <summary>Gets the guidance tracks available on the map.</summary>
    IReadOnlyList<GuidanceTrack> GuidanceTracks { get; }

    /// <summary>Gets the available UI themes.</summary>
    IReadOnlyList<UiTheme> AvailableThemes { get; }

    /// <summary>Gets or sets the selected UI theme.</summary>
    UiTheme SelectedTheme { get; set; }
}
