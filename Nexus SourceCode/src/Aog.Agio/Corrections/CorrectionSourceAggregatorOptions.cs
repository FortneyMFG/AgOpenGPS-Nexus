using System;
using System.Collections.Generic;

namespace Aog.Agio.Corrections;

/// <summary>
/// Configures how the GNSS correction source aggregator should select providers.
/// </summary>
public sealed class CorrectionSourceAggregatorOptions
{
    private static readonly IReadOnlyList<CorrectionSourceKind> DefaultOrder = new[]
    {
        CorrectionSourceKind.LocalBaseStation,
        CorrectionSourceKind.SerialRadio,
        CorrectionSourceKind.NetworkService,
        CorrectionSourceKind.Replay,
    };

    /// <summary>
    /// Gets or sets a value indicating whether network-based correction feeds are allowed.
    /// </summary>
    public bool EnableNetworkSources { get; set; } = true;

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
    public IList<CorrectionSourceKind> PreferredOrder { get; set; } = new List<CorrectionSourceKind>(DefaultOrder);

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
