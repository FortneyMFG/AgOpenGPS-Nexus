using System;
using Aog.UI.Avalonia.Settings;

namespace Aog.UI.Avalonia.Theming;

/// <summary>
/// Coordinates application theme changes.
/// </summary>
public interface IThemeManager
{
    /// <summary>Gets the currently applied theme.</summary>
    UiTheme CurrentTheme { get; }

    /// <summary>Raised when the application theme changes.</summary>
    event EventHandler<UiTheme>? ThemeChanged;

    /// <summary>Applies the specified theme to the running application.
    /// </summary>
    /// <param name="theme">The theme to apply.</param>
    void ApplyTheme(UiTheme theme);
}
