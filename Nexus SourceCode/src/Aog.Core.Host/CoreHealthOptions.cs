using System.ComponentModel.DataAnnotations;

namespace Aog.Core.Host;

/// <summary>
/// Configuration for the health reporting background service.
/// </summary>
public sealed class CoreHealthOptions
{
    /// <summary>
    /// Gets or sets the interval, in seconds, between health heartbeat log messages.
    /// </summary>
    [Range(1, 86400)]
    public int IntervalSeconds { get; set; } = 30;
}
