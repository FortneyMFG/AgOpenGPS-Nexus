using System.ComponentModel.DataAnnotations;

namespace Aog.UI.Avalonia.Hosting;

/// <summary>
/// Options controlling how the Avalonia shell hosts Nexus services.
/// </summary>
public sealed class AvaloniaShellOptions
{
    /// <summary>
    /// Gets or sets the requested run mode.
    /// </summary>
    [Required]
    public AvaloniaRunMode RunMode { get; set; } = AvaloniaRunMode.LocalInProc;
}
