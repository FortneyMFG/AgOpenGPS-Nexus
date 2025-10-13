using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Eventing;

namespace Aog.Core.Replay;

/// <summary>
/// Provides play/pause/seek control over telemetry recorded by the parquet logger.
/// </summary>
public sealed class TelemetryReplayController : IReplayController
{
    private readonly IEventBus _eventBus;
    private readonly TimeProvider _timeProvider;
    private readonly IReadOnlyList<ReplayFrame> _frames;
    private readonly object _gate = new();

    private ReplayState _state;
    private int _currentIndex;
    private CancellationTokenSource? _playbackCts;
    private Task? _playbackTask;
    private bool _disposed;

    private TelemetryReplayController(IEventBus eventBus, TimeProvider timeProvider, IReadOnlyList<ReplayFrame> frames)
    {
        _eventBus = eventBus;
        _timeProvider = timeProvider;
        _frames = frames;

        var duration = frames.Count == 0 ? TimeSpan.Zero : frames[^1].Offset;
        _state = new ReplayState(isPlaying: false, position: TimeSpan.Zero, duration, playbackRate: 1.0);
    }

    /// <summary>
    /// Creates a replay controller by reading telemetry from parquet files on disk.
    /// </summary>
    public static async Task<TelemetryReplayController> CreateAsync(
        IEventBus eventBus,
        TelemetryReplayOptions options,
        TimeProvider? timeProvider = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventBus);
        ArgumentNullException.ThrowIfNull(options);

        var frames = await TelemetryParquetReplayLoader.LoadAsync(options, cancellationToken)
            .ConfigureAwait(false);
        return new TelemetryReplayController(eventBus, timeProvider ?? TimeProvider.System, frames);
    }

    /// <inheritdoc />
    public event EventHandler<ReplayStateChangedEventArgs>? StateChanged;

    /// <inheritdoc />
    public ReplayState State
    {
        get
        {
            lock (_gate)
            {
                return _state;
            }
        }
    }

    /// <inheritdoc />
    public ValueTask PlayAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (_frames.Count == 0)
        {
            return ValueTask.CompletedTask;
        }

        Task playbackTask;
        lock (_gate)
        {
            if (_state.IsPlaying)
            {
                return ValueTask.CompletedTask;
            }

            if (_currentIndex >= _frames.Count)
            {
                _currentIndex = 0;
                _state = _state with { Position = TimeSpan.Zero };
            }

            _state = _state with { IsPlaying = true };
            _playbackCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            playbackTask = _playbackTask = Task.Run(() => RunPlaybackAsync(_playbackCts.Token), CancellationToken.None);
        }

        RaiseStateChanged();
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public async ValueTask PauseAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        Task? pending;
        lock (_gate)
        {
            if (!_state.IsPlaying)
            {
                return;
            }

            _state = _state with { IsPlaying = false };
            pending = _playbackTask;
            _playbackCts?.Cancel();
        }

        RaiseStateChanged();

        if (pending is not null)
        {
            try
            {
                await pending.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }
    }

    /// <inheritdoc />
    public async ValueTask SeekAsync(TimeSpan position, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        Task? pending = null;
        var resume = false;
        lock (_gate)
        {
            var clamped = Clamp(position, TimeSpan.Zero, _state.Duration);
            if (clamped == _state.Position)
            {
                return;
            }

            if (_state.IsPlaying)
            {
                resume = true;
                _state = _state with { IsPlaying = false };
                pending = _playbackTask;
                _playbackCts?.Cancel();
            }

            _currentIndex = FindFrameIndex(clamped);
            _state = _state with { Position = clamped };
        }

        RaiseStateChanged();

        if (pending is not null)
        {
            try
            {
                await pending.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        if (resume)
        {
            await PlayAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async ValueTask SetPlaybackRateAsync(double playbackRate, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (playbackRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(playbackRate));
        }

        Task? pending = null;
        var resume = false;
        lock (_gate)
        {
            if (Math.Abs(_state.PlaybackRate - playbackRate) < 1e-6)
            {
                return;
            }

            _state = _state with { PlaybackRate = playbackRate };
            if (_state.IsPlaying)
            {
                resume = true;
                _state = _state with { IsPlaying = false };
                pending = _playbackTask;
                _playbackCts?.Cancel();
            }
        }

        RaiseStateChanged();

        if (pending is not null)
        {
            try
            {
                await pending.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        if (resume)
        {
            await PlayAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        Task? pending;
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            pending = _playbackTask;
            _playbackCts?.Cancel();
        }

        if (pending is not null)
        {
            try
            {
                await pending.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        lock (_gate)
        {
            _playbackTask = null;
            _playbackCts?.Dispose();
            _playbackCts = null;
        }
    }

    private async Task RunPlaybackAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                ReplayFrame frame;
                TimeSpan previousOffset;
                double playbackRate;

                lock (_gate)
                {
                    if (_currentIndex >= _frames.Count)
                    {
                        return;
                    }

                    frame = _frames[_currentIndex];
                    previousOffset = _currentIndex == 0 ? TimeSpan.Zero : _frames[_currentIndex - 1].Offset;
                    playbackRate = _state.PlaybackRate;
                }

                var delta = frame.Offset - previousOffset;
                if (delta < TimeSpan.Zero)
                {
                    delta = TimeSpan.Zero;
                }

                if (playbackRate <= 0)
                {
                    playbackRate = 1.0;
                }

                var scaledTicks = playbackRate == 1.0
                    ? delta.Ticks
                    : (long)Math.Round(delta.Ticks / playbackRate);
                var delay = scaledTicks <= 0 ? TimeSpan.Zero : TimeSpan.FromTicks(scaledTicks);

                if (delay > TimeSpan.Zero)
                {
                    await _timeProvider.Delay(delay, cancellationToken).ConfigureAwait(false);
                }

                await frame.PublishAsync(_eventBus, cancellationToken).ConfigureAwait(false);

                lock (_gate)
                {
                    _currentIndex++;
                    var newPosition = frame.Offset;
                    if (newPosition > _state.Duration)
                    {
                        newPosition = _state.Duration;
                    }

                    _state = _state with { Position = newPosition };
                }

                RaiseStateChanged();
            }
        }
        finally
        {
            lock (_gate)
            {
                _playbackTask = null;
                _playbackCts?.Dispose();
                _playbackCts = null;

                if (_currentIndex >= _frames.Count)
                {
                    _state = _state with { IsPlaying = false, Position = _state.Duration };
                }
                else
                {
                    _state = _state with { IsPlaying = false };
                }
            }

            RaiseStateChanged();
        }
    }

    private void RaiseStateChanged()
    {
        ReplayState state;
        EventHandler<ReplayStateChangedEventArgs>? handler;
        lock (_gate)
        {
            state = _state;
            handler = StateChanged;
        }

        handler?.Invoke(this, new ReplayStateChangedEventArgs(state));
    }

    private int FindFrameIndex(TimeSpan position)
    {
        for (var i = 0; i < _frames.Count; i++)
        {
            if (_frames[i].Offset >= position)
            {
                return i;
            }
        }

        return _frames.Count;
    }

    private static TimeSpan Clamp(TimeSpan value, TimeSpan minimum, TimeSpan maximum)
    {
        if (value < minimum)
        {
            return minimum;
        }

        if (value > maximum)
        {
            return maximum;
        }

        return value;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(TelemetryReplayController));
        }
    }
}
