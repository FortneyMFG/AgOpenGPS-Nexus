using System;
using ReactiveUI;

namespace Aog.UI.Avalonia.ViewModels.Shell;

/// <summary>
/// View-model backing the shell layout settings dialog.
/// </summary>
public sealed class LayoutSettingsDialogViewModel : ReactiveObject
{
    private readonly BlockLayoutViewModel _layout;
    private double _workspaceSpacing;
    private double _workspaceTileSize;

    public LayoutSettingsDialogViewModel(BlockLayoutViewModel layout)
    {
        _layout = layout ?? throw new ArgumentNullException(nameof(layout));
        var workspace = layout.WorkspaceLayout;
        _workspaceSpacing = workspace?.Spacing ?? 8d;
        _workspaceTileSize = workspace?.BlockSize ?? 112d;
    }

    /// <summary>Gets the title displayed in the dialog header.</summary>
    public string Title => "Layout configuration";

    /// <summary>Gets or sets the spacing applied around workspace tiles.</summary>
    public double WorkspaceSpacing
    {
        get => _workspaceSpacing;
        set => this.RaiseAndSetIfChanged(ref _workspaceSpacing, value);
    }

    /// <summary>Gets or sets the base tile size used for floating content defaults.</summary>
    public double WorkspaceTileSize
    {
        get => _workspaceTileSize;
        set => this.RaiseAndSetIfChanged(ref _workspaceTileSize, value);
    }

    /// <summary>Gets a value indicating whether settings can be edited.</summary>
    public bool CanEdit => !_layout.IsLocked;

    /// <summary>Applies the configured settings to the live layout.</summary>
    public void Apply()
    {
        if (_layout.IsLocked)
        {
            return;
        }

        var sanitizedSpacing = double.IsFinite(_workspaceSpacing) ? _workspaceSpacing : 8d;
        var sanitizedTileSize = double.IsFinite(_workspaceTileSize) ? _workspaceTileSize : 112d;
        _layout.ApplyWorkspaceSettings(sanitizedSpacing, sanitizedTileSize);
    }
}
