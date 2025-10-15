using System;
using System.Windows.Input;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Describes a selectable toolbar option for the zone editor.
/// </summary>
public sealed class ZoneEditorToolOptionViewModel : ObservableObject
{
    private bool _isSelected;

    /// <summary>
    /// Initializes a new instance of the <see cref="ZoneEditorToolOptionViewModel"/> class.
    /// </summary>
    /// <param name="tool">Tool represented by the option.</param>
    /// <param name="displayName">Display label rendered in the UI.</param>
    /// <param name="description">Short description explaining the affordance.</param>
    /// <param name="shortcut">Keyboard shortcut hint.</param>
    /// <param name="onSelect">Callback invoked when the option is chosen.</param>
    public ZoneEditorToolOptionViewModel(
        ZoneEditorTool tool,
        string displayName,
        string description,
        string shortcut,
        Action<ZoneEditorTool> onSelect)
    {
        Tool = tool;
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Shortcut = shortcut ?? throw new ArgumentNullException(nameof(shortcut));

        if (onSelect is null)
        {
            throw new ArgumentNullException(nameof(onSelect));
        }

        SelectCommand = new DelegateCommand(_ => onSelect(tool));
    }

    /// <summary>Gets the tool identifier.</summary>
    public ZoneEditorTool Tool { get; }

    /// <summary>Gets the label displayed to the operator.</summary>
    public string DisplayName { get; }

    /// <summary>Gets the short description shown under the toolbar button.</summary>
    public string Description { get; }

    /// <summary>Gets the keyboard shortcut hint.</summary>
    public string Shortcut { get; }

    /// <summary>Gets a command that triggers selection of the tool.</summary>
    public ICommand SelectCommand { get; }

    /// <summary>Gets or sets a value indicating whether the option is the active selection.</summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
