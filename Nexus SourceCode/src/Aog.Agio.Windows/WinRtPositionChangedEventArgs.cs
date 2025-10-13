using System;

namespace Aog.Agio.Windows;

/// <summary>
/// Event payload emitted when the Windows Runtime geolocator produces a new fix.
/// </summary>
public sealed class WinRtPositionChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WinRtPositionChangedEventArgs"/> class.
    /// </summary>
    /// <param name="position">The updated geolocation fix.</param>
    public WinRtPositionChangedEventArgs(WinRtGeoposition position)
    {
        Position = position;
    }

    /// <summary>
    /// Gets the position associated with the event.
    /// </summary>
    public WinRtGeoposition Position { get; }
}
