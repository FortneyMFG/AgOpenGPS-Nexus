using System;
using System.Threading;
using System.Threading.Tasks;

namespace Aog.Core.Replay;

/// <summary>
/// Defines the capabilities required to control telemetry replay sessions.
/// </summary>
public interface IReplayController : IAsyncDisposable
{
    /// <summary>
    /// Raised whenever the controller state changes.
    /// </summary>
    event EventHandler<ReplayStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// Gets the current controller state.
    /// </summary>
    ReplayState State { get; }

    /// <summary>
    /// Starts playback from the current position.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels the request.</param>
    ValueTask PlayAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Pauses playback without changing the current position.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels the request.</param>
    ValueTask PauseAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Seeks to the specified playback position.
    /// </summary>
    /// <param name="position">The desired playback position.</param>
    /// <param name="cancellationToken">A token that cancels the request.</param>
    ValueTask SeekAsync(TimeSpan position, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the playback rate multiplier.
    /// </summary>
    /// <param name="playbackRate">The new playback rate. Values greater than 1 accelerate playback.</param>
    /// <param name="cancellationToken">A token that cancels the request.</param>
    ValueTask SetPlaybackRateAsync(double playbackRate, CancellationToken cancellationToken = default);
}
