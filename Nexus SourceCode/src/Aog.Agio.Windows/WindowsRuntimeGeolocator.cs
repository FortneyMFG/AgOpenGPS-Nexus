using System;
using System.Threading;
using System.Threading.Tasks;
#if WINDOWS
using Windows.Devices.Geolocation;
using Windows.Foundation;
#endif

namespace Aog.Agio.Windows;

/// <summary>
/// Factory helpers for creating Windows Runtime geolocator adapters.
/// </summary>
public static class WindowsRuntimeGeolocator
{
    /// <summary>
    /// Creates a Windows Runtime backed geolocator adapter.
    /// </summary>
    /// <param name="movementThresholdMeters">Minimum movement required before the Windows API issues an update.</param>
    /// <param name="reportInterval">Optional interval hint for the Windows API.</param>
    /// <returns>An <see cref="IWinRtGeolocator"/> implementation.</returns>
    /// <exception cref="PlatformNotSupportedException">Thrown when the current OS does not expose the Windows Runtime location APIs.</exception>
    public static IWinRtGeolocator Create(double movementThresholdMeters = 1.0, TimeSpan? reportInterval = null)
    {
#if WINDOWS
        return GeolocatorAdapter.Create(movementThresholdMeters, reportInterval);
#else
        throw new PlatformNotSupportedException("Windows Runtime geolocator is only available on Windows.");
#endif
    }

#if WINDOWS
    private sealed class GeolocatorAdapter : IWinRtGeolocator
    {
        private readonly Geolocator _inner;

        private GeolocatorAdapter(Geolocator inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _inner.PositionChanged += OnPositionChanged;
        }

        public event EventHandler<WinRtPositionChangedEventArgs>? PositionChanged;

        public static GeolocatorAdapter Create(double movementThresholdMeters, TimeSpan? reportInterval)
        {
            var geolocator = new Geolocator
            {
                MovementThreshold = movementThresholdMeters,
            };

            if (reportInterval.HasValue)
            {
                var clamped = Math.Clamp(reportInterval.Value.TotalMilliseconds, 0, uint.MaxValue);
                geolocator.ReportInterval = (uint)clamped;
            }

            return new GeolocatorAdapter(geolocator);
        }

        public async Task<WinRtGeoposition> GetGeopositionAsync(CancellationToken cancellationToken)
        {
            using var registration = cancellationToken.Register(static state =>
            {
                var locator = (Geolocator)state!;
                locator?.CancelGetPositionAsync();
            }, _inner);

            var position = await _inner.GetGeopositionAsync().AsTask(cancellationToken).ConfigureAwait(false);
            return Convert(position);
        }

        public ValueTask DisposeAsync()
        {
            _inner.PositionChanged -= OnPositionChanged;
            return ValueTask.CompletedTask;
        }

        private void OnPositionChanged(Geolocator sender, PositionChangedEventArgs args)
        {
            var converted = Convert(args.Position);
            PositionChanged?.Invoke(this, new WinRtPositionChangedEventArgs(converted));
        }

        private static WinRtGeoposition Convert(Geoposition position)
        {
            if (position is null)
            {
                throw new ArgumentNullException(nameof(position));
            }

            var coordinate = position.Coordinate;
            var basic = coordinate.Point?.Position ?? new BasicGeoposition
            {
                Latitude = coordinate.Latitude,
                Longitude = coordinate.Longitude,
                Altitude = coordinate.Altitude ?? double.NaN,
            };

            var snapshot = new WinRtGeocoordinate(
                basic.Latitude,
                basic.Longitude,
                coordinate.Altitude,
                coordinate.Accuracy,
                coordinate.Heading,
                coordinate.Speed);

            return new WinRtGeoposition(coordinate.Timestamp, snapshot);
        }
    }
#endif
}
