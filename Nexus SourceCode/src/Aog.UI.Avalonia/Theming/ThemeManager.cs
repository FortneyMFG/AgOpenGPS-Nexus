using System;
using Avalonia;
using Avalonia.Styling;
using Avalonia.Threading;
using Aog.UI.Avalonia.Settings;

namespace Aog.UI.Avalonia.Theming;

/// <summary>
/// Applies theme changes to the Avalonia application.
/// </summary>
public sealed class ThemeManager : IThemeManager
{
    private UiTheme _currentTheme = UiTheme.Light;

    /// <inheritdoc />
    public event EventHandler<UiTheme>? ThemeChanged;

    /// <inheritdoc />
    public UiTheme CurrentTheme => _currentTheme;

    /// <inheritdoc />
    public void ApplyTheme(UiTheme theme)
    {
        var changed = _currentTheme != theme;
        _currentTheme = theme;

        void Apply()
        {
            if (Application.Current is { } app)
            {
                app.RequestedThemeVariant = theme switch
                {
                    UiTheme.Dark => ThemeVariant.Dark,
                    _ => ThemeVariant.Light,
                };
            }
        }

        if (Dispatcher.UIThread.CheckAccess())
        {
            Apply();
        }
        else
        {
            Dispatcher.UIThread.Post(Apply);
        }

        if (changed)
        {
            ThemeChanged?.Invoke(this, _currentTheme);
        }
    }
}
