using System;
using Aog.UI.Avalonia.Hosting;

namespace Aog.UI.Avalonia.Settings;

/// <summary>
/// Provides access to the persisted UI preferences.
/// </summary>
public interface IUiPreferencesService
{
    /// <summary>Gets a snapshot of the current preferences.</summary>
    UiPreferences GetPreferences();

    /// <summary>Updates the persisted theme preference.</summary>
    /// <param name="theme">The theme to persist.</param>
    void UpdateTheme(UiTheme theme);

    /// <summary>Updates the persisted window placement.</summary>
    /// <param name="placement">The placement to persist.</param>
    void UpdateWindowPlacement(WindowPlacement placement);

    /// <summary>Updates the telemetry opt-in flag.</summary>
    /// <param name="isOptedIn">Whether telemetry uploads are enabled.</param>
    void UpdateTelemetryOptIn(bool isOptedIn);

    /// <summary>Updates the persisted run mode preference.</summary>
    /// <param name="mode">The run mode to record.</param>
    void UpdateRunMode(AvaloniaRunMode mode);
}
