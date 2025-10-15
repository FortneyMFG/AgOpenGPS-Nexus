using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Eventing;
using Aog.Core.Paths;
using Aog.Core.V1;
using Google.Protobuf.WellKnownTypes;

namespace Aog.Core.Layers.Controllers;

/// <summary>
/// Immutable pose frame exposed to layer controller ingestion pipelines.
/// </summary>
/// <param name="Pose">Raw PoseStream message.</param>
/// <param name="Timestamp">Timestamp associated with the pose.</param>
/// <param name="Elapsed">Duration since the previous pose frame.</param>
/// <param name="Position">Planar position relative to the stream origin.</param>
/// <param name="PreviousPosition">Previous planar position when available.</param>
public readonly record struct LayerControllerPoseFrame(
    Pose Pose,
    DateTimeOffset Timestamp,
    TimeSpan Elapsed,
    PlanarPoint Position,
    PlanarPoint? PreviousPosition);

/// <summary>
/// Subscribes to the shared PoseStream and exposes planar pose frames to layer controllers.
/// </summary>
public sealed class PoseStreamIngestionService : ILayerControllerPoseStream, IDisposable
{
    private const double EarthRadiusMeters = 6_378_137d;

    private readonly IEventBus _eventBus;
    private readonly TimeProvider _timeProvider;
    private readonly IDisposable _subscription;
    private readonly object _gate = new();
    private readonly List<Subscription> _subscribers = new();

    private bool _disposed;
    private LayerControllerPoseFrame? _latest;
    private DateTimeOffset? _lastTimestamp;
    private PlanarPoint? _lastPosition;
    private double? _originLatitudeRadians;
    private double? _originLongitudeRadians;

    /// <summary>
    /// Initializes a new instance of the <see cref="PoseStreamIngestionService"/> class.
    /// </summary>
    /// <param name="eventBus">Event bus sourcing PoseStream messages.</param>
    /// <param name="timeProvider">Optional time provider used when pose timestamps are absent.</param>
    public PoseStreamIngestionService(IEventBus eventBus, TimeProvider? timeProvider = null)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _timeProvider = timeProvider ?? TimeProvider.System;
        _subscription = _eventBus.Subscribe<Pose>(OnPoseAsync);
    }

    /// <inheritdoc />
    public IDisposable Subscribe(Func<LayerControllerPoseFrame, CancellationToken, ValueTask> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ThrowIfDisposed();

        var subscription = new Subscription(handler);

        lock (_gate)
        {
            _subscribers.Add(subscription);
        }

        return new SubscriptionHandle(this, subscription);
    }

    /// <inheritdoc />
    public bool TryGetLatestFrame(out LayerControllerPoseFrame frame)
    {
        ThrowIfDisposed();

        lock (_gate)
        {
            if (_latest is { } latest)
            {
                frame = latest;
                return true;
            }
        }

        frame = default;
        return false;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _subscription.Dispose();

        lock (_gate)
        {
            _subscribers.Clear();
            _latest = null;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private async ValueTask OnPoseAsync(Pose pose, CancellationToken cancellationToken)
    {
        if (_disposed)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(pose);

        LayerControllerPoseFrame frame;
        Subscription[] subscribers;

        lock (_gate)
        {
            var timestamp = ExtractTimestamp(pose);
            var elapsed = _lastTimestamp.HasValue
                ? timestamp - _lastTimestamp.Value
                : TimeSpan.Zero;

            if (elapsed < TimeSpan.Zero)
            {
                elapsed = TimeSpan.Zero;
            }

            var position = ComputePlanarPosition(pose.LatitudeDeg, pose.LongitudeDeg);
            var previousPosition = _lastPosition;

            frame = new LayerControllerPoseFrame(pose, timestamp, elapsed, position, previousPosition);

            _lastTimestamp = timestamp;
            _lastPosition = position;
            _latest = frame;
            subscribers = _subscribers.ToArray();
        }

        foreach (var subscriber in subscribers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await subscriber.Handler(frame, cancellationToken).ConfigureAwait(false);
        }
    }

    private DateTimeOffset ExtractTimestamp(Pose pose)
    {
        var timestamp = pose.Header?.Timestamp;
        if (timestamp is not null)
        {
            return timestamp.ToDateTimeOffset();
        }

        return _timeProvider.GetUtcNow();
    }

    private PlanarPoint ComputePlanarPosition(double latitudeDeg, double longitudeDeg)
    {
        if (!double.IsFinite(latitudeDeg) || latitudeDeg < -90 || latitudeDeg > 90)
        {
            throw new ArgumentOutOfRangeException(nameof(latitudeDeg), latitudeDeg, "Latitude must be within [-90, 90] degrees.");
        }

        if (!double.IsFinite(longitudeDeg) || longitudeDeg < -180 || longitudeDeg > 180)
        {
            throw new ArgumentOutOfRangeException(nameof(longitudeDeg), longitudeDeg, "Longitude must be within [-180, 180] degrees.");
        }

        var latitudeRad = DegreesToRadians(latitudeDeg);
        var longitudeRad = DegreesToRadians(longitudeDeg);

        if (!_originLatitudeRadians.HasValue)
        {
            _originLatitudeRadians = latitudeRad;
            _originLongitudeRadians = longitudeRad;
            return new PlanarPoint(0d, 0d);
        }

        var deltaLat = latitudeRad - _originLatitudeRadians.Value;
        var deltaLon = longitudeRad - _originLongitudeRadians!.Value;
        var meanLat = (latitudeRad + _originLatitudeRadians.Value) / 2d;

        var east = EarthRadiusMeters * deltaLon * Math.Cos(meanLat);
        var north = EarthRadiusMeters * deltaLat;

        return new PlanarPoint(east, north);
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180d;

    private void Remove(Subscription subscription)
    {
        lock (_gate)
        {
            _subscribers.RemoveAll(s => s.Id == subscription.Id);
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(PoseStreamIngestionService));
        }
    }

    private sealed class Subscription
    {
        public Subscription(Func<LayerControllerPoseFrame, CancellationToken, ValueTask> handler)
        {
            Handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public Guid Id { get; } = Guid.NewGuid();

        public Func<LayerControllerPoseFrame, CancellationToken, ValueTask> Handler { get; }
    }

    private sealed class SubscriptionHandle : IDisposable
    {
        private readonly PoseStreamIngestionService _owner;
        private Subscription? _subscription;

        public SubscriptionHandle(PoseStreamIngestionService owner, Subscription subscription)
        {
            _owner = owner;
            _subscription = subscription;
        }

        public void Dispose()
        {
            var subscription = Interlocked.Exchange(ref _subscription, null);
            if (subscription is not null)
            {
                _owner.Remove(subscription);
            }
        }
    }
}
