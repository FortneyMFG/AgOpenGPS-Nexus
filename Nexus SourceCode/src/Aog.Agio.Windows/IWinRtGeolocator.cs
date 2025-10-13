using System;
using System.Threading;
using System.Threading.Tasks;

namespace Aog.Agio.Windows;

/// <summary>
/// Abstraction over the Windows Runtime geolocator used to acquire location updates.
/// </summary>
public interface IWinRtGeolocator : IAsyncDisposable
{
    /// <summary>
    /// Raised whenever the Windows location service reports a new position fix.
    /// </summary>
    event EventHandler<WinRtPositionChangedEventArgs>? PositionChanged;

    /// <summary>
    /// Retrieves the most recent location fix from the underlying Windows Runtime geolocator.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The latest available geolocation fix.</returns>
    Task<WinRtGeoposition> GetGeopositionAsync(CancellationToken cancellationToken);
}
