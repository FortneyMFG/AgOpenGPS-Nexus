using System;
using System.Runtime.CompilerServices;
using Aog.Core.V1;

namespace Aog.Bridge.Host.AogLink.Legacy;

/// <summary>
/// Provides helper methods for attaching legacy metadata to steer state messages.
/// </summary>
public static class LegacySteerStateExtensions
{
    private sealed class LegacySteerStateMetadataHolder
    {
        public double? HeadingDegrees { get; set; }

        public bool HasData => HeadingDegrees is not null;
    }

    private static readonly ConditionalWeakTable<SteerState, LegacySteerStateMetadataHolder> Metadata = new();

    /// <summary>
    /// Associates the provided legacy heading value (degrees) with the steer state instance.
    /// </summary>
    public static void SetLegacyHeadingDegrees(this SteerState state, double headingDegrees)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        var holder = Metadata.GetOrCreateValue(state);
        holder.HeadingDegrees = headingDegrees;
    }

    /// <summary>
    /// Attempts to read the legacy heading value (degrees) previously attached to the steer state.
    /// </summary>
    public static bool TryGetLegacyHeadingDegrees(this SteerState? state, out double headingDegrees)
    {
        if (state is null)
        {
            headingDegrees = default;
            return false;
        }

        if (Metadata.TryGetValue(state, out var holder) && holder.HeadingDegrees is double value)
        {
            headingDegrees = value;
            return true;
        }

        headingDegrees = default;
        return false;
    }

    /// <summary>
    /// Removes any previously attached legacy heading metadata from the steer state instance.
    /// </summary>
    public static void ClearLegacyHeadingDegrees(this SteerState state)
    {
        if (state is null)
        {
            return;
        }

        if (Metadata.TryGetValue(state, out var holder))
        {
            holder.HeadingDegrees = null;
            if (!holder.HasData)
            {
                Metadata.Remove(state);
            }
        }
    }
}
