using Avalonia.Controls;

namespace Aog.UI.Avalonia.Settings;

/// <summary>
/// Stores window placement information for restoring the shell layout.
/// </summary>
public sealed class WindowPlacement
{
    /// <summary>Gets or sets the window width in device independent pixels.</summary>
    public double Width { get; set; } = 1000;

    /// <summary>Gets or sets the window height in device independent pixels.</summary>
    public double Height { get; set; } = 700;

    /// <summary>Gets or sets the X coordinate of the window in device pixels.</summary>
    public int? X { get; set; }

    /// <summary>Gets or sets the Y coordinate of the window in device pixels.</summary>
    public int? Y { get; set; }

    /// <summary>Gets or sets the window state.</summary>
    public WindowState WindowState { get; set; } = WindowState.Normal;

    /// <summary>Creates a deep copy of the placement.</summary>
    public WindowPlacement Clone() => new()
    {
        Width = Width,
        Height = Height,
        X = X,
        Y = Y,
        WindowState = WindowState,
    };
}
