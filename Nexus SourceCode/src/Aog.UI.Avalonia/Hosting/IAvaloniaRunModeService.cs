using System;
using System.Threading;
using System.Threading.Tasks;

namespace Aog.UI.Avalonia.Hosting;

/// <summary>
/// Provides the current Avalonia run mode and allows switching between modes.
/// </summary>
public interface IAvaloniaRunModeService
{
    /// <summary>Raised when the run mode changes.</summary>
    event EventHandler<AvaloniaRunModeChangedEventArgs>? ModeChanged;

    /// <summary>Gets the currently active run mode.</summary>
    AvaloniaRunMode CurrentMode { get; }

    /// <summary>Lists the supported run modes.</summary>
    IReadOnlyList<AvaloniaRunMode> SupportedModes { get; }

    /// <summary>Requests a mode change.</summary>
    Task<RunModeChangeResult> SetModeAsync(AvaloniaRunMode mode, CancellationToken cancellationToken = default);
}

/// <summary>Event payload emitted when the run mode changes.</summary>
public sealed class AvaloniaRunModeChangedEventArgs : EventArgs
{
    public AvaloniaRunModeChangedEventArgs(AvaloniaRunMode mode)
    {
        Mode = mode;
    }

    /// <summary>Gets the run mode after the change.</summary>
    public AvaloniaRunMode Mode { get; }
}

/// <summary>Describes the outcome of a run mode change request.</summary>
/// <param name="Mode">The mode that was applied.</param>
/// <param name="RequiresRestart">Indicates whether the UI should restart to honour the change.</param>
/// <param name="Reason">Optional explanation for restart requirements.</param>
public sealed record RunModeChangeResult(AvaloniaRunMode Mode, bool RequiresRestart, string? Reason = null);
