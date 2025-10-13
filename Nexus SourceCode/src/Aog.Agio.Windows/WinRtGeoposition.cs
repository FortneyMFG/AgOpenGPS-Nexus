using System;

namespace Aog.Agio.Windows;

/// <summary>
/// Bundles a coordinate snapshot with the timestamp provided by Windows Location services.
/// </summary>
/// <param name="Timestamp">Timestamp supplied by the Windows Runtime fix.</param>
/// <param name="Coordinate">Coordinate data associated with the fix.</param>
public readonly record struct WinRtGeoposition(
    DateTimeOffset Timestamp,
    WinRtGeocoordinate Coordinate);
