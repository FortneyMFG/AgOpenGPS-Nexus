using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.V1;
using Google.Protobuf.WellKnownTypes;

namespace Aog.Agio.Windows;

/// <summary>
/// Converts Windows Location API readings into <see cref="Pose"/> messages.
/// </summary>
public sealed class WindowsLocationPosePublisher : IAsyncDisposable
{
    private readonly IWinRtGeolocator _geolocator;
    private readonly Func<Pose, CancellationToken, ValueTask> _publish;
    private readonly WindowsLocationPoseOptions _options;
    private long _sequenceCounter;
    private bool _started;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowsLocationPosePublisher"/> class.
    /// </summary>
    /// <param name="geolocator">Wrapped Windows Runtime geolocator.</param>
    /// <param name="publish">Delegate invoked when a new pose should be emitted.</param>
    /// <param name="options">Optional publishing configuration.</param>
    public WindowsLocationPosePublisher(
        IWinRtGeolocator geolocator,
        Func<Pose, CancellationToken, ValueTask> publish,
        WindowsLocationPoseOptions? options = null)
    {
        _geolocator = geolocator ?? throw new ArgumentNullException(nameof(geolocator));
        _publish = publish ?? throw new ArgumentNullException(nameof(publish));
        _options = options ?? new WindowsLocationPoseOptions();

        if (_options.StartingSequence > long.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Starting sequence cannot exceed Int64.MaxValue.");
        }

        _sequenceCounter = unchecked((long)_options.StartingSequence);
    }

    /// <summary>
    /// Starts watching the Windows Location API and publishing pose messages.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the initial geolocation query.</param>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        if (_started)
        {
            throw new InvalidOperationException("Publisher has already been started.");
        }

        try
        {
            var initial = await _geolocator.GetGeopositionAsync(cancellationToken).ConfigureAwait(false);
            await PublishAsync(initial, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            throw;
        }

        _started = true;
        _geolocator.PositionChanged += HandlePositionChanged;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _geolocator.PositionChanged -= HandlePositionChanged;
        await _geolocator.DisposeAsync().ConfigureAwait(false);
    }

    private void HandlePositionChanged(object? sender, WinRtPositionChangedEventArgs args)
    {
        if (!_started)
        {
            return;
        }

        // Fire-and-forget while ensuring exceptions propagate to the thread pool.
        var publish = PublishAsync(args.Position, CancellationToken.None);
        if (!publish.IsCompletedSuccessfully)
        {
            _ = publish.AsTask();
        }
    }

    private async ValueTask PublishAsync(WinRtGeoposition position, CancellationToken cancellationToken)
    {
        var pose = CreatePose(position);
        await _publish(pose, cancellationToken).ConfigureAwait(false);
    }

    private Pose CreatePose(WinRtGeoposition position)
    {
        var coordinate = position.Coordinate;
        var header = new Header
        {
            Sequence = NextSequence(),
            Timestamp = Timestamp.FromDateTimeOffset(position.Timestamp),
            Frame = _options.Frame,
            Source = _options.Source,
        };

        return new Pose
        {
            Header = header,
            LatitudeDeg = coordinate.Latitude,
            LongitudeDeg = coordinate.Longitude,
            AltitudeM = ResolveAltitude(coordinate.Altitude),
            HeadingRad = ToRadians(coordinate.HeadingDeg),
            RollRad = 0,
            PitchRad = 0,
            SpeedMps = ResolveNumeric(coordinate.SpeedMps),
            YawRateRadps = 0,
        };
    }

    private double ResolveAltitude(double? altitude)
    {
        if (!altitude.HasValue || double.IsNaN(altitude.Value))
        {
            return _options.DefaultAltitudeM;
        }

        return altitude.Value;
    }

    private static double ResolveNumeric(double? value)
    {
        if (!value.HasValue || double.IsNaN(value.Value))
        {
            return 0;
        }

        return value.Value;
    }

    private static double ToRadians(double? headingDeg)
    {
        if (!headingDeg.HasValue || double.IsNaN(headingDeg.Value))
        {
            return 0;
        }

        return headingDeg.Value * Math.PI / 180d;
    }

    private ulong NextSequence()
    {
        var next = Interlocked.Increment(ref _sequenceCounter) - 1;
        if (next < 0)
        {
            throw new InvalidOperationException("Sequence counter exceeded Int64.MaxValue.");
        }

        return unchecked((ulong)next);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(WindowsLocationPosePublisher));
        }
    }
}
