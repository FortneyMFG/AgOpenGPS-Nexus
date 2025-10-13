using System;

namespace Aog.Core.Replay;

/// <summary>
/// Represents the instantaneous state of a telemetry replay controller.
/// </summary>
public record struct ReplayState
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReplayState"/> struct.
    /// </summary>
    /// <param name="isPlaying">Indicates whether playback is currently running.</param>
    /// <param name="position">The current playback position.</param>
    /// <param name="duration">The total duration of the loaded session.</param>
    /// <param name="playbackRate">The active playback rate multiplier.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="playbackRate"/> is less than or equal to zero.</exception>
    public ReplayState(bool isPlaying, TimeSpan position, TimeSpan duration, double playbackRate)
    {
        if (playbackRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(playbackRate));
        }

        IsPlaying = isPlaying;
        Position = position;
        Duration = duration;
        PlaybackRate = playbackRate;
    }

    /// <summary>
    /// Gets or sets a value indicating whether playback is running.
    /// </summary>
    public bool IsPlaying { get; init; }

    /// <summary>
    /// Gets or sets the current playback position.
    /// </summary>
    public TimeSpan Position { get; init; }

    /// <summary>
    /// Gets or sets the total duration represented by the loaded telemetry.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Gets or sets the current playback rate multiplier.
    /// </summary>
    public double PlaybackRate { get; init; }
}
