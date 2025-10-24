using System;
using ReactiveUI;

namespace Aog.UI.Avalonia.ViewModels.Shell;

public sealed class FloatingPanelSettingsDialogViewModel : ReactiveObject
{
    private readonly BlockLayoutViewModel _layout;
    private readonly FloatingPanelViewModel _panel;
    private string _title;
    private string? _contentId;
    private bool _isPanelLocked;
    private double _width;
    private double _height;

    public FloatingPanelSettingsDialogViewModel(BlockLayoutViewModel layout, FloatingPanelViewModel panel)
    {
        _layout = layout ?? throw new ArgumentNullException(nameof(layout));
        _panel = panel ?? throw new ArgumentNullException(nameof(panel));

        var spec = panel.Spec;
        _title = spec.Title ?? string.Empty;
        _contentId = spec.ContentId;
        _isPanelLocked = spec.IsLocked;
        var bounds = panel.Bounds;
        _width = bounds.Width;
        _height = bounds.Height;
    }

    public string PanelId => _panel.Id;

    public string Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }

    public string? ContentId
    {
        get => _contentId;
        set => this.RaiseAndSetIfChanged(ref _contentId, value);
    }

    public bool IsPanelLocked
    {
        get => _isPanelLocked;
        set => this.RaiseAndSetIfChanged(ref _isPanelLocked, value);
    }

    public double Width
    {
        get => _width;
        set => this.RaiseAndSetIfChanged(ref _width, value);
    }

    public double Height
    {
        get => _height;
        set => this.RaiseAndSetIfChanged(ref _height, value);
    }

    public bool CanEdit => !_layout.IsLocked && !_panel.IsLocked;

    public void Apply()
    {
        if (_layout.IsLocked)
        {
            return;
        }

        var sanitizedWidth = double.IsFinite(_width) ? _width : _panel.Bounds.Width;
        var sanitizedHeight = double.IsFinite(_height) ? _height : _panel.Bounds.Height;
        _layout.ApplyFloatingPanelSettings(_panel, _title, _contentId, _isPanelLocked, sanitizedWidth, sanitizedHeight);
    }
}
