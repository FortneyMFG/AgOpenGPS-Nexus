using System;
using System.Collections.Generic;

namespace Aog.Agio.Gnss;

/// <summary>
/// Configures how the GNSS position source aggregator should select providers.
/// </summary>
public sealed class PositionSourceAggregatorOptions
{
    private static readonly IReadOnlyList<PositionSourceKind> DefaultOrder = new[]
    {
        PositionSourceKind.SerialCom,
        PositionSourceKind.Gpsd,
        PositionSourceKind.TcpNetwork,
        PositionSourceKind.UdpNetwork,
    };

    /// <summary>
    /// Gets or sets a value indicating whether TCP/UDP network feeds are allowed.
    /// </summary>
    public bool EnableNetworkFeeds { get; set; } = true;

    /// <summary>
    /// Gets or sets the delay applied after a source faults before trying the next candidate.
    /// </summary>
    public TimeSpan SourceFailureBackoff { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets or sets the delay applied when no sources are available before retrying the scan.
    /// </summary>
    public TimeSpan ExhaustedBackoff { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets the preferred order of source kinds.
    /// </summary>
    public IList<PositionSourceKind> PreferredOrder { get; set; } = new List<PositionSourceKind>(DefaultOrder);

    /// <summary>
    /// Validates the configured options.
    /// </summary>
    public void Validate()
    {
        if (SourceFailureBackoff < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(SourceFailureBackoff), "Backoff cannot be negative.");
        }

        if (ExhaustedBackoff < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(ExhaustedBackoff), "Backoff cannot be negative.");
        }
    }
}
