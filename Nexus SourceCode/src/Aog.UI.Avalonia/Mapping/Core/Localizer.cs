using System;
using System.Numerics;

namespace Aog.UI.Avalonia.Mapping.Core;

/// <summary>
/// Maintains a rolling origin to keep GPU coordinates close to zero while preserving double precision on the CPU.
/// </summary>
public sealed class Localizer
{
    private Double3 _anchor = Double3.Zero;
    private readonly double _recenterThreshold;

    public Localizer(double recenterThresholdMeters = 100)
    {
        if (recenterThresholdMeters <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(recenterThresholdMeters));
        }

        _recenterThreshold = recenterThresholdMeters;
    }

    /// <summary>
    /// Gets the current world anchor.
    /// </summary>
    public Double3 Anchor => _anchor;

    /// <summary>
    /// Converts a world position (double precision) into local float space relative to the current anchor.
    /// </summary>
    public Vector3 ToLocal(Double3 world) => new(
        (float)(world.X - _anchor.X),
        (float)(world.Y - _anchor.Y),
        (float)(world.Z - _anchor.Z));

    /// <summary>
    /// Updates the anchor when the supplied world position drifts beyond the recenter threshold.
    /// </summary>
    public bool TryRecenter(Double3 focusWorld)
    {
        var delta = focusWorld - _anchor;
        if (Math.Abs(delta.X) <= _recenterThreshold &&
            Math.Abs(delta.Y) <= _recenterThreshold &&
            Math.Abs(delta.Z) <= _recenterThreshold)
        {
            return false;
        }

        _anchor = focusWorld;
        return true;
    }

    /// <summary>
    /// Forces the anchor to the supplied position.
    /// </summary>
    public void ForceAnchor(Double3 world) => _anchor = world;
}
