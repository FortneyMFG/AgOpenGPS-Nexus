using System;
using System.Threading;
using System.Threading.Tasks;

namespace Aog.Core.Replay;

/// <summary>
/// Fallback replay controller used when no telemetry source is available.
/// </summary>
public sealed class NullReplayController : IReplayController
{
    private ReplayState _state = new(isPlaying: false, position: TimeSpan.Zero, duration: TimeSpan.Zero, playbackRate: 1.0);

    /// <inheritdoc />
    public event EventHandler<ReplayStateChangedEventArgs>? StateChanged;

    /// <inheritdoc />
    public ReplayState State => _state;

    /// <inheritdoc />
    public ValueTask PlayAsync(CancellationToken cancellationToken = default)
    {
        SetState(_state with { IsPlaying = true });
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask PauseAsync(CancellationToken cancellationToken = default)
    {
        SetState(_state with { IsPlaying = false });
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask SeekAsync(TimeSpan position, CancellationToken cancellationToken = default)
    {
        var clamped = position < TimeSpan.Zero ? TimeSpan.Zero : position;
        SetState(_state with { Position = clamped });
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask SetPlaybackRateAsync(double playbackRate, CancellationToken cancellationToken = default)
    {
        if (playbackRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(playbackRate));
        }

        SetState(_state with { PlaybackRate = playbackRate });
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private void SetState(ReplayState state)
    {
        if (_state.Equals(state))
        {
            return;
        }

        _state = state;
        StateChanged?.Invoke(this, new ReplayStateChangedEventArgs(state));
    }
}
