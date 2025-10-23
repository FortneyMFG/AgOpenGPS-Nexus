using System;
using Avalonia;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Represents a button exposed in one of the legacy-inspired side strips.
/// </summary>
public sealed class SidebarButtonViewModel
{
    public SidebarButtonViewModel(string label, DelegateCommand command, string? description = null)
    {
        Label = label ?? throw new ArgumentNullException(nameof(label));
        Command = command ?? throw new ArgumentNullException(nameof(command));
        Description = description;
    }

    /// <summary>Gets the label rendered on the button.</summary>
    public string Label { get; }

    /// <summary>Gets the descriptive tooltip for the button.</summary>
    public string? Description { get; }

    /// <summary>Gets the command invoked when the button is pressed.</summary>
    public DelegateCommand Command { get; }

    /// <summary>Gets or sets the margin applied when rendering the button.</summary>
    public Thickness Margin { get; set; }
}
