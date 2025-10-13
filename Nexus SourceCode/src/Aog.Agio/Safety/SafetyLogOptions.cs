using System;
using System.ComponentModel.DataAnnotations;

namespace Aog.Agio.Safety;

/// <summary>
/// Configures how safety log files are written and retained.
/// </summary>
public sealed class SafetyLogOptions
{
    /// <summary>
    /// Gets or sets the directory where safety log files are written.
    /// </summary>
    [Required]
    public string Directory { get; set; } = System.IO.Path.Combine("logs", "safety");

    /// <summary>
    /// Gets or sets the number of days of log files to retain (inclusive).
    /// </summary>
    [Range(1, 365)]
    public int RetentionDays { get; set; } = 30;

    /// <summary>
    /// Gets or sets the maximum number of log files to retain.
    /// </summary>
    [Range(1, 500)]
    public int MaxFiles { get; set; } = 90;

    internal SafetyLogOptions Clone() => new()
    {
        Directory = Directory,
        RetentionDays = RetentionDays,
        MaxFiles = MaxFiles,
    };
}
